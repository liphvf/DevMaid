namespace FurLab.Core.Models;

/// <summary>
/// Represents the runtime context for a single query execution across multiple servers.
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// The unique identifier for this execution.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// The SQL query being executed.
    /// </summary>
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// The source of the query (inline, file path, etc.).
    /// </summary>
    public string QuerySource { get; set; } = string.Empty;

    /// <summary>
    /// The resolved server-to-databases mapping for this execution.
    /// </summary>
    public List<(ServerConfigEntry Server, string Database)> ServerDatabases { get; set; } = [];

    /// <summary>
    /// The output directory for CSV files.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp used for file naming.
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Cancellation token source for this execution.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    /// <summary>
    /// Query type description (e.g., SELECT, DELETE).
    /// </summary>
    public string QueryTypeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Whether the query is destructive.
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 300;

    /// <summary>
    /// Settings overrides for connection string building.
    /// </summary>
    public QueryExecutionSettings? Settings { get; set; }
}

/// <summary>
/// Settings that can override server configuration during query execution.
/// </summary>
public class QueryExecutionSettings
{
    /// <summary>
    /// Host override.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Port override.
    /// </summary>
    public string? Port { get; set; }

    /// <summary>
    /// Database override.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Username override.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password override.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// SSL mode override.
    /// </summary>
    public string? SslMode { get; set; }

    /// <summary>
    /// Connection timeout override.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Command timeout override.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Whether to execute on all databases.
    /// </summary>
    public bool All { get; set; }

    /// <summary>
    /// Comma-separated list of databases to exclude.
    /// </summary>
    public string? Exclude { get; set; }

    /// <summary>
    /// Whether to skip destructive query confirmation.
    /// </summary>
    public bool NoConfirm { get; set; }
}
