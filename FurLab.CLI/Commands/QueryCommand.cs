using System.CommandLine;
using System.Globalization;
using System.Text;

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
    /// <returns>The configured <see cref="Command"/>.</returns>
    public static Command Build()
    {
        var command = new Command("query", "Execute SQL queries and export results to CSV.");

        var runCommand = new Command("run", "Run a SQL query and export the results to CSV.");

        // Input/Output options
        var inputOption = new Option<string?>("--input", "-i")
        {
            Description = "Path to the SQL input file."
        };

        var commandOption = new Option<string?>("--command", "-c")
        {
            Description = "Inline SQL query to execute (alternative to --input)."
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Path to the CSV output file (or directory when using --all)."
        };

        // Connection string option (takes precedence over individual parameters)
        var npgsqlConnectionStringOption = new Option<string?>("--npgsql-connection-string")
        {
            Description = "Complete Npgsql connection string (e.g., \"Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass\")."
        };

        // Individual connection parameters
        var hostOption = new Option<string?>("--host", "-h")
        {
            Description = "Database host address."
        };

        var portOption = new Option<string?>("--port", "-p")
        {
            Description = "Database port."
        };

        var databaseOption = new Option<string?>("--database", "-d")
        {
            Description = "Database name. Not required when using --all."
        };

        var usernameOption = new Option<string?>("--username", "-U")
        {
            Description = "Database username."
        };

        var passwordOption = new Option<string?>("--password", "-W")
        {
            Description = "Database password. If not provided, will be prompted interactively."
        };

        // Connection options
        var sslModeOption = new Option<string?>("--ssl-mode")
        {
            Description = "SSL mode (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull). Default: Prefer."
        };

        var timeoutOption = new Option<int?>("--timeout")
        {
            Description = "Connection timeout in seconds. Default: 30."
        };

        var commandTimeoutOption = new Option<int?>("--command-timeout")
        {
            Description = "Command timeout in seconds. Default: 300."
        };

        var poolingOption = new Option<bool?>("--pooling")
        {
            Description = "Enable connection pooling. Default: true."
        };

        var minPoolSizeOption = new Option<int?>("--min-pool-size")
        {
            Description = "Minimum pool size. Default: 1."
        };

        var maxPoolSizeOption = new Option<int?>("--max-pool-size")
        {
            Description = "Maximum pool size. Default: 100."
        };

        var keepaliveOption = new Option<int?>("--keepalive")
        {
            Description = "Keepalive interval in seconds. Default: 0."
        };

        var connectionLifetimeOption = new Option<int?>("--connection-lifetime")
        {
            Description = "Connection lifetime in seconds. Default: 0."
        };

        // Multi-database options
        var allOption = new Option<bool>("--all", "-a")
        {
            Description = "Execute the query on all databases on the server."
        };

        var separateFilesOption = new Option<bool>("--separate-files")
        {
            Description = "Generate a separate CSV file for each server instead of a single consolidated file."
        };

        var excludeOption = new Option<string?>("--exclude")
        {
            Description = "Comma-separated list of database names to exclude (e.g., postgres,template0,template1)."
        };

        // Confirmation option
        var noConfirmOption = new Option<bool>("--no-confirm")
        {
            Description = "Skip confirmation prompt for destructive queries (useful for scripts/CI)."
        };

        // Add all options to the command
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
        runCommand.Add(separateFilesOption);
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
                SeparateFiles = parseResult.GetValue(separateFilesOption),
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
    /// <param name="options">The query execution options.</param>
    /// <exception cref="ArgumentException">Thrown when required options are missing or invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the SQL input file does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the user cancels execution of a destructive query.</exception>
    public static void Run(QueryCommandOptions options)
    {
        // Validate mutual exclusivity between -c and -i
        if (!string.IsNullOrWhiteSpace(options.InlineQuery) && !string.IsNullOrWhiteSpace(options.InputFile))
        {
            throw new ArgumentException("Options -c/--command and -i/--input are mutually exclusive. Use only one.");
        }

        // Get SQL query from inline command or input file
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

        // Get configured servers
        var servers = UserConfigService.GetServers();
        if (servers.Count == 0)
        {
            throw new InvalidOperationException("No servers configured. Run 'settings db-servers add' to add a server first.");
        }

        // Show server selection prompt (all pre-selected)
        var selectedServers = SelectServers(servers);
        if (selectedServers.Count == 0)
        {
            throw new OperationCanceledException("No servers selected. Execution cancelled.");
        }

        // Analyze query for destructive operations
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

        // Execute query on selected servers
        ExecuteOnSelectedServers(selectedServers, sqlQuery, options, querySource, queryTypeDescription).GetAwaiter().GetResult();
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

        // Pre-select all servers by default
        foreach (var server in servers)
        {
            prompt.Select(server.Name);
        }

        var selected = AnsiConsole.Prompt(prompt);
        return servers.Where(s => selected.Contains(s.Name)).ToList();
    }

    /// <summary>
    /// Shows confirmation prompt for destructive queries.
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
    /// Executes query on selected servers with parallel execution and consolidated CSV output.
    /// </summary>
    private static async Task ExecuteOnSelectedServers(List<ServerConfigEntry> selectedServers, string sqlQuery, QueryCommandOptions options, string querySource, string queryTypeDescription)
    {
        var defaults = UserConfigService.GetDefaults();
        var outputDirectory = string.IsNullOrWhiteSpace(options.OutputFile)
            ? defaults.OutputDirectory
            : Path.GetFullPath(options.OutputFile);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);

        AnsiConsole.MarkupLine("[bold]Executing query...[/]");
        AnsiConsole.MarkupLine($"Source: [cyan]{Markup.Escape(querySource)}[/]");
        AnsiConsole.MarkupLine($"Type: [cyan]{Markup.Escape(queryTypeDescription)}[/]");
        AnsiConsole.MarkupLine($"Servers: [cyan]{Markup.Escape(string.Join(", ", selectedServers.Select(s => s.Name)))}[/]");
        AnsiConsole.MarkupLine($"Output: [cyan]{Markup.Escape(outputDirectory)}[/]");
        AnsiConsole.WriteLine();

        var allResults = new List<CsvRow>();
        var lockObj = new object();
        var successCount = 0;
        var failureCount = 0;
        var totalRowCount = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = defaults.MaxParallelism
        };

        await Parallel.ForEachAsync(selectedServers, parallelOptions, async (server, ct) =>
        {
            var databases = await GetDatabasesForServerAsync(server, ct);

            if (databases.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No databases found for server '{Markup.Escape(server.Name)}'.[/]");
                return;
            }

            var serverParallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = server.MaxParallelism,
                CancellationToken = ct
            };

            var serverSuccessCount = 0;
            var serverFailureCount = 0;
            var serverRowCount = 0;

            await Parallel.ForEachAsync(databases, serverParallelOptions, async (database, dbCt) =>
            {
                try
                {
                    var connectionString = BuildConnectionStringForServer(server, database);
                    var executedAt = DateTime.UtcNow;
                    var queryResult = await ExecuteQueryWithRetryAsync(connectionString, sqlQuery, server.CommandTimeout, dbCt);
                    var columnNames = queryResult.ColumnNames;
                    var data = queryResult.Data;

                    lock (lockObj)
                    {
                        allResults.Add(new CsvRow(server.Name, database, executedAt, "Success", data.Count, string.Empty, columnNames, data));
                    }

                    Interlocked.Add(ref serverRowCount, data.Count);
                    Interlocked.Increment(ref serverSuccessCount);
                    Interlocked.Add(ref totalRowCount, data.Count);

                    AnsiConsole.MarkupLine($"  [green]✓ {Markup.Escape(server.Name)}/{Markup.Escape(database)}[/] — [green]Success[/] — {data.Count} rows ({executedAt:HH:mm:ss})");
                }
                catch (Exception ex)
                {
                    var executedAt = DateTime.UtcNow;
                    lock (lockObj)
                    {
                        allResults.Add(new CsvRow(server.Name, database, executedAt, "Error", 0, ex.Message, [], []));
                    }
                    Interlocked.Increment(ref serverFailureCount);
                    Interlocked.Increment(ref failureCount);

                    AnsiConsole.MarkupLine($"  [red]✗ {Markup.Escape(server.Name)}/{Markup.Escape(database)}[/] — [red]Error[/] — {Markup.Escape(ex.Message)} ({executedAt:HH:mm:ss})");
                }
            });

            Interlocked.Add(ref successCount, serverSuccessCount);

            AnsiConsole.MarkupLine($"Server [bold]{Markup.Escape(server.Name)}[/]: [green]{serverSuccessCount} ok[/], [red]{serverFailureCount} failed[/], {serverRowCount} rows");
            AnsiConsole.WriteLine();
        });

        var successResults = allResults.Where(r => r.Status == "Success").ToList();

        if (options.SeparateFiles)
        {
            foreach (var server in selectedServers)
            {
                var serverResults = successResults.Where(r => r.Server == server.Name).ToList();
                if (serverResults.Count == 0) continue;

                var serverOutputFile = Path.Combine(outputDirectory, $"{server.Name}_{timestamp}.csv");
                WriteServerCsv(serverOutputFile, server.Name, serverResults);
                AnsiConsole.MarkupLine($"[green]✓[/] Server [bold]{Markup.Escape(server.Name)}[/] exported to: {Markup.Escape(serverOutputFile)}");
            }
        }
        else if (successResults.Count > 0)
        {
            var outputFile = Path.Combine(outputDirectory, $"consolidated_{timestamp}.csv");
            WriteConsolidatedCsv(outputFile, successResults);
            AnsiConsole.MarkupLine($"[green]✓[/] Results exported to: {Markup.Escape(outputFile)}");
        }

        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Server");
        table.AddColumn("Database");
        table.AddColumn("Status");
        table.AddColumn("Rows");
        table.AddColumn("ExecutedAt");
        table.AddColumn("Error");

        foreach (var result in allResults.OrderBy(r => r.Server).ThenBy(r => r.Database))
        {
            var status = result.Status == "Success"
                ? "[green]✓ Success[/]"
                : "[red]✗ Error[/]";
            var rowCount = result.Status == "Success" ? result.RowCount.ToString() : "-";
            var error = string.IsNullOrEmpty(result.Error) ? "" : Markup.Escape(result.Error);

            table.AddRow(
                Markup.Escape(result.Server),
                Markup.Escape(result.Database),
                status,
                rowCount,
                result.ExecutedAt.ToString("HH:mm:ss"),
                error);
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Servers: [bold]{selectedServers.Count}[/] | [green]Success: {successCount}[/] | [red]Failed: {failureCount}[/] | Total rows: {totalRowCount}");

        if (successCount == 0 && failureCount > 0)
        {
            throw new InvalidOperationException("No server responded successfully. Please check the connections.");
        }
    }

    /// <summary>
    /// Executes a query with Polly retry logic for transient failures.
    /// </summary>
    private static async Task<(List<string> ColumnNames, List<Dictionary<string, string>> Data)> ExecuteQueryWithRetryAsync(string connectionString, string sqlQuery, int commandTimeout, CancellationToken ct)
    {
        return await ResiliencePipeline.ExecuteAsync(async (innerCt) =>
        {
            innerCt.ThrowIfCancellationRequested();
            return await ExecuteQueryAsync(connectionString, sqlQuery, commandTimeout, innerCt);
        }, ct);
    }

    /// <summary>
    /// Executes a query and returns column names and data rows.
    /// </summary>
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

    /// <summary>
    /// Gets list of databases for a server (either explicit or auto-discovered).
    /// Validates access to each database before returning.
    /// </summary>
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

    /// <summary>
    /// Validates access to each database by attempting a simple async connection test.
    /// Returns only databases that are accessible.
    /// </summary>
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

                // Simple validation query
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

    /// <summary>
    /// Lists all databases on a server using Npgsql.
    /// </summary>
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

    /// <summary>
    /// Checks if a database name matches a wildcard pattern.
    /// </summary>
    private static bool MatchesPattern(string dbName, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(dbName, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Builds connection string for a specific server and database.
    /// </summary>
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

    private static void WriteConsolidatedCsv(string outputPath, List<CsvRow> successResults)
        => CsvExporter.WriteConsolidatedCsv(outputPath, successResults);

    private static void WriteServerCsv(string outputPath, string serverName, List<CsvRow> serverResults)
        => CsvExporter.WriteServerCsv(outputPath, serverName, serverResults);
}
