using System.Text.Json;
using AzureSdk.SamplesMcp.Services;

namespace AzureSdk.SamplesMcp;

[TestClass]
public class JsonPathTests
{
    #region Cargo Tests
    [TestMethod]
    public void Query_CargoMetadata_ReturnsPackages()
    {
        // Arrange
        var json = """
        {
          "packages": [
            {
              "name": "azure_core",
              "version": "0.31.0",
              "id": "registry+https://github.com/rust-lang/crates.io-index#azure_core@0.31.0"
            },
            {
              "name": "tokio",
              "version": "1.49.0",
              "id": "registry+https://github.com/rust-lang/crates.io-index#tokio@1.49.0"
            }
          ],
          "target_directory": "/Users/heaths/src/azsdk-samples-mcp/tests/secrets/target"
        }
        """;

        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".packages[]").ToList();

        // Assert
        Assert.HasCount(2, results);
        Assert.AreEqual("azure_core", results[0].GetProperty("name").GetString());
        Assert.AreEqual("0.31.0", results[0].GetProperty("version").GetString());
        Assert.AreEqual("tokio", results[1].GetProperty("name").GetString());
        Assert.AreEqual("1.49.0", results[1].GetProperty("version").GetString());
    }
    #endregion

    #region Dotnet Tests
    [TestMethod]
    public void Query_DotnetList_ReturnsTopLevelPackages()
    {
        // Arrange
        var json = """
        {
          "version": 1,
          "projects": [
            {
              "path": "/Users/heaths/src/test/Test.csproj",
              "frameworks": [
                {
                  "framework": "net10.0",
                  "topLevelPackages": [
                    {
                      "id": "Azure.Identity",
                      "requestedVersion": "1.17.1",
                      "resolvedVersion": "1.17.1"
                    },
                    {
                      "id": "Azure.Security.KeyVault.Secrets",
                      "requestedVersion": "4.8.0",
                      "resolvedVersion": "4.8.0"
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".projects[].frameworks[].topLevelPackages[]").ToList();

        // Assert
        Assert.HasCount(2, results);
        Assert.AreEqual("Azure.Identity", results[0].GetProperty("id").GetString());
        Assert.AreEqual("1.17.1", results[0].GetProperty("resolvedVersion").GetString());
        Assert.AreEqual("Azure.Security.KeyVault.Secrets", results[1].GetProperty("id").GetString());
        Assert.AreEqual("4.8.0", results[1].GetProperty("resolvedVersion").GetString());
    }
    #endregion

    #region Pnpm Tests
    [TestMethod]
    public void Query_PnpmList_ReturnsDependencies()
    {
        // Arrange
        var json = """
        [
          {
            "name": "test",
            "version": "1.0.0",
            "path": "/Users/heaths/src/test-js",
            "private": true,
            "dependencies": {
              "@azure/keyvault-secrets": {
                "from": "@azure/keyvault-secrets",
                "version": "4.10.0",
                "resolved": "https://registry.npmjs.org/@azure/keyvault-secrets/-/keyvault-secrets-4.10.0.tgz",
                "path": "/Users/heaths/src/test-js/node_modules/.pnpm/@azure+keyvault-secrets@4.10.0/node_modules/@azure/keyvault-secrets"
              }
            },
            "devDependencies": {
              "mocha": {
                "from": "mocha",
                "version": "11.7.5",
                "resolved": "https://registry.npmjs.org/mocha/-/mocha-11.7.5.tgz",
                "path": "/Users/heaths/src/test-js/node_modules/.pnpm/mocha@11.7.5/node_modules/mocha"
              }
            }
          }
        ]
        """;

        using var doc = JsonDocument.Parse(json);

        // Act - Get dependencies object from first array element
        var results = JsonPath.Query(doc, ".[].dependencies").ToList();

        // Assert
        Assert.HasCount(1, results);
        var deps = results[0];
        Assert.AreEqual(JsonValueKind.Object, deps.ValueKind);
        Assert.IsTrue(deps.TryGetProperty("@azure/keyvault-secrets", out var azureKeyvault));
        Assert.AreEqual("4.10.0", azureKeyvault.GetProperty("version").GetString());
    }
    #endregion

    #region Npm Tests
    [TestMethod]
    public void Query_NpmList_ReturnsDependencies()
    {
        // Arrange
        var json = """
        {
          "version": "1.0.0",
          "name": "test",
          "dependencies": {
            "@azure/keyvault-secrets": {
              "version": "4.10.0",
              "resolved": "https://registry.npmjs.org/@azure/keyvault-secrets/-/keyvault-secrets-4.10.0.tgz",
              "overridden": false
            },
            "mocha": {
              "version": "11.7.5",
              "resolved": "https://registry.npmjs.org/mocha/-/mocha-11.7.5.tgz",
              "overridden": false
            }
          }
        }
        """;

        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".dependencies").ToList();

        // Assert
        Assert.HasCount(1, results);
        var deps = results[0];
        Assert.AreEqual(JsonValueKind.Object, deps.ValueKind);
        Assert.IsTrue(deps.TryGetProperty("@azure/keyvault-secrets", out var azureKeyvault));
        Assert.AreEqual("4.10.0", azureKeyvault.GetProperty("version").GetString());
        Assert.IsTrue(deps.TryGetProperty("mocha", out var mocha));
        Assert.AreEqual("11.7.5", mocha.GetProperty("version").GetString());
    }
    #endregion

    #region Basic Functionality Tests
    [TestMethod]
    public void Query_RootPath_ReturnsRootElement()
    {
        // Arrange
        var json = """{"name": "test", "version": "1.0.0"}""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".").ToList();

        // Assert
        Assert.HasCount(1, results);
        Assert.AreEqual(JsonValueKind.Object, results[0].ValueKind);
        Assert.AreEqual("test", results[0].GetProperty("name").GetString());
    }

    [TestMethod]
    public void Query_SimpleProperty_ReturnsPropertyValue()
    {
        // Arrange
        var json = """{"name": "test", "version": "1.0.0"}""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".name").ToList();

        // Assert
        Assert.HasCount(1, results);
        Assert.AreEqual("test", results[0].GetString());
    }

    [TestMethod]
    public void Query_NestedProperty_ReturnsNestedValue()
    {
        // Arrange
        var json = """{"outer": {"inner": "value"}}""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".outer.inner").ToList();

        // Assert
        Assert.HasCount(1, results);
        Assert.AreEqual("value", results[0].GetString());
    }

    [TestMethod]
    public void Query_SimpleArray_ReturnsArrayElements()
    {
        // Arrange
        var json = """{"items": [1, 2, 3]}""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".items[]").ToList();

        // Assert
        Assert.HasCount(3, results);
        Assert.AreEqual(1, results[0].GetInt32());
        Assert.AreEqual(2, results[1].GetInt32());
        Assert.AreEqual(3, results[2].GetInt32());
    }

    [TestMethod]
    public void Query_RootArray_ReturnsArrayElements()
    {
        // Arrange
        var json = """[{"name": "a"}, {"name": "b"}]""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".[]").ToList();

        // Assert
        Assert.HasCount(2, results);
        Assert.AreEqual("a", results[0].GetProperty("name").GetString());
        Assert.AreEqual("b", results[1].GetProperty("name").GetString());
    }

    [TestMethod]
    public void Query_NonExistentProperty_ReturnsEmpty()
    {
        // Arrange
        var json = """{"name": "test"}""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".nonexistent").ToList();

        // Assert
        Assert.HasCount(0, results);
    }

    [TestMethod]
    public void Query_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var json = """{"items": []}""";
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".items[]").ToList();

        // Assert
        Assert.HasCount(0, results);
    }

    [TestMethod]
    public void Query_MultipleNestedArrays_ReturnsAllElements()
    {
        // Arrange
        var json = """
        {
          "groups": [
            {
              "items": [
                {"value": 1},
                {"value": 2}
              ]
            },
            {
              "items": [
                {"value": 3}
              ]
            }
          ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        // Act
        var results = JsonPath.Query(doc, ".groups[].items[]").ToList();

        // Assert
        Assert.HasCount(3, results);
        Assert.AreEqual(1, results[0].GetProperty("value").GetInt32());
        Assert.AreEqual(2, results[1].GetProperty("value").GetInt32());
        Assert.AreEqual(3, results[2].GetProperty("value").GetInt32());
    }
    #endregion

    #region Error Handling Tests
    [TestMethod]
    public void Query_NullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => JsonPath.Query((JsonDocument)null!, ".").ToList());
    }

    [TestMethod]
    public void Query_NullPath_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"name": "test"}""";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => JsonPath.Query(doc, null!).ToList());
    }

    [TestMethod]
    public void Query_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"name": "test"}""";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => JsonPath.Query(doc, "").ToList());
    }

    [TestMethod]
    public void Query_WhitespacePath_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"name": "test"}""";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => JsonPath.Query(doc, "   ").ToList());
    }
    #endregion
}
