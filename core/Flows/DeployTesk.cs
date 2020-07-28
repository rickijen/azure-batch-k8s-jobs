using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;

using core.Constants;
using core.Interfaces;
using core.Implementations;

namespace core.Flows
{
   public class DeployTesk : AbstractBatchWorkflow
   {
      private const string CMD_DIR = "tesk"; // Directory where tesk engine deployment files are stored

      public DeployTesk(string wtype) : base(wtype)
      {}

      public override List<CloudTask> GetTaskList()
      {
	 List<CloudTask> tasks = new List<CloudTask>();
	 
	 string containerName =
	   Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_K8S_DIRECTORY);
	 IStorageEngine engine = AzureStorageEngine.GetInstance();

	 string taskName = "Deploy-TESK";
	 string command = 
	   $"/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config create -f ./{CMD_DIR}\"";
	 CloudTask task = new CloudTask(taskName,command);
	 task.ResourceFiles = engine.GetResourceFiles(containerName,CMD_DIR);
	 tasks.Add(task);

	 taskName = "Get-Service-IP";
	 command = 
	   $"/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config get svc/tesk-api\"";
	 task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 return(tasks);
      }
   }
}
