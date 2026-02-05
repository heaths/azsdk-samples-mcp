targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment used to generate a short unique hash')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

@description('Id of the principal to assign roles to')
param principalId string = ''

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

module resources './resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    resourceToken: resourceToken
    principalId: principalId
    tags: tags
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId

output AZURE_STORAGE_ACCOUNT_NAME string = resources.outputs.STORAGE_ACCOUNT_NAME
output AZURE_STORAGE_BLOB_URL string = resources.outputs.STORAGE_BLOB_URL

output AZURE_APPCONFIG_ENDPOINT string = resources.outputs.APPCONFIG_ENDPOINT

output AZURE_KEYVAULT_NAME string = resources.outputs.KEYVAULT_NAME
output AZURE_KEYVAULT_ENDPOINT string = resources.outputs.KEYVAULT_ENDPOINT
