// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace AzureSdk.SamplesMcp.Services;

// cspell:words USERPROFILE

/// <summary>
/// Represents an environment.
/// </summary>
internal interface IEnvironment
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
internal class DefaultEnvironment : IEnvironment
{
    public static IEnvironment Default { get; } = new DefaultEnvironment();

    public string? GetString(string name) => Environment.GetEnvironmentVariable(name);
}
