# Azure SDK Samples MCP

An MCP (Model Context Protocol) server that discovers and retrieves code samples from Azure SDK packages. This server provides AI agents and coding assistants with direct access to real-world examples from Azure SDK dependencies, helping developers use the Azure SDKs safely and effectively.

## Overview

When working with Azure SDKs, having access to relevant code examples can significantly improve development efficiency and reduce errors. This MCP server automatically discovers Azure SDK samples from your project's dependencies (NuGet, npm, Cargo) and makes them available to AI agents like GitHub Copilot.

**Key features:**

- Discovers Azure SDK samples from multiple package managers (NuGet, npm, Cargo)
- Lists project dependencies and their available samples
- Integrates seamlessly with AI agents and coding assistants via the MCP protocol
- Supports .NET, Node.js, and Rust projects

## Demo

[![Azure SDK Samples MCP Demo](https://img.youtube.com/vi/MAhdQDmkZOs/0.jpg)](https://www.youtube.com/watch?v=MAhdQDmkZOs)

## Installation

### Quick Start

1. **Authenticate with GitHub Packages** (required even for public packages):

   First, create a [GitHub Personal Access Token (PAT)](https://github.com/settings/tokens/new) with `read:packages` scope.

   Then, configure the NuGet source with your GitHub username and token:

   ```bash
   dotnet nuget add source "https://nuget.pkg.github.com/heaths/index.json" \
     --name github-heaths \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_GITHUB_TOKEN \
     --store-password-in-clear-text
   ```

   Replace `YOUR_GITHUB_USERNAME` with your GitHub username (without quotes) and `YOUR_GITHUB_TOKEN` with your PAT (without quotes).

   > **Note:** The `--store-password-in-clear-text` flag stores the token in plaintext in your NuGet configuration file. This is required because encrypted storage is not supported on all platforms. Keep your token secure and avoid committing NuGet configuration files to version control.

2. Install the MCP server as a global tool:

   ```bash
   dotnet tool install --global AzureSdk.SamplesMcp --add-source github-heaths --prerelease
   ```

3. Add to VS Code settings (⌘, on macOS, Ctrl+, on Windows/Linux):
   - Search for "MCP Servers" and find your AI extension settings
   - Add this configuration:

     ```json
     {
       "azsdk-samples": {
         "command": "AzureSdk.SamplesMcp",
         "args": []
       }
     }
     ```

4. Restart VS Code to load the MCP server

### Building from Source (Optional)

If you prefer to run locally from source:

1. Clone the repository:

   ```bash
   git clone https://github.com/heaths/azsdk-samples-mcp.git
   cd azsdk-samples-mcp
   ```

2. Update VS Code settings to run from source:

   ```json
   {
     "azsdk-samples": {
       "command": "dotnet",
       "args": [
         "run",
         "--project",
         "/path/to/azsdk-samples-mcp/src/AzureSdk.SamplesMcp/AzureSdk.SamplesMcp.csproj",
         "--"
       ]
     }
   }
   ```

   Replace `/path/to/azsdk-samples-mcp` with the actual location of your cloned repository.

3. Restart VS Code to load the MCP server

## Usage

Once integrated into your IDE:

- Ask your AI assistant about Azure SDK samples relevant to your code
- The assistant will have access to real examples from your dependencies
- Get context-specific guidance on how to properly use Azure SDK APIs

## Samples

Example applications demonstrating how to use this MCP server:

- [Download Azure Storage blob in .NET](samples/download-blob/README.md)
- [List Azure App Configuration values in TypeScript](samples/list-appconfig/README.md)
- [List Azure Key Vault secrets in Rust](samples/list-secrets/README.md)

To provision Azure resources for running these samples, see [infra/README.md](infra/README.md).

## License

Licensed under the [MIT](LICENSE.txt) license.
