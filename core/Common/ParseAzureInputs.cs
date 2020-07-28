using System;
using System.Collections.Generic;

using core.Constants;
using core.Interfaces;

namespace core.Common
{
    public static class ParseAzureInputs
    {
        public static Dictionary<string, string> GetEnvVars()
        {
	    Dictionary<string, string> envVars = new Dictionary<string, string>();

	    string tenantId = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_AD_TENANT_ID);
	    if ( string.IsNullOrEmpty(tenantId) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_AD_TENANT_ID}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_AD_TENANT_ID,tenantId);

	    string spClientId = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_AD_SP_CLIENT_ID);
	    if ( string.IsNullOrEmpty(spClientId) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_AD_SP_CLIENT_ID}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_AD_SP_CLIENT_ID,spClientId);

	    string spClientSecret = 
	       Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_AD_SP_CLIENT_SECRET);
	    if ( string.IsNullOrEmpty(spClientSecret) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_AD_SP_CLIENT_SECRET}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_AD_SP_CLIENT_SECRET,spClientSecret);

	    string batchActName = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_BATCH_ACCOUNT_NAME);
	    if ( string.IsNullOrEmpty(batchActName) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_BATCH_ACCOUNT_NAME}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_BATCH_ACCOUNT_NAME,batchActName);

	    string batchActUrl = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_BATCH_ACCOUNT_URL);
	    if ( string.IsNullOrEmpty(batchActUrl) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_BATCH_ACCOUNT_URL}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_BATCH_ACCOUNT_URL,batchActUrl);

	    string storageActName = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_ACCOUNT_NAME);
	    if ( string.IsNullOrEmpty(storageActName) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_STORAGE_ACCOUNT_NAME}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_STORAGE_ACCOUNT_NAME,storageActName);

	    string storageActKey = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_ACCOUNT_KEY);
	    if ( string.IsNullOrEmpty(storageActKey) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_STORAGE_ACCOUNT_KEY}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_STORAGE_ACCOUNT_KEY,storageActKey);

	    string appDir = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_APP_DIRECTORY);
	    if ( string.IsNullOrEmpty(appDir) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_STORAGE_APP_DIRECTORY}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_STORAGE_APP_DIRECTORY,appDir);

	    string acrName = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_ACR_NAME);
	    if ( string.IsNullOrEmpty(acrName) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_ACR_NAME}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_ACR_NAME,acrName);

	    string acrUser = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_ACR_USER);
	    if ( string.IsNullOrEmpty(acrUser) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_ACR_USER}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_ACR_USER,acrUser);

	    string acrUserPwd = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_ACR_USER_PWD);
	    if ( string.IsNullOrEmpty(acrUserPwd) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_ACR_USER_PWD}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_ACR_USER_PWD,acrUserPwd);

	    string vmImageId = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_BATCH_VM_IMAGE_ID);
	    if ( string.IsNullOrEmpty(vmImageId) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_BATCH_VM_IMAGE_ID}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_BATCH_VM_IMAGE_ID,vmImageId);

	    string vmImageSize = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_BATCH_VM_SIZE);
	    if ( string.IsNullOrEmpty(vmImageSize) )
	       throw new ArgumentNullException($"{AzureEnvConstants.AZURE_BATCH_VM_SIZE}");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_BATCH_VM_SIZE,vmImageSize);

	    string vmCount = Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_BATCH_VM_NODE_COUNT);
	    if ( string.IsNullOrEmpty(vmCount) )
	       envVars.Add(AzureEnvConstants.AZURE_BATCH_VM_NODE_COUNT,"1");
	    else
	       envVars.Add(AzureEnvConstants.AZURE_BATCH_VM_NODE_COUNT,vmCount);

	    // User account info.
	    string user = Environment.GetEnvironmentVariable(BatchUser.USER_NAME);
	    if ( string.IsNullOrEmpty(user) )
	       envVars.Add(BatchUser.USER_NAME,"labuser");
	    else
	       envVars.Add(BatchUser.USER_NAME,user);

	    string userPwd = Environment.GetEnvironmentVariable(BatchUser.USER_PWD);
	    if ( string.IsNullOrEmpty(userPwd) )
	       envVars.Add(BatchUser.USER_PWD,"labtest!");
	    else
	       envVars.Add(BatchUser.USER_PWD,userPwd);

	    // Elevation level can be 0 (non-admin) or 1 (admin)
	    string userEleLevel = Environment.GetEnvironmentVariable(BatchUser.USER_ELE_LEVEL);
	    if ( string.IsNullOrEmpty(userEleLevel) )
	       envVars.Add(BatchUser.USER_ELE_LEVEL,"1"); // Default is 'Admin'
	    else
	       envVars.Add(BatchUser.USER_ELE_LEVEL,userEleLevel);

	    return envVars;
        }
    }
}
