using System.CommandLine;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;
using FurLab.Core.Models;

using Npgsql;
using Polly;
using Polly.Retry;
using Spectre.Console;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides commands for executing SQL queries and exporting results to CSV.
/// </summary>
public static class QueryCommand
{
    private static readonly ResiliencePipeline ResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            DelayGenerator = static args =>
            {
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, args.AttemptNumber) * 500);
                return new ValueTask<TimeSpan?>(delay);
            },
            ShouldHandle = static args =>
            {
                var handled = args.Outcome.Exception is NpgsqlException or TimeoutException or OperationCanceledException;
                return new ValueTask<bool>(handled);
            }
        })
        .Build();

        /// <summary>
        /// Builds the query command structure.
        /// </summary>
        public static Command Build()
    {
        var command = new Command("query", "Execute SQL queries and export results to CSV.");
        var runCommand = new Command("run", "Run a SQL query and export the results to CSV.");

        var inputOption = new Option<string?>("--input", "-i") { Description = "Path to the SQL input file." };
        var commandOption = new Option<string?>("--command", "-c") { Description = "Inline SQL query to execute (alternative to --input)." };
        var outputOption = new Option<string?>("--output", "-o") { Description = "Path to the output directory." };
        var npgsqlConnectionStringOption = new Option<string?>("--npgsql-connection-string") { Description = "Complete Npgsql connection string." };
        var hostOption = new Option<string?>("--host", "-h") { Description = "Database host address." };
        var portOption = new Option<string?>("--port", "-p") { Description = "Database port." };
        var databaseOption = new Option<string?>("--database", "-d") { Description = "Database name." };
        var usernameOption = new Option<string?>("--username", "-U") { Description = "Database username." };
        var passwordOption = new Option<string?>("--password", "-W") { Description = "Database password." };
        var sslModeOption = new Option<string?>("--ssl-mode") { Description = "SSL mode. Default: Prefer." };
        var timeoutOption = new Option<int?>("--timeout") { Description = "Connection timeout in seconds. Default: 30." };
        var commandTimeoutOption = new Option<int?>("--command-timeout") { Description = "Command timeout in seconds. Default: 300." };
        var poolingOption = new Option<bool?>("--pooling") { Description = "Enable connection pooling. Default: true." };
        var minPoolSizeOption = new Option<int?>("--min-pool-size") { Description = "Minimum pool size. Default: 1." };
        var maxPoolSizeOption = new Option<int?>("--max-pool-size") { Description = "Maximum pool size. Default: 100." };
        var keepaliveOption = new Option<int?>("--keepalive") { Description = "Keepalive interval in seconds. Default: 0." };
        var connectionLifetimeOption = new Option<int?>("--connection-lifetime") { Description = "Connection lifetime in seconds. Default: 0." };
        var allOption = new Option<bool>("--all", "-a") { Description = "Execute the query on all databases on the server." };
        var excludeOption = new Option<string?>("--exclude") { Description = "Comma-separated list of database names to exclude." };
        var noConfirmOption = new Option<bool>("--no-confirm") { Description = "Skip confirmation prompt for destructive queries." };

        runCommand.Add(inputOption);
        runCommand.Add(commandOption);
        runCommand.Add(outputOption);
        runCommand.Add(npgsqlConnectionStringOption);
        runCommand.Add(hostOption);
        runCommand.Add(portOption);
        runCommand.Add(databaseOption);
        runCommand.Add(usernameOption);
        runCommand.Add(passwordOption);
        runCommand.Add(sslModeOption);
        runCommand.Add(timeoutOption);
        runCommand.Add(commandTimeoutOption);
        runCommand.Add(poolingOption);
        runCommand.Add(minPoolSizeOption);
        runCommand.Add(maxPoolSizeOption);
        runCommand.Add(keepaliveOption);
        runCommand.Add(connectionLifetimeOption);
        runCommand.Add(allOption);
        runCommand.Add(excludeOption);
        runCommand.Add(noConfirmOption);

        runCommand.SetAction(parseResult =>
        {
            var options = new QueryCommandOptions
            {
                InputFile = parseResult.GetValue(inputOption) ?? string.Empty,
                InlineQuery = parseResult.GetValue(commandOption) ?? string.Empty,
                OutputFile = parseResult.GetValue(outputOption) ?? string.Empty,
                NpgsqlConnectionString = parseResult.GetValue(npgsqlConnectionStringOption),
                Host = parseResult.GetValue(hostOption),
                Port = parseResult.GetValue(portOption),
                Database = parseResult.GetValue(databaseOption),
                Username = parseResult.GetValue(usernameOption),
                Password = parseResult.GetValue(passwordOption),
                SslMode = parseResult.GetValue(sslModeOption),
                Timeout = parseResult.GetValue(timeoutOption),
                CommandTimeout = parseResult.GetValue(commandTimeoutOption),
                Pooling = parseResult.GetValue(poolingOption),
                MinPoolSize = parseResult.GetValue(minPoolSizeOption),
                MaxPoolSize = parseResult.GetValue(maxPoolSizeOption),
                Keepalive = parseResult.GetValue(keepaliveOption),
                ConnectionLifetime = parseResult.GetValue(connectionLifetimeOption),
                All = parseResult.GetValue(allOption),
                Exclude = parseResult.GetValue(excludeOption),
                NoConfirm = parseResult.GetValue(noConfirmOption)
            };

            Run(options);
        });

        command.Add(runCommand);
        return command;
    }

        /// <summary>
        /// Executes a SQL query and exports the results to CSV.
        /// </summary>
        public static void Run(QueryCommandOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.InlineQuery) && !string.IsNullOrWhiteSpace(options.InputFile))
        {
            throw new ArgumentException("Options -c/--command and -i/--input are mutually exclusive. Use only one.");
        }

        string sqlQuery;
        string querySource;
        if (!string.IsNullOrWhiteSpace(options.InlineQuery))
        {
            sqlQuery = UnescapeInlineQuery(options.InlineQuery);
            querySource = "inline query";
        }
        else if (!string.IsNullOrWhiteSpace(options.InputFile))
        {
            if (!SecurityUtils.IsValidPath(options.InputFile))
            {
                throw new ArgumentException($"Invalid input path: '{options.InputFile}'. Path traversal not allowed.");
            }

            var inputFullPath = Path.GetFullPath(options.InputFile);
            if (!File.Exists(inputFullPath))
            {
                throw new FileNotFoundException($"SQL input file not found: {inputFullPath}", inputFullPath);
            }

            sqlQuery = File.ReadAllText(inputFullPath, Encoding.UTF8);
            querySource = inputFullPath;
        }
        else
        {
            throw new ArgumentException("Use -c for an inline query or -i for a SQL file.");
        }

        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new ArgumentException("SQL query is empty.");
        }

        var servers = UserConfigService.GetServers();
        if (servers.Count == 0)
        {
            throw new InvalidOperationException("No servers configured. Run 'settings db-servers add' to add a server first.");
        }

        var selectedServers = SelectServers(servers);
        if (selectedServers.Count == 0)
        {
            throw new OperationCanceledException("No servers selected. Execution cancelled.");
        }

        var queryType = SqlQueryAnalyzer.AnalyzeQuery(sqlQuery);
        var queryTypeDescription = SqlQueryAnalyzer.GetQueryTypeDescription(sqlQuery);

        if (queryType == QueryType.Destructive && !options.NoConfirm)
        {
            var defaults = UserConfigService.GetDefaults();
            if (defaults.RequireConfirmation)
            {
                var databaseCount = selectedServers.Sum(s => s.FetchAllDatabases ? 1 : Math.Max(s.Databases.Count, 1));
                if (!ConfirmDestructiveQuery(queryTypeDescription, selectedServers, databaseCount, sqlQuery))
                {
                    throw new OperationCanceledException("Query execution cancelled by user.");
                }
            }
        }

        ExecuteOnSelectedServers(selectedServers, sqlQuery, options, querySource, queryTypeDescription).GetAwaiter().GetResult();
    }

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

    private static async Task ExecuteOnSelectedServers(List<ServerConfigEntry> selectedServers, string sqlQuery, QueryCommandOptions options, string querySource, string queryTypeDescription)
    {
        var defaults = UserConfigService.GetDefaults();
        var baseOutputDirectory = string.IsNullOrWhiteSpace(options.OutputFile)
            ? defaults.OutputDirectory
            : Path.GetFullPath(options.OutputFile);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
        var executionDirectory = Path.Combine(baseOutputDirectory, timestamp);

        if (!Directory.Exists(executionDirectory))
        {
            Directory.CreateDirectory(executionDirectory);
        }

        var errorFilePath = Path.Combine(executionDirectory, $"{timestamp}_erros.csv");
        var logFilePath = Path.Combine(executionDirectory, $"{timestamp}_log.csv");

        var allDatabases = new List<(ServerConfigEntry Server, string Database)>();
        foreach (var server in selectedServers)
        {
            var databases = await GetDatabasesForServerAsync(server, CancellationToken.None);
            foreach (var db in databases)
            {
                allDatabases.Add((server, db));
            }
        }

        var totalDatabases = allDatabases.Count;

        if (totalDatabases == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No accessible databases found across selected servers.[/]");
            return;
        }

        var successCount = 0;
        var failureCount = 0;
        var totalRowCount = 0;

        var channel = Channel.CreateBounded<CsvRow>(
            new BoundedChannelOptions(defaults.MaxParallelism * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });

        var writerCompleted = new TaskCompletionSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var row in channel.Reader.ReadAllAsync())
                {
                    if (row.Status == "Success")
                    {
                        var serverFileName = CsvExporter.SanitizeFilename(row.Server);
                        var serverCsvPath = Path.Combine(executionDirectory, $"{serverFileName}_{timestamp}.csv");
                        CsvExporter.AppendToServerCsv(serverCsvPath, row);
                    }
                    else
                    {
                        CsvExporter.WriteErrorEntry(errorFilePath, row.Server, row.Database, row.ExecutedAt, row.Error);
                    }

                    var logEntry = new ExecutionLogEntry(row.Server, row.Database, row.ExecutedAt, row.Status, row.RowCount, row.DurationMs, row.Error);
                    CsvExporter.WriteLogEntry(logFilePath, logEntry);
                }
            }
            finally
            {
                writerCompleted.SetResult();
            }
        });

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask($"[bold]Executing across {selectedServers.Count} servers, {totalDatabases} databases[/]", maxValue: totalDatabases);

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = defaults.MaxParallelism
                };

                var completedCount = 0;

                await Parallel.ForEachAsync(selectedServers, parallelOptions, async (server, ct) =>
                {
                    var databases = allDatabases
                        .Where(d => d.Server.Name == server.Name)
                        .Select(d => d.Database)
                        .ToList();

                    if (databases.Count == 0)
                    {
                        ctx.AddTask($"[yellow]{Markup.Escape(server.Name)}: no databases[/]").StopTask();
                        return;
                    }

                    var serverParallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = server.MaxParallelism,
                        CancellationToken = ct
                    };

                    await Parallel.ForEachAsync(databases, serverParallelOptions, async (database, dbCt) =>
                    {
                        try
                        {
                            var connectionString = BuildConnectionStringForServer(server, database);
                            var executedAt = DateTime.UtcNow;
                            var sw = Stopwatch.StartNew();
                            var queryResult = await ExecuteQueryWithRetryAsync(connectionString, sqlQuery, server.CommandTimeout, dbCt);
                            sw.Stop();

                            var row = new CsvRow(server.Name, database, executedAt, "Success", queryResult.Data.Count, string.Empty, sw.Elapsed.TotalMilliseconds, queryResult.ColumnNames, queryResult.Data);
                            await channel.Writer.WriteAsync(row, dbCt);

                            Interlocked.Increment(ref successCount);
                            Interlocked.Add(ref totalRowCount, queryResult.Data.Count);

                            ctx.AddTask($"  [green]✓ {Markup.Escape(server.Name)}/{Markup.Escape(database)}[/] — {queryResult.Data.Count} rows ({sw.Elapsed.TotalSeconds:F1}s)").StopTask();
                        }
                        catch (Exception ex)
                        {
                            var executedAt = DateTime.UtcNow;
                            var row = new CsvRow(server.Name, database, executedAt, "Error", 0, ex.Message, 0, [], []);
                            await channel.Writer.WriteAsync(row, dbCt);

                            Interlocked.Increment(ref failureCount);

                            ctx.AddTask($"  [red]✗ {Markup.Escape(server.Name)}/{Markup.Escape(database)}[/] — {Markup.Escape(ex.Message)}").StopTask();
                        }

                        Interlocked.Increment(ref completedCount);
                        progressTask.Increment(1);
                    });
                });

                progressTask.StopTask();
            });

        channel.Writer.Complete();
        await writerCompleted.Task;

        CsvExporter.MergeServerCsvsToConsolidated(executionDirectory, timestamp, selectedServers.Select(s => s.Name).ToList());

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
        AnsiConsole.MarkupLine($"Servers: [bold]{selectedServers.Count}[/] | [green]Success: {successCount}[/] | [red]Failed: {failureCount}[/] | Total rows: {totalRowCount}");

        if (successCount == 0 && failureCount > 0)
        {
            throw new InvalidOperationException("No server responded successfully. Please check the connections.");
        }
    }

    private static async Task<(List<string> ColumnNames, List<Dictionary<string, string>> Data)> ExecuteQueryWithRetryAsync(string connectionString, string sqlQuery, int commandTimeout, CancellationToken ct)
    {
        return await ResiliencePipeline.ExecuteAsync(async (innerCt) =>
        {
            innerCt.ThrowIfCancellationRequested();
            return await ExecuteQueryAsync(connectionString, sqlQuery, commandTimeout, innerCt);
        }, ct);
    }

    private static async Task<(List<string> ColumnNames, List<Dictionary<string, string>> Data)> ExecuteQueryAsync(string connectionString, string sqlQuery, int commandTimeout, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(sqlQuery, connection)
        {
            CommandTimeout = commandTimeout
        };

        await using var reader = await command.ExecuteReaderAsync(ct);

        var columnNames = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

        var data = new List<Dictionary<string, string>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                row[columnNames[i]] = value;
            }
            data.Add(row);
        }

        return (columnNames, data);
    }

    private static async Task<List<string>> GetDatabasesForServerAsync(ServerConfigEntry server, CancellationToken ct)
    {
        if (!server.FetchAllDatabases && server.Databases.Count > 0)
        {
            return await ValidateDatabaseAccessAsync(server, server.Databases, ct);
        }

        if (server.FetchAllDatabases)
        {
            try
            {
                var discoveredDatabases = await ListDatabasesAsync(server, ct);
                return await ValidateDatabaseAccessAsync(server, discoveredDatabases, ct);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Auto-discovery failed for '{server.Name}': {ex.Message}[/]");
                if (server.Databases.Count > 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Falling back to configured databases.[/]");
                    return await ValidateDatabaseAccessAsync(server, server.Databases, ct);
                }
                return [];
            }
        }

        var defaultDb = server.Databases.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrEmpty(defaultDb))
        {
            return [];
        }

        return await ValidateDatabaseAccessAsync(server, [defaultDb], ct);
    }

    private static async Task<List<string>> ValidateDatabaseAccessAsync(ServerConfigEntry server, List<string> databases, CancellationToken ct)
    {
        var accessibleDatabases = new List<string>();

        foreach (var database in databases)
        {
            if (string.IsNullOrWhiteSpace(database))
            {
                continue;
            }

            try
            {
                var connectionString = BuildConnectionStringForServer(server, database);
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(ct);

                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(ct);

                accessibleDatabases.Add(database);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Cannot access database '{database}' on server '{server.Name}': {ex.Message}[/]");
            }
        }

        return accessibleDatabases;
    }

    private static async Task<List<string>> ListDatabasesAsync(ServerConfigEntry server, CancellationToken ct)
    {
        var connectionString = BuildConnectionStringForServer(server, "postgres");
        var databases = new List<string>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true",
            connection);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dbName = reader.GetString(0);
            if (!server.ExcludePatterns.Any(pattern => MatchesPattern(dbName, pattern)))
            {
                databases.Add(dbName);
            }
        }

        return databases;
    }

    private static bool MatchesPattern(string dbName, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(dbName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string BuildConnectionStringForServer(ServerConfigEntry server, string database)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = server.Host,
            Port = server.Port,
            Database = database,
            Username = server.Username,
            Password = server.Password,
            SslMode = ParseSslMode(server.SslMode),
            Timeout = server.Timeout,
            CommandTimeout = server.CommandTimeout,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 100
        };

        return builder.ConnectionString;
    }

    private static SslMode ParseSslMode(string sslMode)
    {
        if (Enum.TryParse<SslMode>(sslMode, true, out var result))
        {
            return result;
        }

        return SslMode.Prefer;
    }
}
