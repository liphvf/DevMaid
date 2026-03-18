using System;
using System.CommandLine;

using DevMaid.Commands;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Models;
using DevMaid.Core.Services;
using DevMaid.Services.Logging;

namespace DevMaid;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Initialize logger
        var logger = new Services.Logging.ConsoleLogger(useColors: true);
        Logger.SetLogger(logger);

        // Initialize core services
        var configurationService = new ConfigurationService(logger);
        var processExecutor = new ProcessExecutor(logger);
        var databaseService = new DatabaseService(processExecutor, logger);
        var fileService = new FileService(logger);
        var wingetService = new WingetService(processExecutor, logger);

        // Register services for commands to use
        ServiceContainer.RegisterServices(
            configurationService,
            databaseService,
            fileService,
            wingetService,
            processExecutor,
            logger);

        var rootCommand = new RootCommand("DevMaid command line tools")
        {
            TableParserCommand.Build(),
            FileCommand.Build(),
            ClaudeCodeCommand.Build(),
            OpenCodeCommand.Build(),
            WingetCommand.Build(),
            TuiCommand.Build(),
            DatabaseCommand.Build(),
            QueryCommand.Build(),
            CleanCommand.Build(),
            WindowsFeaturesCommand.Build()
        };

        return rootCommand.Parse(args).Invoke();
    }
}

/// <summary>
/// Simple service container for dependency injection.
/// </summary>
internal static class ServiceContainer
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
    private static IFileService? _fileService;
    private static IWingetService? _wingetService;
    private static IProcessExecutor? _processExecutor;
    private static Core.Logging.ILogger? _logger;

    public static void RegisterServices(
        IConfigurationService configurationService,
        IDatabaseService databaseService,
        IFileService fileService,
        IWingetService wingetService,
        IProcessExecutor processExecutor,
        Core.Logging.ILogger logger)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;
        _fileService = fileService;
        _wingetService = wingetService;
        _processExecutor = processExecutor;
        _logger = logger;
    }

    public static IConfigurationService ConfigurationService => _configurationService ?? throw new InvalidOperationException("ConfigurationService not registered");
    public static IDatabaseService DatabaseService => _databaseService ?? throw new InvalidOperationException("DatabaseService not registered");
    public static IFileService FileService => _fileService ?? throw new InvalidOperationException("FileService not registered");
    public static IWingetService WingetService => _wingetService ?? throw new InvalidOperationException("WingetService not registered");
    public static IProcessExecutor ProcessExecutor => _processExecutor ?? throw new InvalidOperationException("ProcessExecutor not registered");
    public static Core.Logging.ILogger Logger => _logger ?? throw new InvalidOperationException("Logger not registered");
}
