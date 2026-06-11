using Npgsql;

using Testcontainers.PostgreSql;

namespace FurLab.Tests.Api.Integration;

/// <summary>
/// Base class for integration tests that require a PostgreSQL database.
/// Uses Testcontainers to spin up an ephemeral PostgreSQL container with a random port.
/// No volumes are mounted — all data is destroyed when the container is removed.
/// </summary>
public abstract class PostgreSqlIntegrationTestBase
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Gets the connection string for the test PostgreSQL container.
    /// </summary>
    protected string ConnectionString => _container?.GetConnectionString() ?? throw new InvalidOperationException("Container not started. Call InitializeAsync first.");

    /// <summary>
    /// Gets the mapped port exposed by the container (not the default 5432).
    /// </summary>
    protected int MappedPort => _container?.GetMappedPublicPort(5432) ?? throw new InvalidOperationException("Container not started.");

    /// <summary>
    /// Starts a fresh PostgreSQL container and seeds test databases.
    /// </summary>
    protected async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithPortBinding(0, 5432) // random host port
            .WithAutoRemove(true)
            .Build();

        await _container.StartAsync();

        await SeedDatabasesAsync();
    }

    /// <summary>
    /// Stops and removes the PostgreSQL container.
    /// </summary>
    protected async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
            _container = null;
        }
    }

    private async Task SeedDatabasesAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Create test databases
        var dbNames = new[] { "furlab_test_db1", "furlab_test_db2", "furlab_test_db3" };
        foreach (var dbName in dbNames)
        {
            await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\"; CREATE DATABASE \"{dbName}\";", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Seed each database with a simple table
        foreach (var dbName in dbNames)
        {
            var dbConnectionString = ConnectionString.Replace("Database=postgres", $"Database={dbName}");
            await using var dbConn = new NpgsqlConnection(dbConnectionString);
            await dbConn.OpenAsync();

            await using var createTable = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS test_items (id SERIAL PRIMARY KEY, name TEXT); " +
                "INSERT INTO test_items (name) VALUES ('Alice'), ('Bob'), ('Charlie');",
                dbConn);
            await createTable.ExecuteNonQueryAsync();
        }
    }
}
