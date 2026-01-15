using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp;

static class FileSystem
{
    public static IEnumerable<DirectoryInfo> EnumerateAncestors(string directory, ILogger? logger = default)
    {
        DirectoryInfo? dir = new(directory);
        while (true)
        {
            if (dir is null)
            {
                yield break;
            }

            logger?.LogDebug("Checking directory {}", dir.FullName);
            Console.Error.WriteLine($"Checking directory {dir.FullName}");
            yield return dir;

            dir = dir.Parent;
        }
    }

    public static (string Directory, IDependencyProvider Provider)? FindProvider(string directory, IEnumerable<IDependencyProvider> providers, ILogger? logger = default)
    {
        return EnumerateAncestors(directory)
            .SelectMany(d => providers
                .Where(p => p.HasProject(d.FullName))
                .Select(p => (Directory: d.FullName, Provider: p)))
            .FirstOrDefault();
    }
}
