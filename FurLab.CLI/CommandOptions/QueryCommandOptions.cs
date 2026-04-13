namespace FurLab.CLI.CommandOptions;

/// <summary>
/// Options for SQL query execution commands.
/// </summary>
public class QueryCommandOptions
{
    /// <summary>Path to the SQL input file.</summary>
    public string InputFile { get; set; } = string.Empty;

    /// <summary>Inline SQL query (alternative to InputFile).</summary>
    public string InlineQuery { get; set; } = string.Empty;

    /// <summary>Path to the CSV output directory.</summary>
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>Execute on all databases.</summary>
    public bool All { get; set; }

    /// <summary>Comma-separated list of database names to exclude.</summary>
    public string? Exclude { get; set; }

    /// <summary>Complete Npgsql connection string.</summary>
    public string? NpgsqlConnectionString { get; set; }

    /// <summary>Database host address.</summary>
    public string? Host { get; set; }

    /// <summary>Database port.</summary>
    public string? Port { get; set; }

    /// <summary>Database name.</summary>
    public string? Database { get; set; }

    /// <summary>Database username.</summary>
    public string? Username { get; set; }

    /// <summary>Database password.</summary>
    public string? Password { get; set; }

    /// <summary>SSL mode.</summary>
    public string? SslMode { get; set; }

    /// <summary>Connection timeout in seconds.</summary>
    public int? Timeout { get; set; }

    /// <summary>Command timeout in seconds.</summary>
    public int? CommandTimeout { get; set; }

    /// <summary>Enable connection pooling.</summary>
    public bool? Pooling { get; set; }

    /// <summary>Minimum pool size.</summary>
    public int? MinPoolSize { get; set; }

    /// <summary>Maximum pool size.</summary>
    public int? MaxPoolSize { get; set; }

    /// <summary>Keepalive interval in seconds.</summary>
    public int? Keepalive { get; set; }

    /// <summary>Connection lifetime in seconds.</summary>
    public int? ConnectionLifetime { get; set; }

    /// <summary>Skip confirmation for destructive queries.</summary>
    public bool NoConfirm { get; set; }
}
