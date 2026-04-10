namespace FurLab.CLI.CommandOptions;

/// <summary>
/// Options for SQL query execution commands.
/// </summary>
public class QueryCommandOptions
{
    // Input/Output
    /// <summary>Gets or sets the path to the SQL input file.</summary>
    public string InputFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the path to the CSV output file or directory.</summary>
    public string OutputFile { get; set; } = string.Empty;

    // Multi-database options
    /// <summary>Gets or sets a value indicating whether to execute on all databases.</summary>
    public bool All { get; set; }

    /// <summary>Gets or sets a value indicating whether to generate separate files per database.</summary>
    public bool SeparateFiles { get; set; }

    /// <summary>Gets or sets a comma-separated list of database names to exclude.</summary>
    public string? Exclude { get; set; }

    // Multi-server options
    /// <summary>Gets or sets a value indicating whether to execute on all configured servers.</summary>
    public bool Servers { get; set; }

    /// <summary>Gets or sets a filter pattern for server names.</summary>
    public string? ServerFilter { get; set; }

    // Connection string alternatives
    /// <summary>Gets or sets a complete Npgsql connection string.</summary>
    public string? NpgsqlConnectionString { get; set; }

    // Individual connection parameters
    /// <summary>Gets or sets the database host address.</summary>
    public string? Host { get; set; }

    /// <summary>Gets or sets the database port.</summary>
    public string? Port { get; set; }

    /// <summary>Gets or sets the database name.</summary>
    public string? Database { get; set; }

    /// <summary>Gets or sets the database username.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the database password.</summary>
    public string? Password { get; set; }

    // Connection options
    /// <summary>Gets or sets the SSL mode.</summary>
    public string? SslMode { get; set; }

    /// <summary>Gets or sets the connection timeout in seconds.</summary>
    public int? Timeout { get; set; }

    /// <summary>Gets or sets the command timeout in seconds.</summary>
    public int? CommandTimeout { get; set; }

    /// <summary>Gets or sets a value indicating whether connection pooling is enabled.</summary>
    public bool? Pooling { get; set; }

    /// <summary>Gets or sets the minimum connection pool size.</summary>
    public int? MinPoolSize { get; set; }

    /// <summary>Gets or sets the maximum connection pool size.</summary>
    public int? MaxPoolSize { get; set; }

    /// <summary>Gets or sets the keepalive interval in seconds.</summary>
    public int? Keepalive { get; set; }

    /// <summary>Gets or sets the connection lifetime in seconds.</summary>
    public int? ConnectionLifetime { get; set; }
}
