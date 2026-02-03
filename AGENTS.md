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
├── tests/secrets/                    # Rust test utility
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

### Rust Utilities

Build the secrets test utility:

```bash
cargo build --manifest-path tests/secrets/Cargo.toml
```

## Testing

### .NET Tests

Run unit tests from the repository root:

```bash
dotnet test
```

### MCP Server with Rust

The MCP server can be tested interactively using the Rust `main.rs` utility, which demonstrates listing secrets from an Azure Key Vault:

```bash
cargo build --manifest-path tests/secrets/Cargo.toml
./tests/secrets/target/debug/secrets https://your-vault.vault.azure.net/
```

Or with environment variable:

```bash
export AZURE_KEYVAULT_URL=https://your-vault.vault.azure.net/
./tests/secrets/target/debug/secrets
```

## Code Style and Formatting

For coding conventions and style guidance, also follow the instructions in [.github/copilot-instructions.md](.github/copilot-instructions.md).

Before committing changes, run the formatter to fix style issues:

```bash
dotnet format --severity warn
```

If `dotnet format` cannot fix remaining issues, manually address them according to the error messages. The analyzer in the CI pipeline will verify formatting is correct.

## Linting

Before committing changes, run spell checking and Markdown linting using the repository skills:

- For spelling, use the cspell skill instructions in `.github/skills/cspell/SKILL.md` (honors repo config and version pinning).
- For Markdown, use the markdownlint skill instructions in `.github/skills/markdownlint/SKILL.md` (honors repo config and version pinning).

## Logging

Logging is configured centrally in `Program.cs`. All components should use the injected `ILoggerFactory` to create loggers rather than writing directly to console. The log level defaults to `Information` but can be adjusted in the configuration.

Example:

```csharp
ILogger logger = context.Services!.GetRequiredService<ILoggerFactory>()
    .CreateLogger("ToolName");
logger.LogDebug("Debug message {}", param);
logger.LogInformation("Info message {}", param);
```
