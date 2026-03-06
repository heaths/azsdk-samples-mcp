// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

/// <summary>
/// Provides dependency discovery and sample lookup for Python pip projects.
/// </summary>
internal partial class Pip : IDependencyProvider
{
    /// <summary>
    /// Determines whether the specified directory contains a Python project.
    /// </summary>
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        return fileSystem.FileExists(Path.Combine(directory, "requirements.txt"))
            || fileSystem.FileExists(Path.Combine(directory, "pyproject.toml"))
            || fileSystem.FileExists(Path.Combine(directory, "setup.py"))
            || fileSystem.FileExists(Path.Combine(directory, "setup.cfg"));
    }

    /// <summary>
    /// Retrieves Azure SDK dependencies from Python project files.
    /// </summary>
    public Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = null, FileSystem? fileSystem = null, bool includeDescriptions = false, IEnvironment? environment = null)
    {
        fileSystem ??= FileSystem.Default;
        environment ??= DefaultEnvironment.Default;

        var packages = ParseDependencies(directory, fileSystem, logger);

        if (!includeDescriptions)
        {
            return Task.FromResult(packages.Select(p => new Dependency(p.Name, p.Version)));
        }

        var dependencies = new List<Dependency>();
        foreach (var package in packages)
        {
            string? description = GetPackageDescription(package, fileSystem, logger, environment);
            dependencies.Add(new Dependency(package.Name, package.Version, description));
        }

        return Task.FromResult<IEnumerable<Dependency>>(dependencies);
    }

    /// <summary>
    /// Locates README files for Azure SDK packages in Python site-packages.
    /// </summary>
    public Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = null, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        environment ??= DefaultEnvironment.Default;
        fileSystem ??= FileSystem.Default;

        var sitePackages = FindSitePackagesDirectory(directory, fileSystem, logger, environment);
        if (string.IsNullOrEmpty(sitePackages))
        {
            logger?.LogWarning("Could not determine Python site-packages directory");
            return Task.FromResult<IEnumerable<string>>([]);
        }

        logger?.LogDebug("Python site-packages: {}", sitePackages);
        if (!fileSystem.DirectoryExists(sitePackages))
            return Task.FromResult<IEnumerable<string>>([]);

        var packages = ParseDependencies(directory, fileSystem, logger);

        // If dependencies parameter is not empty, filter to only those specified
        var dependencySet = dependencies.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (dependencySet is { Count: > 0 })
        {
            packages = packages.Where(p => dependencySet.Contains(p.Name)).ToList();
        }

        List<string> samples = [];
        foreach (var package in packages)
        {
            // Python packages use underscores in directory names: azure-keyvault-secrets -> azure_keyvault_secrets
            var packageDirName = package.Name.Replace('-', '_');

            // Check for dist-info directory containing METADATA with description
            foreach (var dir in fileSystem.GetDirectories(sitePackages))
            {
                var dirName = Path.GetFileName(dir);

                // Check package source directory for README
                if (string.Equals(dirName, packageDirName, StringComparison.OrdinalIgnoreCase))
                {
                    var readmePath = Path.Combine(dir, "README.md");
                    if (fileSystem.FileExists(readmePath))
                    {
                        samples.Add(readmePath);
                    }

                    break;
                }

                // Check dist-info directory for README
                if (dirName.StartsWith(packageDirName + "-", StringComparison.OrdinalIgnoreCase) && dirName.EndsWith(".dist-info", StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogDebug("Checking dist-info directory {}", dir);

                    var readmePath = Path.Combine(dir, "README.md");
                    if (fileSystem.FileExists(readmePath))
                    {
                        samples.Add(readmePath);
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<string>>(samples);
    }

    private static IList<PipPackage> ParseDependencies(string directory, FileSystem fileSystem, ILogger? logger)
    {
        // Try requirements.txt first
        var requirementsPath = Path.Combine(directory, "requirements.txt");
        if (fileSystem.FileExists(requirementsPath))
        {
            return ParseRequirementsTxt(requirementsPath, fileSystem, logger);
        }

        // Try pyproject.toml
        var pyprojectPath = Path.Combine(directory, "pyproject.toml");
        if (fileSystem.FileExists(pyprojectPath))
        {
            return ParsePyprojectToml(pyprojectPath, fileSystem, logger);
        }

        return [];
    }

    private static IList<PipPackage> ParseRequirementsTxt(string path, FileSystem fileSystem, ILogger? logger)
    {
        var content = fileSystem.ReadAllText(path);
        List<PipPackage> packages = [];

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();

            // Skip comments, empty lines, and options
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith('-'))
                continue;

            // Parse package==version or package>=version or just package
            var match = RequirementsLinePattern().Match(line);
            if (!match.Success)
                continue;

            var name = match.Groups["name"].Value;
            var version = match.Groups["version"].Success ? match.Groups["version"].Value : null;

            if (!name.StartsWith("azure", StringComparison.OrdinalIgnoreCase))
                continue;

            if (PipPackage.TryCreate(name, version, out var package))
            {
                logger?.LogDebug("Found dependency: {}", name);
                packages.Add(package);
            }
        }

        return packages;
    }

    private static IList<PipPackage> ParsePyprojectToml(string path, FileSystem fileSystem, ILogger? logger)
    {
        List<PipPackage> packages = [];

        try
        {
            var content = fileSystem.ReadAllText(path);
            var toml = Tomlyn.Toml.ToModel(content);

            if (toml.TryGetValue("project", out var projectObj) &&
                projectObj is Tomlyn.Model.TomlTable project &&
                project.TryGetValue("dependencies", out var depsObj) &&
                depsObj is Tomlyn.Model.TomlArray deps)
            {
                foreach (var dep in deps)
                {
                    if (dep is not string depStr)
                        continue;

                    var match = RequirementsLinePattern().Match(depStr);
                    if (!match.Success)
                        continue;

                    var name = match.Groups["name"].Value;
                    var version = match.Groups["version"].Success ? match.Groups["version"].Value : null;

                    if (!name.StartsWith("azure", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (PipPackage.TryCreate(name, version, out var package))
                    {
                        logger?.LogDebug("Found dependency: {}", name);
                        packages.Add(package);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Failed to parse pyproject.toml: {}", ex.Message);
        }

        return packages;
    }

    /// <summary>
    /// Finds the Python site-packages directory.
    /// </summary>
    private static string? FindSitePackagesDirectory(string directory, FileSystem fileSystem, ILogger? logger, IEnvironment? environment = null)
    {
        // Check for virtual environment
        var virtualEnv = environment?.GetString("VIRTUAL_ENV");
        if (!string.IsNullOrEmpty(virtualEnv))
        {
            var libPath = Path.Combine(virtualEnv, "lib");
            if (fileSystem.DirectoryExists(libPath))
            {
                // Look for python3.X/site-packages
                foreach (var pyDir in fileSystem.GetDirectories(libPath))
                {
                    var sitePackages = Path.Combine(pyDir, "site-packages");
                    if (fileSystem.DirectoryExists(sitePackages))
                    {
                        return sitePackages;
                    }
                }
            }
        }

        // Check for .venv in the project directory
        var venvPath = Path.Combine(directory, ".venv", "lib");
        if (fileSystem.DirectoryExists(venvPath))
        {
            foreach (var pyDir in fileSystem.GetDirectories(venvPath))
            {
                var sitePackages = Path.Combine(pyDir, "site-packages");
                if (fileSystem.DirectoryExists(sitePackages))
                {
                    return sitePackages;
                }
            }
        }

        // Check user site-packages
        var home = environment?.HomeDirectory;
        if (!string.IsNullOrEmpty(home))
        {
            var localLib = Path.Combine(home, ".local", "lib");
            if (fileSystem.DirectoryExists(localLib))
            {
                foreach (var pyDir in fileSystem.GetDirectories(localLib))
                {
                    var sitePackages = Path.Combine(pyDir, "site-packages");
                    if (fileSystem.DirectoryExists(sitePackages))
                    {
                        return sitePackages;
                    }
                }
            }
        }

        return null;
    }

    private static string? GetPackageDescription(PipPackage package, FileSystem fileSystem, ILogger? logger, IEnvironment? environment)
    {
        // We'd need to find and parse the METADATA file in the dist-info directory
        // This is complex without knowing the site-packages directory, so return null for now
        return null;
    }

    [GeneratedRegex(@"^(?<name>[a-zA-Z0-9][-a-zA-Z0-9_.]*)\s*(?:[><=!~]+\s*(?<version>[^\s,;]+))?", RegexOptions.Compiled)]
    private static partial Regex RequirementsLinePattern();
}

/// <summary>
/// Represents a Python package with its name and version.
/// </summary>
internal record PipPackage(string Name, string? Version)
{
    public static bool TryCreate(string? name, string? version, [NotNullWhen(true)] out PipPackage? package)
    {
        package = null;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        package = new(name, version);
        return true;
    }
}
