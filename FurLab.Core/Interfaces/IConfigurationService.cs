using FurLab.Core.Models;

using Microsoft.Extensions.Configuration;

namespace FurLab.Core.Interfaces;

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
}
