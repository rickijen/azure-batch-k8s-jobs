using System;
using System.Collections.Generic;

using Microsoft.Azure.Batch;

using core.Interfaces;

namespace core.Flows
{
    public abstract class AbstractBatchWorkflow : IBatchWorkflow
    {
       protected string workflowName;

       public AbstractBatchWorkflow(string wtype)
       {
	  workflowName = wtype;
       }

       public abstract List<CloudTask> GetTaskList();

       public void ExecuteTasks(
	 BatchClient batchClient, 
	 string jobName)
       {
          Console.WriteLine("Creating workflow [{0}]...", workflowName);

          // Add all tasks to the job.
          batchClient.JobOperations.AddTask(jobName, GetTaskList());
       }
    }
}
