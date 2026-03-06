# List Secrets (Java) Sample

This Java sample demonstrates how to list secrets from an Azure Key Vault using developer credentials.

## Prerequisites

- [Java 17+](https://adoptium.net/)
- [Maven](https://maven.apache.org/download.cgi)
- Azure Key Vault with secret access
- Authenticated with Azure (e.g., `az login`)

To provision Azure resources for this sample, see [infra/README.md](../../infra/README.md).

## Using with MCP Server

Enable the MCP server as configured in [.copilot/mcp-config.json](../../.copilot/mcp-config.json).
Start [Copilot CLI](https://github.com/features/copilot/cli/) or use Copilot in [VSCode](https://code.visualstudio.com/) and prompt it with the comment in [src/main/java/com/example/ListSecrets.java](src/main/java/com/example/ListSecrets.java).

## Building

Build the sample:

```bash
cd samples/list-secrets-java
mvn package
```

## Running

Run the sample with a vault URL:

```bash
cd samples/list-secrets-java
mvn exec:java -Dexec.mainClass="com.example.ListSecrets" -Dexec.args="https://your-vault.vault.azure.net/"
```

Or set the vault URL as an environment variable:

```bash
export AZURE_KEYVAULT_URL=https://your-vault.vault.azure.net/
mvn exec:java -Dexec.mainClass="com.example.ListSecrets"
```

If you provisioned resources using `azd`, use the environment variable:

```bash
cd samples/list-secrets-java
mvn exec:java -Dexec.mainClass="com.example.ListSecrets" -Dexec.args="$(azd env get-value AZURE_KEYVAULT_ENDPOINT)"
```
