using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

internal class Node : IDependencyProvider
{
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var manifestPath = Path.Combine(directory, "package.json");
        return fileSystem.FileExists(manifestPath);
    }

    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = default, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        // Check for lock files to determine package manager
        var npmLockPath = Path.Combine(directory, "package-lock.json");
        var pnpmLockPath = Path.Combine(directory, "pnpm-lock.yaml");

        if (fileSystem.FileExists(pnpmLockPath))
        {
            logger?.LogDebug("Found pnpm-lock.yaml, using pnpm");
            return await GetPnpmDependencies(directory, processService, logger).ConfigureAwait(false);
        }
        else if (fileSystem.FileExists(npmLockPath))
        {
            logger?.LogDebug("Found package-lock.json, using npm");
            return await GetNpmDependencies(directory, processService, logger).ConfigureAwait(false);
        }

        logger?.LogWarning("No lock file found (package-lock.json or pnpm-lock.yaml)");
        return [];
    }

    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var nodeModulesPath = Path.Combine(directory, "node_modules");
        if (!fileSystem.DirectoryExists(nodeModulesPath))
        {
            logger?.LogDebug("node_modules directory not found");
            return [];
        }

        var dependencySet = dependencies.Select(d => d.Name).ToHashSet();
        if (dependencySet is not { Count: > 0 })
        {
            logger?.LogDebug("No dependencies to search for samples");
            return [];
        }

        List<string> samples = [];
        foreach (var dependencyName in dependencySet)
        {
            logger?.LogDebug("Checking dependency {}", dependencyName);

            // Handle scoped packages (e.g., @azure/keyvault-secrets)
            var dependencyPath = Path.Combine(nodeModulesPath, dependencyName);

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

    private static async Task<IEnumerable<Dependency>> GetNpmDependencies(string directory, IExternalProcessService processService, ILogger? logger)
    {
        return await GetPackageManagerDependencies("npm", ".dependencies", directory, processService, logger).ConfigureAwait(false);
    }

    private static async Task<IEnumerable<Dependency>> GetPnpmDependencies(string directory, IExternalProcessService processService, ILogger? logger)
    {
        return await GetPackageManagerDependencies("pnpm", ".[].dependencies", directory, processService, logger).ConfigureAwait(false);
    }

    private static async Task<IEnumerable<Dependency>> GetPackageManagerDependencies(
        string commandName,
        string jsonPath,
        string directory,
        IExternalProcessService processService,
        ILogger? logger)
    {
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
                dependencies.Add(new Dependency(package.Name, package.Version));
            }
        }

        return dependencies;
    }
}

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
