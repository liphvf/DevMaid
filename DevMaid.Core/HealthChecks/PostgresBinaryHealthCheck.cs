using System.Threading;
using System.Threading.Tasks;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevMaid.Core.HealthChecks;

public class PostgresBinaryHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;

    public PostgresBinaryHealthCheck(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

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
