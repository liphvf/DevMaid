using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using FurLab.Core.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using ExecutionContext = FurLab.Core.Models.ExecutionContext;

namespace FurLab.Tests.Api.Integration;

[TestClass]
[TestCategory("Integration")]
public class QueryExecutorIntegrationTests : PostgreSqlIntegrationTestBase
{
    private QueryExecutorService _executor = null!;
    private TestProgressObserver _observer = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        await InitializeAsync();

        var credentialMock = new Mock<ICredentialService>();
        credentialMock.Setup(x => x.TryDecrypt(It.IsAny<string?>())).Returns("testpassword");

        var planner = new QueryPlannerService(credentialMock.Object, NullLogger<QueryPlannerService>.Instance);
        _executor = new QueryExecutorService(
            planner,
            new CsvExporterService(),
            NullLogger<QueryExecutorService>.Instance);

        _observer = new TestProgressObserver();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await DisposeAsync();
    }

    [TestMethod(DisplayName = "ExecuteAsync_SingleDatabase_SelectReturnsRows")]
    public async Task ExecuteAsync_SingleDatabase_SelectReturnsRows()
    {
        var context = CreateContext("SELECT * FROM test_items", ["furlab_test_db1"]);

        await _executor.ExecuteAsync(context, _observer);

        Assert.AreEqual(1, _observer.CompletedCount);
        Assert.IsTrue(_observer.Results[0].RowCount >= 3);
        Assert.AreEqual(0, _observer.FailedCount);
    }

    [TestMethod(DisplayName = "ExecuteAsync_MultipleDatabases_RunsAll")]
    public async Task ExecuteAsync_MultipleDatabases_RunsAll()
    {
        var context = CreateContext("SELECT * FROM test_items", ["furlab_test_db1", "furlab_test_db2", "furlab_test_db3"]);

        await _executor.ExecuteAsync(context, _observer);

        Assert.AreEqual(3, _observer.CompletedCount);
        Assert.AreEqual(0, _observer.FailedCount);
        Assert.AreEqual(3, _observer.Results.Count);
    }

    [TestMethod(DisplayName = "ExecuteAsync_DestructiveQuery_InsertsData")]
    public async Task ExecuteAsync_DestructiveQuery_InsertsData()
    {
        var context = CreateContext("INSERT INTO test_items (name) VALUES ('IntegrationTest')", ["furlab_test_db1"]);

        await _executor.ExecuteAsync(context, _observer);

        Assert.AreEqual(1, _observer.CompletedCount);
        Assert.AreEqual(0, _observer.FailedCount);

        // Verify data was actually inserted
        var connectionString = ConnectionString.Replace("Database=postgres", "Database=furlab_test_db1");
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new Npgsql.NpgsqlCommand("SELECT COUNT(*) FROM test_items WHERE name = 'IntegrationTest'", connection);
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        Assert.IsTrue(count > 0);
    }

    [TestMethod(DisplayName = "ExecuteAsync_Cancellation_AbortsExecution")]
    public async Task ExecuteAsync_Cancellation_AbortsExecution()
    {
        var context = CreateContext("SELECT pg_sleep(5)", ["furlab_test_db1"]);
        var cts = new CancellationTokenSource();
        context.CancellationTokenSource = cts;

        var task = _executor.ExecuteAsync(context, _observer, cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        try
        {
            await task;
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    private ExecutionContext CreateContext(string sql, List<string> databaseNames)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
        var outputDir = Path.Combine(Path.GetTempPath(), $"furlab_integration_{timestamp}");
        Directory.CreateDirectory(outputDir);

        var serverDatabases = databaseNames.Select(db =>
        {
            var server = new ServerConfigEntry
            {
                Name = "local",
                Host = "localhost",
                Port = MappedPort,
                Username = "postgres",
                EncryptedPassword = null,
                Databases = [db],
                MaxParallelism = 2
            };
            return (server, db);
        }).ToList();

        return new ExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            SqlQuery = sql,
            QuerySource = "integration-test",
            ServerDatabases = serverDatabases,
            OutputDirectory = outputDir,
            Timestamp = timestamp,
            QueryTypeDescription = SqlQueryAnalyzerService.GetQueryTypeDescription(sql),
            IsDestructive = SqlQueryAnalyzerService.AnalyzeQuery(sql) == QueryType.Destructive,
            CommandTimeout = 300,
            Settings = new QueryExecutionSettings(),
            CancellationTokenSource = new CancellationTokenSource()
        };
    }
}

/// <summary>
/// Test implementation of IProgressObserverService that captures all events.
/// </summary>
public class TestProgressObserver : IProgressObserverService
{
    public List<DatabaseResult> Results { get; } = [];
    public List<DatabaseError> Errors { get; } = [];
    public int CompletedCount => Results.Count;
    public int FailedCount => Errors.Count;
    public bool Finished { get; private set; }

    public Task OnStartedAsync(QueryExecutionInfo info, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnExecutingAsync(DatabaseExecutionInfo info, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync(DatabaseResult result, CancellationToken cancellationToken = default)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task OnFailedAsync(DatabaseError error, CancellationToken cancellationToken = default)
    {
        Errors.Add(error);
        return Task.CompletedTask;
    }

    public Task OnFinishedAsync(ExecutionSummary summary, CancellationToken cancellationToken = default)
    {
        Finished = true;
        return Task.CompletedTask;
    }
}
