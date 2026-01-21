using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace AzureSdk.SamplesMcp.Providers;

class Cargo : IDependencyProvider
{
    public bool HasProject(string directory)
    {
        var manifestPath = Path.Combine(directory, "Cargo.toml");
        return File.Exists(manifestPath);
    }

    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, ILogger? logger = default)
    {
        var manifestPath = Path.Combine(directory, "Cargo.toml");
        var crates = await GetDependencyInfo(manifestPath, false, logger).ConfigureAwait(false);
        return crates.Select(c => new Dependency(c.Name, c.Version));
    }

    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, ILogger? logger = default)
    {
        var root = Environment.GetEnvironmentVariable("CARGO_HOME");
        if (root is null)
        {
            root = FileSystem.HomeDirectory;
            if (root is not { Length: > 0 }) return [];
            root = Path.Combine(root, ".cargo", "registry", "src");
        }

        Console.Error.WriteLine($"Index root: {root}");
        if (!Directory.Exists(root)) return [];
        var indexes = Directory.GetDirectories(root);
        if (indexes.Length == 0) return [];

        var manifestPath = Path.Combine(directory, "Cargo.toml");
        var crates = await GetDependencyInfo(manifestPath, true, logger).ConfigureAwait(false);

        var dependencySet = dependencies.Select(d => $"{d.Name}-{d.Version}").ToHashSet();
        if (dependencySet is { Count: > 0 })
        {
            crates = crates.Where(c => dependencySet.Contains(c.DirectoryName));
        }

        List<string> samples = [];
        foreach (var crate in crates)
        {
            Console.Error.WriteLine($"Checking crate {crate.DirectoryName}");
            foreach (var index in indexes)
            {
                var cacheDirectory = Path.Combine(index, crate.DirectoryName);
                logger?.LogDebug("Checking dependency directory {}", cacheDirectory);
                Console.Error.WriteLine($"Checking dependency directory {cacheDirectory}");
                if (!Directory.Exists(cacheDirectory)) continue;

                var readmePath = Path.Combine(cacheDirectory, "README.md");
                if (File.Exists(readmePath))
                {
                    samples.Add(readmePath);
                }

                // TODO: Also parse Cargo.toml to get `[[example]]` defined in other directories.
                var examplesPath = Path.Combine(cacheDirectory, "examples");
                if (Directory.Exists(examplesPath))
                {
                    foreach (var examplePath in Directory.GetFiles(examplesPath))
                    {
                        samples.Add(examplePath);
                    }
                }
            }
        }

        return samples;
    }

    async static Task<IEnumerable<Crate>> GetDependencyInfo(string manifestPath, bool findPath, ILogger? logger)
    {
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
        var packages = doc
            .RootElement
            .GetProperty("packages")
            .EnumerateArray()
            .Where(e => e
                .GetProperty("name")
                .GetString()?
                .StartsWith("azure_") ?? false)
            .Select(e =>
            {
                Console.Error.WriteLine($"Found dependency: {e.GetProperty("name").GetString()}");
                return e;
            });

        List<Crate> crates = [];
        foreach (var package in packages)
        {
            var name = package.GetProperty("name").GetString();
            var version = package.GetProperty("version").GetString();

            if (Crate.TryCreate(name, version, out var crate))
            {
                Console.Error.WriteLine($"Adding dependency {name}@{version}");
                crates.Add(crate);
            }
        }

        return crates;
    }
}

record Crate(string Name, string Version)
{
    public string DirectoryName => $"{Name}-{Version}";

    public static bool TryCreate(string? name, string? version, [NotNullWhen(true)] out Crate? crate)
    {
        crate = null;
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (string.IsNullOrWhiteSpace(version)) return false;

        crate = new(name, version);
        return true;
    }
}
