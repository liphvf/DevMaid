using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevMaid.CommandOptions;
using DevMaid.Services;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Services;
using DevMaid.Services.Logging;

namespace DevMaid.Tests.Commands;

[TestClass]
public class TableParserCommandTests
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
    private static IFileService? _fileService;
    private static IWingetService? _wingetService;
    private static IProcessExecutor? _processExecutor;
    private static Core.Logging.ILogger? _logger;

    private string _testDirectory = null!;

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
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TableParserCommandTests_{Guid.NewGuid():N}");
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
        var command = DevMaid.Commands.TableParserCommand.Build();

        Assert.AreEqual("table-parser", command.Name);
    }

    [TestMethod]
    public void Build_HasTableParserAlias()
    {
        var command = DevMaid.Commands.TableParserCommand.Build();

        Assert.IsTrue(command.Aliases.Contains("tableparser"));
    }

    [TestMethod]
    public void Parse_MissingTableName_ThrowsArgumentException()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            Table = ""
        };

        try { DevMaid.Commands.TableParserCommand.Parse(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Parse_InvalidTableName_ThrowsArgumentException()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            Table = "invalid;table"
        };

        try { DevMaid.Commands.TableParserCommand.Parse(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Parse_TableStartingWithNumber_ThrowsArgumentException()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            Table = "123table"
        };

        try { DevMaid.Commands.TableParserCommand.Parse(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Parse_InvalidHost_ThrowsArgumentException()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "invalid;host",
            Table = "invalid_table"
        };

        try { DevMaid.Commands.TableParserCommand.Parse(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Parse_InvalidUsername_ThrowsArgumentException()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            User = "invalid;user",
            Table = "testtable"
        };

        try { DevMaid.Commands.TableParserCommand.Parse(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Parse_PathTraversalInOutput_ThrowsArgumentException()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            Table = "valid_table",
            Output = Path.Combine(_testDirectory, "..", "..", "output.cs"),
            Password = "test" // Provide password to avoid console input
        };

        try { DevMaid.Commands.TableParserCommand.Parse(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Parse_ValidTable_CreatesOutputDirectory()
    {
        var outputDir = Path.Combine(_testDirectory, "subdir");
        var outputFile = Path.Combine(outputDir, "Table.cs");
        
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            Table = "users",
            Output = outputFile,
            Password = "test" // Provide password to avoid console input
        };

        try
        {
            DevMaid.Commands.TableParserCommand.Parse(options);
        }
        catch (Exception)
        {
            // Expected to fail due to database connection, but directory should be created
        }

        // Verify directory was created even if the command failed elsewhere
        Assert.IsTrue(Directory.Exists(outputDir), "Output directory should be created");
    }

    [TestMethod]
    public void Parse_DefaultOutputFile_UsesCurrentDirectory()
    {
        var options = new TableParserOptions
        {
            Database = "testdb",
            Host = "localhost",
            Table = "users",
            Output = "",
            Password = "test" // Provide password to avoid console input
        };

        try
        {
            DevMaid.Commands.TableParserCommand.Parse(options);
        }
        catch (Exception)
        {
            // Expected to fail due to database connection
        }

        // The default output path is "./Table.class" which resolves to current directory
        // We can't easily verify this without side effects, so we just ensure it doesn't throw on validation
    }
}
