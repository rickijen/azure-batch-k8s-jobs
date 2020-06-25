using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace batch_k8s_jobs
{
    public class Program
    {
        // Batch account credentials
        private const string BatchAccountName = "batchtes01";

        private const string BatchAccountKey = "QY3GY9dg60q04soQZnE7ivXL8XlDn66e+HUU3RncPeSHxiT2ssrN/P8de1EPMYWO57nk7G933dHr8rKP06NmIg==";
        private const string BatchAccountUrl = "https://batchtes01.westus2.batch.azure.com";

        // Storage account credentials
        private const string StorageAccountName = "samydata";
        private const string StorageAccountKey = "DEniUJ/IWnPlUByum4tPHyAslsKW1IZfFcSw/p5oVJPYgVvWVbERYRWyyWPg+SyfrReV0+gYamIu5EgXIcIGLw==";

        // Batch resource settings
        private const string PoolId = "TES-BATCH-POOL-01";
        private const string JobId = "TES-NO-DRAGEN-JOB-15";
        private const int JobCount = 5;
        private const int PoolDedicatedNodeCount = 1;
        private const int PoolSpotNodeCount = 1;
        private const string PoolVMSize = "STANDARD_D2_V3";
        
        static void Main()
        {

            if (String.IsNullOrEmpty(BatchAccountName) || 
                String.IsNullOrEmpty(BatchAccountKey) ||
                String.IsNullOrEmpty(BatchAccountUrl))
            {
                throw new InvalidOperationException("One or more account credential strings have not been populated. Please ensure that your Batch account credentials have been specified.");
            }

            try
            {
                Console.WriteLine("Batch process start: {0}", DateTime.Now);
                Console.WriteLine();
                Stopwatch timer = new Stopwatch();
                timer.Start();

                // Get a Batch client using account creds
                BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

                using (BatchClient batchClient = BatchClient.Open(cred))
                {
                    Console.WriteLine("Creating pool [{0}]...", PoolId);

                    // Create a Ubuntu Server image with Docker, VM configuration, Batch pool
                    ImageReference imageReference = CreateImageReference();

                    VirtualMachineConfiguration vmConfiguration = CreateVirtualMachineConfiguration(imageReference);

                    CreateBatchPool(batchClient, vmConfiguration);

                    // Create a Batch job
                    Console.WriteLine("Creating job [{0}]...", JobId);

                    try
                    {
                        List<MetadataItem> metadataItem = new List<MetadataItem>();
                        MetadataItem metadataTaskRun = new MetadataItem("TaskRun","DESeq2Test");
                        metadataItem.Add(metadataTaskRun);
                        MetadataItem metadataImage = new MetadataItem("Image","699120554104.dkr.ecr.us-east-1.amazonaws.com/public/wholegenomerna-diffexpr-develop");
                        metadataItem.Add(metadataImage);

                        CloudJob job = batchClient.JobOperations.CreateJob();
                        job.Id = JobId;
                        job.PoolInformation = new PoolInformation { PoolId = PoolId };
                        job.Metadata = metadataItem;

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

                    // Create a collection to hold the tasks that we'll be adding to the job
                    Console.WriteLine("Adding {0} tasks to job [{1}]...", JobCount, JobId);

                    List<CloudTask> tasks = new List<CloudTask>();

                    // Create each of the tasks to process one of the input files. 
                    for (int i = 0; i < JobCount; i++)
                    {
                        string taskId = String.Format("Task-{0}", i);
                        string taskCommandLine = "version --client=true";

                        TaskContainerSettings cmdContainerSettings = new TaskContainerSettings (
                            imageName: "batchtes01.azurecr.io/tes/kubectl",
                            containerRunOptions: "--rm --workdir /"
                            );

                        CloudTask containerTask = new CloudTask(taskId, taskCommandLine);
                        containerTask.ContainerSettings = cmdContainerSettings;
                        tasks.Add(containerTask);
                    }

                    // Add all tasks to the job.
                    batchClient.JobOperations.AddTask(JobId, tasks);

                    // Monitor task success/failure, specifying a maximum amount of time to wait for the tasks to complete.

                    TimeSpan timeout = TimeSpan.FromMinutes(30);
                    Console.WriteLine("Monitoring all tasks for 'Completed' state, timeout in {0}...", timeout);

                    IEnumerable<CloudTask> addedTasks = batchClient.JobOperations.ListTasks(JobId);

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
                        Console.WriteLine("Standard out:");
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
                Console.WriteLine("Sample complete, hit ENTER to exit...");
                Console.ReadLine();
            }
            
        }

        private static void CreateBatchPool(BatchClient batchClient, VirtualMachineConfiguration vmConfiguration)
        {
            try
            {
                CloudPool pool = batchClient.PoolOperations.CreatePool(
                    poolId: PoolId,
                    targetDedicatedComputeNodes: PoolDedicatedNodeCount,
                    targetLowPriorityComputeNodes: PoolSpotNodeCount,
                    virtualMachineSize: PoolVMSize,
                    virtualMachineConfiguration: vmConfiguration);

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
                    throw; // Any other exception is unexpected
                }
            }
        }

        private static VirtualMachineConfiguration CreateVirtualMachineConfiguration(ImageReference imageReference)
        {
            // Specify a container registry
            ContainerRegistry containerRegistry = new ContainerRegistry(
                registryServer: "batchtes01.azurecr.io",
                userName: "batchtes01",
                password: "xMn6QubjIB996o8bLI=kgr2ji=XMohhk");

            // Create container configuration, prefetching Docker images from the container registry
            ContainerConfiguration containerConfig = new ContainerConfiguration();
            containerConfig.ContainerImageNames = new List<string> {
                    "batchtes01.azurecr.io/tes/kubectl" };
            containerConfig.ContainerRegistries = new List<ContainerRegistry> { containerRegistry };

            VirtualMachineConfiguration VMconfig = new VirtualMachineConfiguration(
                imageReference: imageReference,
                nodeAgentSkuId: "batch.node.ubuntu 16.04");
            VMconfig.ContainerConfiguration = containerConfig;

            return VMconfig;
        }

        private static ImageReference CreateImageReference()
        {
            return new ImageReference(
                publisher: "microsoft-azure-batch",
                offer: "ubuntu-server-container",  // Ubuntu Server Container LTS
                sku: "16-04-lts",
                version: "latest");
        }
    }
}
