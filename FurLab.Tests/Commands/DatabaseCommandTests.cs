using System.Linq;

using FurLab.CLI.Services;
using FurLab.CLI.Services.Logging;
using FurLab.Core.Interfaces;
using FurLab.Core.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class DatabaseCommandTests
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
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

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().AddConsole());
        services.AddSingleton(_configurationService);
        services.AddSingleton(_databaseService);
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

}
