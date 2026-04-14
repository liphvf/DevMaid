using System.CommandLine;
using System.Text.Json;

using FurLab.CLI.Commands;
using FurLab.CLI.Services;
using FurLab.Core.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Spectre.Console;

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
        PostgresDatabaseLister.SetServiceProvider(serviceProvider);
        Services.UserConfigService.SetServiceProvider(serviceProvider);
        Services.CredentialService.SetServiceProvider(serviceProvider);

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
            DockerCommand.Build(),
            SettingsCommand.Build()
        };

        try
        {
            // Disable the default exception handler so that exceptions thrown inside
            // command handlers propagate here instead of being swallowed by
            // System.CommandLine and printed as unformatted stack traces.
            var invocationConfig = new InvocationConfiguration
            {
                EnableDefaultExceptionHandler = false
            };

            return rootCommand.Parse(args).Invoke(invocationConfig);
        }
        catch (OperationCanceledException ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Operation cancelled: {Markup.Escape(ex.Message)}[/]");
            return 130; // standard "cancelled by user" exit code
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 2; // misuse of command
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(ex.Message)}[/]");
            return 2;
        }
        catch (DirectoryNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Directory not found: {Markup.Escape(ex.Message)}[/]");
            return 2;
        }
        catch (PlatformNotSupportedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Not supported on this platform: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (JsonException ex)
        {
            AnsiConsole.MarkupLine($"[red]Invalid JSON: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (InvalidDataException ex)
        {
            AnsiConsole.MarkupLine($"[red]Invalid data: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (PostgresBinaryNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (BackupFailedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Backup failed: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (RestoreFailedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Restore failed: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (PathTraversalException ex)
        {
            AnsiConsole.MarkupLine($"[red]Security error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (FurLabFileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(ex.Message)}[/]");
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FurLab");
            logger.LogCritical(ex, "An unexpected error occurred: {Message}", ex.Message);
            AnsiConsole.MarkupLine($"[red]Unexpected error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }
}
