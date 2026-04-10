
using FurLab.Core.Interfaces;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FurLab.Core.HealthChecks;

/// <summary>
/// Health check that verifies the application configuration is properly loaded.
/// </summary>
public class ConfigurationHealthCheck(IConfigurationService configurationService) : IHealthCheck
{
    private readonly IConfigurationService _configurationService = configurationService;

    /// <summary>
    /// Checks the health of the configuration.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            var dbConfig = _configurationService.GetDatabaseConfig();

            if (string.IsNullOrWhiteSpace(dbConfig.Host) && string.IsNullOrWhiteSpace(dbConfig.Username))
            {
                return HealthCheckResult.Degraded("Database configuration is empty. Some features may not work.");
            }

            return HealthCheckResult.Healthy("Configuration loaded successfully.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Configuration check failed: {ex.Message}",
                ex);
        }
    }
}
