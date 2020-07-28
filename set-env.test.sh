#!/bin/bash

# Azure AD env variables
export AZURE_AD_TENANT_ID="Azure Tenant ID"
export AZURE_AD_SP_CLIENT_ID="Azure AD Service Principal Client ID"
export AZURE_AD_SP_CLIENT_SECRET="Azure AD Service Principal Client Secret"

# Batch env variables
export AZURE_BATCH_ACCOUNT_NAME="Azure Batch Account name"
export AZURE_BATCH_ACCOUNT_URL="https://<Batch-account-name>.westus2.batch.azure.com"
export AZURE_BATCH_VM_IMAGE_ID="Azure Shared Image Gallery image ID"
export AZURE_BATCH_VM_SIZE="STANDARD_D2_V3"
export AZURE_BATCH_VM_NODE_COUNT=1

# Container registry env variables
export AZURE_ACR_NAME="<acr-name>.azurecr.io"
export AZURE_ACR_USER="ACR Admin user name"
export AZURE_ACR_USER_PWD="ACR Admin user password"

# Storage env variables
export AZURE_STORAGE_ACCOUNT_NAME="Azure Storage Account name"
export AZURE_STORAGE_ACCOUNT_KEY="Azure Storage Account key"
export AZURE_STORAGE_APP_DIRECTORY="apps"
export AZURE_STORAGE_K8S_DIRECTORY="k8sresources"
