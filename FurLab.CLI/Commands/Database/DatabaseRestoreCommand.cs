using System.Diagnostics;
using System.Text;

using FurLab.CLI.Utils;
using FurLab.Core.Interfaces;

using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Database;

/// <summary>
/// Represents the configuration for a database restore operation.
/// </summary>
internal record DatabaseRestoreConfig
{
    /// <summary>
    /// Gets the database host address.
    /// </summary>
    public string Host { get; init; } = FurLabConstants.DefaultHost;

    /// <summary>
    /// Gets the database port.
    /// </summary>
    public string Port { get; init; } = FurLabConstants.DefaultPort;

    /// <summary>
    /// Gets the database username.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the database password.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets the name of the database to restore.
    /// </summary>
    public string DatabaseName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether to restore all databases from dump files.
    /// </summary>
    public bool RestoreAll { get; init; }

    /// <summary>
    /// Gets the path to the input dump file.
    /// </summary>
    public string? InputFile { get; init; }

    /// <summary>
    /// Gets the directory containing dump files for restore-all mode.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets an optional Npgsql connection string override.
    /// </summary>
    public string? NpgsqlConnectionString { get; init; }

    /// <summary>
    /// Gets the SSL mode for the connection.
    /// </summary>
    public string? SslMode { get; init; }

    /// <summary>
    /// Gets the timeout in seconds for the restore operation.
    /// </summary>
    public int? Timeout { get; init; }

    /// <summary>
    /// Gets the command timeout in seconds for the restore operation.
    /// </summary>
    public int? CommandTimeout { get; init; }
}

/// <summary>
/// Restores a PostgreSQL database from a backup using pg_restore.
/// </summary>
public sealed class DatabaseRestoreCommand : AsyncCommand<DatabaseRestoreCommand.Settings>
{
    private readonly IConfigurationService _configurationService;
    private readonly IPostgresBinaryLocator _binaryLocator;
    private readonly IPostgresPasswordHandler _passwordHandler;
    private readonly IUserConfigService _userConfigService;
    private readonly ICredentialService _credentialService;
    private readonly ILogger<DatabaseRestoreCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRestoreCommand"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="binaryLocator">The PostgreSQL binary locator service.</param>
    /// <param name="passwordHandler">The PostgreSQL password handler service.</param>
    /// <param name="userConfigService">The user configuration service.</param>
    /// <param name="credentialService">The credential encryption service.</param>
    /// <param name="logger">The logger instance.</param>
    public DatabaseRestoreCommand(
        IConfigurationService configurationService,
        IPostgresBinaryLocator binaryLocator,
        IPostgresPasswordHandler passwordHandler,
        IUserConfigService userConfigService,
        ICredentialService credentialService,
        ILogger<DatabaseRestoreCommand> logger)
    {
        _configurationService = configurationService;
        _binaryLocator = binaryLocator;
        _passwordHandler = passwordHandler;
        _userConfigService = userConfigService;
        _credentialService = credentialService;
        _logger = logger;
    }

    /// <summary>
    /// Settings for the database restore command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the name of the database to restore.
        /// Not required when using <see cref="All"/>.
        /// </summary>
        [CommandArgument(0, "[database]")]
        [System.ComponentModel.Description("Name of the database to restore. Not required when using --all.")]
        public string? Database { get; init; }

        /// <summary>
        /// Gets the path to the dump file to restore.
        /// If not provided, looks for <c>&lt;database&gt;.dump</c> in the current directory.
        /// </summary>
        [CommandArgument(1, "[file]")]
        [System.ComponentModel.Description("Path to the dump file to restore. If not provided, looks for <database>.dump in the current directory.")]
        public string? InputFile { get; init; }

        /// <summary>
        /// Gets the database host address.
        /// </summary>
        [CommandOption("-h|--host")]
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
        /// Gets an Npgsql connection string to use instead of individual connection parameters.
        /// </summary>
        [CommandOption("--npgsql-connection-string")]
        [System.ComponentModel.Description("An Npgsql connection string to use instead of individual connection parameters.")]
        public string? NpgsqlConnectionString { get; init; }

