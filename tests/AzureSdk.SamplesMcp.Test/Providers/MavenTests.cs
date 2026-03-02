// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Providers;

[TestClass]
public class MavenTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(MavenTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_WhenPomXmlExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";

        // Act
        var result = maven.HasProject(directory, fileSystem);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_WhenPomXmlDoesNotExist()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "nonexistent";

        // Act
        var result = maven.HasProject(directory, fileSystem);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetDependencies_ReturnsAzureDependencies()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await maven.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("azure-security-keyvault-secrets", dependencyList[0].Name);
        Assert.AreEqual("4.9.0", dependencyList[0].Version);
    }

    [TestMethod]
    public async Task GetDependencies_FiltersNonAzurePackages()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await maven.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("azure-")));
    }

    [TestMethod]
    public async Task GetDependencies_IncludesDescriptions_WhenRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";
        var processService = new MockProcessService("");

        var environment = new TestEnvironment
        {
            { "MAVEN_REPO_LOCAL", ".m2/repository" }
        };

        // Act
        var dependencies = await maven.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: true, environment: environment);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("azure-security-keyvault-secrets", dependencyList[0].Name);
        Assert.AreEqual("4.9.0", dependencyList[0].Version);
        Assert.AreEqual("Azure Key Vault Secrets client library for Java", dependencyList[0].Description);
    }

    [TestMethod]
    public async Task GetDependencies_ExcludesDescriptions_WhenNotRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await maven.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: false);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("azure-security-keyvault-secrets", dependencyList[0].Name);
        Assert.AreEqual("4.9.0", dependencyList[0].Version);
        Assert.IsNull(dependencyList[0].Description);
    }

    [TestMethod]
    public async Task GetSamples_ReturnsReadme_WhenArtifactExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";
        var processService = new MockProcessService("");
        var dependencies = new List<Dependency>
        {
            new("azure-security-keyvault-secrets", "4.9.0")
        };

        var environment = new TestEnvironment
        {
            { "MAVEN_REPO_LOCAL", ".m2/repository" }
        };

        // Act
        var samples = await maven.GetSamples(directory, dependencies, processService, environment: environment, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(1, sampleList);
        StringAssert.Contains(sampleList[0], "azure-security-keyvault-secrets");
        StringAssert.EndsWith(sampleList[0], "README.md");
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenRepoNotFound()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var maven = new Maven();
        var directory = "maven-project";
        var processService = new MockProcessService("");
        var dependencies = new List<Dependency>
        {
            new("azure-security-keyvault-secrets", "4.9.0")
        };

        var environment = new TestEnvironment
        {
            { "MAVEN_REPO_LOCAL", "nonexistent-repo" }
        };

        // Act
        var samples = await maven.GetSamples(directory, dependencies, processService, environment: environment, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(0, sampleList);
    }
}
