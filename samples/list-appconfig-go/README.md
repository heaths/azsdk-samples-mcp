# List App Configuration Values (Go) Sample

This Go sample demonstrates how to list configuration values from Azure App Configuration using developer credentials.

## Prerequisites

- [Go 1.23+](https://go.dev/dl/)
- Azure App Configuration store with read access
- Authenticated with Azure (e.g., `az login`)

To provision Azure resources for this sample, see [infra/README.md](../../infra/README.md).

## Using with MCP Server

Enable the MCP server as configured in [.copilot/mcp-config.json](../../.copilot/mcp-config.json).
Start [Copilot CLI](https://github.com/features/copilot/cli/) or use Copilot in [VSCode](https://code.visualstudio.com/) and prompt it with the comment in [main.go](main.go).

## Building

Build the sample:

```bash
cd samples/list-appconfig-go
go build -o list-appconfig-go .
```

## Running

Run the sample with an App Configuration endpoint:

```bash
cd samples/list-appconfig-go
go run . https://your-appconfig.azconfig.io
```

Or set the endpoint as an environment variable:

```bash
export AZURE_APPCONFIG_ENDPOINT=https://your-appconfig.azconfig.io
cd samples/list-appconfig-go
go run .
```

If you provisioned resources using `azd`, use the environment variable:

```bash
cd samples/list-appconfig-go
go run . $(azd env get-value AZURE_APPCONFIG_ENDPOINT)
```
