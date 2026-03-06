// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Xml.Linq;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp.Providers;

/// <summary>
/// Provides dependency discovery and sample lookup for Java Maven projects.
/// </summary>
internal class Maven : IDependencyProvider
{
    /// <summary>
    /// Determines whether the specified directory contains a Maven project.
    /// </summary>
    public bool HasProject(string directory, FileSystem? fileSystem = null)
    {
        fileSystem ??= FileSystem.Default;

        var pomPath = Path.Combine(directory, "pom.xml");
        return fileSystem.FileExists(pomPath);
    }

    /// <summary>
    /// Retrieves Azure SDK dependencies from the Maven POM file.
    /// </summary>
    public async Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = null, FileSystem? fileSystem = null, bool includeDescriptions = false, IEnvironment? environment = null)
    {
        fileSystem ??= FileSystem.Default;
        environment ??= DefaultEnvironment.Default;

        var pomPath = Path.Combine(directory, "pom.xml");
        IEnumerable<MavenArtifact> artifacts = await ParsePom(pomPath, fileSystem, logger).ConfigureAwait(false);

        if (!includeDescriptions)
        {
            return artifacts.Select(a => new Dependency(a.ArtifactId, a.Version));
        }

        var dependencies = new List<Dependency>();
        foreach (var artifact in artifacts)
        {
            string? description = GetArtifactDescription(artifact, fileSystem, logger, environment);
            dependencies.Add(new Dependency(artifact.ArtifactId, artifact.Version, description));
        }

        return dependencies;
    }

    /// <summary>
    /// Locates README files for Azure SDK packages in the Maven local repository.
    /// </summary>
    public async Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = null, IEnvironment? environment = null, FileSystem? fileSystem = null)
    {
        environment ??= DefaultEnvironment.Default;
        fileSystem ??= FileSystem.Default;

        var localRepo = GetLocalRepositoryPath(environment);
        if (string.IsNullOrEmpty(localRepo))
        {
            logger?.LogWarning("Could not determine Maven local repository path");
            return [];
        }

        logger?.LogDebug("Maven local repository: {}", localRepo);
        if (!fileSystem.DirectoryExists(localRepo))
            return [];

        var pomPath = Path.Combine(directory, "pom.xml");
        IEnumerable<MavenArtifact> artifacts = await ParsePom(pomPath, fileSystem, logger).ConfigureAwait(false);

        // If dependencies parameter is not empty, filter to only those specified
        var dependencySet = dependencies.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (dependencySet is { Count: > 0 })
        {
            artifacts = artifacts.Where(a => dependencySet.Contains(a.ArtifactId));
        }

        List<string> samples = [];
        foreach (var artifact in artifacts)
        {
            var artifactDir = GetArtifactDirectory(localRepo, artifact);
            logger?.LogDebug("Checking artifact directory {}", artifactDir);

            if (!fileSystem.DirectoryExists(artifactDir))
                continue;

            // Check for README.md directly in the artifact directory
            var readmePath = Path.Combine(artifactDir, "README.md");
            if (fileSystem.FileExists(readmePath))
            {
                samples.Add(readmePath);
                continue;
            }

            // Try to extract README.md from the JAR file (JARs are ZIP files)
            var jarPath = Path.Combine(artifactDir, $"{artifact.ArtifactId}-{artifact.Version}.jar");
            if (fileSystem.FileExists(jarPath))
            {
                var extracted = TryExtractReadmeFromJar(jarPath, fileSystem, logger);
                if (extracted is not null)
                {
                    samples.Add(extracted);
                }
            }
        }

        return samples;
    }

    private static async Task<IEnumerable<MavenArtifact>> ParsePom(string pomPath, FileSystem fileSystem, ILogger? logger)
    {
        using var stream = fileSystem.OpenRead(pomPath);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);

        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var dependenciesElement = doc.Root?.Element(ns + "dependencies");
        if (dependenciesElement is null)
        {
            return [];
        }

        List<MavenArtifact> artifacts = [];
        foreach (var dep in dependenciesElement.Elements(ns + "dependency"))
        {
            var groupId = dep.Element(ns + "groupId")?.Value;
            var artifactId = dep.Element(ns + "artifactId")?.Value;
            var version = dep.Element(ns + "version")?.Value;

            if (groupId?.StartsWith("com.azure", StringComparison.OrdinalIgnoreCase) != true)
                continue;

            if (MavenArtifact.TryCreate(groupId, artifactId, version, out var artifact))
            {
                logger?.LogDebug("Found dependency: {}", artifactId);
                artifacts.Add(artifact);
            }
        }

        return artifacts;
    }

    /// <summary>
    /// Gets the Maven local repository path.
    /// </summary>
    private static string? GetLocalRepositoryPath(IEnvironment? environment = null)
    {
        // Check MAVEN_REPO_LOCAL environment variable
        var repoLocal = environment?.GetString("MAVEN_REPO_LOCAL");
        if (!string.IsNullOrEmpty(repoLocal))
        {
            return repoLocal;
        }

        // Check M2_HOME environment variable
        var m2Home = environment?.GetString("M2_HOME");
        if (!string.IsNullOrEmpty(m2Home))
        {
            return Path.Combine(m2Home, "repository");
        }

        // Default: ~/.m2/repository
        var home = environment?.HomeDirectory;
        if (string.IsNullOrEmpty(home))
        {
            return null;
        }

        return Path.Combine(home, ".m2", "repository");
    }

    private static string GetArtifactDirectory(string localRepo, MavenArtifact artifact)
    {
        // Maven stores artifacts as: {repo}/{groupId as path}/{artifactId}/{version}
        var groupPath = artifact.GroupId.Replace('.', Path.DirectorySeparatorChar);
        return Path.Combine(localRepo, groupPath, artifact.ArtifactId, artifact.Version);
    }

    private static string? GetArtifactDescription(MavenArtifact artifact, FileSystem fileSystem, ILogger? logger, IEnvironment? environment)
    {
        var localRepo = GetLocalRepositoryPath(environment);
        if (string.IsNullOrEmpty(localRepo) || !fileSystem.DirectoryExists(localRepo))
        {
            return null;
        }

        var artifactDir = GetArtifactDirectory(localRepo, artifact);
        var pomPath = Path.Combine(artifactDir, $"{artifact.ArtifactId}-{artifact.Version}.pom");

        if (!fileSystem.FileExists(pomPath))
        {
            return null;
        }

        try
        {
            using var stream = fileSystem.OpenRead(pomPath);
            var doc = XDocument.Load(stream);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            return doc.Root?.Element(ns + "description")?.Value;
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Failed to read description from {}: {}", pomPath, ex.Message);
        }

        return null;
    }

    private static string? TryExtractReadmeFromJar(string jarPath, FileSystem fileSystem, ILogger? logger)
    {
        try
        {
            using var stream = fileSystem.OpenRead(jarPath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = archive.GetEntry("README.md");
            if (entry is not null)
            {
                // Return the JAR path with an indicator that README is inside
                return jarPath + "!/README.md";
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Failed to read JAR {}: {}", jarPath, ex.Message);
        }

        return null;
    }
}

/// <summary>
/// Represents a Maven artifact with group ID, artifact ID, and version.
/// </summary>
internal record MavenArtifact(string GroupId, string ArtifactId, string Version)
{
    public static bool TryCreate(string? groupId, string? artifactId, string? version, [NotNullWhen(true)] out MavenArtifact? artifact)
    {
        artifact = null;
        if (string.IsNullOrWhiteSpace(groupId))
            return false;
        if (string.IsNullOrWhiteSpace(artifactId))
            return false;
        if (string.IsNullOrWhiteSpace(version))
            return false;

        artifact = new(groupId, artifactId, version);
        return true;
    }
}
