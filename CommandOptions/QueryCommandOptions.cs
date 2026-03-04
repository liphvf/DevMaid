namespace DevMaid.CommandOptions;

public class QueryCommandOptions
{
    // Input/Output
    public string InputFile { get; set; } = string.Empty;
    public string OutputFile { get; set; } = string.Empty;

    // Multi-database options
    public bool All { get; set; }
    public bool SeparateFiles { get; set; }
    public string? Exclude { get; set; }

    // Multi-server options
    public bool Servers { get; set; }
    public string? ServerFilter { get; set; }

    // Connection string alternatives
    public string? NpgsqlConnectionString { get; set; }

    // Individual connection parameters
    public string? Host { get; set; }
    public string? Port { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    // Connection options
    public string? SslMode { get; set; }
    public int? Timeout { get; set; }
    public int? CommandTimeout { get; set; }
    public bool? Pooling { get; set; }
    public int? MinPoolSize { get; set; }
    public int? MaxPoolSize { get; set; }
    public int? Keepalive { get; set; }
    public int? ConnectionLifetime { get; set; }
}
