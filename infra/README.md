# Azure Infrastructure

This directory contains Azure Developer CLI (azd) infrastructure-as-code templates for provisioning Azure resources used by the samples.

## Resources Provisioned

- **Storage Account** (`st<unique>`) - Contains an `examples` container with a sample `README.md` file for testing blob downloads
- **App Configuration** (`appconfig-<unique>`) - Contains two configuration values:
  - `greeting`: Plain text value "Hello, World!"
  - `settings`: JSON value `{"enabled":true,"timeout":30}`
- **Key Vault** (`kv-<unique>`) - Contains two secrets:
  - `database-connection`: Sample database connection string
  - `api-key`: Sample API key

## Deployment

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- Azure subscription

### Deploy Infrastructure

1. Initialize the environment (first time only):

   ```bash
   azd env new <environment-name>
   ```

2. Provision the Azure resources:

   ```bash
   azd provision
   ```

   This will:
   - Create a resource group
   - Deploy all Azure resources
   - Assign RBAC roles to your user for accessing the resources
   - Automatically upload README.md to the blob storage
   - Output environment variables with resource endpoints

### Using the Resources

After deployment, use the output values to run the samples:

```bash
# Storage blob URL
azd env get-value AZURE_STORAGE_BLOB_URL

# App Configuration endpoint
azd env get-value AZURE_APPCONFIG_ENDPOINT

# Key Vault endpoint
azd env get-value AZURE_KEYVAULT_ENDPOINT
```

### Clean Up

To delete all resources:

```bash
azd down
```

## RBAC Roles

The following roles are automatically assigned to the user who runs `azd provision`:

- **Storage Blob Data Contributor** - Read/write access to blob storage
- **App Configuration Data Reader** - Read access to App Configuration values
- **Key Vault Secrets User** - Read access to Key Vault secrets
