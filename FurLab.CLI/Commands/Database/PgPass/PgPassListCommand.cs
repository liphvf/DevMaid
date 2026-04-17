using FurLab.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Database.PgPass;

/// <summary>
/// Lists all entries in the pgpass.conf file with masked passwords.
/// </summary>
public sealed class PgPassListCommand : AsyncCommand<PgPassListCommand.Settings>
{
    private readonly IPgPassService _pgPassService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgPassListCommand"/> class.
    /// </summary>
    /// <param name="pgPassService">The pgpass service.</param>
    public PgPassListCommand(IPgPassService pgPassService)
    {
        _pgPassService = pgPassService;
    }

    /// <summary>
    /// Settings for the pgpass list command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var entries = _pgPassService.ListEntries().ToList();

        if (entries.Count == 0)
        {
            AnsiConsole.WriteLine("Nenhuma entrada configurada em pgpass.conf.");
            return Task.FromResult(0);
        }

        var table = new Table()
            .Border(TableBorder.Square)
            .Title("[bold]pgpass.conf[/]")
            .AddColumn(new TableColumn("[bold]HOSTNAME[/]"))
            .AddColumn(new TableColumn("[bold]PORTA[/]"))
            .AddColumn(new TableColumn("[bold]BANCO[/]"))
            .AddColumn(new TableColumn("[bold]USUÁRIO[/]"))
            .AddColumn(new TableColumn("[bold]SENHA[/]"));

        foreach (var entry in entries)
        {
            table.AddRow(
                entry.Hostname,
                entry.Port,
                entry.Database,
                entry.Username,
                "****");
        }

        AnsiConsole.Write(table);

        return Task.FromResult(0);
    }
}
