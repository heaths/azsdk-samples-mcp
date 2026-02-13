// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using AzureSdk.SamplesMcp.Services;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Providers;

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
    public async Task GetDependencies_ReturnsAzureDependencies_ForNpm()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Node();
        var directory = ".";

        // Mock npm list output
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            },
            "@azure/identity": {
              "version": "4.7.0"
            },
            "express": {
              "version": "4.18.2"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(2, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("@azure/")));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "@azure/keyvault-secrets"));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "@azure/identity"));
    }

    [TestMethod]
    public async Task GetDependencies_FiltersNonAzurePackages_ForNpm()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Node();
        var directory = ".";

        // Mock npm list output with multiple packages
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            },
            "lodash": {
              "version": "4.17.21"
            },
            "express": {
              "version": "4.18.2"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("@azure/keyvault-secrets", dependencyList[0].Name);
        Assert.AreEqual("4.10.0", dependencyList[0].Version);
    }

    [TestMethod]
    public async Task GetDependencies_ReturnsEmpty_WhenNoAzurePackages()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Node();
        var directory = ".";

        // Mock npm list output without Azure packages
        var mockOutput = """
        {
          "dependencies": {
            "express": {
              "version": "4.18.2"
            },
            "lodash": {
              "version": "4.17.21"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(0, dependencyList);
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

        // Mock npm list output
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        var samples = await provider.GetSamples(".", dependencies, processService, fileSystem: fs);
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

        // Mock pnpm list output
        var mockOutput = """
        [
          {
            "dependencies": {
              "@azure/identity": {
                "version": "4.7.0"
              }
            }
          }
        ]
        """;

        var processService = new MockProcessService(mockOutput);

        var samples = await provider.GetSamples("pnpm-test", dependencies, processService, fileSystem: fs);
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

        var mockOutput = "{}";
        var processService = new MockProcessService(mockOutput);

        var samples = await provider.GetSamples(".cargo", dependencies, processService, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(0, sampleList);
    }

    [TestMethod]
    public async Task GetSamples_ReturnsAllAzurePackages_WhenDependenciesEmpty()
    {
        FileSystem fs = CreateFileSystem();
        Node provider = new();
        var dependencies = new List<Dependency>();

        // Mock npm list output with multiple Azure packages
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            },
            "@azure/identity": {
              "version": "4.7.0"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        var samples = await provider.GetSamples(".", dependencies, processService, fileSystem: fs);
        var sampleList = samples.ToList();

        // Only @azure/keyvault-secrets exists in test content
        Assert.HasCount(1, sampleList);
        Assert.IsTrue(sampleList.Any(s => s.Contains("@azure/keyvault-secrets")));
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

        // Mock npm list output
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        var samples = await provider.GetSamples(".", dependencies, processService, fileSystem: fs);
        var sampleList = samples.ToList();

        Assert.HasCount(0, sampleList);
    }

    [TestMethod]
    public async Task GetDependencies_IncludesDescriptions_WhenRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Node();
        var directory = ".";

        // Mock npm list output
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: true);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("@azure/keyvault-secrets", dependencyList[0].Name);
        Assert.AreEqual("4.10.0", dependencyList[0].Version);
        Assert.AreEqual("Azure Key Vault Secrets client library for JavaScript", dependencyList[0].Description);
    }

    [TestMethod]
    public async Task GetDependencies_ExcludesDescriptions_WhenNotRequested()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var provider = new Node();
        var directory = ".";

        // Mock npm list output
        var mockOutput = """
        {
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0"
            }
          }
        }
        """;

        var processService = new MockProcessService(mockOutput);

        // Act
        var dependencies = await provider.GetDependencies(directory, processService, fileSystem: fileSystem, includeDescriptions: false);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("@azure/keyvault-secrets", dependencyList[0].Name);
        Assert.AreEqual("4.10.0", dependencyList[0].Version);
        Assert.IsNull(dependencyList[0].Description);
    }
}
