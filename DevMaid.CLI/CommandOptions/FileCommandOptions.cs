namespace DevMaid.CLI.CommandOptions;

/// <summary>
/// Options for file utility commands.
/// </summary>
public class FileCommandOptions
{
    /// <summary>Gets or sets the input file pattern (e.g., *.sql).</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>Gets or sets the output file path.</summary>
    public string? Output { get; set; }
}
