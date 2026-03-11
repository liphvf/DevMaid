using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Command = System.CommandLine.Command;

namespace DevMaid.Tests.Commands;

[TestClass]
public class CleanCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CleanCommandTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Clean_ValidDirectory_RemovesBinAndObjFolders()
    {
        var projectDir = Path.Combine(_testDirectory, "Project1");
        var binDir = Path.Combine(projectDir, "bin");
        var objDir = Path.Combine(projectDir, "obj");
        
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);
        
        var srcFile = Path.Combine(binDir, "test.dll");
        File.WriteAllText(srcFile, "test");

        DevMaid.Commands.CleanCommand.Clean(projectDir);

        Assert.IsFalse(Directory.Exists(binDir), "bin folder should be removed");
        Assert.IsFalse(Directory.Exists(objDir), "obj folder should be removed");
        Assert.IsTrue(Directory.Exists(projectDir), "project folder should remain");
    }

    [TestMethod]
    public void Clean_NullDirectory_UsesCurrentDirectory()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"CleanTest_{Guid.NewGuid():N}");
        
        try
        {
            Directory.CreateDirectory(tempDir);
            var binDir = Path.Combine(tempDir, "bin");
            Directory.CreateDirectory(binDir);
            
            Directory.SetCurrentDirectory(tempDir);
            
            DevMaid.Commands.CleanCommand.Clean(null);
            
            Assert.IsFalse(Directory.Exists(binDir), "bin folder should be removed");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [TestMethod]
    public void Clean_FilePath_UsesParentDirectory()
    {
        var projectDir = Path.Combine(_testDirectory, "Project2");
        var binDir = Path.Combine(projectDir, "bin");
        Directory.CreateDirectory(binDir);
        
        var projectFile = Path.Combine(projectDir, "Project2.csproj");
        File.WriteAllText(projectFile, "<Project />");

        DevMaid.Commands.CleanCommand.Clean(projectFile);

        Assert.IsFalse(Directory.Exists(binDir), "bin folder should be removed");
    }

    [TestMethod]
    public void Clean_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var nonExistentDir = Path.Combine(_testDirectory, "NonExistent");
        
        try { DevMaid.Commands.CleanCommand.Clean(nonExistentDir); Assert.Fail(); } catch (DirectoryNotFoundException) { }
    }

    [TestMethod]
    public void Clean_NestedDirectories_RemovesAllBinAndObj()
    {
        var rootDir = Path.Combine(_testDirectory, "Solution");
        var project1Dir = Path.Combine(rootDir, "Project1", "bin");
        var project2Dir = Path.Combine(rootDir, "Project2", "obj");
        var srcDir = Path.Combine(rootDir, "Project1", "src");
        
        Directory.CreateDirectory(project1Dir);
        Directory.CreateDirectory(project2Dir);
        Directory.CreateDirectory(srcDir);

        DevMaid.Commands.CleanCommand.Clean(rootDir);

        Assert.IsFalse(Directory.Exists(Path.Combine(rootDir, "Project1", "bin")));
        Assert.IsFalse(Directory.Exists(Path.Combine(rootDir, "Project2", "obj")));
        Assert.IsTrue(Directory.Exists(srcDir), "src folder should remain");
    }

    [TestMethod]
    public void Clean_DirectoryWithOnlyBinAndObj_RemovesBoth()
    {
        var projectDir = Path.Combine(_testDirectory, "EmptyProject");
        var binDir = Path.Combine(projectDir, "bin");
        var objDir = Path.Combine(projectDir, "obj");
        
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);

        DevMaid.Commands.CleanCommand.Clean(projectDir);

        Assert.IsFalse(Directory.Exists(binDir));
        Assert.IsFalse(Directory.Exists(objDir));
    }

    [TestMethod]
    public void Build_ReturnsCommandWithCorrectNameAndDescription()
    {
        var command = DevMaid.Commands.CleanCommand.Build();

        Assert.AreEqual("clean", command.Name);
        Assert.AreEqual("Remove bin and obj folders from solution.", command.Description);
    }

    [TestMethod]
    public void Build_CommandHasDirectoryArgument()
    {
        var command = DevMaid.Commands.CleanCommand.Build();

        Assert.AreEqual(1, command.Arguments.Count);
        Assert.AreEqual("directory", command.Arguments[0].Name);
    }
}