        /// <summary>
        /// Gets the SSL mode for the database connection.
        /// </summary>
        [CommandOption("--ssl-mode")]
        [System.ComponentModel.Description("SSL mode for the database connection (Disable, Prefer, Require, VerifyCA, VerifyFull).")]
        public string? SslMode { get; init; }

        /// <summary>
        /// Gets the timeout in seconds for the restore operation.
        /// </summary>
        [CommandOption("--timeout")]
        [System.ComponentModel.Description("Timeout in seconds for the restore operation.")]
        public int? Timeout { get; init; }

        /// <summary>
        /// Gets the command timeout in seconds for the restore operation.
        /// </summary>
        [CommandOption("--command-timeout")]
        [System.ComponentModel.Description("Command timeout in seconds for the restore operation.")]
        public int? CommandTimeout { get; init; }

        /// <summary>
        /// Gets a value indicating whether to restore all databases from dump files.
        /// </summary>
        [CommandOption("-a|--all")]
        [System.ComponentModel.Description("Restore all databases from dump files in the specified directory.")]
        public bool All { get; init; }

        /// <summary>
        /// Gets the directory containing .dump files for <see cref="All"/> mode.
        /// </summary>
        [CommandOption("-d|--directory")]
        [System.ComponentModel.Description("Directory containing .dump files (for --all). If not provided, uses current directory.")]
        public string? Directory { get; init; }

        /// <summary>
        /// Gets a value indicating whether to enable verbose output from pg_restore.
        /// </summary>
        [CommandOption("-v|--verbose")]
        [System.ComponentModel.Description("Enable verbose output from pg_restore.")]
        public bool Verbose { get; init; }

        /// <summary>
        /// Gets a value indicating whether to skip the clean (drop) step before restoring.
        /// By default, pg_restore runs with <c>-c</c> to drop objects before creating them.
        /// </summary>
        [CommandOption("--no-clean")]
        [System.ComponentModel.Description("Do not drop database objects before creating them. By default, --clean is used.")]
        public bool NoClean { get; init; }

        /// <summary>
        /// Gets a value indicating whether to skip restoration of object ownership.
        /// </summary>
        [CommandOption("--no-owner")]
        [System.ComponentModel.Description("Skip restoration of object ownership.")]
        public bool NoOwner { get; init; }

        /// <summary>
        /// Gets a value indicating whether to skip restoration of access privileges.
        /// </summary>
        [CommandOption("--no-acl")]
        [System.ComponentModel.Description("Skip restoration of access privileges.")]
        public bool NoAcl { get; init; }

        /// <summary>
        /// Gets a value indicating whether to execute the restore as a single transaction.
        /// </summary>
        [CommandOption("--single-transaction")]
        [System.ComponentModel.Description("Execute the restore as a single transaction.")]
        public bool SingleTransaction { get; init; }

