using './main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azsdk-samples-dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'westus')
param principalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')
