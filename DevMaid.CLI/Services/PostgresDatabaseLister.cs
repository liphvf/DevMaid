using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DevMaid.Core.Interfaces;

namespace DevMaid.CLI.Services;

/// <summary>
/// Provides methods to list PostgreSQL databases.
/// This is a compatibility wrapper using DevMaid.Core.Services.DatabaseService.
/// </summary>
public static class PostgresDatabaseLister
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
    /// Gets the database service.
    /// </summary>
    private static IDatabaseService GetDatabaseService()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider not initialized. Call SetServiceProvider first.");
        }

        return _serviceProvider.GetRequiredService<IDatabaseService>();
    }

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

            return GetDatabaseService().ListDatabasesAsync(options).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to list databases: {ex.Message}", ex);
        }
    }
}
