namespace DevMaid.CLI.CommandOptions;

/// <summary>
/// Options for the table parser command.
/// </summary>
public class TableParserOptions
{
    /// <summary>Gets or sets the database username.</summary>
    public string User { get; set; } = "postgres";

    /// <summary>Gets or sets the database name.</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Gets or sets the database password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the database host address.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Gets or sets the output file path for the generated class.</summary>
    public string Output { get; set; } = "./Table.class";

    /// <summary>Gets or sets the table name to parse.</summary>
    public string? Table { get; set; }

    /// <summary>Gets the connection string built from the current options.</summary>
    public string ConnectionStringDatabase => Utils.GetConnectionString(Host, Database, User, Password);
}
