using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Batch.Common;

using core.Constants;
using core.Common;
using core.Interfaces;

/**
 * Author : GR @Microsoft
 * Description:
 * This class provides an abstract Azure Batch workflow engine implementation. All workflow engines
 * wanting to leverage Azure Batch must extend this class. 
 * Implementations can choose to override 'GetContainerImageList' method and provide a list of 
 * container images to download to the pool VM.
 *
 * Dated: 07-20-2020
 *
 * Notes: Capture updates below.
 */
namespace core.Implementations
{
    public abstract class AbstractWorkflowEngine : IWorkflowEngine
    {
       protected Dictionary<string, string> EnvVars;
       protected string PoolName { get; }
       private BatchTokenCredentials Creds { get; set; }
       private List<IBatchWorkflow> Workflows;

       public AbstractWorkflowEngine(string poolName)
       {
	  this.PoolName = poolName;
	  this.EnvVars = ParseAzureInputs.GetEnvVars();
	  this.Workflows = new List<IBatchWorkflow>();

	  InitEngine();
       }

       // Engines wanting to load custom container images onto the Batch pool VM should override this method
       public virtual Dictionary<string, string> GetContainerImageList()
       {
	  // return null
	  return(null);
       }

       private void InitEngine()
       {
          Console.WriteLine("Initializing batch engine: {0}", DateTime.Now);

          // Get a Batch client using account creds
          // BatchSharedKeyCredentials cred = 
 	  // new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);
	  Func<Task<string>> tokenProvider =
	    async () => await GetAuthenticationTokenAsync(
	      AzureEnvConstants.AZURE_AD_TOKEN_EP + EnvVars[AzureEnvConstants.AZURE_AD_TENANT_ID],
	      EnvVars[AzureEnvConstants.AZURE_AD_SP_CLIENT_ID],
	      EnvVars[AzureEnvConstants.AZURE_AD_SP_CLIENT_SECRET]);

	  Creds =
	    new BatchTokenCredentials(EnvVars[AzureEnvConstants.AZURE_BATCH_ACCOUNT_URL],tokenProvider);
       }

       private List<MetadataItem> GetMetadataItems(Dictionary<string, string> mdata)
       {
          List<MetadataItem> metadataItems = new List<MetadataItem>();
          MetadataItem metadataItem = null;
	  foreach (KeyValuePair<string, string> ite in mdata)
	  {
	     metadataItem = new MetadataItem(ite.Key, ite.Value);
             metadataItems.Add(metadataItem);
	  }
	  return(metadataItems);
       }

       public void AddWorkflow(string wtype)
       {
	  IBatchWorkflowFactory factory = BatchWorkflowFactory.GetInstance();
	  IBatchWorkflow workflow = factory.GetWorkflow(wtype);

	  Workflows.Add(workflow);
       }

       public void ExecuteWorkflow(
	 string jobName,
	 Dictionary<string, string> poolMetadata,
	 Dictionary<string, string> jobMetadata,
	 bool delPool = false,
	 bool delJob = false)
       {
	  if ( Workflows.Count == 0 )
	     throw new BatchOperationException("No workflows to execute, exiting...");

          using (BatchClient batchClient = BatchClient.Open(Creds))
          {
	     // Create an Azure Batch Pool 
             CreateBatchPool(batchClient, poolMetadata);
	     // Create a Job
	     CreateJob(batchClient, jobName, jobMetadata);

	     // Execute workflow tasks in a job
	     Workflows.ForEach(workflow => workflow.ExecuteTasks(batchClient,jobName));
	     
	     // Monitor the Job
	     MonitorJob(batchClient,jobName);

             // Clean up Batch resources (if the user so chooses)
             Console.WriteLine();
             if ( delJob )
	     {
                Console.WriteLine("Deleting job: {0}", jobName);
                batchClient.JobOperations.DeleteJob(jobName);
	     };

             if ( delPool )
	     {
                Console.WriteLine("Deleting pool: {0}", PoolName);
                batchClient.PoolOperations.DeletePool(PoolName);
	     };
          };
       }

       protected void CreateJob(
         BatchClient batchClient,
	 string jobName,
	 Dictionary<string, string> jobMetadata)
       {
          // Create a Batch job
          Console.WriteLine("Creating job [{0}]...", jobName);
          try
          {
             // Create a Batch job
             CloudJob job = batchClient.JobOperations.CreateJob();
             job.Id = jobName;
             job.PoolInformation = new PoolInformation { PoolId = PoolName }; // Associate the job to a PoolId

	     if ( jobMetadata != null )
                job.Metadata = GetMetadataItems(jobMetadata);

             // Commit the job to Batch Service
             job.Commit();
          }
          catch (BatchException be)
          {
             // Accept the specific error code JobExists as that is expected if the job already exists
             if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.JobExists)
                Console.WriteLine("The job [{0}] already exists", jobName);
             else
                throw; // Any other exception is unexpected
          };
       }

