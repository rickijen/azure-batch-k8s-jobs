using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Azure.Batch;

namespace core.Flows
{
   public class K8sClusterInfo : AbstractBatchWorkflow
   {
      public K8sClusterInfo(string wtype) : base(wtype)
      {}

      public override List<CloudTask> GetTaskList()
      {
	 List<CloudTask> tasks = new List<CloudTask>();

	 /* string taskName = "Get-env-vars";
	 string command = "/bin/sh -c \"echo Startup dir: $AZ_BATCH_NODE_STARTUP_DIR\"";
	 CloudTask task = new CloudTask(taskName,command);
	 tasks.Add(task); */

	 string taskName = "Get-cluster-info";
	 string command = "/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config cluster-info\"";
	 CloudTask task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 taskName = "Get-version-info";
	 command = "/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config version\"";
	 task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 taskName = "Get-all-pods";
	 command = "/bin/sh -c \"kubectl --kubeconfig=$AZ_BATCH_NODE_STARTUP_DIR/config get pods --all-namespaces\"";
	 task = new CloudTask(taskName,command);
	 tasks.Add(task);

	 return(tasks);
      }
   }
}
