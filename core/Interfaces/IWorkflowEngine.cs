using System;
using System.Collections.Generic;

namespace core.Interfaces
{
    public interface IWorkflowEngine
    {
       public Dictionary<string, string> GetContainerImageList();

       public void AddWorkflow(string wtype);

       public void ExecuteWorkflow(
         string jobName,
	 Dictionary<string, string> poolMetadata,
	 Dictionary<string, string> jobMetadata,
	 bool delPool = false,
	 bool delJob = false);
    }
}
