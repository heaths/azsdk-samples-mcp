// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Providers;

[TestClass]
public class GoModTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(GoModTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_WhenGoModExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";

        // Act
        var result = goMod.HasProject(directory, fileSystem);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_WhenGoModDoesNotExist()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "nonexistent";

        // Act
        var result = goMod.HasProject(directory, fileSystem);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetDependencies_ReturnsAzureDependencies()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await goMod.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets", dependencyList[0].Name);
        Assert.AreEqual("v1.3.0", dependencyList[0].Version);
    }

    [TestMethod]
    public async Task GetDependencies_FiltersNonAzureModules()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await goMod.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("github.com/Azure/azure-sdk-for-go/sdk/")));
    }

    [TestMethod]
    public async Task GetDependencies_ExcludesDescriptions_WhenNotRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await goMod.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: false);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.IsNull(dependencyList[0].Description);
    }

    [TestMethod]
    public async Task GetDependencies_IncludesDescriptions_WhenRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";
        var processService = new MockProcessService("");

        var environment = new TestEnvironment
        {
            { "GOMODCACHE", "go/pkg/mod" }
        };

        // Act
        var dependencies = await goMod.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: true, environment: environment);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets", dependencyList[0].Name);
        Assert.AreEqual("v1.3.0", dependencyList[0].Version);
        Assert.AreEqual("Azure Key Vault Secrets client module for Go.", dependencyList[0].Description);
    }

    [TestMethod]
    public async Task GetSamples_ReturnsReadmeAndExamples()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";
        var processService = new MockProcessService("");
        var dependencies = new List<Dependency>
        {
            new("github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets", "v1.3.0")
        };

        var environment = new TestEnvironment
        {
            { "GOMODCACHE", "go/pkg/mod" }
        };

        // Act
        var samples = await goMod.GetSamples(directory, dependencies, processService, environment: environment, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(2, sampleList);
        Assert.IsTrue(sampleList.Any(s => s.EndsWith("README.md")));
        Assert.IsTrue(sampleList.Any(s => s.EndsWith("example_test.go")));
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenCacheNotFound()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var goMod = new GoMod();
        var directory = "go-project";
        var processService = new MockProcessService("");
        var dependencies = new List<Dependency>
        {
            new("github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets", "v1.3.0")
        };

        var environment = new TestEnvironment
        {
            { "GOMODCACHE", "nonexistent-cache" }
        };

        // Act
        var samples = await goMod.GetSamples(directory, dependencies, processService, environment: environment, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(0, sampleList);
    }
}
