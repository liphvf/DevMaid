using System.Text.RegularExpressions;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FurLab.Core.Services;

/// <summary>
/// Plans query execution by resolving which servers and databases to target,
/// including auto-discovery of databases when configured.
/// </summary>
public class QueryPlannerService
{
    private readonly ICredentialService _credentialService;
    private readonly ILogger<QueryPlannerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryPlannerService"/> class.
    /// </summary>
    public QueryPlannerService(ICredentialService credentialService, ILogger<QueryPlannerService> logger)
    {
        _credentialService = credentialService;
        _logger = logger;
    }

    /// <summary>
    /// Plans the execution by resolving the full list of (server, database) pairs to query.
    /// </summary>
    public async Task<List<(ServerConfigEntry Server, string Database)>> PlanExecutionAsync(
        List<ServerConfigEntry> selectedServers,
        HashSet<string> excludeNames,
        CancellationToken cancellationToken = default)
    {
        var allDatabases = new List<(ServerConfigEntry Server, string Database)>();
        var hasAutoDiscover = selectedServers.Any(s => s.FetchAllDatabases);

        if (hasAutoDiscover)
        {
            foreach (var server in selectedServers)
            {
                _logger.LogInformation("Discovering databases on {ServerName}...", server.Name);
                var databases = await GetDatabasesForServerAsync(server, excludeNames, cancellationToken);
                foreach (var db in databases)
                {
                    allDatabases.Add((server, db));
                }
            }
        }
        else
        {
            foreach (var server in selectedServers)
            {
                var databases = await GetDatabasesForServerAsync(server, excludeNames, cancellationToken);
                foreach (var db in databases)
                {
                    allDatabases.Add((server, db));
                }
            }
        }

        return allDatabases;
    }

    /// <summary>
    /// Gets the list of databases for a server, applying exclude filters.
    /// </summary>
    public async Task<List<string>> GetDatabasesForServerAsync(
        ServerConfigEntry server,
        HashSet<string> excludeNames,
        CancellationToken ct = default)
    {
        List<string> databases;

        if (server.FetchAllDatabases)
        {
            try
            {
                databases = await ListDatabasesAsync(server, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Auto-discovery failed for '{ServerName}': {Error}", server.Name, ex.Message);
                if (server.Databases.Count > 0)
                {
                    _logger.LogWarning("Falling back to configured databases for '{ServerName}'.", server.Name);
                    databases = server.Databases;
                }
                else
                {
                    return [];
                }
            }
        }
        else
        {
            if (server.Databases.Count > 0)
            {
                databases = server.Databases;
            }
            else
            {
                return [];
            }
        }

        if (excludeNames.Count > 0)
        {
            databases = databases.Where(db => !excludeNames.Contains(db)).ToList();
        }

        return databases;
    }

    /// <summary>
    /// Lists all databases on a server using pg_database.
    /// </summary>
    public async Task<List<string>> ListDatabasesAsync(ServerConfigEntry server, CancellationToken ct = default)
    {
        var connectionString = BuildConnectionStringForServer(server, "postgres", null);
        var databases = new List<string>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true",
            connection);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dbName = reader.GetString(0);
            if (!server.ExcludePatterns.Any(pattern => MatchesPattern(dbName, pattern)))
            {
                databases.Add(dbName);
            }
        }

        return databases;
    }

    /// <summary>
    /// Builds a connection string for a specific server and database.
    /// </summary>
    public string BuildConnectionStringForServer(ServerConfigEntry server, string database, QueryExecutionSettings? settings = null)
    {
        var password = ResolvePassword(server, settings);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = ResolveSetting(settings?.Host, server.Host),
            Port = ResolvePort(settings?.Port, server.Port),
            Database = database,
            Username = ResolveSetting(settings?.Username, server.Username),
            Password = password,
            SslMode = ParseSslMode(ResolveSetting(settings?.SslMode, server.SslMode)),
            Timeout = settings?.Timeout ?? server.Timeout,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 100
        };

        return builder.ConnectionString;
    }

    private string ResolvePassword(ServerConfigEntry server, QueryExecutionSettings? settings = null)
    {
        if (!string.IsNullOrWhiteSpace(settings?.Password))
            return settings.Password;

        var decrypted = _credentialService.TryDecrypt(server.EncryptedPassword);
        if (decrypted != null)
            return decrypted;

        throw new InvalidOperationException($"No password found for server '{server.Name}'. Use 'fur settings db-servers set-password' to save it permanently.");
    }

    private static string? ResolveSetting(string? overrideValue, string configValue)
    {
        return string.IsNullOrWhiteSpace(overrideValue) ? configValue : overrideValue;
    }

    private static int ResolvePort(string? overrideValue, int configValue)
    {
        return int.TryParse(overrideValue, out var port) ? port : configValue;
    }

    private static SslMode ParseSslMode(string? sslMode)
    {
        if (!string.IsNullOrWhiteSpace(sslMode) && Enum.TryParse<SslMode>(sslMode, true, out var result))
            return result;

        return SslMode.Prefer;
    }

    private static bool MatchesPattern(string dbName, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(dbName, regexPattern, RegexOptions.IgnoreCase);
    }
}
