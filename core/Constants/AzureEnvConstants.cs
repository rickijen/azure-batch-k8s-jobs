// 
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;

/**
 * Description:
 * This class defines constant values for Azure Environment
 * 
 * Author: GR @Microsoft
 * Dated: 07-17-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace core.Constants
{
    public static class AzureEnvConstants
    {
	public const string AZURE_AD_TENANT_ID = "AZURE_AD_TENANT_ID";
	public const string AZURE_AD_TOKEN_EP = "https://login.microsoftonline.com/";
	public const string AZURE_AD_SP_CLIENT_ID = "AZURE_AD_SP_CLIENT_ID";
	public const string AZURE_AD_SP_CLIENT_SECRET = "AZURE_AD_SP_CLIENT_SECRET";

	public const string AZURE_STORAGE_ACCOUNT_NAME = "AZURE_STORAGE_ACCOUNT_NAME";
	public const string AZURE_STORAGE_ACCOUNT_KEY = "AZURE_STORAGE_ACCOUNT_KEY";
	public const string AZURE_STORAGE_APP_DIRECTORY = "AZURE_STORAGE_APP_DIRECTORY";
	public const string AZURE_STORAGE_K8S_DIRECTORY = "AZURE_STORAGE_K8S_DIRECTORY";

	public const string AZURE_ACR_NAME = "AZURE_ACR_NAME";
	public const string AZURE_ACR_USER = "AZURE_ACR_USER";
	public const string AZURE_ACR_USER_PWD = "AZURE_ACR_USER_PWD";

	public const string AZURE_BATCH_RESOURCE_URI = "https://batch.core.windows.net/";
	public const string AZURE_BATCH_ACCOUNT_NAME = "AZURE_BATCH_ACCOUNT_NAME";
	public const string AZURE_BATCH_ACCOUNT_URL = "AZURE_BATCH_ACCOUNT_URL";
	public const string AZURE_BATCH_VM_IMAGE_ID = "AZURE_BATCH_VM_IMAGE_ID";
	public const string AZURE_BATCH_VM_SIZE = "AZURE_BATCH_VM_SIZE";
	public const string AZURE_BATCH_VM_NODE_COUNT = "AZURE_BATCH_VM_NODE_COUNT";
	public const int AZURE_BATCH_JOB_TIMEOUT = 10; // 10 minutes default
    }
}
