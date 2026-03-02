// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

/// <summary>
/// Provides dependency discovery and sample lookup for Go module projects.
/// </summary>
internal partial class GoMod : IDependencyProvider
{
    /// <summary>
    /// Determines whether the specified directory contains a Go module.
    /// </summary>
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var goModPath = Path.Combine(directory, "go.mod");
        return fileSystem.FileExists(goModPath);
    }

    /// <summary>
    /// Retrieves Azure SDK dependencies from the go.mod file.
    /// </summary>
    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = null, FileSystem? fileSystem = null, bool includeDescriptions = false, IEnvironment? environment = null)
    {
        fileSystem ??= FileSystem.Default;
        environment ??= DefaultEnvironment.Default;

        var goModPath = Path.Combine(directory, "go.mod");
        var modules = ParseGoMod(goModPath, fileSystem, logger);

        if (!includeDescriptions)
        {
            return modules.Select(m => new Dependency(m.Path, m.Version));
        }

        var dependencies = new List<Dependency>();
        foreach (var module in modules)
        {
            string? description = GetModuleDescription(module, fileSystem, logger, environment);
            dependencies.Add(new Dependency(module.Path, module.Version, description));
        }

        return await Task.FromResult<IEnumerable<Dependency>>(dependencies).ConfigureAwait(false);
    }

    /// <summary>
    /// Locates README and example files for Azure SDK modules in the Go module cache.
    /// </summary>
    public Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = null, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        environment ??= DefaultEnvironment.Default;
        fileSystem ??= FileSystem.Default;

        var modCache = GetModCacheDirectory(environment);
        if (string.IsNullOrEmpty(modCache))
        {
            logger?.LogWarning("Could not determine Go module cache directory");
            return Task.FromResult<IEnumerable<string>>([]);
        }

        logger?.LogDebug("Go module cache: {}", modCache);
        if (!fileSystem.DirectoryExists(modCache))
            return Task.FromResult<IEnumerable<string>>([]);

        var goModPath = Path.Combine(directory, "go.mod");
        var modules = ParseGoMod(goModPath, fileSystem, logger);

        // If dependencies parameter is not empty, filter to only those specified
        var dependencySet = dependencies.Select(d => d.Name).ToHashSet();
        if (dependencySet is { Count: > 0 })
        {
            modules = modules.Where(m => dependencySet.Contains(m.Path)).ToList();
        }

        List<string> samples = [];
        foreach (var module in modules)
        {
            var moduleDir = GetModuleCacheDirectory(modCache, module);
            logger?.LogDebug("Checking module directory {}", moduleDir);

            if (!fileSystem.DirectoryExists(moduleDir))
                continue;

            // Check for README.md
            var readmePath = Path.Combine(moduleDir, "README.md");
            if (fileSystem.FileExists(readmePath))
            {
                samples.Add(readmePath);
            }

            // Look for Go example files (*_example_test.go, example_test.go, example_*.go)
            foreach (var file in fileSystem.GetFiles(moduleDir))
            {
                var fileName = Path.GetFileName(file);
                if (ExampleFilePattern().IsMatch(fileName))
                {
                    samples.Add(file);
                }
            }
        }

        return Task.FromResult<IEnumerable<string>>(samples);
    }

    private static IList<GoModule> ParseGoMod(string goModPath, FileSystem fileSystem, ILogger? logger)
    {
        var content = fileSystem.ReadAllText(goModPath);
        List<GoModule> modules = [];

        var inRequireBlock = false;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("require (", StringComparison.Ordinal))
            {
                inRequireBlock = true;
                continue;
            }

            if (inRequireBlock && line == ")")
            {
                inRequireBlock = false;
                continue;
            }

            // Handle single-line require: require github.com/Azure/... v1.0.0
            if (line.StartsWith("require ", StringComparison.Ordinal))
            {
                var parts = line["require ".Length..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && IsAzureModule(parts[0]))
                {
                    if (GoModule.TryCreate(parts[0], parts[1], out var module))
                    {
                        logger?.LogDebug("Found dependency: {}", parts[0]);
                        modules.Add(module);
                    }
                }

                continue;
            }

            // Handle lines inside require block
            if (inRequireBlock)
            {
                // Skip comments and indirect dependencies
                if (line.StartsWith("//", StringComparison.Ordinal) || line.Contains("// indirect", StringComparison.Ordinal))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && IsAzureModule(parts[0]))
                {
                    if (GoModule.TryCreate(parts[0], parts[1], out var module))
                    {
                        logger?.LogDebug("Found dependency: {}", parts[0]);
                        modules.Add(module);
                    }
                }
            }
        }

        return modules;
    }

    private static bool IsAzureModule(string path)
    {
        return path.StartsWith("github.com/Azure/azure-sdk-for-go/sdk/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the Go module cache directory.
    /// </summary>
    private static string? GetModCacheDirectory(IEnvironment? environment = null)
    {
        // Check GOMODCACHE environment variable
        var goModCache = environment?.GetString("GOMODCACHE");
        if (!string.IsNullOrEmpty(goModCache))
        {
            return goModCache;
        }

        // Check GOPATH environment variable
        var goPath = environment?.GetString("GOPATH");
        if (!string.IsNullOrEmpty(goPath))
        {
            return Path.Combine(goPath, "pkg", "mod");
        }

        // Default: ~/go/pkg/mod
        var home = environment?.HomeDirectory;
        if (string.IsNullOrEmpty(home))
        {
            return null;
        }

        return Path.Combine(home, "go", "pkg", "mod");
    }

    private static string GetModuleCacheDirectory(string modCache, GoModule module)
    {
        // Go module cache uses case-encoded paths: uppercase letters become !lowercase
        var encodedPath = EncodeModulePath(module.Path);
        return Path.Combine(modCache, encodedPath + "@" + module.Version);
    }

    /// <summary>
    /// Encodes a Go module path for the module cache filesystem.
    /// Uppercase letters are replaced with '!' followed by the lowercase letter.
    /// </summary>
    private static string EncodeModulePath(string path)
    {
        var sb = new System.Text.StringBuilder(path.Length);
        foreach (var c in path)
        {
            if (char.IsUpper(c))
            {
                sb.Append('!');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string? GetModuleDescription(GoModule module, FileSystem fileSystem, ILogger? logger, IEnvironment? environment)
    {
        var modCache = GetModCacheDirectory(environment);
        if (string.IsNullOrEmpty(modCache) || !fileSystem.DirectoryExists(modCache))
        {
            return null;
        }

        var moduleDir = GetModuleCacheDirectory(modCache, module);
        var readmePath = Path.Combine(moduleDir, "README.md");

        if (!fileSystem.FileExists(readmePath))
        {
            return null;
        }

        try
        {
            var content = fileSystem.ReadAllText(readmePath);

            // Extract the first paragraph after the title as description
            var lines = content.Split('\n');
            var foundTitle = false;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith('#'))
                {
                    foundTitle = true;
                    continue;
                }

                if (foundTitle && !string.IsNullOrWhiteSpace(line))
                {
                    return line;
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Failed to read description from {}: {}", readmePath, ex.Message);
        }

        return null;
    }

    [GeneratedRegex(@"^(example[_.].*\.go|.*_example_test\.go|example_test\.go)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExampleFilePattern();
}

/// <summary>
/// Represents a Go module dependency with its module path and version.
/// </summary>
internal record GoModule(string Path, string Version)
{
    public static bool TryCreate(string? path, string? version, [NotNullWhen(true)] out GoModule? module)
    {
        module = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;
        if (string.IsNullOrWhiteSpace(version))
            return false;

        module = new(path, version);
        return true;
    }
}
