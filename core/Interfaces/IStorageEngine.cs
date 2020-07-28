using System;
using System.Collections.Generic;

using Microsoft.Azure.Batch;

namespace core.Interfaces
{
    public interface IStorageEngine
    {
       public ResourceFile GetResourceFile(
	 string containerName,
	 string blobName,
	 string filePath);

       public List<ResourceFile> GetResourceFiles(
	 string containerName,
	 string folderName);
    }
}
