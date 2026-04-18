using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Database.PgPass;

/// <summary>
/// Removes a specific entry from the pgpass.conf file.
/// </summary>
public sealed class PgPassRemoveCommand : AsyncCommand<PgPassRemoveCommand.Settings>
{
    private readonly IPgPassService _pgPassService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgPassRemoveCommand"/> class.
    /// </summary>
    /// <param name="pgPassService">The pgpass service.</param>
    public PgPassRemoveCommand(IPgPassService pgPassService)
    {
        _pgPassService = pgPassService;
    }

    /// <summary>
    /// Settings for the pgpass remove command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the database name of the entry to remove (or * for wildcard).
        /// </summary>
        [CommandArgument(0, "<database>")]
        [System.ComponentModel.Description("Database name of the entry to remove (or * for wildcard).")]
        public string Database { get; init; } = string.Empty;

        /// <summary>
        /// Gets the hostname of the entry to remove.
        /// </summary>
        [CommandOption("--host")]
        [System.ComponentModel.Description("Hostname of the entry to remove.")]
        public string? Host { get; init; }

        /// <summary>
        /// Gets the port of the entry to remove.
        /// </summary>
        [CommandOption("--port")]
        [System.ComponentModel.Description("Port of the entry to remove.")]
        public string? Port { get; init; }

        /// <summary>
        /// Gets the username of the entry to remove.
        /// </summary>
        [CommandOption("--username")]
        [System.ComponentModel.Description("Username of the entry to remove.")]
        public string? Username { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(settings.Database))
        {
            AnsiConsole.MarkupLine("[red]Error: the <database> argument is required.[/]");
            return Task.FromResult(2);
        }

        var key = new PgPassEntry
        {
            Hostname = settings.Host ?? "localhost",
            Port = settings.Port ?? "5432",
            Database = settings.Database,
            Username = settings.Username ?? "postgres",
            Password = "placeholder"
        };

        var result = _pgPassService.RemoveEntry(key);

        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
            return Task.FromResult(0);
        }
        else if (result.Message.StartsWith("Entry not found"))
        {
            AnsiConsole.MarkupLine($"[yellow]{result.Message}[/]");
            return Task.FromResult(0);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
            return Task.FromResult(1);
        }
    }
}
