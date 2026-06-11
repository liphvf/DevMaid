using System.Globalization;
using System.Text;
using FurLab.Core.Constants;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using FurLab.Core.Services;
using Npgsql;
using Spectre.Console;
using Spectre.Console.Cli;

using ExecutionContext = FurLab.Core.Models.ExecutionContext;

namespace FurLab.CLI.Commands.Query.Run;

/// <summary>
/// Executes a SQL query across one or more configured servers and exports results to CSV.
/// Supports inline queries and file-based input, server selection, destructive query confirmation,
/// Channels-based parallel execution, Polly retry, and progressive CSV output.
/// </summary>
public sealed class QueryRunCommand : AsyncCommand<QueryRunSettings>
{
    private readonly IUserConfigService _userConfigService;
    private readonly ICredentialService _credentialService;
    private readonly CsvExporterService _csvExporter;
    private readonly QueryPlannerService _queryPlanner;
    private readonly QueryExecutorService _queryExecutor;
    private readonly ConsoleObserverService _consoleObserver;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryRunCommand"/> class.
    /// </summary>
    public QueryRunCommand(
        IUserConfigService userConfigService,
        ICredentialService credentialService,
        CsvExporterService csvExporter,
        QueryPlannerService queryPlanner,
        QueryExecutorService queryExecutor,
        ConsoleObserverService consoleObserver)
    {
        _userConfigService = userConfigService;
        _credentialService = credentialService;
        _csvExporter = csvExporter;
        _queryPlanner = queryPlanner;
        _queryExecutor = queryExecutor;
        _consoleObserver = consoleObserver;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, QueryRunSettings settings, CancellationToken cancellation)
    {
        if (!string.IsNullOrWhiteSpace(settings.Command) && !string.IsNullOrWhiteSpace(settings.Input))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Options -c/--command and -i/--input are mutually exclusive. Use only one.");
            return 1;
        }

        string sqlQuery;
        string querySource;

        if (!string.IsNullOrWhiteSpace(settings.Command))
        {
            sqlQuery = UnescapeInlineQuery(settings.Command);
            querySource = "inline query";
        }
        else if (!string.IsNullOrWhiteSpace(settings.Input))
        {
            if (!SecurityUtils.IsValidPath(settings.Input))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid input path: '{settings.Input.EscapeMarkup()}'. Path traversal not allowed.");
                return 1;
            }

            var inputFullPath = Path.GetFullPath(settings.Input);
            if (!File.Exists(inputFullPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] SQL input file not found: {inputFullPath.EscapeMarkup()}");
                return 1;
            }

