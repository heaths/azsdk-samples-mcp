// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

/// <summary>
/// Provides dependency discovery and sample lookup for Node.js projects.
/// </summary>
internal class Node : IDependencyProvider
{
    /// <summary>
    /// Determines whether the specified directory contains a Node.js project.
    /// </summary>
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var manifestPath = Path.Combine(directory, "package.json");
        return fileSystem.FileExists(manifestPath);
    }

    /// <summary>
    /// Retrieves Azure SDK dependencies from the project lock file.
    /// </summary>
    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = default, FileSystem? fileSystem = null, bool includeDescriptions = false)
    {
        fileSystem ??= FileSystem.Default;

        // Check for lock files to determine package manager
        var npmLockPath = Path.Combine(directory, "package-lock.json");
        var pnpmLockPath = Path.Combine(directory, "pnpm-lock.yaml");

        if (fileSystem.FileExists(pnpmLockPath))
        {
            logger?.LogDebug("Found pnpm-lock.yaml, using pnpm");
            return await GetPnpmDependencies(directory, processService, logger, fileSystem, includeDescriptions).ConfigureAwait(false);
        }
        else if (fileSystem.FileExists(npmLockPath))
        {
            logger?.LogDebug("Found package-lock.json, using npm");
            return await GetNpmDependencies(directory, processService, logger, fileSystem, includeDescriptions).ConfigureAwait(false);
        }

        logger?.LogWarning("No lock file found (package-lock.json or pnpm-lock.yaml)");
        return [];
    }

    /// <summary>
    /// Locates README files for Azure SDK packages installed in node_modules.
    /// </summary>
    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var nodeModulesPath = Path.Combine(directory, "node_modules");
        if (!fileSystem.DirectoryExists(nodeModulesPath))
        {
            logger?.LogDebug("node_modules directory not found");
            return [];
        }

        // Get all @azure/* packages from npm/pnpm list
        IEnumerable<Dependency> allDependencies = await GetDependencies(directory, processService, logger, fileSystem).ConfigureAwait(false);

        // If dependencies parameter is not empty, filter to only those specified
        var dependencySet = dependencies.Select(d => d.Name).ToHashSet();
        if (dependencySet is { Count: > 0 })
        {
            allDependencies = allDependencies.Where(d => dependencySet.Contains(d.Name));
        }

        List<string> samples = [];
        foreach (var dependency in allDependencies)
        {
            logger?.LogDebug("Checking dependency {}", dependency.Name);

            // Handle scoped packages (e.g., @azure/keyvault-secrets)
            var dependencyPath = Path.Combine(nodeModulesPath, dependency.Name);

            logger?.LogDebug("Checking dependency directory {}", dependencyPath);
            if (!fileSystem.DirectoryExists(dependencyPath))
            {
                logger?.LogDebug("Dependency directory not found: {}", dependencyPath);
                continue;
            }

            var readmePath = Path.Combine(dependencyPath, "README.md");
            if (fileSystem.FileExists(readmePath))
            {
                logger?.LogDebug("Found README.md at {}", readmePath);
                samples.Add(readmePath);
            }
        }

        return samples;
    }

    private static async Task<IEnumerable<Dependency>> GetNpmDependencies(string directory, IExternalProcessService processService, ILogger? logger, FileSystem? fileSystem = null, bool includeDescriptions = false)
    {
        return await GetPackageManagerDependencies("npm", ".dependencies", directory, processService, logger, fileSystem, includeDescriptions).ConfigureAwait(false);
    }

    private static async Task<IEnumerable<Dependency>> GetPnpmDependencies(string directory, IExternalProcessService processService, ILogger? logger, FileSystem? fileSystem = null, bool includeDescriptions = false)
    {
        return await GetPackageManagerDependencies("pnpm", ".[].dependencies", directory, processService, logger, fileSystem, includeDescriptions).ConfigureAwait(false);
    }

    private static async Task<IEnumerable<Dependency>> GetPackageManagerDependencies(
        string commandName,
        string jsonPath,
        string directory,
        IExternalProcessService processService,
        ILogger? logger,
        FileSystem? fileSystem = null,
        bool includeDescriptions = false)
    {
        fileSystem ??= FileSystem.Default;
        var arguments = "list --json --depth=0";
        logger?.LogDebug("Running: {} {}", commandName, arguments);

        ProcessResult result = await processService.ExecuteAsync(
            commandName,
            arguments,
            workingDirectory: directory,
            cancellationToken: default).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new McpException($"{commandName} failed with exit code {result.ExitCode}");
        }

        var stdout = result.Output;
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(stdout);
        var depsElement = JsonPath.Query(doc, jsonPath).FirstOrDefault();

        if (depsElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        List<Dependency> dependencies = [];
        foreach (var property in depsElement.EnumerateObject())
        {
            var name = property.Name;

            // Filter for @azure packages
            if (!name.StartsWith("@azure/"))
            {
                continue;
            }

            var version = property.Value.TryGetProperty("version", out var versionElement)
                ? versionElement.GetString()
                : null;

            if (NodePackage.TryCreate(name, version, out NodePackage? package))
            {
                logger?.LogDebug("Adding dependency {}@{}", name, version);
                
                string? description = null;
                if (includeDescriptions)
                {
                    description = GetPackageDescription(directory, package, fileSystem, logger);
                }
                
                dependencies.Add(new Dependency(package.Name, package.Version, description));
            }
        }

        return dependencies;
    }

    private static string? GetPackageDescription(string directory, NodePackage package, FileSystem fileSystem, ILogger? logger)
    {
        var packageJsonPath = Path.Combine(directory, "node_modules", package.Name, "package.json");
        
        if (!fileSystem.FileExists(packageJsonPath))
        {
            logger?.LogDebug("package.json not found at {}", packageJsonPath);
            return null;
        }

        try
        {
            var packageJsonContent = fileSystem.ReadAllText(packageJsonPath);
            using var doc = JsonDocument.Parse(packageJsonContent);
            
            if (doc.RootElement.TryGetProperty("description", out var descElement))
            {
                return descElement.GetString();
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug("Failed to read description from {}: {}", packageJsonPath, ex.Message);
        }
        
        return null;
    }
}

/// <summary>
/// Represents a resolved Node.js package with its name and version.
/// </summary>
internal record NodePackage(string Name, string Version)
{
    public static bool TryCreate(string? name, string? version, [NotNullWhen(true)] out NodePackage? package)
    {
        package = null;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (string.IsNullOrWhiteSpace(version))
            return false;

        package = new(name, version);
        return true;
    }
}
