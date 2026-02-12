// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Providers;

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

    [TestMethod]
    public async Task GetDependencies_IncludesDescriptions_WhenRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Dotnet();
        var directory = ".";

        // Mock dotnet list package output
        var mockOutput = """
        {
          "version": 1,
          "projects": [
            {
              "frameworks": [
                {
                  "framework": "net10.0",
                  "topLevelPackages": [
                    {
                      "id": "Azure.Security.KeyVault.Secrets",
                      "resolvedVersion": "4.8.0"
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var globalPackagesOutput = "global-packages: .nuget/packages";
        var processService = new MockMultiProcessService(new Dictionary<string, string>
        {
            { "dotnet list package --format json", mockOutput },
            { "dotnet nuget locals global-packages --list", globalPackagesOutput }
        });

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: true);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("Azure.Security.KeyVault.Secrets", dependencyList[0].Name);
        Assert.AreEqual("4.8.0", dependencyList[0].Version);
        Assert.AreEqual("Azure Key Vault Secrets client library for .NET", dependencyList[0].Description);
    }

    [TestMethod]
    public async Task GetDependencies_ExcludesDescriptions_WhenNotRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Dotnet();
        var directory = ".";

        // Mock dotnet list package output
        var mockOutput = """
        {
          "version": 1,
          "projects": [
            {
              "frameworks": [
                {
                  "framework": "net10.0",
                  "topLevelPackages": [
                    {
                      "id": "Azure.Security.KeyVault.Secrets",
                      "resolvedVersion": "4.8.0"
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var processService = new MockProcessService(mockOutput);

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: false);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("Azure.Security.KeyVault.Secrets", dependencyList[0].Name);
        Assert.AreEqual("4.8.0", dependencyList[0].Version);
        Assert.IsNull(dependencyList[0].Description);
    }
}
