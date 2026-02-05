string blobUrl = args switch
{
    [var url, ..] => url,
    _ => throw new Exception("Blob URL required"),
};

// Using configured MCP servers, download a blob from Azure Blob Storage given a blob URL using developer credentials.
// The blob contents should be saved to a file specified by an optional second argument, or written to stdout.
// Make sure the sample builds.
