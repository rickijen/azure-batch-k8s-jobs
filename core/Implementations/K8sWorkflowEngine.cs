using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace core.Implementations
{
    public class K8sWorkflowEngine : AbstractWorkflowEngine
    {
	public K8sWorkflowEngine(string poolName) : base(poolName)
	{
	}

	public override Dictionary<string, string> GetContainerImageList()
	{
 	   /** if ( ImageList is null )	
	   {
	      ImageList = new Dictionary<string, string>();
	      ImageList.Add("kubectl","batchtest01.azurecr.io/tes/kubectl");
	   }; **/

	   return(null);
	}
    }
}
