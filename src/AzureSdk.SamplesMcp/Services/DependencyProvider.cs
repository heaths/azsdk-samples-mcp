// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp.Services;

/// <summary>
/// Provides language-specific implementations for discovering Azure SDK dependencies and retrieving their sample code.
/// </summary>
internal interface IDependencyProvider
{
    /// <summary>
    /// Determines whether a project of this provider's language type exists in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to check for a project.</param>
    /// <param name="fileSystem">Optional file system abstraction. If <c>null</c>, uses the default file system.</param>
    /// <returns><c>true</c> if a project exists; otherwise, <c>false</c>.</returns>
    bool HasProject(string directory, FileSystem? fileSystem = null);

    /// <summary>
    /// Discovers Azure SDK dependencies in the specified project directory.
    /// </summary>
    /// <param name="directory">The project directory to scan for dependencies.</param>
    /// <param name="processService">Service for executing external commands.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <param name="fileSystem">Optional file system abstraction. If <c>null</c>, uses the default file system.</param>
    /// <param name="includeDescriptions">When <c>true</c>, includes package descriptions in the returned dependencies.</param>
    /// <param name="environment">Optional environment variable accessor. If <c>null</c>, uses the default environment.</param>
    /// <returns>A collection of discovered Azure SDK dependencies.</returns>
    Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = null, FileSystem? fileSystem = null, bool includeDescriptions = false, IEnvironment? environment = null);

    /// <summary>
    /// Locates sample code files (README, examples, etc.) for the specified Azure SDK dependencies.
    /// </summary>
    /// <param name="directory">The project directory.</param>
    /// <param name="dependencies">The Azure SDK dependencies to find samples for.</param>
    /// <param name="processService">Service for executing external commands.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    /// <param name="environment">Optional environment variable accessor. If <c>null</c>, uses the default environment.</param>
    /// <param name="fileSystem">Optional file system abstraction. If <c>null</c>, uses the default file system.</param>
    /// <returns>A collection of file paths containing sample code for the dependencies.</returns>
    Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = null, IEnvironment? environment = null, FileSystem? fileSystem = null);
}

/// <summary>
/// Represents an Azure SDK dependency with its name, version, and optional description.
/// </summary>
/// <param name="Name">The package or crate name.</param>
/// <param name="Version">The resolved version of the dependency, or <c>null</c> if not available.</param>
/// <param name="Description">An optional description of the package functionality.</param>
internal record Dependency(string Name, string? Version, string? Description = null);
