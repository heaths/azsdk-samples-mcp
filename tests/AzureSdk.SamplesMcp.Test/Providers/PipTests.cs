// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Providers;

[TestClass]
public class PipTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(PipTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_WhenRequirementsTxtExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "pip-project";

        // Act
        var result = pip.HasProject(directory, fileSystem);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_WhenNoPythonProjectFiles()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "nonexistent";

        // Act
        var result = pip.HasProject(directory, fileSystem);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetDependencies_ReturnsAzureDependencies()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "pip-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await pip.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(2, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("azure-")));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "azure-identity"));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "azure-keyvault-secrets"));
    }

    [TestMethod]
    public async Task GetDependencies_FiltersNonAzurePackages()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "pip-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await pip.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("azure-")));
        Assert.IsFalse(dependencyList.Any(d => d.Name == "requests"));
    }

    [TestMethod]
    public async Task GetDependencies_IncludesVersions()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "pip-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await pip.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        var identity = dependencyList.First(d => d.Name == "azure-identity");
        Assert.AreEqual("1.21.0", identity.Version);

        var secrets = dependencyList.First(d => d.Name == "azure-keyvault-secrets");
        Assert.AreEqual("4.9.0", secrets.Version);
    }

    [TestMethod]
    public async Task GetDependencies_ExcludesDescriptions_WhenNotRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "pip-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await pip.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: false);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.IsTrue(dependencyList.All(d => d.Description is null));
    }

    [TestMethod]
    public async Task GetSamples_ReturnsReadme_WhenDistInfoExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "pip-project";
        var processService = new MockProcessService("");
        var dependencies = new List<Dependency>
        {
            new("azure-identity", "1.21.0")
        };

        // Act
        var samples = await pip.GetSamples(directory, dependencies, processService, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(1, sampleList);
        StringAssert.Contains(sampleList[0], "azure_identity");
        StringAssert.EndsWith(sampleList[0], "README.md");
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenSitePackagesNotFound()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var pip = new Pip();
        var directory = "cargo-project"; // A directory without Python stuff
        var processService = new MockProcessService("");
        var dependencies = new List<Dependency>
        {
            new("azure-identity", "1.21.0")
        };

        // Act
        var samples = await pip.GetSamples(directory, dependencies, processService, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(0, sampleList);
    }
}