       protected void MonitorJob(
         BatchClient batchClient,
	 string jobName)
       {
          // Monitor task success/failure, specifying a maximum amount of time to wait for the tasks to 
	  // complete.
          TimeSpan timeout = TimeSpan.FromMinutes(AzureEnvConstants.AZURE_BATCH_JOB_TIMEOUT);
          Console.WriteLine("Monitoring all tasks for 'Completed' state, timeout set to {0} minutes.", timeout);

          IEnumerable<CloudTask> addedTasks = batchClient.JobOperations.ListTasks(jobName);

          // Monitor job progress and wait for all tasks to complete
          batchClient.Utilities.CreateTaskStateMonitor().WaitAll(addedTasks, TaskState.Completed, timeout);

          Console.WriteLine("All tasks reached state Completed.");

          // Print task output
          Console.WriteLine();
          Console.WriteLine("Printing task output...");

          IEnumerable<CloudTask> completedtasks = batchClient.JobOperations.ListTasks(jobName);

          foreach (CloudTask task in completedtasks)
          {
             string nodeId = String.Format(task.ComputeNodeInformation.ComputeNodeId);
             Console.WriteLine("---------BEGIN---------");
             Console.WriteLine("Task: {0}", task.Id);
             Console.WriteLine("CommandLine: {0}", task.CommandLine);
             Console.WriteLine("Node: {0}", nodeId);
             Console.WriteLine("Standard error =>");
             Console.WriteLine(task.GetNodeFile(
               Microsoft.Azure.Batch.Constants.StandardErrorFileName).ReadAsString());
             Console.WriteLine("Standard out =>");
             Console.WriteLine(task.GetNodeFile(
               Microsoft.Azure.Batch.Constants.StandardOutFileName).ReadAsString());
             Console.WriteLine("----------END----------");
          };
       }

