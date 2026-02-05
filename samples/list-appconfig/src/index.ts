#!/usr/bin/env node

const endpoint = process.argv[2];
if (!endpoint) {
    throw new Error('App Configuration endpoint required');
}

// Using configured MCP servers, list values from Azure App Configuration given an endpoint using developer credentials.
// The values should be printed alphabetically in the format "{name}: {type} = {value}" on each line.
// Make sure the sample builds.
