using System.Reflection;
using AzureSdk.SamplesMcp.Providers;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp;

[TestClass]
public class DotnetTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(DotnetTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_ForExistingCsprojFile()
    {
        FileSystem fs = CreateFileSystem();
        var provider = new Dotnet();

        var result = provider.HasProject(".", fs);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_ForDirectoryWithoutCsprojFile()
    {
        FileSystem fs = CreateFileSystem();
        var provider = new Dotnet();

        var result = provider.HasProject(".cargo", fs);

        Assert.IsFalse(result);
    }
}
