
using DevMaid.Core.Interfaces;
using DevMaid.Core.Models;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevMaid.Core.HealthChecks;

public class DatabaseConnectionHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;

    public DatabaseConnectionHealthCheck(
        IConfigurationService configurationService,
        IDatabaseService databaseService)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var dbConfig = _configurationService.GetDatabaseConfig();
        var host = dbConfig.Host ?? "localhost";
        var port = dbConfig.Port ?? "5432";

        var connectionOptions = new DatabaseConnectionOptions
        {
            Host = host,
            Port = port,
            Username = dbConfig.Username,
            Password = dbConfig.Password
        };

        try
        {
            var isConnected = await _databaseService.TestConnectionAsync(connectionOptions, cancellationToken);

            if (isConnected)
            {
                return HealthCheckResult.Healthy($"Successfully connected to PostgreSQL at {host}:{port}");
            }

            return HealthCheckResult.Unhealthy("Failed to connect to PostgreSQL server.");
        }
        catch (System.Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Database connection failed: {ex.Message}",
                ex);
        }
    }
}
