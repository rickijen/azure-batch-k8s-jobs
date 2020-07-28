using System;

using core.Common;
using core.Interfaces;

namespace core.Implementations
{
    public static class BatchEngineFactory
    {
	public const string ENGINE_TYPE_K8S = "Kubernetes";

        public static IWorkflowEngine GetEngine(
	  string name,
	  string poolName)
        {
	    IWorkflowEngine engine = null;
	    if ( name.Equals(BatchEngineFactory.ENGINE_TYPE_K8S) )
	       engine = new K8sWorkflowEngine(poolName);
	    else 
	       throw new ArgumentOutOfRangeException($"Engine type: {name} not supported!");

	    return engine;
        }
    }
}
