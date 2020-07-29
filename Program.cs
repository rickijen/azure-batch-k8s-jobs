using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

using core.Constants;
using core.Interfaces;
using core.Implementations;

public class Program
{
   static void Main()
   {
      // 0. First define workflow types and implementation classes. Register these with 
      // the batch workflow factory.
      // As an example, a workflow 'type' could constitute a sequence of tasks to deploy
      // an application.  For example, the sample type 'K8S_DETAILS' below retrieves and 
      // prints info. on the kubernetes cluster deployed on Azure Batch.
 
      // 1. Implement and register workflow type(s) with the workflow factory
      IBatchWorkflowFactory factory = BatchWorkflowFactory.GetInstance();
      factory.RegisterWorkflowType(WorkflowTypes.K8S_DETAILS,"core.Flows.K8sClusterInfo");
      factory.RegisterWorkflowType(WorkflowTypes.TESK_DEPLOY,"core.Flows.DeployTesk");

      // Add a couple more tasks to test TESK deployment
      // factory.RegisterWorkflowType("test-tesk-deployment","core.Flows.VerifyTeskDeployment");

      // 2. Instantiate the Azure batch Kubernetes workflow engine
      IWorkflowEngine engine = 
	BatchEngineFactory.GetEngine(
	  BatchEngineFactory.ENGINE_TYPE_K8S, // Batch workflow engine of type "Kubernetes"
	  "k8s-pool"); // Name of the Azure Batch pool

      // Add batch pool metadata
      Dictionary<string, string> poolMetaData = new Dictionary<string, string>();
      poolMetaData.Add("Env","Test");
      poolMetaData.Add("OS","Ubuntu Server 18.04 LTS");
      poolMetaData.Add("ImageType","Custom image with docker & kubeadm");

      // Add batch job metadata
      Dictionary<string, string> jobMetaData = new Dictionary<string, string>();
      jobMetaData.Add("Type","Get K8s Information");

      // 3. Add workflow type(s) to be executed by the workflow engine
      engine.AddWorkflow(WorkflowTypes.K8S_DETAILS);
      engine.AddWorkflow(WorkflowTypes.TESK_DEPLOY);
      // engine.AddWorkflow("test-tesk-deployment"); // verify Tesk deployment

      // 4. Execute workflows in the context of a batch job on the engine
      engine.ExecuteWorkflow(
	"k8s-job1",  // name of Azure Batch job
	poolMetaData, // metadata to be assigned to the batch pool 
	jobMetaData); // metadata to be assigned to the job
   }
}
