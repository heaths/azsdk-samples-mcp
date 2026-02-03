# Copilot Instructions

This file provides instructions for GitHub Copilot when working on the azsdk-samples-mcp repository.

## Code Style

- Use .NET style guide conventions
- Ensure all code follows the project's formatting standards by running `dotnet format --severity warn`
- Fix any formatting issues that the formatter reports but cannot automatically correct
- Use structured logging with `ILogger` instead of `Console.WriteLine`
- All C# source files must include a copyright header:

  ```csharp
  // Copyright {Year} Heath Stewart.
  // Licensed under the MIT License. See LICENSE.txt in the project root for license information.
  ```

  Use the year when the source file was created.

For project structure and common commands, see [AGENTS.md](../AGENTS.md).

## Repository Overview

This is an MCP server that discovers and retrieves samples from Azure SDK dependencies. The main logic is in C# (.NET 10.0), with Rust test utilities for integration testing with Azure services.

**Key Components:**

- `Tools.cs`: Defines MCP tools for listing dependencies and samples
- `Providers/Cargo.cs`: Implements dependency discovery for Rust Cargo projects
- `Program.cs`: Configures logging and MCP server setup

## Test Naming Conventions

Test source files and classes should end in "Tests", with the base name matching the class being tested:

- File: `FileSystemTests.cs`
- Class: `FileSystemTests`
- Tests for: `FileSystem`

Use MSTest 4.0 assertions and patterns for all tests.

## Commits and Pull Requests

- Follow recommended git commit style (conventional commits format)
- Pull request titles should follow git commit title style
- Pull request descriptions should provide an overview of changes without being overly verbose
- Don't describe every file—focus on the big picture of what changed and why

## Important Notes

- Logging must be configured in `Program.cs` only
- All components should inject and use `ILoggerFactory` for logging
