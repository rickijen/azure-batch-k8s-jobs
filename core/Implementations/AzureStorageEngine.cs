using System;
using System.Collections.Generic;

using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

using Microsoft.Azure.Batch;

using core.Constants;
using core.Interfaces;

namespace core.Implementations
{
    public class AzureStorageEngine : IStorageEngine
    {
       private static readonly AzureStorageEngine engine =
         new AzureStorageEngine();

       private string AccountName;
       private string AccountKey;
       private int SasKeyExpiryTime;

       private AzureStorageEngine()
       {
	  AccountName = 
	    Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_ACCOUNT_NAME);
	  AccountKey =
	    Environment.GetEnvironmentVariable(AzureEnvConstants.AZURE_STORAGE_ACCOUNT_KEY);
	  SasKeyExpiryTime = 1; // Default 1 Hour
       }

       public static IStorageEngine GetInstance() => engine;

       public ResourceFile GetResourceFile(
	 string containerName,
	 string blobName,
	 string filePath)
       {
          // Create a service level SAS that only allows reading from service
	  // level APIs
	  AccountSasBuilder sas = new AccountSasBuilder
	  {
	     // Allow access to blobs
	     Services = AccountSasServices.Blobs,

             // Allow access to all service level APIs
             ResourceTypes = AccountSasResourceTypes.All,
	     
	     // Specify token expiration in hours
	     ExpiresOn = DateTimeOffset.UtcNow.AddHours(SasKeyExpiryTime)
	  };

	  // Allow all access => Create, Delete, List, Process, Read, Write & Update
	  sas.SetPermissions(AccountSasPermissions.All);

	  // Create a SharedKeyCredential that we can use to sign the SAS token
	  StorageSharedKeyCredential credential = 
	    new StorageSharedKeyCredential(AccountName, AccountKey);
	
	  // Build a SAS URI
	  string storageAccountUri = String.Format("https://{0}.blob.core.windows.net",AccountName);
	  BlobUriBuilder blobUri = new BlobUriBuilder(new Uri(storageAccountUri));
	  blobUri.BlobContainerName = containerName;
	  // filePath if specified must include the trailing slash '/'
	  string resName = filePath + blobName;
	  blobUri.BlobName = resName;
	  blobUri.Query = sas.ToSasQueryParameters(credential).ToString();

	  return ResourceFile.FromUrl(blobUri.ToUri().ToString(),resName);
       }

       public List<ResourceFile> GetResourceFiles(
	 string containerName,
	 string folderName)
       {
	  List<ResourceFile> fileList = new List<ResourceFile>();

          // Create a service level SAS that only allows reading from service
	  // level APIs
	  AccountSasBuilder sas = new AccountSasBuilder
	  {
	     // Allow access to blobs
	     Services = AccountSasServices.Blobs,

             // Allow access to all service level APIs
             ResourceTypes = AccountSasResourceTypes.All,
	     
	     // Specify token expiration in hours
	     ExpiresOn = DateTimeOffset.UtcNow.AddHours(SasKeyExpiryTime)
	  };

	  // Allow all access => Create, Delete, List, Process, Read, Write & Update
	  sas.SetPermissions(AccountSasPermissions.All);

	  // Create a SharedKeyCredential that we can use to sign the SAS token
	  StorageSharedKeyCredential credential = 
	    new StorageSharedKeyCredential(AccountName, AccountKey);

	  string storageAccountUri = String.Format("https://{0}.blob.core.windows.net",AccountName);

	  BlobServiceClient blobSvcClient = new BlobServiceClient(new Uri(storageAccountUri), credential);
	  BlobContainerClient containerClient = blobSvcClient.GetBlobContainerClient(containerName);

	  ResourceFile file = null;
	  // folderName is a prefix, does not need to include trailing slash '/'
	  foreach (BlobItem blobItem in 
	    containerClient.GetBlobs(Azure.Storage.Blobs.Models.BlobTraits.None,
		                     Azure.Storage.Blobs.Models.BlobStates.None,
				     folderName))
	  {
	     BlobUriBuilder blobUri = new BlobUriBuilder(new Uri(storageAccountUri));
	     blobUri.BlobContainerName = containerName;
	     blobUri.BlobName = blobItem.Name;
	     blobUri.Query = sas.ToSasQueryParameters(credential).ToString();

	     file = ResourceFile.FromUrl(blobUri.ToUri().ToString(),blobItem.Name);
	     // file.FilePath = folderName;
	     fileList.Add(file);
	  };

	  return(fileList);
       }
    }
}
