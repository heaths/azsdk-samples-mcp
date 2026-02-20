# Agent Instructions

This document provides guidance for agents working on the azsdk-samples-mcp repository.

## Repository Overview

This repository contains a Model Context Protocol (MCP) server built in C# that provides tools for discovering and retrieving samples from Azure SDK dependencies. It also includes Rust utilities for testing.

### Repository Structure

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

## Style Guidelines

- Use .NET style guide conventions.
- Ensure all code follows project formatting standards by running `dotnet format --severity warn`.
- Fix any formatting issues that the formatter reports but cannot automatically correct.
- Use structured logging with `ILogger` instead of `Console.WriteLine`.
- Logging must be configured in `Program.cs` only.
- All components should inject and use `ILoggerFactory` for logging.
- All C# source files must include a copyright header:

   ```csharp
   // Copyright {Year} Heath Stewart.
   // Licensed under the MIT License. See LICENSE.txt in the project root for license information.
   ```

   Use the year when the source file was created.

### Test Naming

Test source files and classes should end in "Tests", with the base name matching the class being tested:

- File: `FileSystemTests.cs`
- Class: `FileSystemTests`
- Tests for: `FileSystem`

Use MSTest 4.0 assertions and patterns for all tests.

### Logging Example

```csharp
ILogger logger = context.Services!.GetRequiredService<ILoggerFactory>()
      .CreateLogger("ToolName");
logger.LogDebug("Debug message {}", param);
logger.LogInformation("Info message {}", param);
```

## Development Commands

### Building

Build the main MCP server from the repository root:

```bash
dotnet build
```

This builds both `src/AzureSdk.SamplesMcp` and the tests in `tests/AzureSdk.SamplesMcp.Test`.

### Testing

Run unit tests from the repository root:

```bash
dotnet test
```

### Linting

**ALWAYS use the repository skills for linting. Do not run linting commands directly.**

#### When to Run Linting

Run linting checks at these key times during development:

- **Early**: After adding new code with technical terms, library names, or identifiers
- **During development**: After creating or modifying markdown documentation
- **Before first commit**: Before running `report_progress` for the first time
- **Throughout**: Whenever you add new words that might be flagged as misspellings
- **Final check**: As part of the pre-commit checklist

#### Skills

- **cspell**: [Check and fix spelling in project source files using cSpell.](.github/skills/cspell/SKILL.md)
- **markdownlint**: [Check and fix formatting and other issues in markdown files using markdownlint-cli2.](.github/skills/markdownlint/SKILL.md)

### Formatting

Before committing changes, run:

```bash
dotnet format --severity warn
```

If `dotnet format` cannot fix remaining issues, manually address them according to the error messages. The analyzer in the CI pipeline verifies formatting.

## Samples and Provisioning

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

## Pre-commit Checklist

Before creating a commit or pull request, verify all quality checks have passed.

This is a final verification. Linting should already be run during development (see [Linting](#linting)).

1. Complete [Formatting](#formatting).
2. Run both linting skills listed in [Skills](#skills).
3. Complete [Building](#building).
4. Complete [Testing](#testing).

All checks must pass before committing.

## Commits and Pull Requests

See [.github/instructions/commits.instructions.md](.github/instructions/commits.instructions.md) for commit message and pull request guidelines.
