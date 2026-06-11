using System.Diagnostics;
using System.Threading.Channels;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;

namespace FurLab.Core.Services;

/// <summary>
/// Executes SQL queries across multiple servers and databases in parallel,
/// with retry logic and cancellation support.
/// </summary>
public class QueryExecutorService
{
    private readonly QueryPlannerService _planner;
    private readonly CsvExporterService _csvExporter;
    private readonly ILogger<QueryExecutorService> _logger;

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
                var handled = args.Outcome.Exception is NpgsqlException or TimeoutException;
                return new ValueTask<bool>(handled);
            }
        })
        .Build();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutorService"/> class.
    /// </summary>
    public QueryExecutorService(
        QueryPlannerService planner,
        CsvExporterService csvExporter,
        ILogger<QueryExecutorService> logger)
    {
        _planner = planner;
        _csvExporter = csvExporter;
        _logger = logger;
    }

    /// <summary>
    /// Executes a query across all planned server/database pairs with parallel execution,
    /// progressive CSV output, and real-time progress reporting.
    /// </summary>
    public async Task ExecuteAsync(
        Models.ExecutionContext context,
        IProgressObserverService observer,
        CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.CancellationTokenSource.Token);

        var allDatabases = context.ServerDatabases;
        var totalDatabases = allDatabases.Count;

        if (totalDatabases == 0)
        {
            _logger.LogWarning("No accessible databases found across selected servers.");
            await observer.OnFinishedAsync(new ExecutionSummary(0, 0, 0, context.OutputDirectory), cts.Token);
            return;
        }

        await observer.OnStartedAsync(
            new QueryExecutionInfo(context.ExecutionId.ToString(), context.ServerDatabases.Select(s => s.Server.Name).Distinct().Count(), totalDatabases, DateTime.UtcNow),
            cts.Token);

        var successCount = 0;
        var failureCount = 0;
        var totalRowCount = 0;

        var channel = Channel.CreateBounded<CsvRow>(
            new BoundedChannelOptions(8)
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
                        var serverFileName = _csvExporter.SanitizeFilename(row.Server);
                        var serverCsvPath = Path.Combine(context.OutputDirectory, $"{serverFileName}_{context.Timestamp}.csv");
                        _csvExporter.AppendToServerCsv(serverCsvPath, row);
                    }
                    else
                    {
                        var errorFilePath = Path.Combine(context.OutputDirectory, $"{context.Timestamp}_erros.csv");
                        _csvExporter.WriteErrorEntry(errorFilePath, row.Server, row.Database, row.ExecutedAt, row.Error);
                    }

                    var logFilePath = Path.Combine(context.OutputDirectory, $"{context.Timestamp}_log.csv");
                    var logEntry = new ExecutionLogEntry(row.Server, row.Database, row.ExecutedAt, row.Status, row.RowCount, row.DurationMs, row.Error);
                    _csvExporter.WriteLogEntry(logFilePath, logEntry);
                }
            }
            catch (Exception ex)
            {
                writerCompleted.TrySetException(ex);
                return;
            }

            writerCompleted.TrySetResult();
        });

        var selectedServers = context.ServerDatabases.Select(s => s.Server).Distinct().ToList();

        try
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cts.Token
            };

            await Parallel.ForEachAsync(selectedServers, parallelOptions, async (server, ct) =>
            {
                var databases = allDatabases
                    .Where(d => d.Server.Name == server.Name)
                    .Select(d => d.Database)
                    .ToList();

                if (databases.Count == 0)
                    return;

                var serverParallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = server.MaxParallelism,
                    CancellationToken = ct
                };

                await Parallel.ForEachAsync(databases, serverParallelOptions, async (database, dbCt) =>
                {
                    try
                    {
                        var connectionString = _planner.BuildConnectionStringForServer(server, database, context.Settings);
                        var executedAt = DateTime.UtcNow;

                        await observer.OnExecutingAsync(new DatabaseExecutionInfo(server.Name, database, executedAt), dbCt);

                        var sw = Stopwatch.StartNew();
                        var queryResult = await ExecuteQueryWithRetryAsync(connectionString, context.SqlQuery, context.CommandTimeout, dbCt);
                        sw.Stop();

                        var row = new CsvRow(server.Name, database, executedAt, "Success", queryResult.Data.Count, string.Empty, sw.Elapsed.TotalMilliseconds, queryResult.ColumnNames, queryResult.Data);
                        await channel.Writer.WriteAsync(row, dbCt);

                        Interlocked.Increment(ref successCount);
                        Interlocked.Add(ref totalRowCount, queryResult.Data.Count);

                        await observer.OnCompletedAsync(
                            new DatabaseResult(server.Name, database, executedAt, "Success", queryResult.Data.Count, string.Empty, sw.Elapsed.TotalMilliseconds, queryResult.ColumnNames, queryResult.Data),
                            dbCt);
                    }
                    catch (Exception ex)
                    {
                        var executedAt = DateTime.UtcNow;
                        var row = new CsvRow(server.Name, database, executedAt, "Error", 0, ex.Message, 0, [], []);
                        await channel.Writer.WriteAsync(row, dbCt);

                        Interlocked.Increment(ref failureCount);

                        await observer.OnFailedAsync(
                            new DatabaseError(server.Name, database, executedAt, ex.Message),
                            dbCt);
                    }
                });
            });
        }
        finally
        {
            channel.Writer.Complete();
            await writerCompleted.Task.WaitAsync(cts.Token);

            _csvExporter.MergeServerCsvsToConsolidated(context.OutputDirectory, context.Timestamp, selectedServers.Select(s => s.Name).ToList());

            await observer.OnFinishedAsync(
                new ExecutionSummary(successCount, failureCount, totalRowCount, context.OutputDirectory),
                cts.Token);
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
    /// Executes a query and returns column names and all data rows as string dictionaries.
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
}
