# Agent Instructions

This document provides guidance for agents working on the azsdk-samples-mcp repository.

## Repository Structure

The repository contains a Model Context Protocol (MCP) server built in C# that provides tools for discovering and retrieving samples from Azure SDK dependencies. It also includes Rust utilities for testing.

```text
├── src/AzureSdk.SamplesMcp/          # Main MCP server (.NET)
│   ├── Tools.cs                      # MCP tool definitions (dependencies, samples)
│   ├── Providers/                    # Dependency provider implementations
│   │   └── Cargo.cs                  # Rust Cargo provider
│   ├── Services/                     # Service utilities
│   │   └── Command.cs                # Command execution utility
│   ├── FileSystem.cs                 # File system abstraction
│   ├── Environment.cs                # Environment variable access
│   ├── Program.cs                    # Application entry point
│   └── AzureSdk.SamplesMcp.csproj    # Project file
├── tests/AzureSdk.SamplesMcp.Test/   # Unit tests (.NET)
├── samples/list-secrets/             # Rust sample utility
│   ├── src/main.rs                   # Secrets sample using Azure SDK
│   └── Cargo.toml                    # Rust project manifest
└── .github/workflows/                # CI/CD workflows
```

## Building

### .NET Project

Build the main MCP server from the repository root:

```bash
dotnet build
```

This builds both the `src/AzureSdk.SamplesMcp` project and the test suite in `tests/AzureSdk.SamplesMcp.Test`.

## Testing

### .NET Tests

Run unit tests from the repository root:

```bash
dotnet test
```

### Provisioning Azure Resources

To provision Azure resources for testing the samples, see [infra/README.md](infra/README.md) for instructions on deploying Storage, App Configuration, and Key Vault resources using Azure Developer CLI.

### MCP Server with .NET

Test the MCP server using the .NET sample utility, which downloads a blob from Azure Blob Storage.
See [samples/download-blob/README.md](samples/download-blob/README.md) for details on how to build and run the sample.

### MCP Server with Rust

Test the MCP server using the Rust sample utility, which lists secrets from an Azure Key Vault.
See [samples/list-secrets/README.md](samples/list-secrets/README.md) for details on how to build and run the sample.

### MCP Server with TypeScript

Test the MCP server using the TypeScript sample utility, which lists configuration values from Azure App Configuration.
See [samples/list-appconfig/README.md](samples/list-appconfig/README.md) for details on how to build and run the sample.

### Running All Samples

To run all samples with provisioned resources:

1. **Provision Azure resources** interactively:

   ```bash
   azd provision
   ```

   Follow the prompts to select subscription, location, and other parameters.

2. **Process all samples' comment prompts** using the MCP server configured in your AI assistant. For each sample, use the comment at the top of the source file as a prompt to implement the functionality using Azure SDK samples.

3. **Run all the samples** using the provisioned resources:

   ```bash
   # .NET sample
   dotnet run --project samples/download-blob/download-blob.csproj -- $(azd env get-value AZURE_STORAGE_BLOB_URL)

   # Rust sample
   cargo run --manifest-path samples/list-secrets/Cargo.toml -- $(azd env get-value AZURE_KEYVAULT_ENDPOINT)

   # TypeScript sample (build first)
   cd samples/list-appconfig && npm run build
   npm exec list-appconfig -- $(azd env get-value AZURE_APPCONFIG_ENDPOINT)
   ```

## Code Style and Formatting

For coding conventions and style guidance, also follow the instructions in [.github/copilot-instructions.md](.github/copilot-instructions.md).

Before committing changes, run the formatter to fix style issues:

```bash
dotnet format --severity warn
```

If `dotnet format` cannot fix remaining issues, manually address them according to the error messages. The analyzer in the CI pipeline will verify formatting is correct.

## Linting

**ALWAYS use the repository skills for linting. Do not run linting commands directly.**

For all linting tasks:

- **Spelling**: Follow the cspell skill instructions in [.github/skills/cspell/SKILL.md](.github/skills/cspell/SKILL.md) — This reads your `package.json` for version pinning and applies the project's cspell configuration.
- **Markdown**: Follow the markdownlint skill instructions in [.github/skills/markdownlint/SKILL.md](.github/skills/markdownlint/SKILL.md) — This reads your `package.json` for version pinning and applies the project's markdownlint configuration.

## Pre-commit Checklist

Before creating a commit or pull request, run all quality checks:

```bash
# 1. Format .NET code
dotnet format --severity warn

# 2. Check spelling (see .github/skills/cspell/SKILL.md for command)
# 3. Lint Markdown (see .github/skills/markdownlint/SKILL.md for command)

# 4. Run tests
dotnet test

# 5. Build to verify no compilation errors
dotnet build
```

All checks must pass before committing. See the skill files for the exact linting commands with proper version pinning.

## Logging

Logging is configured centrally in `Program.cs`. All components should use the injected `ILoggerFactory` to create loggers rather than writing directly to console. The log level defaults to `Information` but can be adjusted in the configuration.

Example:

```csharp
ILogger logger = context.Services!.GetRequiredService<ILoggerFactory>()
    .CreateLogger("ToolName");
logger.LogDebug("Debug message {}", param);
logger.LogInformation("Info message {}", param);
```
