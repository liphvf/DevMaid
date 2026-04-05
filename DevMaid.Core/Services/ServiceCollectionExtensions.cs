using DevMaid.Core.HealthChecks;
using DevMaid.Core.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevMaid.Core.Services;

/// <summary>
/// Extension methods for configuring DevMaid services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DevMaid core services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDevMaidServices(this IServiceCollection services)
    {
        _ = services.AddLogging(builder =>
        {
            _ = builder.AddConsole();
            _ = builder.SetMinimumLevel(LogLevel.Information);
        });

        _ = services.AddHealthChecks()
            .AddCheck<PostgresBinaryHealthCheck>("postgres_binaries", tags: ["infrastructure"])
            .AddCheck<ConfigurationHealthCheck>("configuration", tags: ["core"]);

        _ = services.AddSingleton<IConfigurationService, ConfigurationService>();
        _ = services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        _ = services.AddSingleton<IDatabaseService, DatabaseService>();
        _ = services.AddSingleton<IFileService, FileService>();
        _ = services.AddSingleton<IPgPassService, PgPassService>();

        return services;
    }
}
