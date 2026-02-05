# Download Blob Sample

This .NET sample demonstrates how to download a blob from Azure Blob Storage using developer credentials.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Azure Storage account with blob access
- Authenticated with Azure (e.g., `az login`)

To provision Azure resources for this sample, see [infra/README.md](../../infra/README.md).

## Using with MCP Server

Enable the MCP server as configured in [.copilot/mcp-config.json](../../.copilot/mcp-config.json).
Start [Copilot CLI](https://github.com/features/copilot/cli/) or use Copilot in [VSCode](https://code.visualstudio.com/) and prompt it with the comment in [Program.cs](Program.cs).

## Building

Build the sample:

```bash
dotnet build samples/download-blob/download-blob.csproj
```

## Running

Run the sample with a blob URL and write to stdout:

```bash
dotnet run --project samples/download-blob/download-blob.csproj -- https://your-storage.blob.core.windows.net/container/blob
```

Or save to a file:

```bash
dotnet run --project samples/download-blob/download-blob.csproj -- https://your-storage.blob.core.windows.net/container/blob output.txt
```

If you provisioned resources using `azd`, use the environment variable:

```bash
dotnet run --project samples/download-blob/download-blob.csproj -- $(azd env get-value AZURE_STORAGE_BLOB_URL)
```

Note: `dotnet run` automatically builds the project if needed.
