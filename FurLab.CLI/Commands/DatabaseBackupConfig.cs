namespace FurLab.CLI.Commands;

/// <summary>
/// Configuration for database backup operations.
/// </summary>
internal record DatabaseBackupConfig
{
    public string Host { get; init; } = string.Empty;
    public string Port { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string DatabaseName { get; init; } = string.Empty;
    public bool BackupAll { get; init; }
    public string[]? ExcludeTableData { get; init; }
    public string? OutputPath { get; init; }
}
