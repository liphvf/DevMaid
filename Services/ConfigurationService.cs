using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace DevMaid.Services;

/// <summary>
/// Provides centralized configuration management for the DevMaid application.
/// </summary>
public static class ConfigurationService
{
    private static IConfigurationRoot? _configuration;
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public static IConfigurationRoot Configuration
    {
        get
        {
            if (_configuration == null)
            {
                lock (_lock)
                {
                    _configuration ??= BuildConfiguration();
                }
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Gets the database connection configuration.
    /// </summary>
    public static DatabaseConnectionConfig GetDatabaseConfig()
    {
        // Try to get from "Database" section first (new format)
        var dbConfig = Configuration.GetSection("Database").Get<DatabaseConnectionConfig>();
        if (dbConfig != null)
        {
            return dbConfig;
        }

        // Fallback to "Servers" section for backward compatibility
        var primaryServer = Configuration["Servers:PrimaryServer"];
        if (!string.IsNullOrWhiteSpace(primaryServer))
        {
            var serversList = Configuration.GetSection("Servers:ServersList").Get<List<ServerConfig>>();
            if (serversList != null)
            {
                var primaryServerConfig = serversList.FirstOrDefault(s => 
                    s.Name.Equals(primaryServer, StringComparison.OrdinalIgnoreCase));
                
                if (primaryServerConfig != null)
                {
                    return new DatabaseConnectionConfig
                    {
                        Host = primaryServerConfig.Host,
                        Port = primaryServerConfig.Port,
                        Username = primaryServerConfig.Username,
                        Password = primaryServerConfig.Password
                    };
                }
            }
        }

        return new DatabaseConnectionConfig();
    }

    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    public static void Reload()
    {
        lock (_lock)
        {
            _configuration = BuildConfiguration();
        }
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "DevMaid");
        Directory.CreateDirectory(configFolder);

        return new ConfigurationBuilder()
            .SetBasePath(configFolder)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}

/// <summary>
/// Represents database connection configuration.
/// </summary>
public class DatabaseConnectionConfig
{
    /// <summary>
    /// Gets or sets the database host address.
    /// </summary>
    public string? Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database port.
    /// </summary>
    public string? Port { get; set; } = "5432";

    /// <summary>
    /// Gets or sets the database username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the database password.
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Represents a server configuration in the Servers list.
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database port.
    /// </summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of databases.
    /// </summary>
    public List<string> Databases { get; set; } = new List<string>();
}
