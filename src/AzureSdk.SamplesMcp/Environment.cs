namespace AzureSdk.SamplesMcp;

/// <summary>
/// Represents an environment.
/// </summary>
interface IEnvironment
{
    /// <summary>
    /// Gets the user home directory.
    /// </summary>
    string? HomeDirectory => GetString("HOME") ?? GetString("USERPROFILE");

    /// <summary>
    /// Gets an environment variable string.
    /// </summary>
    /// <param name="name">Name of the environment variable.</param>
    /// <returns>The string value or null if not found. The value may be an empty string.</returns>
    string? GetString(string name);
}

/// <summary>
/// The current process environment.
/// </summary>
class DefaultEnvironment : IEnvironment
{
    public static IEnvironment Default { get; } = new DefaultEnvironment();

    public string? GetString(string name) => Environment.GetEnvironmentVariable(name);
}
