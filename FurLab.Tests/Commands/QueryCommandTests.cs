using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;
using FurLab.CLI.Services.Logging;
using FurLab.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FurLab.Tests.Commands;

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
        var logger = new ConsoleLogger(useColors: false);
        _logger = logger;

        // Initialize core services
        _configurationService = new Core.Services.ConfigurationService(logger);
        _processExecutor = new Core.Services.ProcessExecutor(logger);
        _databaseService = new Core.Services.DatabaseService(_processExecutor, logger);
        _fileService = new Core.Services.FileService(logger);
        _wingetService = new Core.Services.WingetService(_processExecutor, logger);

        // Create service collection and register services
        var services = new ServiceCollection();
        services.AddSingleton(_configurationService);
        services.AddSingleton(_databaseService);
        services.AddSingleton(_fileService);
        services.AddSingleton(_wingetService);
        services.AddSingleton(_processExecutor);
        services.AddSingleton(_logger);

        var serviceProvider = services.BuildServiceProvider();

        // Set service provider for static services
        Logger.SetServiceProvider(serviceProvider);
        ConfigurationService.SetServiceProvider(serviceProvider);
        PostgresDatabaseLister.SetServiceProvider(serviceProvider);
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
        var command = CLI.Commands.QueryCommand.Build();

        Assert.AreEqual("query", command.Name);
    }

    [TestMethod]
    public void Build_ContainsRunSubcommand()
    {
        var command = CLI.Commands.QueryCommand.Build();

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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_NonExistentInputFile_ThrowsFileNotFoundException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "nonexistent.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (FileNotFoundException) { }
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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_MissingOutputFile_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = ""
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Run_PathTraversalInInput_ThrowsArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "..", "..", "test.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); }
        catch (ArgumentException) { }
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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
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

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void BuildConnectionString_ExplicitParameters_DoNotRequireServersConfigurationOrDatabaseAccess()
    {
        var mockConfigurationService = new Mock<IConfigurationService>();
        mockConfigurationService
            .SetupGet(x => x.Configuration)
            .Returns(new ConfigurationBuilder().AddInMemoryCollection().Build());
        mockConfigurationService
            .Setup(x => x.GetDatabaseConfig())
            .Returns(new Core.Models.DatabaseConnectionConfig());

        var services = new ServiceCollection();
        services.AddSingleton(mockConfigurationService.Object);
        ConfigurationService.SetServiceProvider(services.BuildServiceProvider());

        var options = new QueryCommandOptions
        {
            Host = "db.internal",
            Port = "5433",
            Database = "reporting",
            Username = "readonly_user",
            Password = "secret123"
        };

        var method = typeof(CLI.Commands.QueryCommand).GetMethod("BuildConnectionString", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var connectionString = method.Invoke(null, [options]) as string;

        Assert.IsNotNull(connectionString);
        StringAssert.Contains(connectionString, "Host=db.internal");
        StringAssert.Contains(connectionString, "Port=5433");
        StringAssert.Contains(connectionString, "Database=reporting");
        StringAssert.Contains(connectionString, "Username=readonly_user");
        StringAssert.Contains(connectionString, "Password=secret123");

        mockConfigurationService.Verify(x => x.GetDatabaseConfig(), Times.Once);
        mockConfigurationService.VerifyGet(x => x.Configuration, Times.Never);
        mockConfigurationService.VerifyNoOtherCalls();
    }
}
