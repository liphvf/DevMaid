
namespace FurLab.CLI.CommandOptions;

/// <summary>
/// Configuration for a single PostgreSQL server
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// Unique name/identifier for this server (used for output directory naming)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Database host address
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Database port
    /// </summary>
    public string Port { get; set; } = "5432";

    /// <summary>
    /// Database username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Database password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Default database for this server (used when not using --all or when Databases list is empty)
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Optional: Specific databases to query on this server.
    /// If empty or null, will query all databases (when using --all)
    /// or use the default database from this server's Database property.
    /// </summary>
    public List<string>? Databases { get; set; }

    /// <summary>
    /// Optional: SSL mode for this server
    /// </summary>
    public string? SslMode { get; set; }

    /// <summary>
    /// Optional: Connection timeout in seconds
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Optional: Command timeout in seconds
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Optional: Enable connection pooling
    /// </summary>
    public bool? Pooling { get; set; }

    /// <summary>
    /// Optional: Minimum pool size
    /// </summary>
    public int? MinPoolSize { get; set; }

    /// <summary>
    /// Optional: Maximum pool size
    /// </summary>
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// Optional: Keepalive interval in seconds
    /// </summary>
    public int? Keepalive { get; set; }

    /// <summary>
    /// Optional: Connection lifetime in seconds
    /// </summary>
    public int? ConnectionLifetime { get; set; }
}
