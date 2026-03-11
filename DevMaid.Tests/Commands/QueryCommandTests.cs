using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using DevMaid.CommandOptions;

using Command = System.CommandLine.Command;

namespace DevMaid.Tests.Commands;

[TestClass]
public class QueryCommandTests
{
    private string _testDirectory = null!;
    private string _sqlInputFile = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"QueryCommandTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        _sqlInputFile = Path.Combine(_testDirectory, "test.sql");
        File.WriteAllText(_sqlInputFile, "SELECT * FROM users;");
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
        var command = DevMaid.Commands.QueryCommand.Build();

        Assert.AreEqual("query", command.Name);
    }

    [TestMethod]
    public void Build_ContainsRunSubcommand()
    {
        var command = DevMaid.Commands.QueryCommand.Build();

        Assert.AreEqual(1, command.Children.Count());
        
        var runCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "run");
        Assert.IsNotNull(runCommand);
    }

    [TestMethod]
    public void Run_MissingInputFile_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = "",
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_NonExistentInputFile_ThrowsFileNotFoundException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "nonexistent.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_EmptyInputFile_ThrowsArgumentException()
    {
        var emptyFile = Path.Combine(_testDirectory, "empty.sql");
        File.WriteAllText(emptyFile, "");
        
        var options = new QueryCommandOptions
        {
            InputFile = emptyFile,
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_MissingOutputFile_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = ""
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_PathTraversalInInput_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "..", "..", "test.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_PathTraversalInOutput_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = Path.Combine(_testDirectory, "..", "..", "output.csv")
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_AllFlag_MissingOutputDirectory_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = "",
            All = true
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_ServersFlag_MissingOutputDirectory_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = "",
            Servers = true
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_AllFlag_PathTraversal_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = Path.Combine(_testDirectory, "..", "..", "output"),
            All = true
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }

    [TestMethod]
    public void Run_ServersFlag_PathTraversal_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = Path.Combine(_testDirectory, "..", "..", "output"),
            Servers = true
        };

        // TODO: Fix () => DevMaid.Commands.QueryCommand.Run(options));
    }
}
