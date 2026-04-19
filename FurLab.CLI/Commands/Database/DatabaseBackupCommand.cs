using System.Diagnostics;
using System.Text;
using FurLab.CLI.Exceptions.Database;
using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using FurLab.Core.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Database;

/// <summary>
/// Creates a backup of a PostgreSQL database using pg_dump.
/// </summary>
public sealed class DatabaseBackupCommand : AsyncCommand<DatabaseBackupCommand.Settings>
{
    private readonly IPostgresBinaryLocator _binaryLocator;
    private readonly IPostgresPasswordHandler _passwordHandler;
    private readonly IUserConfigService _userConfigService;
    private readonly ICredentialService _credentialService;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<DatabaseBackupCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseBackupCommand"/> class.
    /// </summary>
    /// <param name="binaryLocator">The PostgreSQL binary locator service.</param>
    /// <param name="passwordHandler">The PostgreSQL password handler service.</param>
    /// <param name="userConfigService">The user configuration service.</param>
    /// <param name="credentialService">The credential encryption service.</param>
    /// <param name="databaseService">The database service for listing databases.</param>
    /// <param name="logger">The logger instance.</param>
    public DatabaseBackupCommand(
        IPostgresBinaryLocator binaryLocator,
        IPostgresPasswordHandler passwordHandler,
        IUserConfigService userConfigService,
        ICredentialService credentialService,
        IDatabaseService databaseService,
        ILogger<DatabaseBackupCommand> logger)
    {
        _binaryLocator = binaryLocator;
        _passwordHandler = passwordHandler;
        _userConfigService = userConfigService;
        _credentialService = credentialService;
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Settings for the database backup command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the name of the database to backup.
        /// Not required when using <see cref="All"/>.
        /// </summary>
        [CommandArgument(0, "[database]")]
        [System.ComponentModel.Description("Name of the database to backup. Not required when using --all.")]
        public string? Database { get; init; }

        /// <summary>
        /// Gets the database host address.
        /// </summary>
        [CommandOption("-H|--host")]
        [System.ComponentModel.Description("Database host address.")]
        public string? Host { get; init; }

        /// <summary>
        /// Gets the database port.
        /// </summary>
        [CommandOption("-p|--port")]
        [System.ComponentModel.Description("Database port.")]
        public string? Port { get; init; }

        /// <summary>
        /// Gets the database username.
        /// </summary>
        [CommandOption("-U|--username")]
        [System.ComponentModel.Description("Database username.")]
        public string? Username { get; init; }

        /// <summary>
        /// Gets the database password.
        /// If not provided, will be resolved from user config or prompted interactively.
        /// </summary>
        [CommandOption("-W|--password")]
        [System.ComponentModel.Description("Database password. If not provided, will be resolved from user config or prompted interactively.")]
        public string? Password { get; init; }

        /// <summary>
        /// Gets the SSL mode for the connection.
        /// </summary>
        [CommandOption("--ssl-mode")]
        [System.ComponentModel.Description("SSL mode. Default: Prefer.")]
        public string? SslMode { get; init; }

        /// <summary>
        /// Gets the connection timeout in seconds.
        /// </summary>
        [CommandOption("--timeout")]
        [System.ComponentModel.Description("Connection timeout in seconds. Default: 30.")]
        public int? Timeout { get; init; }

        /// <summary>
        /// Gets the command timeout in seconds.
        /// </summary>
        [CommandOption("--command-timeout")]
        [System.ComponentModel.Description("Command timeout in seconds. Default: 300.")]
        public int? CommandTimeout { get; init; }

        /// <summary>
        /// Gets a value indicating whether to backup all databases on the server.
        /// </summary>
        [CommandOption("-a|--all")]
        [System.ComponentModel.Description("Backup all databases on the server. Each database will have its own .dump file.")]
        public bool All { get; init; }

        /// <summary>
        /// Gets the table patterns to exclude entirely from the backup.
        /// </summary>
        [CommandOption("--exclude")]
        [System.ComponentModel.Description("Exclude tables matching the specified pattern(s). Can be specified multiple times. Example: --exclude 'log*'")]
        public string[]? Exclude { get; init; }

        /// <summary>
        /// Gets the table patterns whose data should be excluded from the backup.
        /// </summary>
        [CommandOption("--exclude-table-data")]
        [System.ComponentModel.Description("Exclude table data matching the specified pattern(s). Can be specified multiple times. Example: --exclude-table-data 'log*'")]
        public string[]? ExcludeTableData { get; init; }

        /// <summary>
        /// Gets a value indicating whether to enable verbose output from pg_dump.
        /// </summary>
        [CommandOption("-v|--verbose")]
        [System.ComponentModel.Description("Enable verbose output from pg_dump.")]
        public bool Verbose { get; init; }

        /// <summary>
        /// Gets the output file path or directory path.
        /// </summary>
        [CommandOption("-o|--output")]
        [System.ComponentModel.Description("Output file path (for single database) or folder path (for --all). If not provided, uses current directory.")]
        public string? Output { get; init; }

        /// <summary>
        /// Gets the output format for the backup.
        /// </summary>
        [CommandOption("-F|--format")]
        [System.ComponentModel.Description("Output format: p (plain), c (custom), d (directory), t (tar). Default: c.")]
        public string? Format { get; init; }
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var (config, errorCode) = LoadAndValidateBackupConfiguration(settings);

        if (config == null)
        {
            return errorCode;
        }

        try
        {
            if (config.BackupAll)
            {
                await BackupAllDatabasesAsync(config, settings, cancellation);
            }
            else
            {
                BackupSingleDatabase(config, settings);
            }

            return 0;
        }
        catch (PostgresBinaryNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 3;
        }
        catch (BackupFailedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private (DatabaseBackupConfig? Config, int ErrorCode) LoadAndValidateBackupConfiguration(Settings settings)
    {
        var host = settings.Host ?? FurLabConstants.DefaultHost;
        var port = settings.Port ?? FurLabConstants.DefaultPort;
        var username = settings.Username;
        var password = settings.Password ?? ResolvePasswordFromUserConfig(host, port);

        if (!SecurityUtils.IsValidHost(host))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid host: '{host}'");
            return (null, 2);
        }

        if (!SecurityUtils.IsValidPort(port))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid port: '{port}'. Port must be between 1 and 65535.");
            return (null, 2);
        }

        if (!string.IsNullOrWhiteSpace(username) && !SecurityUtils.IsValidUsername(username))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid username: '{username}'");
            return (null, 2);
        }

