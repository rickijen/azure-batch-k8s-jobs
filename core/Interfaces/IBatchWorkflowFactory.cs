using System;

namespace core.Interfaces
{
    public interface IBatchWorkflowFactory
    {
       public void RegisterWorkflowType(string type, string typeName);
       public IBatchWorkflow GetWorkflow(string type);
    }
}
