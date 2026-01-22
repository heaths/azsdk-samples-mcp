using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp;

interface IDependencyProvider
{
    bool HasProject(string directory, FileSystem? fileSystem = null);
    Task<IEnumerable<Dependency>> GetDependencies(string directory, ILogger? logger = default, FileSystem? fileSystem = null);
    Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, ILogger? logger = default, IEnvironment? environment = null, FileSystem? fileSystem = null);
}

record Dependency(string Name, string? Version);
