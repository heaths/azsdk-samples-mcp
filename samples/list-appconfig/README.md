# List App Configuration Values Sample

This TypeScript sample demonstrates how to list configuration values from Azure App Configuration using developer credentials.

## Prerequisites

- [Node.js](https://nodejs.org/) (LTS version)
- Azure App Configuration store with read access
- Authenticated with Azure (e.g., `az login`)

To provision Azure resources for this sample, see [infra/README.md](../../infra/README.md).

## Using with MCP Server

Enable the MCP server as configured in [.copilot/mcp-config.json](../../.copilot/mcp-config.json).
Start [Copilot CLI](https://github.com/features/copilot/cli/) or use Copilot in [VSCode](https://code.visualstudio.com/) and prompt it with the comment in [src/index.ts](src/index.ts).

## Building

Install dependencies and build the sample:

```bash
cd samples/list-appconfig
npm install
npm run build
```

## Running

Build the sample first:

```bash
cd samples/list-appconfig
npm run build
```

Then run the sample with an App Configuration endpoint:

```bash
npm exec list-appconfig -- https://your-appconfig.azconfig.io
```

If you provisioned resources using `azd`, use the environment variable:

```bash
npm exec list-appconfig -- $(azd env get-value AZURE_APPCONFIG_ENDPOINT)
```

Note: You must run `npm run build` after any code changes before running the sample.
