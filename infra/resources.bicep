param location string
param resourceToken string
param principalId string
param tags object

var abbrs = loadJsonContent('./abbreviations.json')

// Storage Account with blob
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: '${abbrs.storageStorageAccounts}${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }

  resource blobService 'blobServices' = {
    name: 'default'

    resource container 'containers' = {
      name: 'examples'
    }
  }
}

// App Configuration
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: '${abbrs.appConfigurationConfigurationStores}${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'standard'
  }
  properties: {
    enablePurgeProtection: false
  }

  resource keyValue1 'keyValues' = {
    name: 'greeting'
    properties: {
      value: 'Hello, World!'
      contentType: 'text/plain'
    }
  }

  resource keyValue2 'keyValues' = {
    name: 'settings'
    properties: {
      value: '{"enabled":true,"timeout":30}'
      contentType: 'application/json'
    }
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${abbrs.keyVaultVaults}${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }

  resource secret1 'secrets' = {
    name: 'database-connection'
    properties: {
      value: 'Server=localhost;Database=mydb;User=admin;Password=secret123'
    }
  }

  resource secret2 'secrets' = {
    name: 'api-key'
    properties: {
      value: 'sk-1234567890abcdef'
    }
  }
}

// RBAC role assignments for the principal
var storageBlobDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var appConfigDataReaderRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071')
var keyVaultSecretsUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: storageAccount
  name: guid(storageAccount.id, principalId, storageBlobDataContributorRole)
  properties: {
    roleDefinitionId: storageBlobDataContributorRole
    principalId: principalId
    principalType: 'User'
  }
}

resource appConfigRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: appConfig
  name: guid(appConfig.id, principalId, appConfigDataReaderRole)
  properties: {
    roleDefinitionId: appConfigDataReaderRole
    principalId: principalId
    principalType: 'User'
  }
}

resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: keyVault
  name: guid(keyVault.id, principalId, keyVaultSecretsUserRole)
  properties: {
    roleDefinitionId: keyVaultSecretsUserRole
    principalId: principalId
    principalType: 'User'
  }
}

output STORAGE_ACCOUNT_NAME string = storageAccount.name
output STORAGE_BLOB_URL string = 'https://${storageAccount.name}.blob.${environment().suffixes.storage}/${storageAccount::blobService::container.name}/README.md'

output APPCONFIG_ENDPOINT string = appConfig.properties.endpoint

output KEYVAULT_NAME string = keyVault.name
output KEYVAULT_ENDPOINT string = keyVault.properties.vaultUri
