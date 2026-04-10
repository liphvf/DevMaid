using FurLab.Core.HealthChecks;
using FurLab.Core.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FurLab.Core.Services;

/// <summary>
/// Extension methods for configuring FurLab services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds FurLab core services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFurLabServices(this IServiceCollection services)
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
