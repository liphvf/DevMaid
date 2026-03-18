using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

using DevMaid.CommandOptions;
using DevMaid.Core.Models;

namespace DevMaid.Services;

/// <summary>
/// Provides centralized configuration management for the DevMaid application.
/// This is a compatibility wrapper around the Core ConfigurationService.
/// </summary>
public static class ConfigurationService
{
    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public static IConfiguration Configuration => ServiceContainer.ConfigurationService.Configuration;

    /// <summary>
    /// Gets the database connection configuration.
    /// </summary>
    public static DevMaid.CommandOptions.DatabaseConnectionConfig GetDatabaseConfig()
    {
        var coreConfig = ServiceContainer.ConfigurationService.GetDatabaseConfig();
        return new DevMaid.CommandOptions.DatabaseConnectionConfig
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
        ServiceContainer.ConfigurationService.Reload();
    }
}
