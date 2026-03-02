# List Blobs (Python) Sample

This Python sample demonstrates how to list blobs from Azure Blob Storage using developer credentials.

## Prerequisites

- [Python 3.9+](https://www.python.org/downloads/)
- Azure Storage account with blob access
- Authenticated with Azure (e.g., `az login`)

To provision Azure resources for this sample, see [infra/README.md](../../infra/README.md).

## Using with MCP Server

Enable the MCP server as configured in [.copilot/mcp-config.json](../../.copilot/mcp-config.json).
Start [Copilot CLI](https://github.com/features/copilot/cli/) or use Copilot in [VSCode](https://code.visualstudio.com/) and prompt it with the comment in [list_blobs.py](list_blobs.py).

## Building

Install dependencies:

```bash
cd samples/list-blobs-python
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Running

Run the sample with a storage blob URL:

```bash
cd samples/list-blobs-python
source .venv/bin/activate
python list_blobs.py https://your-storage.blob.core.windows.net/container
```

If you provisioned resources using `azd`, use the environment variable:

```bash
cd samples/list-blobs-python
source .venv/bin/activate
python list_blobs.py $(azd env get-value AZURE_STORAGE_BLOB_URL)
```
