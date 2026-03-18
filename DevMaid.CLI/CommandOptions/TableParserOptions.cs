namespace DevMaid.CommandOptions;

public class TableParserOptions
{
    public string User { get; set; } = "postgres";

    public string Database { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string Host { get; set; } = "localhost";

    public string Output { get; set; } = "./Table.class";

    public string? Table { get; set; }

    public string ConnectionStringDatabase => Utils.GetConnectionString(Host, Database, User, Password);
}
