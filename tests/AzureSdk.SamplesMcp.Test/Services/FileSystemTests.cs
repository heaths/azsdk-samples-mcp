// Copyright 2026 Heath Stewart.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp.Services;

[TestClass]
public class FileSystemTests
{
    private static FileSystem CreateFileSystem()
    {
        Assembly assembly = typeof(FileSystemTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void DirectoryExists_ReturnsTrue_ForExistingDirectory()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.DirectoryExists(".cargo");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DirectoryExists_ReturnsFalse_ForNonExistingDirectory()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.DirectoryExists("nonexistent");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void FileExists_ReturnsTrue_ForExistingFile()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.FileExists("README.md");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void FileExists_ReturnsFalse_ForNonExistingFile()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.FileExists("nonexistent.txt");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetDirectories_ReturnsDirectoryNames()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.GetDirectories(".cargo/registry/src/index.crates.io-abcd1234").ToList();

        CollectionAssert.Contains(result, ".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0");
        CollectionAssert.Contains(result, ".cargo/registry/src/index.crates.io-abcd1234/tokio-1.49.0");
    }

    [TestMethod]
    public void GetFiles_ReturnsFileNames()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.GetFiles(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0").ToList();

        CollectionAssert.Contains(result, ".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0/README.md");
    }

    [TestMethod]
    public void ReadAllText_ReturnsFileContents()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.ReadAllText("README.md");

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void GetParent_ReturnsParentDirectory_ForNestedFile()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.GetParent(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0/README.md");

        Assert.IsNotNull(result);
        Assert.EndsWith("azure_core-0.1.0", result);
    }

    [TestMethod]
    public void GetParent_ReturnsParentDirectory_ForNestedDirectory()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.GetParent(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0");

        Assert.IsNotNull(result);
        Assert.EndsWith("index.crates.io-abcd1234", result);
    }

    [TestMethod]
    public void GetParent_ReturnsNull_ForRootPath()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.GetParent(".cargo");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void EnumerateAncestors_ReturnsAllAncestors_ForNestedDirectory()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.EnumerateAncestors(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0").ToList();

        Assert.IsNotEmpty(result);

        // Should include the starting directory and all ancestors up to root
        var names = result.Select(d => d.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
        CollectionAssert.Contains(names, "azure_core-0.1.0");
        CollectionAssert.Contains(names, "index.crates.io-abcd1234");
        CollectionAssert.Contains(names, "src");
        CollectionAssert.Contains(names, "registry");
        CollectionAssert.Contains(names, ".cargo");
    }

    [TestMethod]
    public void EnumerateAncestors_ReturnsSingleDirectory_ForRootLevel()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.EnumerateAncestors(".cargo").ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual(".cargo", result[0].Name);
    }

    [TestMethod]
    public void EnumerateAncestors_ThrowsException_ForNonExistentDirectory()
    {
        FileSystem fs = CreateFileSystem();
        Assert.Throws<System.IO.DirectoryNotFoundException>(() => fs.EnumerateAncestors("nonexistent").ToList());
    }

    [TestMethod]
    public void EnumerateAncestors_ThrowsException_ForFilePath()
    {
        FileSystem fs = CreateFileSystem();
        Assert.Throws<System.IO.DirectoryNotFoundException>(() => fs.EnumerateAncestors("README.md").ToList());
    }

    [TestMethod]
    public void EnumerateAncestors_YieldsInCorrectOrder_FromChildToRoot()
    {
        FileSystem fs = CreateFileSystem();
        var result = fs.EnumerateAncestors(".cargo/registry/src").ToList();

        Assert.IsGreaterThanOrEqualTo(result.Count, 3);

        // Verify order: child to root by checking names
        Assert.AreEqual("src", result[0].Name);
        Assert.AreEqual("registry", result[1].Name);
        Assert.AreEqual(".cargo", result[2].Name);
    }
}
