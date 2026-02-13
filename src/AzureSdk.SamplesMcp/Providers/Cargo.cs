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
/// Provides dependency discovery and sample lookup for Rust projects.
/// </summary>
internal class Cargo : IDependencyProvider
{
    /// <summary>
    /// Determines whether the specified directory contains a Rust project.
    /// </summary>
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var manifestPath = Path.Combine(directory, "Cargo.toml");
        return fileSystem.FileExists(manifestPath);
    }

    /// <summary>
    /// Retrieves Azure SDK dependencies from the Cargo manifest.
    /// </summary>
    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = null, FileSystem? fileSystem = null, bool includeDescriptions = false, IEnvironment? environment = null)
    {
        fileSystem ??= FileSystem.Default;
        var manifestPath = Path.Combine(directory, "Cargo.toml");
        IEnumerable<Crate> crates = await GetDependencyInfo(manifestPath, processService, logger: logger, fileSystem: fileSystem).ConfigureAwait(false);

        if (!includeDescriptions)
        {
            return crates.Select(c => new Dependency(c.Name, c.Version));
        }

        // Read descriptions from each crate's manifest in the cargo cache
        var dependencies = new List<Dependency>();

        foreach (var crate in crates)
        {
            string? description = await GetCrateDescriptionFromCache(crate, fileSystem, logger, environment).ConfigureAwait(false);
            dependencies.Add(new Dependency(crate.Name, crate.Version, description));
        }

        return dependencies;
    }

    /// <summary>
    /// Locates README and example files for Azure SDK crates referenced by the project.
    /// </summary>
    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = null, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        environment ??= DefaultEnvironment.Default;
        fileSystem ??= FileSystem.Default;

        var root = GetCargoCacheDirectory(environment);
        if (string.IsNullOrEmpty(root))
        {
            return [];
        }

        logger?.LogDebug("Index root: {}", root);
        if (!fileSystem.DirectoryExists(root))
            return [];
        var indexes = fileSystem.GetDirectories(root).ToArray();
        if (indexes.Length == 0)
            return [];

        var manifestPath = Path.Combine(directory, "Cargo.toml");

        // Get all azure_* crates from cargo metadata
        IEnumerable<Crate> crates = await GetDependencyInfo(manifestPath, processService, logger: logger, fileSystem: fileSystem).ConfigureAwait(false);

        // If dependencies parameter is not empty, filter to only those specified
        var dependencySet = dependencies.Select(d => $"{d.Name}-{d.Version}").ToHashSet();
        if (dependencySet is { Count: > 0 })
        {
            crates = crates.Where(c => dependencySet.Contains(c.DirectoryName));
        }

        List<string> samples = [];
        foreach (Crate crate in crates)
        {
            logger?.LogDebug("Checking crate {}", crate.DirectoryName);
            foreach (var index in indexes)
            {
                var cacheDirectory = Path.Combine(index, crate.DirectoryName);
                logger?.LogDebug("Checking dependency directory {}", cacheDirectory);
                if (!fileSystem.DirectoryExists(cacheDirectory))
                    continue;

                var readmePath = Path.Combine(cacheDirectory, "README.md");
                if (fileSystem.FileExists(readmePath))
                {
                    samples.Add(readmePath);
                }

                // TODO: Also parse Cargo.toml to get `[[example]]` defined in other directories.
                var examplesPath = Path.Combine(cacheDirectory, "examples");
                if (fileSystem.DirectoryExists(examplesPath))
                {
                    foreach (var examplePath in fileSystem.GetFiles(examplesPath))
                    {
                        samples.Add(examplePath);
                    }
                }
            }
        }

        return samples;
    }

    private static async Task<IEnumerable<Crate>> GetDependencyInfo(string manifestPath, IExternalProcessService processService, ILogger? logger, FileSystem? fileSystem)
    {
        fileSystem ??= FileSystem.Default;

        var arguments = $"metadata --format-version 1 --manifest-path \"{manifestPath}\"";

        logger?.LogDebug("Running: cargo {}", arguments);

        ProcessResult result = await processService.ExecuteAsync(
            "cargo",
            arguments,
            workingDirectory: null,
            cancellationToken: default).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new McpException($"cargo failed with exit code {result.ExitCode}");
        }

        var stdout = result.Output;
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(stdout);
        IEnumerable<JsonElement> packages = JsonPath.Query(doc, ".packages[]")
            .Where(e => e
                .GetProperty("name")
                .GetString()?
                .StartsWith("azure_") ?? false)
            .Select(e =>
            {
                logger?.LogDebug("Found dependency: {}", e.GetProperty("name").GetString());
                return e;
            });

        List<Crate> crates = [];
        foreach (JsonElement package in packages)
        {
            var name = package.GetProperty("name").GetString();
            var version = package.GetProperty("version").GetString();

            if (Crate.TryCreate(name, version, out Crate? crate))
            {
                logger?.LogDebug("Adding dependency {}@{}", name, version);
                crates.Add(crate);
            }
        }

        return crates;
    }

    /// <summary>
    /// Gets the cargo cache directory path for registry sources.
    /// </summary>
    /// <param name="environment">Optional environment to read variables from. If null, uses system environment.</param>
    /// <returns>The cargo cache directory path, or null if it cannot be determined.</returns>
    private static string? GetCargoCacheDirectory(IEnvironment? environment = null)
    {
        var cargoHome = environment?.GetString("CARGO_HOME") ?? System.Environment.GetEnvironmentVariable("CARGO_HOME");
        if (!string.IsNullOrEmpty(cargoHome))
        {
            return Path.Combine(cargoHome, "registry", "src");
        }

        var home = environment?.HomeDirectory ?? System.Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            return null;
        }

        return Path.Combine(home, ".cargo", "registry", "src");
    }

    private static async Task<string?> GetCrateDescriptionFromCache(Crate crate, FileSystem fileSystem, ILogger? logger, IEnvironment? environment = null)
    {
        var home = GetCargoCacheDirectory(environment);
        if (string.IsNullOrEmpty(home) || !fileSystem.DirectoryExists(home))
        {
            return null;
        }

        // Look for the crate in any index directory
        var indexes = fileSystem.GetDirectories(home);
        foreach (var index in indexes)
        {
            var crateDir = Path.Combine(index, crate.DirectoryName);
            if (!fileSystem.DirectoryExists(crateDir))
            {
                continue;
            }

            var manifestPath = Path.Combine(crateDir, "Cargo.toml");
            if (!fileSystem.FileExists(manifestPath))
            {
                continue;
            }

            try
            {
                var tomlContent = fileSystem.ReadAllText(manifestPath);
                var tomlModel = Tomlyn.Toml.ToModel(tomlContent);

                if (tomlModel is Tomlyn.Model.TomlTable table &&
                    table.TryGetValue("package", out var packageObj) &&
                    packageObj is Tomlyn.Model.TomlTable package &&
                    package.TryGetValue("description", out var descObj) &&
                    descObj is string description)
                {
                    // Handle multiline descriptions by trimming and joining lines
                    var lines = description.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line));
                    return string.Join(" ", lines);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("Failed to read description from {}: {}", manifestPath, ex.Message);
            }
        }

        return null;
    }
}

/// <summary>
/// Represents a Rust crate with name and version metadata.
/// </summary>
internal record Crate(string Name, string Version)
{
    public string DirectoryName => $"{Name}-{Version}";

    public static bool TryCreate(string? name, string? version, [NotNullWhen(true)] out Crate? crate)
    {
        crate = null;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (string.IsNullOrWhiteSpace(version))
            return false;

        crate = new(name, version);
        return true;
    }
}
