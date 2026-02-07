// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AzureSdk.SamplesMcp;

internal static class ConfigCommandHandler
{
    private const string ProjectFileName = "AzureSdk.SamplesMcp.csproj";
    private const string ServerName = "azsdk-samples";

    public static async Task<int> HandleAsync(string target, bool isGlobal)
    {
        // Validate target
        if (target != "copilot" && target != "claude" && target != "vscode")
        {
            Console.Error.WriteLine($"Error: Unknown target '{target}'. Valid targets are: copilot, claude, vscode");
            return 1;
        }

        // Validate vscode + global combination
        if (target == "vscode" && isGlobal)
        {
            Console.Error.WriteLine("Error: VS Code configuration does not support --global flag.");
            Console.Error.WriteLine("Use VS Code settings.json for global configuration instead.");
            return 1;
        }

        // Determine config file path
        string? configPath = GetConfigPath(target, isGlobal);
        if (configPath is null)
        {
            Console.Error.WriteLine($"Error: Could not determine configuration path for {target}.");
            if (!isGlobal)
            {
                Console.Error.WriteLine("Could not find repository root (no .git directory found).");
            }
            return 1;
        }

        // Create or update config file
        try
        {
            await CreateOrUpdateConfigAsync(configPath, target);
            Console.WriteLine($"Successfully created/updated configuration at: {configPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating configuration: {ex.Message}");
            return 1;
        }
    }

    private static string? GetConfigPath(string target, bool isGlobal)
    {
        if (isGlobal)
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return target switch
            {
                "copilot" => Path.Combine(homeDir, ".copilot", "mcp-config.json"),
                "claude" => Path.Combine(homeDir, ".claude.json"),
                _ => null
            };
        }
        else
        {
            string? repoRoot = FindRepositoryRoot();
            if (repoRoot is null)
            {
                return null;
            }

            return target switch
            {
                "copilot" => Path.Combine(repoRoot, ".copilot", "mcp-config.json"),
                "claude" => Path.Combine(repoRoot, ".mcp.json"),
                "vscode" => Path.Combine(repoRoot, ".vscode", "mcp.json"),
                _ => null
            };
        }
    }

    private static string? FindRepositoryRoot()
    {
        string? currentDir = Directory.GetCurrentDirectory();

        while (currentDir is not null)
        {
            string gitPath = Path.Combine(currentDir, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return currentDir;
            }

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return null;
    }

    private static string? FindProjectFile()
    {
        // Start from the executable's directory
        string? executablePath = Environment.ProcessPath;
        if (executablePath is null)
        {
            return null;
        }

        string? currentDir = Path.GetDirectoryName(executablePath);
        string? repoRoot = FindRepositoryRoot();

        // Search from executable directory up to repository root
        while (currentDir is not null)
        {
            string projectPath = Path.Combine(currentDir, ProjectFileName);
            if (File.Exists(projectPath))
            {
                return projectPath;
            }

            // Stop at repository root
            if (currentDir == repoRoot)
            {
                break;
            }

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return null;
    }

    private static string GetCommandName()
    {
        string? processPath = Environment.ProcessPath;
        if (processPath is not null)
        {
            return Path.GetFileName(processPath);
        }

        return ServerName;
    }

    private static (string command, JsonArray? args) GetCommandConfiguration()
    {
        string? projectPath = FindProjectFile();

        if (projectPath is not null)
        {
            // Running from source - use dotnet run with relative path
            string? repoRoot = FindRepositoryRoot();
            string relativePath = projectPath;

            if (repoRoot is not null && projectPath.StartsWith(repoRoot))
            {
                relativePath = Path.GetRelativePath(repoRoot, projectPath);
            }

            JsonArray args = new() { "run", "--project", relativePath, "--" };
            return ("dotnet", args);
        }
        else
        {
            // Using installed tool
            return (GetCommandName(), null);
        }
    }

    private static async Task CreateOrUpdateConfigAsync(string configPath, string target)
    {
        // Ensure parent directory exists
        string? directory = Path.GetDirectoryName(configPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Determine command configuration
        (string command, JsonArray? args) = GetCommandConfiguration();

        // Read existing config or create new
        JsonObject rootConfig;
        if (File.Exists(configPath))
        {
            string existingContent = await File.ReadAllTextAsync(configPath);
            rootConfig = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
        }
        else
        {
            rootConfig = new JsonObject();
        }

        // Build server configuration
        JsonObject serverConfig = BuildServerConfig(target, command, args);

        // Add to appropriate section based on target
        if (target == "vscode")
        {
            AddVsCodeConfig(rootConfig, serverConfig);
        }
        else
        {
            AddMcpServerConfig(rootConfig, serverConfig);
        }

        // Write config file
        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };
        string jsonContent = rootConfig.ToJsonString(options);
        await File.WriteAllTextAsync(configPath, jsonContent);
    }

    private static JsonObject BuildServerConfig(string target, string command, JsonArray? args)
    {
        JsonObject serverConfig = new()
        {
            ["type"] = "stdio",
            ["command"] = command
        };

        // Add args if present
        if (args is not null)
        {
            serverConfig["args"] = args;
        }

        // Add tools for Copilot
        if (target == "copilot")
        {
            serverConfig["tools"] = new JsonArray { "*" };
        }

        return serverConfig;
    }

    private static void AddVsCodeConfig(JsonObject rootConfig, JsonObject serverConfig)
    {
        // VS Code uses "servers" not "mcpServers"
        if (!rootConfig.ContainsKey("servers"))
        {
            rootConfig["servers"] = new JsonObject();
        }

        // Ensure inputs array exists
        if (!rootConfig.ContainsKey("inputs"))
        {
            rootConfig["inputs"] = new JsonArray();
        }

        JsonObject servers = rootConfig["servers"]!.AsObject();
        servers[ServerName] = serverConfig;
    }

    private static void AddMcpServerConfig(JsonObject rootConfig, JsonObject serverConfig)
    {
        // Copilot and Claude use "mcpServers"
        if (!rootConfig.ContainsKey("mcpServers"))
        {
            rootConfig["mcpServers"] = new JsonObject();
        }

        JsonObject mcpServers = rootConfig["mcpServers"]!.AsObject();
        mcpServers[ServerName] = serverConfig;
    }
}