        /// <summary>
        /// Gets the number of parallel jobs for restore (custom-format archives only).
        /// </summary>
        [CommandOption("-j|--jobs")]
        [System.ComponentModel.Description("Number of parallel jobs for restore (custom-format only).")]
        public int? Jobs { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var (nullableConfig, errorCode) = LoadAndValidateRestoreConfiguration(settings);

        if (nullableConfig is not { } config)
        {
            return Task.FromResult(errorCode);
        }

        try
        {
            if (config.RestoreAll)
            {
                RestoreAllDatabases(config, settings);
            }
            else
            {
                RestoreSingleDatabase(config, settings);
            }

            return Task.FromResult(0);
        }
        catch (PostgresBinaryNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(1);
        }
        catch (RestoreFailedException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(1);
        }
        catch (PathTraversalException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(2);
        }
        catch (FurLabFileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(2);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Loads and validates the restore configuration from settings and service defaults.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>A tuple containing the validated config or null, and an error code if validation failed.</returns>
    private (DatabaseRestoreConfig? Config, int ErrorCode) LoadAndValidateRestoreConfiguration(Settings settings)
    {
        var dbConfig = _configurationService.GetDatabaseConfig();

        var host = settings.Host ?? dbConfig.Host ?? FurLabConstants.DefaultHost;
        var port = settings.Port ?? dbConfig.Port ?? FurLabConstants.DefaultPort;
        var username = settings.Username ?? dbConfig.Username;
        var password = settings.Password ?? ResolvePasswordFromUserConfig(host, port) ?? dbConfig.Password;

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

        if (!string.IsNullOrWhiteSpace(settings.NpgsqlConnectionString) &&
            !SecurityUtils.IsValidPath(settings.NpgsqlConnectionString))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Invalid connection string.");
            return (null, 2);
        }

        if (!string.IsNullOrWhiteSpace(settings.SslMode))
        {
            var validSslModes = new[] { "Disable", "Prefer", "Require", "VerifyCA", "VerifyFull" };
            if (!validSslModes.Contains(settings.SslMode, StringComparer.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid SSL mode: '{settings.SslMode}'. Valid values: Disable, Prefer, Require, VerifyCA, VerifyFull.");
                return (null, 2);
            }
        }

        if (settings.Timeout.HasValue && settings.Timeout.Value <= 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Timeout must be a positive integer.");
            return (null, 2);
        }

        if (settings.CommandTimeout.HasValue && settings.CommandTimeout.Value <= 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Command timeout must be a positive integer.");
            return (null, 2);
        }

        var config = new DatabaseRestoreConfig
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            DatabaseName = databaseName,
            RestoreAll = settings.All,
            InputFile = settings.InputFile,
            OutputPath = settings.Directory,
            NpgsqlConnectionString = settings.NpgsqlConnectionString,
            SslMode = settings.SslMode,
            Timeout = settings.Timeout,
            CommandTimeout = settings.CommandTimeout
        };

        return ((DatabaseRestoreConfig?)config, 0);
    }

    /// <summary>
    /// Resolves the database password from the user configuration store.
    /// </summary>
    /// <param name="host">The database host.</param>
    /// <param name="port">The database port.</param>
    /// <returns>The decrypted password, or null if not found.</returns>
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

    /// <summary>
    /// Gets the password, prompting interactively if not provided.
    /// </summary>
    /// <param name="password">The password or null.</param>
    /// <returns>The resolved password.</returns>
    private string GetPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return _passwordHandler.ReadPasswordInteractively("Enter password: ");
        }

