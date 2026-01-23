using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace AzureSdk.SamplesMcp;

/// <summary>
/// Basic file system operations used throughout the MCP server.
/// </summary>
/// <param name="fileProvider">Optional <see cref="IFileProvider"/>. The default uses a <see cref="PhysicalFileProvider"/> rooted as "/".</param>
#pragma warning disable CA1822 // Mark members as static
internal class FileSystem(IFileProvider? fileProvider = null)
{
    private readonly IFileProvider _fileProvider = fileProvider ?? new PhysicalFileProvider("/");

    /// <summary>
    /// Gets a <see cref="FileSystem"/> using the physical file system rooted as "/".
    /// </summary>
    public static FileSystem Default { get; } = new();

    /// <summary>
    /// Checks whether a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> if the directory exists; otherwise, <see langword="false"/>.</returns>
    public bool DirectoryExists(string path) => _fileProvider.GetDirectoryContents(path).Exists;

    /// <summary>
    /// Checks whether a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    public bool FileExists(string path) => _fileProvider.GetFileInfo(path) is { } info && info.Exists && !info.IsDirectory;

    /// <summary>
    /// Gets the parent directory of the specified path.
    /// </summary>
    /// <param name="path">The path for which to get the parent.</param>
    /// <returns>The parent directory path, or <see langword="null"/> if the path has no parent.</returns>
    public string? GetParent(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
        {
            return null;
        }

        return parent;
    }

    /// <summary>
    /// Gets the names of subdirectories in the specified directory.
    /// </summary>
    /// <param name="path">The path of the directory to search.</param>
    /// <returns>An enumerable collection of directory names.</returns>
    public IEnumerable<string> GetDirectories(string path) => _fileProvider.GetDirectoryContents(path).Where(d => d.IsDirectory).Select(d => Path.Combine(path, d.Name));

    /// <summary>
    /// Gets the names of files in the specified directory.
    /// </summary>
    /// <param name="path">The path of the directory to search.</param>
    /// <returns>An enumerable collection of file names.</returns>
    public IEnumerable<string> GetFiles(string path) => _fileProvider.GetDirectoryContents(path).Where(d => !d.IsDirectory).Select(d => Path.Combine(path, d.Name));

    /// <summary>
    /// Reads all text from the file at the specified path.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The contents of the file as a string.</returns>
    public string ReadAllText(string path) => new System.IO.StreamReader(_fileProvider.GetFileInfo(path).CreateReadStream()).ReadToEnd();

    /// <summary>
    /// Enumerates all ancestor directories starting from the specified directory up to the root.
    /// </summary>
    /// <param name="directory">The directory from which to start enumerating ancestors.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>An enumerable collection of <see cref="IFileInfo"/> objects representing each ancestor directory.</returns>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    public IEnumerable<IFileInfo> EnumerateAncestors(string directory, ILogger? logger = null)
    {
        if (!DirectoryExists(directory))
        {
            throw new System.IO.DirectoryNotFoundException($"""Directory "{directory}" not found""");
        }

        directory = Path.TrimEndingDirectorySeparator(directory);
        while (true)
        {
            logger?.LogDebug("Checking directory {}", directory);

            IFileInfo dir = _fileProvider.GetFileInfo(directory);
            yield return dir;

            var parent = Path.GetDirectoryName(directory);
            if (string.IsNullOrEmpty(parent))
            {
                yield break;
            }

            directory = parent;
            if (!DirectoryExists(directory))
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Finds the first dependency provider that has a project file in the specified directory or any of its ancestors.
    /// </summary>
    /// <param name="directory">The directory from which to start searching.</param>
    /// <param name="providers">The collection of dependency providers to check.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>A tuple containing the directory path and matching provider, or <see langword="null"/> if no provider is found.</returns>
    public (string Directory, IDependencyProvider Provider)? FindProvider(string directory, IEnumerable<IDependencyProvider> providers)
    {
        return EnumerateAncestors(directory)
            .SelectMany(d => providers
                .Where(p => d.PhysicalPath is { Length: > 0 } && p.HasProject(d.PhysicalPath))
                .Select(p => (Directory: d.PhysicalPath!, Provider: p)))
            .FirstOrDefault();
    }
}
#pragma warning restore CA1822 // Mark members as static
