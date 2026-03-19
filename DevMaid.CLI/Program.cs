using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.CommandLine;

using DevMaid.CLI.Commands;
using DevMaid.Core.Services;

namespace DevMaid.CLI;

internal static class Program
{
    private static int Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register DevMaid services
                services.AddDevMaidServices();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Set service provider for static services (for backward compatibility)
        DevMaid.CLI.Services.Logging.Logger.SetServiceProvider(serviceProvider);
        DevMaid.CLI.Services.ConfigurationService.SetServiceProvider(serviceProvider);
        DevMaid.CLI.Services.PostgresDatabaseLister.SetServiceProvider(serviceProvider);

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
