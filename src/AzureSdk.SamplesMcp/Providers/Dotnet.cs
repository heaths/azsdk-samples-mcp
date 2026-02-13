// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Xml.Linq;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

/// <summary>
/// Provides dependency discovery and sample lookup for .NET projects.
/// </summary>
internal class Dotnet : IDependencyProvider
{
    private string? _globalPackages;

    /// <summary>
    /// Determines whether the specified directory contains a .NET project file.
    /// </summary>
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var projectFiles = fileSystem.GetFiles(directory).Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        return projectFiles.Any();
    }

    /// <summary>
    /// Retrieves Azure SDK dependencies from the project using the .NET CLI.
    /// </summary>
    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = default, FileSystem? fileSystem = null, bool includeDescriptions = false)
    {
        fileSystem ??= FileSystem.Default;
        IEnumerable<DotnetPackage> packages = await GetDependencyInfo(directory, processService, logger: logger).ConfigureAwait(false);

        if (!includeDescriptions)
        {
            return packages.Select(p => new Dependency(p.Id, p.ResolvedVersion));
        }

        // Read descriptions from each package's .nuspec file
        var dependencies = new List<Dependency>();

        foreach (var package in packages)
        {
            string? description = await GetPackageDescription(package, processService, fileSystem, logger).ConfigureAwait(false);
            dependencies.Add(new Dependency(package.Id, package.ResolvedVersion, description));
        }

        return dependencies;
    }

    /// <summary>
    /// Locates README files for Azure SDK packages referenced by the project.
    /// </summary>
    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        environment ??= DefaultEnvironment.Default;
        fileSystem ??= FileSystem.Default;

        // Get the NuGet global packages directory from environment, cache, or dotnet command
        var globalPackages = environment.GetString("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(globalPackages))
        {
            globalPackages = _globalPackages;
        }

        if (string.IsNullOrEmpty(globalPackages))
        {
            globalPackages = await GetGlobalPackagesDirectory(processService, logger).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(globalPackages))
            {
                _globalPackages = globalPackages;
            }
        }

        if (string.IsNullOrEmpty(globalPackages))
        {
            logger?.LogWarning("Could not determine NuGet global packages directory");
            return [];
        }

        logger?.LogDebug("Global packages directory: {}", globalPackages);
        if (!fileSystem.DirectoryExists(globalPackages))
            return [];

        // Get all Azure.* packages from dotnet list
        IEnumerable<DotnetPackage> packages = await GetDependencyInfo(directory, processService, logger: logger).ConfigureAwait(false);

        // If dependencies parameter is not empty, filter to only those specified
        var dependencySet = dependencies.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (dependencySet is { Count: > 0 })
        {
            packages = packages.Where(p => dependencySet.Contains(p.Id));
        }

        List<string> samples = [];
        foreach (DotnetPackage package in packages)
        {
            logger?.LogDebug("Checking package {}", package.DirectoryName);

            // NuGet packages are stored as: {globalPackages}/{id}/{version}
            var packageDirectory = Path.Combine(globalPackages, package.DirectoryName);
            logger?.LogDebug("Checking package directory {}", packageDirectory);

            if (!fileSystem.DirectoryExists(packageDirectory))
                continue;

            var readmePath = Path.Combine(packageDirectory, "README.md");
            if (fileSystem.FileExists(readmePath))
            {
                samples.Add(readmePath);
            }
        }

        return samples;
    }

    private static async Task<string?> GetGlobalPackagesDirectory(IExternalProcessService processService, ILogger? logger)
    {
        var arguments = "nuget locals global-packages --list";
        logger?.LogDebug("Running: dotnet {}", arguments);

        ProcessResult result = await processService.ExecuteAsync(
            "dotnet",
            arguments,
            workingDirectory: null,
            cancellationToken: default).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new McpException($"dotnet failed with exit code {result.ExitCode}");
        }

        var stdout = result.Output;
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return null;
        }

        // Parse output: "global-packages: /path/to/packages/"
        var parts = stdout.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return parts[1].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return null;
    }

    private static async Task<IEnumerable<DotnetPackage>> GetDependencyInfo(string directory, IExternalProcessService processService, ILogger? logger)
    {
        var arguments = "list package --format json";
        logger?.LogDebug("Running: dotnet {}", arguments);

        ProcessResult result = await processService.ExecuteAsync(
            "dotnet",
            arguments,
            workingDirectory: directory,
            environmentVariables: new Dictionary<string, string> { { "DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE", "true" } },
            cancellationToken: default).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new McpException($"dotnet failed with exit code {result.ExitCode}");
        }

        var stdout = result.Output;
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(stdout);

        // Query: .projects[].frameworks[].topLevelPackages[]
        IEnumerable<JsonElement> packageElements = JsonPath.Query(doc, ".projects[].frameworks[].topLevelPackages[]")
            .Where(e => e
                .GetProperty("id")
                .GetString()?
                .StartsWith("Azure.", StringComparison.OrdinalIgnoreCase) ?? false)
            .Select(e =>
            {
                logger?.LogDebug("Found dependency: {}", e.GetProperty("id").GetString());
                return e;
            });

        List<DotnetPackage> packages = [];
        foreach (JsonElement packageElement in packageElements)
        {
            var id = packageElement.GetProperty("id").GetString();
            var resolvedVersion = packageElement.GetProperty("resolvedVersion").GetString();

            if (DotnetPackage.TryCreate(id, resolvedVersion, out DotnetPackage? package))
            {
                logger?.LogDebug("Adding dependency {}@{}", id, resolvedVersion);
                packages.Add(package);
            }
        }

        return packages;
    }

    private async Task<string?> GetPackageDescription(DotnetPackage package, IExternalProcessService processService, FileSystem fileSystem, ILogger? logger)
    {
        // Get the NuGet global packages directory
        var globalPackages = _globalPackages;
        if (string.IsNullOrEmpty(globalPackages))
        {
            globalPackages = await GetGlobalPackagesDirectory(processService, logger).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(globalPackages))
            {
                _globalPackages = globalPackages;
            }
        }

        if (string.IsNullOrEmpty(globalPackages))
        {
            return null;
        }

        // NuGet packages are stored as: {globalPackages}/{id}/{version}/{id}.nuspec
        // NuGet stores package directories in lowercase
        var packageDirectory = Path.Combine(globalPackages, package.Id.ToLowerInvariant(), package.ResolvedVersion);
        var nuspecPath = Path.Combine(packageDirectory, $"{package.Id}.nuspec");

        if (!fileSystem.FileExists(nuspecPath))
        {
            logger?.LogWarning(".nuspec file not found at {}", nuspecPath);
            return null;
        }

        try
        {
            var nuspecContent = await Task.Run(() => fileSystem.ReadAllText(nuspecPath)).ConfigureAwait(false);
            var doc = await Task.Run(() => XDocument.Parse(nuspecContent)).ConfigureAwait(false);

            // The nuspec file uses XML namespace
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            var descriptionElement = doc.Root?
                .Element(ns + "metadata")?
                .Element(ns + "description");

            return descriptionElement?.Value;
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Failed to read description from {}: {}", nuspecPath, ex.Message);
        }

        return null;
    }
}

/// <summary>
/// Represents a resolved NuGet package with its identifier and version.
/// </summary>
internal record DotnetPackage(string Id, string ResolvedVersion)
{
    // NuGet packages are stored in lowercase: {id}/{version}
    public string DirectoryName => $"{Id.ToLowerInvariant()}/{ResolvedVersion}";

    public static bool TryCreate(string? id, string? resolvedVersion, [NotNullWhen(true)] out DotnetPackage? package)
    {
        package = null;
        if (string.IsNullOrWhiteSpace(id))
            return false;
        if (string.IsNullOrWhiteSpace(resolvedVersion))
            return false;

        package = new(id, resolvedVersion);
        return true;
    }
}
