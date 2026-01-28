using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

internal class Dotnet : IDependencyProvider
{
    private string? _globalPackages;

    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var projectFiles = fileSystem.GetFiles(directory).Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        return projectFiles.Any();
    }

    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, ILogger? logger = default, FileSystem? fileSystem = null)
    {
        IEnumerable<DotnetPackage> packages = await GetDependencyInfo(directory, logger: logger).ConfigureAwait(false);
        return packages.Select(p => new Dependency(p.Id, p.ResolvedVersion));
    }

    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null)
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
            globalPackages = await GetGlobalPackagesDirectory(logger).ConfigureAwait(false);
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

        IEnumerable<DotnetPackage> packages = await GetDependencyInfo(directory, logger: logger).ConfigureAwait(false);

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

    private static async Task<string?> GetGlobalPackagesDirectory(ILogger? logger)
    {
        using Command dotnet = new("dotnet", logger)
        {
            Arguments =
            {
                "nuget",
                "locals",
                "global-packages",
                "--list",
            },
        };

        var exitCode = await dotnet.Run().ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new McpException($"{dotnet.Name} failed with exit code {exitCode}");
        }

        var stdout = dotnet.StandardOutput;
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

    private static async Task<IEnumerable<DotnetPackage>> GetDependencyInfo(string directory, ILogger? logger)
    {
        using Command dotnet = new("dotnet", logger)
        {
            Arguments =
            {
                "list",
                "package",
                "--format",
                "json",
            },
            WorkingDirectory = directory,
        };

        var exitCode = await dotnet.Run().ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new McpException($"{dotnet.Name} failed with exit code {exitCode}");
        }

        var stdout = dotnet.StandardOutput;
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
}

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
