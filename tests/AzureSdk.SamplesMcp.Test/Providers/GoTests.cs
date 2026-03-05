// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Providers;

[TestClass]
public class GoTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(GoTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void HasProject_ReturnsTrue_WhenGoModExists()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
        var directory = "go-project";

        // Act
        var result = go.HasProject(directory, fileSystem);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasProject_ReturnsFalse_WhenGoModDoesNotExist()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
        var directory = "nonexistent";

        // Act
        var result = go.HasProject(directory, fileSystem);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetDependencies_ReturnsAzureDependencies()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
        var directory = "go-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await go.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(3, dependencyList);
        Assert.IsTrue(dependencyList.Any(d => d.Name == "github.com/Azure/azure-sdk-for-go/sdk/azidentity" && d.Version == "v1.8.2"));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets" && d.Version == "v1.3.0"));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "github.com/Azure/azure-sdk-for-go/sdk/azcore" && d.Version == "v1.17.0"));
    }

    [TestMethod]
    public async Task GetDependencies_FiltersNonAzureModules()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
        var directory = "go-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await go.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(3, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("github.com/Azure/azure-sdk-for-go/sdk/")));
    }

    [TestMethod]
    public async Task GetDependencies_ExcludesDescriptions_WhenNotRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
        var directory = "go-project";
        var processService = new MockProcessService("");

        // Act
        var dependencies = await go.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: false);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(3, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Description is null));
    }

    [TestMethod]
    public async Task GetDependencies_IncludesDescriptions_WhenRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
        var directory = "go-project";
        var processService = new MockProcessService("");

        var environment = new TestEnvironment
        {
            { "GOMODCACHE", "go/pkg/mod" }
        };

        // Act
        var dependencies = await go.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: true, environment: environment);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(3, dependencyList);

        var azidentity = dependencyList.First(d => d.Name == "github.com/Azure/azure-sdk-for-go/sdk/azidentity");
        Assert.AreEqual("v1.8.2", azidentity.Version);
        Assert.AreEqual("Azure Identity provides Microsoft Entra ID token authentication support across the Azure SDK.", azidentity.Description);

        var azsecrets = dependencyList.First(d => d.Name == "github.com/Azure/azure-sdk-for-go/sdk/security/keyvault/azsecrets");
        Assert.AreEqual("v1.3.0", azsecrets.Version);
        Assert.AreEqual("Azure Key Vault Secrets client module for Go.", azsecrets.Description);

        var azcore = dependencyList.First(d => d.Name == "github.com/Azure/azure-sdk-for-go/sdk/azcore");
        Assert.AreEqual("v1.17.0", azcore.Version);
        Assert.AreEqual("Azure Core provides shared primitives, abstractions, and helpers for Azure SDK client modules.", azcore.Description);
    }

    [TestMethod]
    public async Task GetSamples_ReturnsReadmeAndExamples()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
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
        var samples = await go.GetSamples(directory, dependencies, processService, environment: environment, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(3, sampleList);
        Assert.IsTrue(sampleList.Any(s => s.EndsWith("README.md")));
        Assert.IsTrue(sampleList.Any(s => s.EndsWith("example_test.go")));
        Assert.IsTrue(sampleList.Any(s => s.EndsWith("example_secrets_test.go")));
    }

    [TestMethod]
    public async Task GetSamples_ReturnsEmpty_WhenCacheNotFound()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var go = new Go();
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
        var samples = await go.GetSamples(directory, dependencies, processService, environment: environment, fileSystem: fileSystem);
        var sampleList = samples.ToList();

        // Assert
        Assert.HasCount(0, sampleList);
    }
}
