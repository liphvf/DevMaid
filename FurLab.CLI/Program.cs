using FurLab.CLI.Commands.Query;
using FurLab.CLI.Infrastructure;
using FurLab.Core.Services;

using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddFurLabServices();
        services.AddSingleton<CsvExporter>();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("fur");

            config.SetExceptionHandler((ex, resolver) =>
            {
                // Display only the clean error message to the user.
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");

#if DEBUG
                // In debug mode, we still want to see the stack trace for development.
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
#endif

                return ex switch
                {
                    NpgsqlException { SqlState: not null } => 10,
                    NpgsqlException => 11,
                    DirectoryNotFoundException => 21,
                    FileNotFoundException => 22,
                    IOException => 20,
                    UnauthorizedAccessException => 30,
                    ArgumentException => 41,
                    InvalidOperationException => 40,
                    TimeoutException => 50,
                    OperationCanceledException => 130,
                    _ => 1
                };
            });

            config.AddBranch("file", file =>
            {
                file.SetDescription("File utilities.");
                file.AddCommand<Commands.FileUtils.FileCombineCommand>("combine");
            });

            config.AddBranch("claude", claude =>
            {
                claude.SetDescription("Comandos para Claude Code");
                claude.AddCommand<Commands.Claude.ClaudeInstallCommand>("install");
                claude.AddBranch("settings", settings =>
                {
                    settings.SetDescription("Configuracoes do Claude Code");
                    settings.AddCommand<Commands.Claude.Settings.ClaudeMcpDatabaseCommand>("mcp-database");
                    settings.AddCommand<Commands.Claude.Settings.ClaudeWinEnvCommand>("win-env");
                });
            });

            config.AddBranch("opencode", opencode =>
            {
                opencode.SetDescription("Comandos para OpenCode");
                opencode.AddBranch("settings", settings =>
                {
                    settings.SetDescription("Configuracoes do OpenCode");
                    settings.AddCommand<Commands.OpenCode.Settings.OpenCodeMcpDatabaseCommand>("mcp-database");
                    settings.AddCommand<Commands.OpenCode.Settings.OpenCodeDefaultModelCommand>("default-model");
                });
            });

            config.AddBranch("winget", winget =>
            {
                winget.SetDescription("Manage winget packages.");
                winget.AddCommand<Commands.Winget.WingetBackupCommand>("backup");
                winget.AddCommand<Commands.Winget.WingetRestoreCommand>("restore");
            });

            config.AddBranch("database", db =>
            {
                db.SetDescription("Database utilities.");
                db.AddCommand<Commands.Database.DatabaseBackupCommand>("backup");
                db.AddCommand<Commands.Database.DatabaseRestoreCommand>("restore");
                db.AddBranch("pgpass", pgpass =>
                {
                    pgpass.SetDescription("Gerencia o arquivo pgpass.conf");
                    pgpass.AddCommand<Commands.Database.PgPass.PgPassAddCommand>("add");
                    pgpass.AddCommand<Commands.Database.PgPass.PgPassListCommand>("list");
                    pgpass.AddCommand<Commands.Database.PgPass.PgPassRemoveCommand>("remove");
                });
            });

            config.AddBranch("query", query =>
            {
                query.SetDescription("Execute SQL queries and export results to CSV.");
                query.AddCommand<QueryRunCommand>("run");
            });

            config.AddBranch("windowsfeatures", wf =>
            {
                wf.SetDescription("Export and import Windows optional features.");
                wf.AddCommand<Commands.WindowsFeatures.WindowsFeaturesExportCommand>("export");
                wf.AddCommand<Commands.WindowsFeatures.WindowsFeaturesImportCommand>("import");
                wf.AddCommand<Commands.WindowsFeatures.WindowsFeaturesListCommand>("list");
            });

            config.AddBranch("docker", docker =>
            {
                docker.SetDescription("Docker utilities.");
                docker.AddCommand<Commands.Docker.DockerPostgresCommand>("postgres");
            });

            config.AddBranch("settings", settings =>
            {
                settings.SetDescription("Manage FurLab settings and server configurations.");
                settings.AddBranch("db-servers", dbs =>
                {
                    dbs.SetDescription("Manage configured database servers.");
                    dbs.AddCommand<Commands.Settings.DbServers.DbServersListCommand>("ls");
                    dbs.AddCommand<Commands.Settings.DbServers.DbServersAddCommand>("add");
                    dbs.AddCommand<Commands.Settings.DbServers.DbServersRemoveCommand>("rm");
                    dbs.AddCommand<Commands.Settings.DbServers.DbServersTestCommand>("test");
                    dbs.AddCommand<Commands.Settings.DbServers.DbServersSetPasswordCommand>("set-password");
                });
            });
        });

        return await app.RunAsync(args);
    }
}
