using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevMaid.Services;

/// <summary>
/// Provides methods to list PostgreSQL databases.
/// This is a compatibility wrapper using DevMaid.Core.Services.DatabaseService.
/// </summary>
public static class PostgresDatabaseLister
{
    /// <summary>
    /// Lists all non-template databases on the PostgreSQL server.
    /// </summary>
    /// <param name="host">The database host address.</param>
    /// <param name="port">The database port.</param>
    /// <param name="username">The database username.</param>
    /// <param name="password">The database password.</param>
    /// <returns>A list of database names.</returns>
    /// <exception cref="Exception">Thrown when the operation fails.</exception>
    public static List<string> ListAllDatabases(string host, string port, string username, string password)
    {
        try
        {
            var options = new DevMaid.Core.Models.DatabaseConnectionOptions
            {
                Host = host,
                Port = port,
                Username = username,
                Password = password
            };

            return ServiceContainer.DatabaseService.ListDatabasesAsync(options).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to list databases: {ex.Message}", ex);
        }
    }
}
