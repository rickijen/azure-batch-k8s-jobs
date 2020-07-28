using System;
using System.Collections.Generic;

using core.Constants;
using core.Flows;
using core.Interfaces;

namespace core.Implementations
{
    public class BatchWorkflowFactory : IBatchWorkflowFactory
    {
	private static readonly BatchWorkflowFactory factory = new BatchWorkflowFactory();
	public static IBatchWorkflowFactory GetInstance() => factory;

	private Dictionary<string, string> WorkflowClasses;

	private BatchWorkflowFactory()
	{
	   WorkflowClasses = new Dictionary<string, string>();
	}

	public void RegisterWorkflowType(string type, string typeName)
	{
	   WorkflowClasses.Add(type,typeName);
	}

        public IBatchWorkflow GetWorkflow(string type)
        {
	    string typeClass = WorkflowClasses[type]; // This will throw an exception if type not found!
	    Console.WriteLine($"Instantiating workflow class: {typeClass}");
	    var workflowType = Type.GetType(typeClass);

	    IBatchWorkflow workflow = (IBatchWorkflow) Activator.CreateInstance(workflowType,type);

	    return workflow;
        }
    }
}
