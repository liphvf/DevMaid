
using FurLab.Core.Interfaces;
using FurLab.Core.Services;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FurLab.Core.HealthChecks;

/// <summary>
/// Health check that verifies PostgreSQL binaries are available in the system.
/// </summary>
public class PostgresBinaryHealthCheck(IConfigurationService configurationService) : IHealthCheck
{
    private readonly IConfigurationService _configurationService = configurationService;

    /// <summary>
    /// Checks the health of PostgreSQL binary availability.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var pgDumpPath = PostgresBinaryLocator.FindPgDump();
        var pgRestorePath = PostgresBinaryLocator.FindPgRestore();
        var psqlPath = PostgresBinaryLocator.FindPsql();

        if (pgDumpPath == null || pgRestorePath == null || psqlPath == null)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL binaries not found. Please ensure PostgreSQL is installed and in PATH.");
        }

        return HealthCheckResult.Healthy(
            $"PostgreSQL binaries found: pg_dump={pgDumpPath}, pg_restore={pgRestorePath}, psql={psqlPath}");
    }
}
