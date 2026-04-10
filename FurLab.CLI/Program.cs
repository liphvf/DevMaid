using System.CommandLine;
using System.Text.Json;

using FurLab.CLI.Commands;
using FurLab.Core.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FurLab.CLI;

internal static class Program
{
    private static int Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddFurLabServices();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        Services.Logging.Logger.SetServiceProvider(serviceProvider);
        Services.ConfigurationService.SetServiceProvider(serviceProvider);
        Services.PostgresDatabaseLister.SetServiceProvider(serviceProvider);

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FurLab");
            var exception = eventArgs.ExceptionObject as Exception;
            logger.LogCritical(exception, "Unhandled exception occurred. Application will terminate.");
            Environment.Exit(1);
        };

        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FurLab");
            logger.LogError(eventArgs.Exception, "Unobserved task exception occurred.");
            eventArgs.SetObserved();
        };

        var rootCommand = new RootCommand("FurLab command line tools")
        {
            FileCommand.Build(),
            ClaudeCodeCommand.Build(),
            OpenCodeCommand.Build(),
            WingetCommand.Build(),
            DatabaseCommand.Build(),
            QueryCommand.Build(),
            CleanCommand.Build(),
            WindowsFeaturesCommand.Build(),
            DockerCommand.Build()
        };

        try
        {
            return rootCommand.Parse(args).Invoke();
        }
        catch (OperationCanceledException)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FurLab");
            logger.LogWarning("Operation was cancelled by user.");
            return 130;
        }
        catch (JsonException ex)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FurLab");
            logger.LogError(ex, "Failed to parse configuration file. Please check your appsettings.json for syntax errors.");
            return 1;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FurLab");
            logger.LogCritical(ex, "An unexpected error occurred: {Message}", ex.Message);
            return 1;
        }
    }
}
