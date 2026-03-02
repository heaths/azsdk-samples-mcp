"""List blobs from Azure Blob Storage using developer credentials."""

import sys
import os

endpoint = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("AZURE_STORAGE_BLOB_URL")
if not endpoint:
    raise SystemExit("Storage blob URL or $AZURE_STORAGE_BLOB_URL required")

# Using configured MCP servers, list blobs in an Azure Storage container given a blob URL using developer credentials.
# The blob names should be sorted and printed on separate lines.
# Make sure the sample runs.