        var databaseName = settings.Database ?? string.Empty;

        if (!settings.All && string.IsNullOrWhiteSpace(databaseName))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Database name is required when --all is not specified.");
            return (null, 2);
        }

        var config = new DatabaseBackupConfig
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            DatabaseName = databaseName,
            BackupAll = settings.All,
            ExcludeTableData = settings.ExcludeTableData,
            OutputPath = settings.Output
        };

        return (config, 0);
    }

    private string? ResolvePasswordFromUserConfig(string host, string port)
    {
        try
        {
            var servers = _userConfigService.GetServers();
            var matchingServer = servers.FirstOrDefault(s =>
                string.Equals(s.Host, host, StringComparison.OrdinalIgnoreCase) &&
                s.Port.ToString() == port);

            if (matchingServer?.EncryptedPassword != null)
            {
                return _credentialService.TryDecrypt(matchingServer.EncryptedPassword);
            }
        }
        catch
        {
        }

        return null;
    }

    private string GetPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return _passwordHandler.ReadPasswordInteractively("Enter password: ");
        }

        return password;
    }

    private void BackupSingleDatabase(DatabaseBackupConfig config, Settings settings)
    {
        var password = GetPassword(config.Password);

        var outputPath = config.OutputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = $"{config.DatabaseName}{FurLabConstants.DumpExtension}";
        }

        _logger.LogInformation("Creating backup of database '{DatabaseName}'...", config.DatabaseName);
        _logger.LogInformation("Host: {Host}:{Port}", config.Host, config.Port);
        _logger.LogInformation("Username: {Username}", config.Username ?? "N/A");
        _logger.LogInformation("Output: {Output}", Path.GetFullPath(outputPath ?? "unknown"));

        var pgDumpPath = _binaryLocator.FindPgDump();
        if (pgDumpPath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgDumpExecutable);
        }

        var arguments = BuildPgDumpArguments(config, settings, outputPath ?? string.Empty);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["PGPASSWORD"] = password;

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new BackupFailedException("Failed to start pg_dump process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new BackupFailedException($"pg_dump failed with exit code {process.ExitCode}. Error: {error}");
            }

            _logger.LogInformation("Backup created successfully at: {FullPath}", Path.GetFullPath(outputPath ?? "unknown"));

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("{Output}", output);
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private async Task BackupAllDatabasesAsync(DatabaseBackupConfig config, Settings settings, CancellationToken cancellation)
    {
        var password = GetPassword(config.Password);

        _logger.LogInformation("Listing all databases...");
        _logger.LogInformation("Host: {Host}:{Port}", config.Host, config.Port);
        _logger.LogInformation("Username: {Username}", config.Username ?? "N/A");

        var connectionOptions = new DatabaseConnectionOptions
        {
            Host = config.Host,
            Port = config.Port,
            Username = config.Username ?? string.Empty,
            Password = password
        };

        var databases = await _databaseService.ListDatabasesAsync(connectionOptions, cancellation);

        if (databases.Count == 0)
        {
            _logger.LogWarning("No databases found.");
            return;
        }

        _logger.LogInformation("Found {Count} databases:", databases.Count);
        foreach (var db in databases)
        {
            _logger.LogInformation("  - {Database}", db);
        }

        _logger.LogInformation(string.Empty);

        var outputDirectory = config.OutputPath;
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            _logger.LogInformation("Created output directory: {Directory}", outputDirectory);
        }

        _logger.LogInformation("Output directory: {FullPath}", Path.GetFullPath(outputDirectory));
        _logger.LogInformation(string.Empty);

        var successCount = 0;
        var failureCount = 0;

        foreach (var database in databases)
        {
            if (cancellation.IsCancellationRequested)
            {
                break;
            }

            var dumpPath = Path.Combine(outputDirectory, $"{database}{FurLabConstants.DumpExtension}");

            _logger.LogInformation("Backing up database '{Database}'...", database);

            try
            {
                var singleConfig = new DatabaseBackupConfig
                {
                    Host = config.Host,
                    Port = config.Port,
                    Username = config.Username,
                    Password = password,
                    DatabaseName = database,
                    ExcludeTableData = config.ExcludeTableData,
                    OutputPath = dumpPath
                };

                BackupSingleDatabaseInternal(singleConfig, settings);
                _logger.LogInformation("Backup created successfully: {File}", $"{database}{FurLabConstants.DumpExtension}");
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup database '{Database}': {Message}", database, ex.Message);
                failureCount++;
            }

            _logger.LogInformation(string.Empty);
        }

        PrintBackupSummary(successCount, failureCount, databases.Count, outputDirectory);
    }

    private void BackupSingleDatabaseInternal(DatabaseBackupConfig config, Settings settings)
    {
        var pgDumpPath = _binaryLocator.FindPgDump();
        if (pgDumpPath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgDumpExecutable);
        }

        var arguments = BuildPgDumpArguments(config, settings, config.OutputPath ?? string.Empty);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["PGPASSWORD"] = config.Password;

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new BackupFailedException("Failed to start pg_dump process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new BackupFailedException($"pg_dump failed with exit code {process.ExitCode}. Error: {error}");
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("{Output}", output);
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private string BuildPgDumpArguments(DatabaseBackupConfig config, Settings settings, string outputPath)
    {
        var format = settings.Format ?? "c";
        var arguments = new StringBuilder($"-F{format} -h \"{config.Host}\" -p {config.Port} -U \"{config.Username}\" -d \"{config.DatabaseName}\" -f \"{outputPath}\"");

        if (settings.Verbose)
        {
            arguments.Append(" -v");
        }

        if (settings.Exclude != null && settings.Exclude.Length > 0)
        {
            foreach (var pattern in settings.Exclude)
            {
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    arguments.Append($" --exclude-table \"{pattern}\"");
                    _logger.LogInformation("Excluding table matching pattern: {Pattern}", pattern);
                }
            }
        }

        if (config.ExcludeTableData != null && config.ExcludeTableData.Length > 0)
        {
            foreach (var pattern in config.ExcludeTableData)
            {
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    arguments.Append($" --exclude-table-data \"{pattern}\"");
                    _logger.LogInformation("Excluding table data matching pattern: {Pattern}", pattern);
                }
            }
        }

        return arguments.ToString();
    }

    private void PrintBackupSummary(int successCount, int failureCount, int totalCount, string outputDirectory)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("Backup Summary:");
        _logger.LogInformation("  Successful: {Success}", successCount);
        _logger.LogInformation("  Failed: {Failure}", failureCount);
        _logger.LogInformation("  Total: {Total}", totalCount);
        _logger.LogInformation("  Output directory: {Directory}", Path.GetFullPath(outputDirectory));
        _logger.LogInformation("========================================");
    }
}
