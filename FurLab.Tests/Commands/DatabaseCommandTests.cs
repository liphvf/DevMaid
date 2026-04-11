using System;
using System.Linq;

using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;
using FurLab.CLI.Services.Logging;
using FurLab.Core.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class DatabaseCommandTests
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
    private static IFileService? _fileService;
    private static IWingetService? _wingetService;
    private static IProcessExecutor? _processExecutor;
    private static Core.Logging.ILogger? _logger;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var logger = new ConsoleLogger(useColors: false);
        _logger = logger;

        _configurationService = new Core.Services.ConfigurationService(logger);
        _processExecutor = new Core.Services.ProcessExecutor(logger);
        _databaseService = new Core.Services.DatabaseService(_processExecutor, logger);
        _fileService = new Core.Services.FileService(logger);
        _wingetService = new Core.Services.WingetService(_processExecutor, logger);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().AddConsole());
        services.AddSingleton(_configurationService);
        services.AddSingleton(_databaseService);
        services.AddSingleton(_fileService);
        services.AddSingleton(_wingetService);
        services.AddSingleton(_processExecutor);
        services.AddSingleton(_logger);

        var serviceProvider = services.BuildServiceProvider();

        Logger.SetServiceProvider(serviceProvider);
        ConfigurationService.SetServiceProvider(serviceProvider);
        PostgresDatabaseLister.SetServiceProvider(serviceProvider);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
    }

    [TestMethod]
    public void Build_ReturnsCommandWithCorrectName()
    {
        var command = CLI.Commands.DatabaseCommand.Build();

        Assert.AreEqual("database", command.Name);
        Assert.AreEqual("Database utilities.", command.Description);
    }

    [TestMethod]
    public void Build_ContainsBackupAndRestoreSubcommands()
    {
        var command = CLI.Commands.DatabaseCommand.Build();

        Assert.AreEqual(3, command.Children.Count());

        var backupCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "backup");
        var restoreCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "restore");
        var pgpassCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "pgpass");

        Assert.IsNotNull(backupCommand);
        Assert.IsNotNull(restoreCommand);
        Assert.IsNotNull(pgpassCommand);
    }

    [TestMethod]
    public void Backup_MissingDatabaseName_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "",
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try { CLI.Commands.DatabaseCommand.Backup(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Restore_MissingDatabaseName_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "",
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try { CLI.Commands.DatabaseCommand.Restore(options); Assert.Fail(); } catch (ArgumentException) { }
    }
}
