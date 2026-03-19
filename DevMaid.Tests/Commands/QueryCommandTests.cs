using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using DevMaid.CommandOptions;
using DevMaid.Services;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Services;
using DevMaid.Services.Logging;

using Command = System.CommandLine.Command;

namespace DevMaid.Tests.Commands;

[TestClass]
public class QueryCommandTests
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
    private static IFileService? _fileService;
    private static IWingetService? _wingetService;
    private static IProcessExecutor? _processExecutor;
    private static Core.Logging.ILogger? _logger;

    private string _testDirectory = null!;
    private string _sqlInputFile = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // Initialize logger
        var logger = new Services.Logging.ConsoleLogger(useColors: false);
        _logger = logger;

        // Initialize core services
        _configurationService = new Core.Services.ConfigurationService(logger);
        _processExecutor = new Core.Services.ProcessExecutor(logger);
        _databaseService = new Core.Services.DatabaseService(_processExecutor, logger);
        _fileService = new Core.Services.FileService(logger);
        _wingetService = new Core.Services.WingetService(_processExecutor, logger);

        // Register services for commands to use via reflection
        var serviceContainerType = typeof(DevMaid.ServiceContainer);
        
        // Set the private static fields using reflection
        SetPrivateStaticField(serviceContainerType, "_configurationService", _configurationService);
        SetPrivateStaticField(serviceContainerType, "_databaseService", _databaseService);
        SetPrivateStaticField(serviceContainerType, "_fileService", _fileService);
        SetPrivateStaticField(serviceContainerType, "_wingetService", _wingetService);
        SetPrivateStaticField(serviceContainerType, "_processExecutor", _processExecutor);
        SetPrivateStaticField(serviceContainerType, "_logger", _logger);
    }

    private static void SetPrivateStaticField(Type type, string fieldName, object? value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, value);
    }

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

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_NonExistentInputFile_ThrowsFileNotFoundException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "nonexistent.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (FileNotFoundException) { }
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

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_MissingOutputFile_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = ""
        };

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_PathTraversalInInput_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "..", "..", "test.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
        catch (FileNotFoundException) { Assert.Fail("Should throw ArgumentException before checking file existence"); }
    }

    [TestMethod]
    public void Run_PathTraversalInOutput_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = Path.Combine(_testDirectory, "..", "..", "output.csv")
        };

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_AllFlag_PathTraversal_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = Path.Combine(_testDirectory, "..", "..", "output"),
            All = true,
            Password = "test" // Provide password to avoid console input
        };

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { DevMaid.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }
}