       protected void CreateBatchPool(
         BatchClient batchClient,
	 Dictionary<string, string> poolMetadata)
       {
          int nodeCount = int.Parse(EnvVars[AzureEnvConstants.AZURE_BATCH_VM_NODE_COUNT]);
	  string vmSize = EnvVars[AzureEnvConstants.AZURE_BATCH_VM_SIZE];
	  VirtualMachineConfiguration vmConfiguration =
	    CreateVirtualMachineConfiguration(batchClient, CreateImageReference());

          try
          {
             Console.WriteLine("Creating pool [{0}]...", PoolName);

             CloudPool pool = batchClient.PoolOperations.CreatePool(
               poolId: PoolName,
               targetDedicatedComputeNodes: nodeCount,
               // targetLowPriorityComputeNodes: PoolSpotNodeCount,
               virtualMachineSize: vmSize,
               virtualMachineConfiguration: vmConfiguration);

	     string sshPrivateKey = new Guid().ToString();
             Console.WriteLine("Batch pool VM SSH private key: {0}", sshPrivateKey);

	     ElevationLevel lvl = 
	       EnvVars[BatchUser.USER_ELE_LEVEL].Equals("1") ? ElevationLevel.Admin : ElevationLevel.NonAdmin;
	     UserAccount ua =
	       new UserAccount(
	         name: EnvVars[BatchUser.USER_NAME],
		 password: EnvVars[BatchUser.USER_PWD],
		 elevationLevel: lvl,
		 linuxUserConfiguration: new LinuxUserConfiguration(
		   uid: 1075,
		   gid: 1075,
		   sshPrivateKey: sshPrivateKey));
	     pool.UserAccounts = new List<UserAccount> { ua };

	     // Use start task to initialize a single node k8s cluster
	     pool.StartTask = 
	       BatchStartTaskFactory.GetStartTask(
		 IApplicationTypes.K8S_SINGLE_NODE,
		 EnvVars[BatchUser.USER_NAME]);

	     if ( poolMetadata != null )
                pool.Metadata = GetMetadataItems(poolMetadata);

             pool.Commit(); // Sync call

	     // PrintStartTaskInfo(pool);
          }
          catch (BatchException be)
          {
             // Accept the specific error code PoolExists as that is expected if the pool already exists
             if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.PoolExists)
             {
                Console.WriteLine("The pool [{0}] already exists", PoolName);
                CloudPool pool = batchClient.PoolOperations.GetPool(poolId: PoolName);

                int? numDedicated = pool.CurrentDedicatedComputeNodes;
                int? numLowPri = pool.CurrentLowPriorityComputeNodes;

                Console.WriteLine("The pool [{0}] has [{1}] current Dedicated Compute Nodes", PoolName, numDedicated);
                Console.WriteLine("The pool [{0}] has [{1}] current Spot Nodes", PoolName, numLowPri);
             }
             else
                throw be; // Any other exception is unexpected
          }
       }

       private void PrintStartTaskInfo(CloudPool pool)
       {
	  IPagedEnumerable<ComputeNode> nodes = pool.ListComputeNodes();
	  PagedEnumerableExtensions.ForEachAsync<ComputeNode>(nodes, delegate(ComputeNode node)
	  {
	     Console.WriteLine("***** Start Task Details *****");
	     Console.WriteLine($"Start time: [{node.StartTaskInformation.StartTime}]");
	     Console.WriteLine($"State: [{node.StartTaskInformation.State}]");
	     if ( node.StartTaskInformation.Result.Equals(TaskExecutionResult.Failure) )
	     {
	        Console.WriteLine($"Error Category: [{node.StartTaskInformation.FailureInformation.Category}]");
	        Console.WriteLine($"Error Details: [{node.StartTaskInformation.FailureInformation.Details}]");
	        Console.WriteLine($"Error Message: [{node.StartTaskInformation.FailureInformation.Message}]");
	        Console.WriteLine($"Failure Code: [{node.StartTaskInformation.FailureInformation.Code}]");
	     }
	     Console.WriteLine($"End time: [{node.StartTaskInformation.EndTime}]");
	  }).Wait();
       }

       private VirtualMachineConfiguration CreateVirtualMachineConfiguration(
	 BatchClient batchClient,
         ImageReference imageReference)
       {
	  List<ImageInformation> nodeAgentSkus = null;

	  IPagedEnumerable<ImageInformation> agentSkus =
	    batchClient.PoolOperations.ListSupportedImages();
	  Task<List<ImageInformation>> nodeAgentTask =
	    PagedEnumerableExtensions.ToListAsync<ImageInformation>(agentSkus);
	  nodeAgentTask.Wait();
	  if ( nodeAgentTask.Status == TaskStatus.Faulted )
	     throw new BatchOperationException(
	       "Couldn't retrieve list of Batch supported VM images, aborting ...");
	  else
	     nodeAgentSkus = nodeAgentTask.Result;

	  /** nodeAgentSkus.ForEach(delegate(ImageInformation img)
          {
	     Console.WriteLine("-------------------------");
	     Console.WriteLine("Node SKU: {0}", img.NodeAgentSkuId);
	     Console.WriteLine("Publisher {0}", img.ImageReference.Publisher);
	     Console.WriteLine("Offer {0}", img.ImageReference.Offer);
	     Console.WriteLine("Sku {0}", img.ImageReference.Sku);
	     Console.WriteLine("-------------------------");
	  }); **/

	  ImageInformation ubuntuAgentSku = nodeAgentSkus.Find(
	    imageRef =>
	    imageRef.ImageReference.Publisher == BatchVmConfig.VM_IMAGE_PUBLISHER &&
	    imageRef.ImageReference.Offer == BatchVmConfig.VM_IMAGE_OFFER &&
	    imageRef.ImageReference.Sku.Contains(BatchVmConfig.VM_IMAGE_SKU));
	    
	  Console.WriteLine("Batch node agent: {0}",ubuntuAgentSku.NodeAgentSkuId);
          VirtualMachineConfiguration vmConfig = new VirtualMachineConfiguration(
            imageReference: imageReference,
            nodeAgentSkuId: ubuntuAgentSku.NodeAgentSkuId);
            // nodeAgentSkuId: "batch.node.ubuntu 18.04");

	  Dictionary<string, string> imageList = GetContainerImageList();
	  if ( (imageList != null) && (imageList.Count > 0 ) )
	  {
             string contRegistry = EnvVars[AzureEnvConstants.AZURE_ACR_NAME];
	     string contRegistryUser = EnvVars[AzureEnvConstants.AZURE_ACR_USER];
	     string contRegistryUserPwd = EnvVars[AzureEnvConstants.AZURE_ACR_USER_PWD];

             // Specify a container registry
             ContainerRegistry containerRegistry = new ContainerRegistry(
               registryServer: contRegistry,
               userName: contRegistryUser,
               password: contRegistryUserPwd);

             // Create container configuration, prefetching Docker images from the container registry
             ContainerConfiguration containerConfig = new ContainerConfiguration();
             containerConfig.ContainerImageNames = new List<string>(imageList.Values);
             containerConfig.ContainerRegistries = new List<ContainerRegistry> { containerRegistry };

             vmConfig.ContainerConfiguration = containerConfig;
	  };

          return vmConfig;
       }

       private ImageReference CreateImageReference()
       {
          /**
	return new ImageReference(
               publisher: "microsoft-azure-batch",
               offer: "ubuntu-server-container",  // Ubuntu Server Container LTS
               sku: "18-04-lts",
               version: "latest");
          */
	  string imageId = EnvVars[AzureEnvConstants.AZURE_BATCH_VM_IMAGE_ID];
	  return new ImageReference(virtualMachineImageId: imageId);
       }

       private async Task<string> GetAuthenticationTokenAsync(
         string authUri,
	 string clientId,
	 string clientKey)
       {
	  AuthenticationContext authContext = new AuthenticationContext(authUri);
	  AuthenticationResult authResult = 
	    await authContext.AcquireTokenAsync(
	      AzureEnvConstants.AZURE_BATCH_RESOURCE_URI, new ClientCredential(clientId, clientKey));

	  return authResult.AccessToken;
       }
    }
}
