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
   public class VerifyTeskDeployment : AbstractBatchWorkflow
   {
      // Define directory locations where Tesk task json files are stored
      private const string APP1_DIR = "Env"; 
      private const string APP2_DIR = "Hello"; 
      private const string APP3_DIR = "Stdout";

      public VerifyTeskDeployment(string wtype) : base(wtype)
      {}

      public override List<CloudTask> GetTaskList()
      {
	 List<CloudTask> tasks = new List<CloudTask>();
	 
	 string containerName = "tesk-tasks"; // Name of the storage container containing Tesk tasks
	 IStorageEngine engine = AzureStorageEngine.GetInstance();

	 string endpoint = "http://127.0.0.1:31882/v1/tasks"; // Adjust NodePort no.

	 /* string taskName = "Get-Local-IP";
	 string command = "/bin/sh -c \"ifconfig\"";
	 CloudTask task = new CloudTask(taskName,command);
	 tasks.Add(task); */

	 string taskName = "Get-Tesk-Pods";
	 string command = "/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config get pods\"";
	 CloudTask task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 taskName = "Describe-Tesk-Pod";
	 command = "/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config describe pod\"";
	 task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 taskName = "Print-Env-Vars";
	 command = 
	   $"/bin/sh -c \"curl -X POST -s --header 'Content-Type: application/json' -d @./{APP1_DIR}/env.json {endpoint}\"";
	 task = new CloudTask(taskName,command);
	 task.ResourceFiles = engine.GetResourceFiles(containerName,APP1_DIR);
	 tasks.Add(task);

	 taskName = "Hello-Task";
	 command = 
	   $"/bin/sh -c \"curl -X POST -s --header 'Content-Type: application/json' -d @./{APP2_DIR}/hello.json {endpoint}\"";
	 task = new CloudTask(taskName,command);
	 task.ResourceFiles = engine.GetResourceFiles(containerName,APP2_DIR);
	 tasks.Add(task);

	 // This task requires configuration of PV and PVC's !
	 /* taskName = "Submit-Tesk-Demo-Task";
	 command = 
	   $"/bin/sh -c \"curl -X POST -s --header 'Content-Type: application/json' --header 'Accept: application/json' -d @./{APP3_DIR}/stdout.json {endpoint}\"";
	 task = new CloudTask(taskName,command);
	 task.ResourceFiles = engine.GetResourceFiles(containerName,APP3_DIR);
	 tasks.Add(task); */

	 taskName = "Get-Tesk-Jobs";
	 command = "/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config get jobs\"";
	 task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 taskName = "Get-Task-Status";
	 command = 
	   $"/bin/sh -c \"curl -X GET -s --header 'Accept: application/json' {endpoint}\"";
	 task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 return(tasks);
      }
   }
}