        return password;
    }

    /// <summary>
    /// Restores a single database from a dump file.
    /// </summary>
    /// <param name="config">The validated restore configuration.</param>
    /// <param name="settings">The command settings.</param>
    private void RestoreSingleDatabase(DatabaseRestoreConfig config, Settings settings)
    {
        var password = GetPassword(config.Password);

        var inputPath = config.InputFile;
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            inputPath = $"{config.DatabaseName}{FurLabConstants.DumpExtension}";
        }

        var fullPath = Path.GetFullPath(inputPath);
        if (!SecurityUtils.IsValidPath(fullPath))
        {
            throw new PathTraversalException(inputPath);
        }

        if (!System.IO.File.Exists(fullPath))
        {
            throw new FurLabFileNotFoundException(fullPath);
        }

        _logger.LogInformation("Restoring database '{DatabaseName}'...", config.DatabaseName);
        _logger.LogInformation("Host: {Host}:{Port}", config.Host, config.Port);
        _logger.LogInformation("Username: {Username}", config.Username ?? "N/A");
        _logger.LogInformation("Input: {FullPath}", Path.GetFullPath(fullPath));

        var pgRestorePath = _binaryLocator.FindPgRestore();
        if (pgRestorePath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgRestoreExecutable);
        }

        CreateDatabaseIfNeeded(config.Host, config.Port, config.Username ?? string.Empty, password, config.DatabaseName);

        var arguments = BuildPgRestoreArguments(config, settings, fullPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgRestorePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["PGPASSWORD"] = password;

        if (!string.IsNullOrWhiteSpace(config.SslMode))
        {
            startInfo.Environment["PGSSLMODE"] = config.SslMode;
        }

        if (config.Timeout.HasValue)
        {
            startInfo.Environment["PGCONNECT_TIMEOUT"] = config.Timeout.Value.ToString();
        }

        if (config.CommandTimeout.HasValue)
        {
            startInfo.Environment["PGCOMMAND_TIMEOUT"] = config.CommandTimeout.Value.ToString();
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start pg_restore process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new RestoreFailedException($"pg_restore failed with exit code {process.ExitCode}. Error: {error}");
            }

            _logger.LogInformation("Database '{DatabaseName}' restored successfully from: {FullPath}", config.DatabaseName, Path.GetFullPath(fullPath));

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

    /// <summary>
    /// Restores all databases from dump files in a directory.
    /// </summary>
    /// <param name="config">The validated restore configuration.</param>
    /// <param name="settings">The command settings.</param>
    private void RestoreAllDatabases(DatabaseRestoreConfig config, Settings settings)
    {
        var password = GetPassword(config.Password);

        var inputDirectory = config.OutputPath;
        if (string.IsNullOrWhiteSpace(inputDirectory))
        {
            inputDirectory = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(inputDirectory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {Path.GetFullPath(inputDirectory)}");
        }

        _logger.LogInformation("Scanning for {Extension} files in: {FullPath}", FurLabConstants.DumpExtension, Path.GetFullPath(inputDirectory));

        var dumpFiles = Directory.GetFiles(inputDirectory, $"*{FurLabConstants.DumpExtension}", SearchOption.TopDirectoryOnly);

        if (dumpFiles.Length == 0)
        {
            _logger.LogWarning("No {Extension} files found in the directory.", FurLabConstants.DumpExtension);
            return;
        }

        _logger.LogInformation("Found {Count} {Extension} files:", dumpFiles.Length, FurLabConstants.DumpExtension);
        foreach (var file in dumpFiles)
        {
            _logger.LogInformation("  - {File}", Path.GetFileName(file));
        }

        _logger.LogInformation(string.Empty);

        var successCount = 0;
        var failureCount = 0;

        foreach (var dumpFile in dumpFiles)
        {
            var databaseName = Path.GetFileNameWithoutExtension(dumpFile);

            if (!SecurityUtils.IsValidPostgreSQLIdentifier(databaseName))
            {
                _logger.LogWarning("Skipping invalid database name: '{Database}'", databaseName);
                failureCount++;
                continue;
            }

            _logger.LogInformation("Restoring database '{Database}'...", databaseName);

            try
            {
                var singleConfig = config with
                {
                    DatabaseName = databaseName,
                    InputFile = dumpFile,
                    Password = password
                };

                RestoreSingleDatabaseInternal(singleConfig, settings);
                _logger.LogInformation("Database restored successfully: {Database}", databaseName);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore database '{Database}': {Message}", databaseName, ex.Message);
                failureCount++;
            }

            _logger.LogInformation(string.Empty);
        }

        PrintRestoreSummary(successCount, failureCount, dumpFiles.Length, inputDirectory);
    }

    /// <summary>
    /// Restores a single database from a dump file without interactive output.
    /// Used internally by the bulk restore operation.
    /// </summary>
    /// <param name="config">The validated restore configuration.</param>
    /// <param name="settings">The command settings.</param>
    private void RestoreSingleDatabaseInternal(DatabaseRestoreConfig config, Settings settings)
    {
        var pgRestorePath = _binaryLocator.FindPgRestore();
        if (pgRestorePath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgRestoreExecutable);
        }

        CreateDatabaseIfNeeded(config.Host, config.Port, config.Username ?? string.Empty, config.Password ?? string.Empty, config.DatabaseName);

        var arguments = BuildPgRestoreArguments(config, settings, config.InputFile ?? string.Empty);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgRestorePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["PGPASSWORD"] = config.Password;

        if (!string.IsNullOrWhiteSpace(config.SslMode))
        {
            startInfo.Environment["PGSSLMODE"] = config.SslMode;
        }

        if (config.Timeout.HasValue)
        {
            startInfo.Environment["PGCONNECT_TIMEOUT"] = config.Timeout.Value.ToString();
        }

        if (config.CommandTimeout.HasValue)
        {
            startInfo.Environment["PGCOMMAND_TIMEOUT"] = config.CommandTimeout.Value.ToString();
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start pg_restore process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new RestoreFailedException($"pg_restore failed with exit code {process.ExitCode}. Error: {error}");
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

    /// <summary>
    /// Creates the target database if it does not already exist.
    /// </summary>
    /// <param name="host">The database host.</param>
    /// <param name="port">The database port.</param>
    /// <param name="username">The database username.</param>
    /// <param name="password">The database password.</param>
    /// <param name="databaseName">The name of the database to create if needed.</param>
    private void CreateDatabaseIfNeeded(string host, string port, string username, string password, string databaseName)
    {
        if (!SecurityUtils.IsValidPostgreSQLIdentifier(databaseName))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid database name: '{databaseName}'");
            throw new InvalidOperationException($"Invalid database name: '{databaseName}'");
        }

        var psqlPath = _binaryLocator.FindPsql();
        if (psqlPath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PsqlExecutable);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = psqlPath,
            Arguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'\"",
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
                throw new Exception("Failed to start psql process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode == 3)
            {
                var createStartInfo = new ProcessStartInfo
                {
                    FileName = psqlPath,
                    Arguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"CREATE DATABASE \\\"{databaseName}\\\"\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                createStartInfo.Environment["PGPASSWORD"] = password;

                using var createProcess = Process.Start(createStartInfo);
                if (createProcess == null)
                {
                    throw new Exception("Failed to start psql process.");
                }

                createProcess.WaitForExit();

                if (createProcess.ExitCode != 0)
                {
                    var createError = createProcess.StandardError.ReadToEnd();
                    throw new Exception($"Failed to create database: {createError}");
                }
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    /// <summary>
    /// Builds the pg_restore command-line arguments from the configuration and settings.
    /// </summary>
    /// <param name="config">The validated restore configuration.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="filePath">The path to the dump file.</param>
    /// <returns>The formatted argument string for pg_restore.</returns>
    private string BuildPgRestoreArguments(DatabaseRestoreConfig config, Settings settings, string filePath)
    {
        var arguments = new StringBuilder($"-h \"{config.Host}\" -p {config.Port} -U \"{config.Username}\" -d \"{config.DatabaseName}\"");

        if (!settings.NoClean)
        {
            arguments.Append(" -c");
        }

        if (settings.Verbose)
        {
            arguments.Append(" -v");
        }

        if (settings.NoOwner)
        {
            arguments.Append(" --no-owner");
        }

        if (settings.NoAcl)
        {
            arguments.Append(" --no-acl");
        }

        if (settings.SingleTransaction)
        {
            arguments.Append(" --single-transaction");
        }

        if (settings.Jobs.HasValue && settings.Jobs.Value > 0)
        {
            arguments.Append($" -j {settings.Jobs.Value}");
        }

        arguments.Append($" \"{filePath}\"");

        return arguments.ToString();
    }

    /// <summary>
    /// Prints a summary of the bulk restore operation results.
    /// </summary>
    /// <param name="successCount">The number of successfully restored databases.</param>
    /// <param name="failureCount">The number of failed restore operations.</param>
    /// <param name="totalCount">The total number of dump files processed.</param>
    /// <param name="inputDirectory">The directory that was scanned for dump files.</param>
    private void PrintRestoreSummary(int successCount, int failureCount, int totalCount, string inputDirectory)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("Restore Summary:");
        _logger.LogInformation("  Successful: {Success}", successCount);
        _logger.LogInformation("  Failed: {Failure}", failureCount);
        _logger.LogInformation("  Total: {Total}", totalCount);
        _logger.LogInformation("  Input directory: {Directory}", Path.GetFullPath(inputDirectory));
        _logger.LogInformation("========================================");
    }
}
