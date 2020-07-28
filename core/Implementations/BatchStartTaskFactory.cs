using System;
using System.Collections.Generic;

using Microsoft.Azure.Batch;

using core.Constants;
using core.Common;
using core.Interfaces;

namespace core.Implementations
{
    public static class BatchStartTaskFactory
    {
	/// <summary>
	/// Returns a start task to be executed by the batch pool vm as soon as it's spun up
	/// </summary>
	/// <returns>
	/// A start task
	/// </returns>
	/// <param name="name">Blob name</param>
	/// <param name="uname">Task user identity name</params>
        public static StartTask GetStartTask(string name, string uname)
        {
	    string containerName =
	      Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_APP_DIRECTORY);

	    IStorageEngine engine = AzureStorageEngine.GetInstance();
	    // ResourceFile resource = engine.GetResourceFile(containerName,name,filePath);
	    ResourceFile resource = engine.GetResourceFile(containerName,name,"");
	    List<ResourceFile> apps = new List<ResourceFile>(){ resource };

	    StartTask task = null;
	    if ( name.Equals(IApplicationTypes.K8S_SINGLE_NODE) ) {
	       task = new StartTask();
	       task.CommandLine = $"/bin/sh -c ./{name}";
	       task.MaxTaskRetryCount = 2; // set 2 retries
	       task.ResourceFiles = apps;
	       task.UserIdentity = new UserIdentity(uname);
	       task.WaitForSuccess = true;
	    }
	    else
	       throw new BatchOperationException($"Application Type: {name}, not supported.  Aborting...");

	    return task;
        }
    }
}
