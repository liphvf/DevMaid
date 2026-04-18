using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Database.PgPass;

/// <summary>
/// Adds a new PostgreSQL credential entry to the pgpass.conf file.
/// </summary>
public sealed class PgPassAddCommand : AsyncCommand<PgPassAddCommand.Settings>
{
    private readonly IPgPassService _pgPassService;
    private readonly IPostgresPasswordHandler _passwordHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgPassAddCommand"/> class.
    /// </summary>
    /// <param name="pgPassService">The pgpass service.</param>
    /// <param name="passwordHandler">The PostgreSQL password handler.</param>
    public PgPassAddCommand(IPgPassService pgPassService, IPostgresPasswordHandler passwordHandler)
    {
        _pgPassService = pgPassService;
        _passwordHandler = passwordHandler;
    }

    /// <summary>
    /// Settings for the pgpass add command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the database name (or * for wildcard).
        /// </summary>
        [CommandOption("--database")]
        [System.ComponentModel.Description("Database name (or * for wildcard).")]
        public string? Database { get; init; }

        /// <summary>
        /// Gets the hostname or IP address of the PostgreSQL server.
        /// </summary>
        [CommandOption("--host")]
        [System.ComponentModel.Description("Hostname or IP address of the PostgreSQL server.")]
        public string? Host { get; init; }

        /// <summary>
        /// Gets the TCP port of the PostgreSQL server.
        /// </summary>
        [CommandOption("--port")]
        [System.ComponentModel.Description("TCP port of the PostgreSQL server.")]
        public string? Port { get; init; }

        /// <summary>
        /// Gets the PostgreSQL username.
        /// </summary>
        [CommandOption("--username")]
        [System.ComponentModel.Description("PostgreSQL username.")]
        public string? Username { get; init; }

        /// <summary>
        /// Gets the PostgreSQL password. If not provided, it will be prompted interactively.
        /// </summary>
        [CommandOption("--password")]
        [System.ComponentModel.Description("PostgreSQL password. Prompted interactively if not provided.")]
        public string? Password { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(settings.Database))
        {
            AnsiConsole.MarkupLine("[red]Error: the --database argument is required.[/]");
            return Task.FromResult(2);
        }

        var host = settings.Host ?? "localhost";
        if (!SecurityUtils.IsValidHost(host))
        {
            AnsiConsole.MarkupLine($"[red]Error: invalid host format: \"{host}\".[/]");
            return Task.FromResult(2);
        }

        var port = settings.Port ?? "5432";
        if (!SecurityUtils.IsValidPort(port))
        {
            AnsiConsole.MarkupLine("[red]Error: port must be a number between 1 and 65535.[/]");
            return Task.FromResult(2);
        }

        var username = settings.Username ?? "postgres";

        var password = settings.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = _passwordHandler.ReadPasswordInteractively("Password: ");
        }

        if (string.IsNullOrEmpty(password))
        {
            AnsiConsole.MarkupLine("[red]Error: password cannot be empty.[/]");
            return Task.FromResult(2);
        }

        var entry = new PgPassEntry
        {
            Hostname = host,
            Port = port,
            Database = settings.Database,
            Username = username,
            Password = password
        };

        var result = _pgPassService.AddEntry(entry);

        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
            return Task.FromResult(0);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
            return Task.FromResult(1);
        }
    }
}
