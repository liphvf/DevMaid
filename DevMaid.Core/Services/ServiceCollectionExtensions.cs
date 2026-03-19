using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;

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
        // Register logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register core services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IWingetService, WingetService>();

        return services;
    }
}
