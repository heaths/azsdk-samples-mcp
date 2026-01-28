using System.Reflection;
using AzureSdk.SamplesMcp.Providers;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp;

[TestClass]
public class CargoTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(CargoTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_WhenCargoTomlExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var cargo = new Cargo();
        var directory = "cargo-project";

        // Act
        var result = cargo.HasProject(directory, fileSystem);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_WhenCargoTomlDoesNotExist()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var cargo = new Cargo();
        var directory = "nonexistent";

        // Act
        var result = cargo.HasProject(directory, fileSystem);

        // Assert
        Assert.IsFalse(result);
    }
}
