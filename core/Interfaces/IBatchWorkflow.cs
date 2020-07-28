using System;
using System.Collections.Generic;

using Microsoft.Azure.Batch;

namespace core.Interfaces
{
    // Internal interface to be implemented by batch workflows
    public interface IBatchWorkflow
    {
       public List<CloudTask> GetTaskList();

       public void ExecuteTasks(
	 BatchClient client, // Azure batch client
         string jobName); // Job to execute the tasks on
    }
}
