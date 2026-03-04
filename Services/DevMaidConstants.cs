namespace DevMaid.Services;

/// <summary>
/// Contains constant values used throughout the DevMaid application.
/// </summary>
public static class DevMaidConstants
{
    #region File Extensions

    /// <summary>
    /// PostgreSQL dump file extension.
    /// </summary>
    public const string DumpExtension = ".dump";

    /// <summary>
    /// Backup file extension.
    /// </summary>
    public const string BackupExtension = ".backup";

    /// <summary>
    /// CSV file extension.
    /// </summary>
    public const string CsvExtension = ".csv";

    /// <summary>
    /// JSON file extension.
    /// </summary>
    public const string JsonExtension = ".json";

    #endregion

    #region Default Values

    /// <summary>
    /// Default database host.
    /// </summary>
    public const string DefaultHost = "localhost";

    /// <summary>
    /// Default database port.
    /// </summary>
    public const string DefaultPort = "5432";

    /// <summary>
    /// Default database username.
    /// </summary>
    public const string DefaultUsername = "postgres";

    /// <summary>
    /// Default output directory (current directory).
    /// </summary>
    public const string DefaultOutputDirectory = ".";

    #endregion

    #region PostgreSQL Binaries

    /// <summary>
    /// PostgreSQL dump utility executable name.
    /// </summary>
    public const string PgDumpExecutable = "pg_dump";

    /// <summary>
    /// PostgreSQL restore utility executable name.
    /// </summary>
    public const string PgRestoreExecutable = "pg_restore";

    /// <summary>
    /// PostgreSQL command-line utility executable name.
    /// </summary>
    public const string PsqlExecutable = "psql";

    #endregion

    #region Timeouts

    /// <summary>
    /// Default connection timeout in seconds.
    /// </summary>
    public const int DefaultConnectionTimeout = 30;

    /// <summary>
    /// Default command timeout in seconds.
    /// </summary>
    public const int DefaultCommandTimeout = 300;

    /// <summary>
    /// Default process timeout in milliseconds.
    /// </summary>
    public const int DefaultProcessTimeout = 300000; // 5 minutes

    #endregion

    #region PostgreSQL

    /// <summary>
    /// PostgreSQL SSL modes.
    /// </summary>
    public static class SslMode
    {
        public const string Disable = "Disable";
        public const string Allow = "Allow";
        public const string Prefer = "Prefer";
        public const string Require = "Require";
        public const string VerifyCA = "VerifyCA";
        public const string VerifyFull = "VerifyFull";
    }

    /// <summary>
    /// Default PostgreSQL SSL mode.
    /// </summary>
    public const string DefaultSslMode = SslMode.Prefer;

    #endregion

    #region Connection Pooling

    /// <summary>
    /// Default minimum pool size.
    /// </summary>
    public const int DefaultMinPoolSize = 1;

    /// <summary>
    /// Default maximum pool size.
    /// </summary>
    public const int DefaultMaxPoolSize = 100;

    /// <summary>
    /// Default keepalive interval in seconds (0 = disabled).
    /// </summary>
    public const int DefaultKeepalive = 0;

    /// <summary>
    /// Default connection lifetime in seconds (0 = no limit).
    /// </summary>
    public const int DefaultConnectionLifetime = 0;

    #endregion

    #region File I/O

    /// <summary>
    /// Default buffer size for file operations in bytes.
    /// </summary>
    public const int DefaultBufferSize = 4096;

    /// <summary>
    /// Default encoding for file operations.
    /// </summary>
    public const string DefaultEncoding = "UTF-8";

    #endregion

    #region Directories

    /// <summary>
    /// Name of the DevMaid configuration directory.
    /// </summary>
    public const string ConfigDirectoryName = "DevMaid";

    /// <summary>
    /// Name of the application settings file.
    /// </summary>
    public const string AppSettingsFileName = "appsettings.json";

    #endregion

    #region CLI

    /// <summary>
    /// Default CLI command name.
    /// </summary>
    public const string DefaultCommandName = "devmaid";

    #endregion
}
