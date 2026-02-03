// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections;

namespace AzureSdk.SamplesMcp.Services;

internal class TestEnvironment : IEnvironment, IEnumerable<KeyValuePair<string, string>>
{
    private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    public string? GetString(string name)
    {
        return _variables.TryGetValue(name, out var value) ? value : null;
    }

    public void Add(string key, string value)
    {
        _variables.Add(key, value);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _variables.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[TestClass]
public class TestEnvironmentTests
{
    [TestMethod]
    public void GetString_ReturnsNull_ForMissingVariable()
    {
        var env = new TestEnvironment();
        var result = env.GetString("MISSING");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetString_ReturnsEmptyString_ForEmptyVariable()
    {
        var env = new TestEnvironment
        {
            { "EMPTY", "" }
        };
        var result = env.GetString("EMPTY");

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void HomeDirectory_ReturnsHomeValue_WhenHomeIsSet()
    {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IEnvironment env = new TestEnvironment
        {
            { "HOME", "/home/heaths" }
        };
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
        var result = env.HomeDirectory;

        Assert.AreEqual("/home/heaths", result);
    }
}
