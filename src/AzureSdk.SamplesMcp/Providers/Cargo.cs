using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

internal class Cargo : IDependencyProvider
{
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var manifestPath = Path.Combine(directory, "Cargo.toml");
        return fileSystem.FileExists(manifestPath);
    }

    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, ILogger? logger = default, FileSystem? fileSystem = null)
    {
        var manifestPath = Path.Combine(directory, "Cargo.toml");
        IEnumerable<Crate> crates = await GetDependencyInfo(manifestPath, logger: logger, fileSystem: fileSystem).ConfigureAwait(false);
        return crates.Select(c => new Dependency(c.Name, c.Version));
    }

    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        environment ??= DefaultEnvironment.Default;
        fileSystem ??= FileSystem.Default;

        var root = environment.GetString("CARGO_HOME");
        if (string.IsNullOrEmpty(root))
        {
            root = environment.HomeDirectory;
            if (root is not { Length: > 0 })
                return [];
            root = Path.Combine(root, ".cargo", "registry", "src");
        }

        logger?.LogDebug("Index root: {}", root);
        if (!fileSystem.DirectoryExists(root))
            return [];
        var indexes = fileSystem.GetDirectories(root).ToArray();
        if (indexes.Length == 0)
            return [];

        var manifestPath = Path.Combine(directory, "Cargo.toml");
        IEnumerable<Crate> crates = await GetDependencyInfo(manifestPath, logger: logger, fileSystem: fileSystem).ConfigureAwait(false);

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

    private static async Task<IEnumerable<Crate>> GetDependencyInfo(string manifestPath, ILogger? logger, FileSystem? fileSystem)
    {
        fileSystem ??= FileSystem.Default;

        using Command cargo = new("cargo", logger)
        {
            Arguments =
            {
                "metadata",
                "--format-version", "1",
                "--manifest-path", manifestPath,
            },
            WorkingDirectory = Path.GetDirectoryName(manifestPath),
        };

        var exitCode = await cargo.Run().ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new McpException($"{cargo.Name} failed with exit code {exitCode}");
        }

        var stdout = cargo.StandardOutput;
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(stdout);
        IEnumerable<JsonElement> packages = doc
            .RootElement
            .GetProperty("packages")
            .EnumerateArray()
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
}

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