            sqlQuery = File.ReadAllText(inputFullPath, Encoding.UTF8);
            querySource = inputFullPath;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Use -c for an inline query or -i for a SQL file.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] SQL query is empty.");
            return 1;
        }

        var queryType = SqlQueryAnalyzerService.AnalyzeQuery(sqlQuery);
        var queryTypeDescription = SqlQueryAnalyzerService.GetQueryTypeDescription(sqlQuery);

        List<ServerConfigEntry> selectedServers;

        if (!string.IsNullOrWhiteSpace(settings.NpgsqlConnectionString))
        {
            var adHocServer = CreateAdHocServer(settings);
            selectedServers = [adHocServer];
        }
        else
        {
            var servers = _userConfigService.GetServers();
            if (servers.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No servers configured. Run 'settings db-servers add' to add a server first.");
                return 1;
            }

            selectedServers = SelectServers(servers);
            if (selectedServers.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No servers selected. Execution cancelled.[/]");
                return 1;
            }

            if (settings.All)
            {
                foreach (var server in selectedServers)
                {
                    server.FetchAllDatabases = true;
                }
            }
        }

        var excludeNames = ParseExcludeList(settings.Exclude);

        if (queryType == QueryType.Destructive && !settings.NoConfirm)
        {
            var defaults = _userConfigService.GetDefaults();
            if (defaults.RequireConfirmation)
            {
                var databaseCount = selectedServers.Sum(s => s.FetchAllDatabases ? 1 : Math.Max(s.Databases.Count, 1));
                if (!ConfirmDestructiveQuery(queryTypeDescription, selectedServers, databaseCount, sqlQuery))
                {
                    AnsiConsole.MarkupLine("[yellow]Query execution cancelled by user.[/]");
                    return 1;
                }
            }
        }

        try
        {
            await ExecuteOnSelectedServers(selectedServers, sqlQuery, settings, querySource, queryTypeDescription, excludeNames, cancellation);
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }

    /// <summary>
    /// Creates an ad-hoc <see cref="ServerConfigEntry"/> from the connection string settings.
    /// Used when <c>--npgsql-connection-string</c> is provided, bypassing server selection.
    /// Individual connection options (<c>--host</c>, <c>--port</c>, etc.) override the parsed connection string values.
    /// </summary>
    private static ServerConfigEntry CreateAdHocServer(QueryRunSettings settings)
    {
        var builder = new NpgsqlConnectionStringBuilder(settings.NpgsqlConnectionString);

        if (!string.IsNullOrWhiteSpace(settings.Host))
        {
            builder.Host = settings.Host;
        }

        if (!string.IsNullOrWhiteSpace(settings.Port))
        {
            builder.Port = int.TryParse(settings.Port, out var port) ? port : builder.Port;
        }

        if (!string.IsNullOrWhiteSpace(settings.Database))
        {
            builder.Database = settings.Database;
        }

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            builder.Username = settings.Username;
        }

        if (!string.IsNullOrWhiteSpace(settings.Password))
        {
            builder.Password = settings.Password;
        }

        if (!string.IsNullOrWhiteSpace(settings.SslMode))
        {
            builder.SslMode = ParseSslMode(settings.SslMode);
        }

        if (settings.Timeout.HasValue)
        {
            builder.Timeout = settings.Timeout.Value;
        }

        if (settings.CommandTimeout.HasValue)
        {
            builder.CommandTimeout = settings.CommandTimeout.Value;
        }

        if (settings.Pooling.HasValue)
        {
            builder.Pooling = settings.Pooling.Value;
        }

        if (settings.MinPoolSize.HasValue)
        {
            builder.MinPoolSize = settings.MinPoolSize.Value;
        }

        if (settings.MaxPoolSize.HasValue)
        {
            builder.MaxPoolSize = settings.MaxPoolSize.Value;
        }

        if (settings.Keepalive.HasValue)
        {
            builder["keepalive"] = settings.Keepalive.Value;
        }

        if (settings.ConnectionLifetime.HasValue)
        {
            builder["connection lifetime"] = settings.ConnectionLifetime.Value;
        }

        var database = builder.Database ?? "postgres";

        return new ServerConfigEntry
        {
            Name = builder.Host ?? "ad-hoc",
            Host = builder.Host ?? FurLabConstants.DefaultHost,
            Port = builder.Port,
            Username = builder.Username ?? string.Empty,
            EncryptedPassword = null,
            Databases = [database],
            FetchAllDatabases = settings.All,
            SslMode = builder.SslMode.ToString(),
            Timeout = builder.Timeout,
            MaxParallelism = 4
        };
    }

    /// <summary>
    /// Parses a comma-separated exclude list into a set of database names (trimmed, empty entries removed).
    /// </summary>
    private static HashSet<string> ParseExcludeList(string? exclude)
    {
        if (string.IsNullOrWhiteSpace(exclude))
        {
            return [];
        }

        return new HashSet<string>(
            exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Unescapes an inline SQL query string received from the command line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On Windows, the shell and <c>System.CommandLine</c> do not always strip
    /// the outer quotes from arguments, especially when the value contains spaces
    /// or special characters. This method performs a best-effort strip of a single
    /// outer quote pair (either <c>"…"</c> or <c>'…'</c>) and then unescapes
    /// any backslash-escaped quotes inside the value.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description><c>"SELECT 1"</c> → <c>SELECT 1</c></description></item>
    ///   <item><description><c>'SELECT 1'</c> → <c>SELECT 1</c></description></item>
    ///   <item><description><c>SELECT \"name\" FROM t</c> → <c>SELECT "name" FROM t</c></description></item>
    ///   <item><description><c>SELECT 1</c> (no outer quotes) → <c>SELECT 1</c> (unchanged)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    internal static string UnescapeInlineQuery(string query)
    {
        if ((query.StartsWith('"') && query.EndsWith('"')) ||
            (query.StartsWith('\'') && query.EndsWith('\'')))
        {
            query = query.Substring(1, query.Length - 2);
        }

        query = query.Replace("\\\"", "\"").Replace("\\'", "'");
        return query;
    }

    /// <summary>
    /// Shows interactive server selection prompt with all servers pre-selected.
    /// If only one server is configured, returns it directly without a prompt.
    /// </summary>
    private static List<ServerConfigEntry> SelectServers(IReadOnlyList<ServerConfigEntry> servers)
    {
        if (servers.Count == 1)
        {
            return [servers[0]];
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select servers to execute query on:")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more servers)[/]")
            .InstructionsText("[grey](Press <space> to toggle, <enter> to accept)[/]")
            .AddChoices(servers.Select(s => s.Name));

        foreach (var server in servers)
        {
            prompt.Select(server.Name);
        }

        var selected = AnsiConsole.Prompt(prompt);
        return servers.Where(s => selected.Contains(s.Name)).ToList();
    }

    /// <summary>
    /// Shows a confirmation prompt for destructive queries, displaying the query type,
    /// number of affected servers and databases, and a preview of the SQL.
    /// </summary>
    private static bool ConfirmDestructiveQuery(string queryType, List<ServerConfigEntry> selectedServers, int databaseCount, string sqlQuery)
    {
        var preview = sqlQuery.Length > 200 ? sqlQuery.Substring(0, 200) + "..." : sqlQuery;

        var table = new Table();
        table.AddColumn("Property");
        table.AddColumn("Value");
        table.AddRow("Query Type", $"[red]{queryType}[/]");
        table.AddRow("Servers Affected", $"[yellow]{selectedServers.Count}[/]");
        table.AddRow("Databases Affected", $"[yellow]~{databaseCount}[/]");
        table.AddRow("Preview", $"[grey]{Markup.Escape(preview)}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold red]WARNING: DESTRUCTIVE QUERY DETECTED[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        return AnsiConsole.Confirm("Proceed with execution?");
    }

    /// <summary>
    /// Executes the query on all selected servers by delegating to the core query services.
    /// </summary>
    private async Task ExecuteOnSelectedServers(
        List<ServerConfigEntry> selectedServers,
        string sqlQuery,
        QueryRunSettings settings,
        string querySource,
        string queryTypeDescription,
        HashSet<string> excludeNames,
        CancellationToken cancellationToken)
    {
        var defaults = _userConfigService.GetDefaults();
        var baseOutputDirectory = string.IsNullOrWhiteSpace(settings.Output)
            ? defaults.OutputDirectory
            : Path.GetFullPath(settings.Output);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
        var executionDirectory = Path.Combine(baseOutputDirectory, timestamp);

        if (!Directory.Exists(executionDirectory))
        {
            Directory.CreateDirectory(executionDirectory);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var allDatabases = new List<(ServerConfigEntry Server, string Database)>();
        var hasAutoDiscover = selectedServers.Any(s => s.FetchAllDatabases);

        if (hasAutoDiscover)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("grey"))
                .StartAsync("Discovering databases...", async ctx =>
                {
                    foreach (var server in selectedServers)
                    {
                        ctx.Status($"Discovering databases on [bold]{Markup.Escape(server.Name)}[/]...");
                        var databases = await _queryPlanner.GetDatabasesForServerAsync(server, excludeNames, cts.Token);
                        foreach (var db in databases)
                        {
                            allDatabases.Add((server, db));
                        }
                    }
                });
        }
        else
        {
            foreach (var server in selectedServers)
            {
                var databases = await _queryPlanner.GetDatabasesForServerAsync(server, excludeNames, cts.Token);
                foreach (var db in databases)
                {
                    allDatabases.Add((server, db));
                }
            }
        }

        var totalDatabases = allDatabases.Count;

        if (totalDatabases == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No accessible databases found across selected servers.[/]");
            return;
        }

        var executionContext = new ExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            SqlQuery = sqlQuery,
            QuerySource = querySource,
            ServerDatabases = allDatabases,
            OutputDirectory = executionDirectory,
            Timestamp = timestamp,
            QueryTypeDescription = queryTypeDescription,
            IsDestructive = SqlQueryAnalyzerService.AnalyzeQuery(sqlQuery) == QueryType.Destructive,
            CommandTimeout = settings?.CommandTimeout ?? 300,
            Settings = MapSettings(settings),
            CancellationTokenSource = cts
        };

        await _queryExecutor.ExecuteAsync(executionContext, _consoleObserver, cts.Token);

        var errorFilePath = Path.Combine(executionDirectory, $"{timestamp}_erros.csv");
        var logFilePath = Path.Combine(executionDirectory, $"{timestamp}_log.csv");
        var consolidatedPath = Path.Combine(executionDirectory, $"consolidated_{timestamp}.csv");

        AnsiConsole.WriteLine();

        if (File.Exists(consolidatedPath))
        {
            AnsiConsole.MarkupLine($"[green]✅ Consolidated →[/] {Markup.Escape(consolidatedPath)}");
        }

        if (File.Exists(errorFilePath))
        {
            AnsiConsole.MarkupLine($"[red]❌ Errors       →[/] {Markup.Escape(errorFilePath)}");
        }

        AnsiConsole.MarkupLine($"[grey]Log           →[/] {Markup.Escape(logFilePath)}");
        AnsiConsole.WriteLine();
    }

    private static QueryExecutionSettings? MapSettings(QueryRunSettings? settings)
    {
        if (settings == null) return null;

        return new QueryExecutionSettings
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password,
            SslMode = settings.SslMode,
            Timeout = settings.Timeout,
            CommandTimeout = settings.CommandTimeout,
            All = settings.All,
            Exclude = settings.Exclude,
            NoConfirm = settings.NoConfirm
        };
    }

    private static SslMode ParseSslMode(string? sslMode)
    {
        if (!string.IsNullOrWhiteSpace(sslMode) && Enum.TryParse<SslMode>(sslMode, true, out var result))
        {
            return result;
        }

        return SslMode.Prefer;
    }

    private static string? ResolveSetting(string? overrideValue, string configValue)
    {
        return string.IsNullOrWhiteSpace(overrideValue) ? configValue : overrideValue;
    }

    private static int ResolvePort(string? overrideValue, int configValue)
    {
        return int.TryParse(overrideValue, out var port) ? port : configValue;
    }
}
