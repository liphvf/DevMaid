using System.Globalization;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using Spectre.Console;

namespace FurLab.CLI.Commands.Query;

/// <summary>
/// Implements <see cref="IProgressObserverService"/> by displaying progress
/// via Spectre.Console for the CLI interface.
/// </summary>
public class ConsoleObserverService : IProgressObserverService
{
    private readonly Table _resultsTable;
    private readonly Lock _tableLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleObserverService"/> class.
    /// </summary>
    public ConsoleObserverService()
    {
        _resultsTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[grey]Status[/]").Centered().Width(8))
            .AddColumn(new TableColumn("[grey]Server[/]"))
            .AddColumn(new TableColumn("[grey]Database[/]"))
            .AddColumn(new TableColumn("[grey]Rows[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]Duration[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]Detail[/]"));
    }

    /// <inheritdoc/>
    public Task OnStartedAsync(QueryExecutionInfo info, CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Executing on [bold]{info.Servers}[/] servers, [bold]{info.Databases}[/] databases...");
        AnsiConsole.WriteLine();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnExecutingAsync(DatabaseExecutionInfo info, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnCompletedAsync(DatabaseResult result, CancellationToken cancellationToken = default)
    {
        lock (_tableLock)
        {
            _resultsTable.AddRow(
                "[green]✓[/]",
                Markup.Escape(result.Server),
                Markup.Escape(result.Database),
                result.RowCount.ToString(CultureInfo.InvariantCulture),
                $"{result.DurationMs / 1000:F1}s",
                "[grey]—[/]");
            AnsiConsole.Write(_resultsTable);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnFailedAsync(DatabaseError error, CancellationToken cancellationToken = default)
    {
        lock (_tableLock)
        {
            _resultsTable.AddRow(
                "[red]✗[/]",
                Markup.Escape(error.Server),
                Markup.Escape(error.Database),
                "[grey]—[/]",
                "[grey]—[/]",
                $"[red]{Markup.Escape(error.Message)}[/]");
            AnsiConsole.Write(_resultsTable);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnFinishedAsync(ExecutionSummary summary, CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Success: {summary.SuccessCount}[/] | [red]Failed: {summary.FailureCount}[/] | Total rows: {summary.TotalRows}");
        return Task.CompletedTask;
    }
}
