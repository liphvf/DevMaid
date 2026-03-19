using System;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevMaid.CLI.CommandOptions;

using Command = System.CommandLine.Command;

namespace DevMaid.Tests.Commands;

[TestClass]
public class FileCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileCommandTests_{Guid.NewGuid():N}");
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
    public void Build_ReturnsCommandWithCorrectName()
    {
        var command = DevMaid.CLI.Commands.FileCommand.Build();

        Assert.AreEqual("file", command.Name);
        Assert.AreEqual("File utilities.", command.Description);
    }

    [TestMethod]
    public void Build_ContainsCombineSubcommand()
    {
        var command = DevMaid.CLI.Commands.FileCommand.Build();

        var combineCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "combine");
        Assert.IsNotNull(combineCommand);
    }

    [TestMethod]
    public void Combine_ValidPattern_CombinesFiles()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        
        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(_testDirectory, "combined.txt")
        };

        DevMaid.CLI.Commands.FileCommand.Combine(options);

        Assert.IsTrue(File.Exists(options.Output));
        var content = File.ReadAllText(options.Output);
        Assert.IsTrue(content.Contains("Content 1"));
        Assert.IsTrue(content.Contains("Content 2"));
    }

    [TestMethod]
    public void Combine_EmptyPattern_ThrowsArgumentException()
    {
        var options = new FileCommandOptions
        {
            Input = "",
            Output = Path.Combine(_testDirectory, "output.txt")
        };

        try { DevMaid.CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Combine_InvalidPath_ThrowsArgumentException()
    {
        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(Path.GetTempPath(), "outside", "output.txt")
        };

        try { DevMaid.CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Combine_NoFilesFound_ThrowsException()
    {
        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "nonexistent*.txt"),
            Output = Path.Combine(_testDirectory, "output.txt")
        };

        try { DevMaid.CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (Exception) { }
    }

    [TestMethod]
    public void Combine_PathTraversalInInput_ThrowsArgumentException()
    {
        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "..", "*.txt"),
            Output = Path.Combine(_testDirectory, "output.txt")
        };

        try { DevMaid.CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Combine_PathTraversalInOutput_ThrowsArgumentException()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        File.WriteAllText(file1, "Content");

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(_testDirectory, "..", "output.txt")
        };

        try { DevMaid.CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Combine_DefaultOutputFile_UsesCombineFilesExtension()
    {
        var file1 = Path.Combine(_testDirectory, "test1.sql");
        File.WriteAllText(file1, "SELECT 1;");

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.sql")
        };

        DevMaid.CLI.Commands.FileCommand.Combine(options);

        var expectedOutput = Path.Combine(_testDirectory, "CombineFiles.sql");
        Assert.IsTrue(File.Exists(expectedOutput));
    }

    [TestMethod]
    public void Combine_MultipleFilesWithEncoding_PreservesEncoding()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        
        var content1 = "Content with special chars: áéíóú";
        var content2 = "More content: ãõû";
        
        File.WriteAllText(file1, content1, Encoding.UTF8);
        File.WriteAllText(file2, content2, Encoding.UTF8);

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(_testDirectory, "combined.txt")
        };

        DevMaid.CLI.Commands.FileCommand.Combine(options);

        var result = File.ReadAllText(options.Output, Encoding.UTF8);
        Assert.IsTrue(result.Contains(content1));
        Assert.IsTrue(result.Contains(content2));
    }
}
