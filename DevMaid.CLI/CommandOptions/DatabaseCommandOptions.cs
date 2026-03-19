namespace DevMaid.CLI.CommandOptions;

public class DatabaseCommandOptions
{
    public string DatabaseName { get; set; } = string.Empty;
    public bool All { get; set; }
    public string? Host { get; set; }
    public string? Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? OutputPath { get; set; }
    public string? InputFile { get; set; }
    public string[]? ExcludeTableData { get; set; }
}
