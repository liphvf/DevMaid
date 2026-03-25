using DevMaid.Core.Models;

using Microsoft.Extensions.Configuration;

namespace DevMaid.Core.Interfaces;

/// <summary>
/// Defines a service for managing application configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the database connection configuration.
    /// </summary>
    /// <returns>The database connection configuration.</returns>
    DatabaseConnectionConfig GetDatabaseConfig();

    /// <summary>
    /// Updates the database connection configuration.
    /// </summary>
    /// <param name="config">The configuration to set.</param>
    void UpdateDatabaseConfig(DatabaseConnectionConfig config);

    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    void Reload();

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value, or null if not found.</returns>
    string? GetValue(string key);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    void SetValue(string key, string value);
}
