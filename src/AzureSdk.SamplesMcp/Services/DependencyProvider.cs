// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp.Services;

internal interface IDependencyProvider
{
    bool HasProject(string directory, FileSystem? fileSystem = null);
    Task<IEnumerable<Dependency>> GetDependencies(string directory, IExternalProcessService processService, ILogger? logger = default, FileSystem? fileSystem = null, bool includeDescriptions = false);
    Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, IExternalProcessService processService, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null);
}

internal record Dependency(string Name, string? Version, string? Description = null);
