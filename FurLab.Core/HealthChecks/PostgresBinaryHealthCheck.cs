
using FurLab.Core.Interfaces;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FurLab.Core.HealthChecks;

/// <summary>
/// Health check that verifies PostgreSQL binaries are available in the system.
/// </summary>
public class PostgresBinaryHealthCheck(
    IPostgresBinaryLocator postgresBinaryLocator) : IHealthCheck
{
    private readonly IPostgresBinaryLocator _postgresBinaryLocator = postgresBinaryLocator;

    /// <summary>
    /// Checks the health of PostgreSQL binary availability.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var pgDumpPath = _postgresBinaryLocator.FindPgDump();
        var pgRestorePath = _postgresBinaryLocator.FindPgRestore();
        var psqlPath = _postgresBinaryLocator.FindPsql();

        if (pgDumpPath == null || pgRestorePath == null || psqlPath == null)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL binaries not found. Please ensure PostgreSQL is installed and in PATH.");
        }

        return HealthCheckResult.Healthy(
            $"PostgreSQL binaries found: pg_dump={pgDumpPath}, pg_restore={pgRestorePath}, psql={psqlPath}");
    }
}
