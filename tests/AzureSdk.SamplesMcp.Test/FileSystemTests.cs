using Microsoft.Extensions.FileProviders;

namespace AzureSdk.SamplesMcp;

[TestClass]
public class FileSystemTests
{
    static FileSystem CreateFileSystem()
    {
        var assembly = typeof(FileSystemTests).Assembly;
        var provider = new ManifestEmbeddedFileProvider(assembly, "Content");
        return new FileSystem(provider);
    }

    [TestMethod]
    public void DirectoryExists_ReturnsTrue_ForExistingDirectory()
    {
        var fs = CreateFileSystem();
        var result = fs.DirectoryExists(".cargo");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DirectoryExists_ReturnsFalse_ForNonExistingDirectory()
    {
        var fs = CreateFileSystem();
        var result = fs.DirectoryExists("nonexistent");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void FileExists_ReturnsTrue_ForExistingFile()
    {
        var fs = CreateFileSystem();
        var result = fs.FileExists("README.md");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void FileExists_ReturnsFalse_ForNonExistingFile()
    {
        var fs = CreateFileSystem();
        var result = fs.FileExists("nonexistent.txt");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetDirectories_ReturnsDirectoryNames()
    {
        var fs = CreateFileSystem();
        var result = fs.GetDirectories(".cargo/registry/src/index.crates.io-abcd1234").ToList();

        CollectionAssert.Contains(result, ".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0");
        CollectionAssert.Contains(result, ".cargo/registry/src/index.crates.io-abcd1234/tokio-1.49.0");
    }

    [TestMethod]
    public void GetFiles_ReturnsFileNames()
    {
        var fs = CreateFileSystem();
        var result = fs.GetFiles(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0").ToList();

        CollectionAssert.Contains(result, ".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0/README.md");
    }

    [TestMethod]
    public void ReadAllText_ReturnsFileContents()
    {
        var fs = CreateFileSystem();
        var result = fs.ReadAllText("README.md");

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
    }

    [TestMethod]
    public void GetParent_ReturnsParentDirectory_ForNestedFile()
    {
        var fs = CreateFileSystem();
        var result = fs.GetParent(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0/README.md");

        Assert.IsNotNull(result);
        StringAssert.EndsWith(result, "azure_core-0.1.0");
    }

    [TestMethod]
    public void GetParent_ReturnsParentDirectory_ForNestedDirectory()
    {
        var fs = CreateFileSystem();
        var result = fs.GetParent(".cargo/registry/src/index.crates.io-abcd1234/azure_core-0.1.0");

        Assert.IsNotNull(result);
        StringAssert.EndsWith(result, "index.crates.io-abcd1234");
    }

    [TestMethod]
    public void GetParent_ReturnsNull_ForRootPath()
    {
        var fs = CreateFileSystem();
        var result = fs.GetParent(".cargo");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void EnumerateAncestors_ReturnsAllAncestors_ForNestedDirectory()
    {
        var fs = CreateFileSystem();
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
        var fs = CreateFileSystem();
        var result = fs.EnumerateAncestors(".cargo").ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual(".cargo", result[0].Name);
    }

    [TestMethod]
    public void EnumerateAncestors_ThrowsException_ForNonExistentDirectory()
    {
        var fs = CreateFileSystem();
        Assert.Throws<System.IO.DirectoryNotFoundException>(() => fs.EnumerateAncestors("nonexistent").ToList());
    }

    [TestMethod]
    public void EnumerateAncestors_ThrowsException_ForFilePath()
    {
        var fs = CreateFileSystem();
        Assert.Throws<System.IO.DirectoryNotFoundException>(() => fs.EnumerateAncestors("README.md").ToList());
    }

    [TestMethod]
    public void EnumerateAncestors_YieldsInCorrectOrder_FromChildToRoot()
    {
        var fs = CreateFileSystem();
        var result = fs.EnumerateAncestors(".cargo/registry/src").ToList();

        Assert.IsGreaterThanOrEqualTo(result.Count, 3);

        // Verify order: child to root by checking names
        Assert.AreEqual("src", result[0].Name);
        Assert.AreEqual("registry", result[1].Name);
        Assert.AreEqual(".cargo", result[2].Name);
    }
}
