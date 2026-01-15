using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp;

interface IDependencyProvider
{
    bool HasProject(string directory);
    Task<IEnumerable<Dependency>> GetDependencies(string directory, ILogger? logger);
    Task<IEnumerable<string>> GetSamples(string directory, IEnumerable<Dependency> dependencies, ILogger? logger);
}

record Dependency(string Name, string? Version);
