namespace FurLab.CLI.Commands;

/// <summary>
/// Configuration for database restore operations.
/// </summary>
internal record DatabaseRestoreConfig
{
    public string Host { get; init; } = string.Empty;
    public string Port { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string DatabaseName { get; init; } = string.Empty;
    public bool RestoreAll { get; init; }
    public string? InputFile { get; init; }
    public string? OutputPath { get; init; }
}
