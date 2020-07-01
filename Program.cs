using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Batch.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace batch_k8s_jobs
{
    public class Program
    {
	/***** Environment variables ** BEGIN *****/

	// Constants for AD resources
	public const string AD_TENANT_ID = "AD_TENANT_ID";
	public const string SP_CLIENT_ID = "SP_CLIENT_ID";
	public const string SP_CLIENT_SECRET = "SP_CLIENT_SECRET";

        // Constants for Batch account
	public const string BATCH_ACCOUNT_NAME = "BATCH_ACCOUNT_NAME";
	// public const string BATCH_ACCOUNT_KEY = "BATCH_ACCOUNT_KEY";
	public const string BATCH_ACCOUNT_URL = "BATCH_ACCOUNT_URL";

  	// Constants for Storage account
	public const string STORAGE_ACCOUNT_NAME = "STORAGE_ACCOUNT_NAME";
	public const string STORAGE_ACCOUNT_KEY = "STORAGE_ACCOUNT_KEY";

	// Constants for azure container registry
	public const string REGISTRY_NAME = "REGISTRY_NAME"; // xxx.azurecr.io
	public const string REGISTRY_USER = "REGISTRY_USER";
	public const string REGISTRY_USER_PWD = "REGISTRY_USER_PWD";

	// Constant for Image ID
	public const String VM_IMAGE_ID = "VM_IMAGE_ID";

	/***** Environment variables ** END *****/

        // Constants for Batch resource settings
        private const string PoolIdSmall = "TES-BATCH-POOL-SMALL";
        private const string PoolIdMeduim = "TES-BATCH-POOL-MEDIUM";
        private const string PoolIdLarge = "TES-BATCH-POOL-LARGE";
        private const string PoolIdFPGA = "TES-BATCH-POOL-FPGA";
        private const string JobId = "TES-NO-DRAGEN-JOB-16";

        private const int PoolDedicatedNodeCount = 1; // Number of Dedicated VM nodes
        private const int PoolSpotNodeCount = 1; // Number of Spot VM nodes
        private const string PoolVMSizeSmall = "STANDARD_D2_V3";
        
	private const string AuthorityBaseUri = "https://login.microsoftonline.com/";
	private const string BatchResourceUri = "https://batch.core.windows.net/";

        // Pool SKU
        private const string PoolId = PoolIdSmall;
        private const string PoolVMSize = PoolVMSizeSmall;

	// Task Container Images
	private const string TaskImageK8sCli = "batchtes01.azurecr.io/tes/kubectl";

	private const int NodeInitTime = 6; // In minutes
        
        static void Main()
        {
	    // AD Tenant ID
	    string TenantId = Environment.GetEnvironmentVariable(AD_TENANT_ID);
	    string ClientId = Environment.GetEnvironmentVariable(SP_CLIENT_ID);
	    string ClientSecret = Environment.GetEnvironmentVariable(SP_CLIENT_SECRET);

            // Batch account credentials
            string BatchAccountName = Environment.GetEnvironmentVariable(BATCH_ACCOUNT_NAME);
            // string BatchAccountKey = Environment.GetEnvironmentVariable(BATCH_ACCOUNT_KEY);
            string BatchAccountUrl = Environment.GetEnvironmentVariable(BATCH_ACCOUNT_URL);

            // Storage account credentials, Storage for INPUT/OUTPUT files.
            string StorageAccountName = Environment.GetEnvironmentVariable(STORAGE_ACCOUNT_NAME);
            string StorageAccountKey = Environment.GetEnvironmentVariable(STORAGE_ACCOUNT_KEY);

	    // Shared Image Gallery Image ID
	    string ImageId = Environment.GetEnvironmentVariable(VM_IMAGE_ID);

	    if ( String.IsNullOrEmpty(TenantId) ) 
		throw new ArgumentNullException("AD Tenant ID is required!");
	    string AuthorityUri = AuthorityBaseUri + TenantId;

            if ( String.IsNullOrEmpty(ClientId) || 
                 String.IsNullOrEmpty(ClientSecret) )
		throw new ArgumentNullException("Service Principal Client ID and Secret are required!");

            if ( String.IsNullOrEmpty(BatchAccountName) || 
//                 String.IsNullOrEmpty(BatchAccountKey) ||
                 String.IsNullOrEmpty(BatchAccountUrl) )
                throw new ArgumentNullException("Batch account name and url are required!");

	    if ( String.IsNullOrEmpty(ImageId) )
		throw new ArgumentNullException("Image ID (Managed Image resource) is required to create a Batch pool!");

            try
            {
                Console.WriteLine("Batch process start: {0}", DateTime.Now);
                Console.WriteLine();
                Stopwatch timer = new Stopwatch();
                timer.Start();

                // Get a Batch client using account creds
                // BatchSharedKeyCredentials cred = 
 		    // new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);
		Func<Task<string>> tokenProvider =
		    () => GetAuthenticationTokenAsync(AuthorityUri,ClientId,ClientSecret);
		BatchTokenCredentials cred =
		    new BatchTokenCredentials(BatchAccountUrl,tokenProvider);

                using (BatchClient batchClient = BatchClient.Open(cred))
                {
                    Console.WriteLine("Creating pool [{0}]...", PoolId);

                    // Create a Ubuntu Server image with Docker, VM configuration, Batch pool
                    ImageReference imageReference = CreateImageReference();

                    VirtualMachineConfiguration vmConfiguration = 
		        CreateVirtualMachineConfiguration(imageReference);

                    CreateBatchPool(batchClient, vmConfiguration);

                    // Create a Batch job
                    Console.WriteLine("Creating job [{0}]...", JobId);

                    try
                    {
                        // Job metadata with TaskRun details
                        List<MetadataItem> metadataItems = new List<MetadataItem>();
                        MetadataItem metadataTaskRun = new MetadataItem("TaskRun","DESeq2Test");
                        metadataItems.Add(metadataTaskRun);
                        MetadataItem metadataImage = new MetadataItem("Image","699120554104.dkr.ecr.us-east-1.amazonaws.com/public/wholegenomerna-diffexpr-develop");
                        metadataItems.Add(metadataImage);

                        // Create a Batch job
                        CloudJob job = batchClient.JobOperations.CreateJob();
                        job.Id = JobId;
                        job.PoolInformation = new PoolInformation { PoolId = PoolId }; // Associate the job to a PoolId
                        job.Metadata = metadataItems;

                        // Commit the job to Batch Service
                        job.Commit();
                    }
                    catch (BatchException be)
                    {
                        // Accept the specific error code JobExists as that is expected if the job already exists
                        if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.JobExists)
                        {
                            Console.WriteLine("The job {0} already existed when we tried to create it", JobId);
                        }
                        else
                        {
                            throw; // Any other exception is unexpected
                        }
                    }

                    Console.WriteLine("Pausing {0} minutes for k8s to initialize on batch node.", NodeInitTime);
		    Thread.Sleep(NodeInitTime * 60 * 1000); // In ms

                    List<CloudTask> tasks = new List<CloudTask>();

                    // Create a task to run the bitnami container and execute kubectl version

                    TaskContainerSettings cmdContainerSettings = new TaskContainerSettings (
                           imageName: TaskImageK8sCli,
                           containerRunOptions: "--rm -v /home/labuser/.kube/config:/.kube/config"
                         );

                    string taskId = "kubectl-cluster-info";
                    Console.WriteLine("Adding task [{0}] to job [{1}]...", taskId, JobId);
                    string taskCommandLine = "cluster-info";
                    CloudTask containerTask = new CloudTask(taskId, taskCommandLine);
                    containerTask.ContainerSettings = cmdContainerSettings;
                    tasks.Add(containerTask);

                    taskId = "kubectl-version";
                    Console.WriteLine("Adding task [{0}] to job [{1}]...", taskId, JobId);
                    taskCommandLine = "version";
                    containerTask = new CloudTask(taskId, taskCommandLine);
                    containerTask.ContainerSettings = cmdContainerSettings;
                    tasks.Add(containerTask);

                    taskId = "get-pods-all-namespaces";
                    Console.WriteLine("Adding task [{0}] to job [{1}]...", taskId, JobId);
		    taskCommandLine = "get pods --all-namespaces";
		    containerTask = new CloudTask(taskId, taskCommandLine);
                    containerTask.ContainerSettings = cmdContainerSettings;
                    tasks.Add(containerTask);

                    // Add all tasks to the job.
                    batchClient.JobOperations.AddTask(JobId, tasks);

                    // Monitor task success/failure, specifying a maximum amount of time to wait for the tasks to complete.
                    TimeSpan timeout = TimeSpan.FromMinutes(30);
                    Console.WriteLine("Monitoring all tasks for 'Completed' state, timeout in {0}...", timeout);

                    IEnumerable<CloudTask> addedTasks = batchClient.JobOperations.ListTasks(JobId);

                    // Monitor job progress and wait for all tasks to complete
                    batchClient.Utilities.CreateTaskStateMonitor().WaitAll(addedTasks, TaskState.Completed, timeout);

                    Console.WriteLine("All tasks reached state Completed.");

                    // Print task output
                    Console.WriteLine();
                    Console.WriteLine("Printing task output...");

                    IEnumerable<CloudTask> completedtasks = batchClient.JobOperations.ListTasks(JobId);

                    foreach (CloudTask task in completedtasks)
                    {
                        string nodeId = String.Format(task.ComputeNodeInformation.ComputeNodeId);
                        Console.WriteLine("---------BEGIN---------");
                        Console.WriteLine("Task: {0}", task.Id);
                        Console.WriteLine("Node: {0}", nodeId);
                        Console.WriteLine("Standard out =>");
                        Console.WriteLine(task.GetNodeFile(Constants.StandardOutFileName).ReadAsString());
                        Console.WriteLine("----------END----------");
                    }

                    // Print out some timing info
                    timer.Stop();
                    Console.WriteLine();
                    Console.WriteLine("Sample end: {0}", DateTime.Now);
                    Console.WriteLine("Elapsed time: {0}", timer.Elapsed);

                    // Clean up Batch resources (if the user so chooses)
                    Console.WriteLine();
                    Console.Write("Delete job? [yes] no: ");
                    string response = Console.ReadLine().ToLower();
                    if (response != "n" && response != "no")
                    {
                        batchClient.JobOperations.DeleteJob(JobId);
                    }

                    Console.Write("Delete pool? [yes] no: ");
                    response = Console.ReadLine().ToLower();
                    if (response != "n" && response != "no")
                    {
                        batchClient.PoolOperations.DeletePool(PoolId);
                    }
                }
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            }
            
        }

        private static void CreateBatchPool(BatchClient batchClient, VirtualMachineConfiguration vmConfiguration)
        {
            // Pool metadata with Customer details
            List<MetadataItem> metadataItems = new List<MetadataItem>();
            MetadataItem metadataPool = new MetadataItem("Customer","Customer-01");
            metadataItems.Add(metadataPool);

            try
            {
                CloudPool pool = batchClient.PoolOperations.CreatePool(
                    poolId: PoolId,
                    targetDedicatedComputeNodes: PoolDedicatedNodeCount,
                    // targetLowPriorityComputeNodes: PoolSpotNodeCount,
                    virtualMachineSize: PoolVMSize,
                    virtualMachineConfiguration: vmConfiguration);
                pool.Metadata = metadataItems;
                pool.Commit();
            }
            catch (BatchException be)
            {
                // Accept the specific error code PoolExists as that is expected if the pool already exists
                if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", PoolId);
                    CloudPool pool = batchClient.PoolOperations.GetPool(poolId: PoolId);

                    int? numDedicated = pool.CurrentDedicatedComputeNodes;
                    int? numLowPri = pool.CurrentLowPriorityComputeNodes;

                    Console.WriteLine("The pool {0} has {1} current Dedicated Compute Nodes", PoolId, numDedicated);
                    Console.WriteLine("The pool {0} has {1} current Spot Nodes", PoolId, numLowPri);
                }
                else
                {
                    throw be; // Any other exception is unexpected
                }
            }
        }

        private static VirtualMachineConfiguration CreateVirtualMachineConfiguration(ImageReference imageReference)
        {
	    string contRegistry = Environment.GetEnvironmentVariable(REGISTRY_NAME);
	    string contRegistryUser = Environment.GetEnvironmentVariable(REGISTRY_USER);
	    string contRegistryUserPwd = Environment.GetEnvironmentVariable(REGISTRY_USER_PWD);

            if ( String.IsNullOrEmpty(contRegistry) || 
                 String.IsNullOrEmpty(contRegistryUser) ||
                 String.IsNullOrEmpty(contRegistryUserPwd) )
                throw new ArgumentNullException("Container registry name, user name and/or password is missing. Exiting!");

            // Specify a container registry
            ContainerRegistry containerRegistry = new ContainerRegistry(
                registryServer: contRegistry,
                userName: contRegistryUser,
                password: contRegistryUserPwd);

            // Create container configuration, prefetching Docker images from the container registry
            ContainerConfiguration containerConfig = new ContainerConfiguration();
            containerConfig.ContainerImageNames = new List<string> { TaskImageK8sCli };
            containerConfig.ContainerRegistries = new List<ContainerRegistry> { containerRegistry };

            VirtualMachineConfiguration VMconfig = new VirtualMachineConfiguration(
                imageReference: imageReference,
                nodeAgentSkuId: "batch.node.ubuntu 18.04");
            VMconfig.ContainerConfiguration = containerConfig;

            return VMconfig;
        }

        private static ImageReference CreateImageReference()
        {
            /**
		return new ImageReference(
                publisher: "microsoft-azure-batch",
                offer: "ubuntu-server-container",  // Ubuntu Server Container LTS
                sku: "18-04-lts",
                version: "latest");
	    */
	    string ImageId = Environment.GetEnvironmentVariable(VM_IMAGE_ID);
	    return new ImageReference(virtualMachineImageId: ImageId);
        }

	private static async Task<string> GetAuthenticationTokenAsync(
		string authUri,
		string clientId,
		string clientKey)
	{
	    AuthenticationContext authContext = new AuthenticationContext(authUri);
	    AuthenticationResult authResult = await authContext.AcquireTokenAsync(BatchResourceUri, new ClientCredential(clientId, clientKey));

	    return authResult.AccessToken;
	}
    }
}
