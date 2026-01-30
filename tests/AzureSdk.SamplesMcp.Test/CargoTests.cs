using System.Reflection;
using System.Text.Json;
using AzureSdk.SamplesMcp.Providers;
using AzureSdk.SamplesMcp.Services;
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

    private class MockProcessService : IExternalProcessService
    {
        private readonly string _output;
        private readonly int _exitCode;

        public MockProcessService(string output, int exitCode = 0)
        {
            _output = output;
            _exitCode = exitCode;
        }

        public Task<ProcessResult> ExecuteAsync(
            string executablePath,
            string arguments,
            IDictionary<string, string>? environmentVariables = default,
            int operationTimeoutSeconds = 300,
            CancellationToken cancellationToken = default)
        {
            var result = new ProcessResult(_exitCode, _output, string.Empty, $"{executablePath} {arguments}");
            return Task.FromResult(result);
        }

        public JsonElement ParseJsonOutput(ProcessResult result)
        {
            if (result.ExitCode != 0)
            {
                return JsonDocument.Parse("{}").RootElement;
            }

            using var jsonDocument = JsonDocument.Parse(result.Output);
            return jsonDocument.RootElement.Clone();
        }
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

    [TestMethod]
    public async Task GetDependencies_ReturnsAzureDependencies()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var cargo = new Cargo();
        var directory = "cargo-project";

        // Mock cargo metadata output
        var mockMetadata = """
        {
          "packages": [
            {
              "name": "azure_core",
              "version": "0.31.0",
              "id": "azure_core 0.31.0 (registry+https://github.com/rust-lang/crates.io-index)"
            },
            {
              "name": "tokio",
              "version": "1.49.0",
              "id": "tokio 1.49.0 (registry+https://github.com/rust-lang/crates.io-index)"
            }
          ],
          "workspace_members": [],
          "resolve": null,
          "target_directory": "/test/target",
          "version": 1,
          "workspace_root": "/test"
        }
        """;

        var processService = new MockProcessService(mockMetadata);

        // Act
        var dependencies = await cargo.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(1, dependencyList);
        Assert.AreEqual("azure_core", dependencyList[0].Name);
        Assert.AreEqual("0.31.0", dependencyList[0].Version);
    }

    [TestMethod]
    public async Task GetDependencies_FiltersNonAzurePackages()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var cargo = new Cargo();
        var directory = "cargo-project";

        // Mock cargo metadata output with multiple packages
        var mockMetadata = """
        {
          "packages": [
            {
              "name": "azure_core",
              "version": "0.31.0",
              "id": "azure_core 0.31.0 (registry+https://github.com/rust-lang/crates.io-index)"
            },
            {
              "name": "azure_identity",
              "version": "0.20.0",
              "id": "azure_identity 0.20.0 (registry+https://github.com/rust-lang/crates.io-index)"
            },
            {
              "name": "tokio",
              "version": "1.49.0",
              "id": "tokio 1.49.0 (registry+https://github.com/rust-lang/crates.io-index)"
            },
            {
              "name": "serde",
              "version": "1.0.0",
              "id": "serde 1.0.0 (registry+https://github.com/rust-lang/crates.io-index)"
            }
          ],
          "workspace_members": [],
          "resolve": null,
          "target_directory": "/test/target",
          "version": 1,
          "workspace_root": "/test"
        }
        """;

        var processService = new MockProcessService(mockMetadata);

        // Act
        var dependencies = await cargo.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(2, dependencyList);
        Assert.IsTrue(dependencyList.All(d => d.Name!.StartsWith("azure_")));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "azure_core"));
        Assert.IsTrue(dependencyList.Any(d => d.Name == "azure_identity"));
    }

    [TestMethod]
    public async Task GetDependencies_ReturnsEmpty_WhenNoAzurePackages()
    {
        // Arrange
        var fileSystem = CreateFileSystem();
        var cargo = new Cargo();
        var directory = "cargo-project";

        // Mock cargo metadata output without Azure packages
        var mockMetadata = """
        {
          "packages": [
            {
              "name": "tokio",
              "version": "1.49.0",
              "id": "tokio 1.49.0 (registry+https://github.com/rust-lang/crates.io-index)"
            },
            {
              "name": "serde",
              "version": "1.0.0",
              "id": "serde 1.0.0 (registry+https://github.com/rust-lang/crates.io-index)"
            }
          ],
          "workspace_members": [],
          "resolve": null,
          "target_directory": "/test/target",
          "version": 1,
          "workspace_root": "/test"
        }
        """;

        var processService = new MockProcessService(mockMetadata);

        // Act
        var dependencies = await cargo.GetDependencies(directory, processService, fileSystem: fileSystem);
        var dependencyList = dependencies.ToList();

        // Assert
        Assert.HasCount(0, dependencyList);
    }
}
