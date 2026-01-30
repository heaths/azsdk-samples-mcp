using System.Reflection;
using AzureSdk.SamplesMcp.Providers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace AzureSdk.SamplesMcp;

[TestClass]
public class NodeTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(NodeTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_ForNpmProject()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();

        var result = provider.HasProject(".", fs);

        Assert.IsTrue(result, "Should detect package.json in root");
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_ForPnpmProject()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();

        var result = provider.HasProject("pnpm-test", fs);

        Assert.IsTrue(result, "Should detect package.json in pnpm-test");
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_ForNonNodeProject()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();

        var result = provider.HasProject(".cargo", fs);

        Assert.IsFalse(result, "Should not detect package.json in .cargo directory");
    }

    [TestMethod]
    public async Task GetSamples_ReturnsReadme_ForNpmProject()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();
        var dependencies = new List<Dependency>
        {
            new("@azure/keyvault-secrets", "4.10.0")
        };

        var samples = await provider.GetSamples(".", dependencies, processService: null!, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(1, sampleList);
        StringAssert.Contains(sampleList[0], "@azure/keyvault-secrets");
        StringAssert.EndsWith(sampleList[0], "README.md");
    }

    [TestMethod]
    public async Task GetSamples_ReturnsReadme_ForPnpmProject()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();
        var dependencies = new List<Dependency>
        {
            new("@azure/identity", "4.7.0")
        };

        var samples = await provider.GetSamples("pnpm-test", dependencies, processService: null!, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(1, sampleList);
        StringAssert.Contains(sampleList[0], "@azure/identity");
        StringAssert.EndsWith(sampleList[0], "README.md");
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenNodeModulesNotFound()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();
        var dependencies = new List<Dependency>
        {
            new("@azure/identity", "4.7.0")
        };

        var samples = await provider.GetSamples(".cargo", dependencies, processService: null!, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(0, sampleList);
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenNoDependencies()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();
        var dependencies = new List<Dependency>();

        var samples = await provider.GetSamples(".", dependencies, processService: null!, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(0, sampleList);
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenDependencyNotInNodeModules()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();
        var dependencies = new List<Dependency>
        {
            new("@azure/nonexistent", "1.0.0")
        };

        var samples = await provider.GetSamples(".", dependencies, processService: null!, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(0, sampleList);
    }
}
