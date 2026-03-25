using DevMaid.Core.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevMaid.CLI.Services;

/// <summary>
/// Provides centralized configuration management for the DevMaid application.
/// This is a compatibility wrapper around the Core ConfigurationService.
/// </summary>
public static class ConfigurationService
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Sets the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    internal static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the configuration service.
    /// </summary>
    private static IConfigurationService GetConfigurationService()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider not initialized. Call SetServiceProvider first.");
        }

        return _serviceProvider.GetRequiredService<IConfigurationService>();
    }

    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public static IConfiguration Configuration => GetConfigurationService().Configuration;

    /// <summary>
    /// Gets the database connection configuration.
    /// </summary>
    public static DevMaid.CLI.CommandOptions.DatabaseConnectionConfig GetDatabaseConfig()
    {
        var coreConfig = GetConfigurationService().GetDatabaseConfig();
        return new DevMaid.CLI.CommandOptions.DatabaseConnectionConfig
        {
            Host = coreConfig.Host ?? "localhost",
            Port = coreConfig.Port ?? "5432",
            Username = coreConfig.Username,
            Password = coreConfig.Password
        };
    }

    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    public static void Reload()
    {
        GetConfigurationService().Reload();
    }
}
