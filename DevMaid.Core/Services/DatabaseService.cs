using System.Diagnostics;
using System.Text;

using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Models;

namespace DevMaid.Core.Services;

/// <summary>
/// Provides methods for database operations including backup and restore.
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly IProcessExecutor _processExecutor;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    /// <param name="processExecutor">The process executor instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DatabaseService(IProcessExecutor processExecutor, ILogger logger)
    {
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a backup of a PostgreSQL database.
    /// </summary>
    /// <param name="options">The backup options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the backup operation with the result.</returns>
    public async Task<DatabaseBackupResult> BackupAsync(
        DatabaseBackupOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var startTime = DateTime.UtcNow;

        try
        {
            if (options.All)
            {
                return await BackupAllDatabasesAsync(options, progress, cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.DatabaseName))
                {
                    return DatabaseBackupResult.FailureResult(
                        "Database name is required when --all is not specified",
                        duration: DateTime.UtcNow - startTime);
                }

                return await BackupSingleDatabaseAsync(options, progress, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Backup failed: {ex.Message}");
            return DatabaseBackupResult.FailureResult(
                ex.Message,
                ex,
                DateTime.UtcNow - startTime);
        }
    }

    /// <summary>
    /// Restores a PostgreSQL database from a backup file.
    /// </summary>
    /// <param name="options">The restore options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the restore operation with the result.</returns>
    public async Task<DatabaseRestoreResult> RestoreAsync(
        DatabaseRestoreOptions options,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Validate input file
            var inputPath = options.InputFile;
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                if (string.IsNullOrWhiteSpace(options.DatabaseName))
                {
                    return DatabaseRestoreResult.FailureResult(
                        "Either input file or database name must be specified",
                        duration: DateTime.UtcNow - startTime);
                }
                inputPath = $"{options.DatabaseName}.dump";
            }

            var fullPath = Path.GetFullPath(inputPath);
            if (!File.Exists(fullPath))
            {
                return DatabaseRestoreResult.FailureResult(
                    $"Backup file not found: {fullPath}",
                    duration: DateTime.UtcNow - startTime);
            }

            if (string.IsNullOrWhiteSpace(options.DatabaseName))
            {
                return DatabaseRestoreResult.FailureResult(
                    "Database name is required",
                    duration: DateTime.UtcNow - startTime);
            }

            // Create database if needed
            if (options.CreateDatabase)
            {
                await CreateDatabaseIfNeededAsync(
                    options.Host ?? "localhost",
                    options.Port ?? "5432",
                    options.Username ?? string.Empty,
                    options.Password ?? string.Empty,
                    options.DatabaseName,
                    cancellationToken);
            }

            // Execute restore
            var pgRestorePath = PostgresBinaryLocator.FindPgRestore();
            if (pgRestorePath == null)
            {
                return DatabaseRestoreResult.FailureResult(
                    "pg_restore executable not found. Please ensure PostgreSQL is installed.",
                    duration: DateTime.UtcNow - startTime);
            }

            var arguments = BuildPgRestoreArguments(options, fullPath);
            var environmentVariables = new Dictionary<string, string>
            {
                ["PGPASSWORD"] = options.Password ?? string.Empty
            };

            progress?.Report(OperationProgress.Create(0, 1, "Restoring database..."));

            var result = await _processExecutor.ExecuteAsync(
                new ProcessExecutionOptions
                {
                    FileName = pgRestorePath,
                    Arguments = arguments,
                    EnvironmentVariables = environmentVariables
                },
                progress,
                cancellationToken);

            if (!result.Success)
            {
                return DatabaseRestoreResult.FailureResult(
                    $"pg_restore failed: {result.StandardError}",
                    duration: DateTime.UtcNow - startTime);
            }

            progress?.Report(OperationProgress.Create(1, 1, "Restore completed"));

            return DatabaseRestoreResult.SuccessResult(
                options.DatabaseName,
                fullPath,
                0, // TablesRestored would need to be parsed from output
                DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Restore failed: {ex.Message}");
            return DatabaseRestoreResult.FailureResult(
                ex.Message,
                ex,
                DateTime.UtcNow - startTime);
        }
    }

    /// <summary>
    /// Lists all databases on the PostgreSQL server.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with a list of database names.</returns>
    public async Task<List<string>> ListDatabasesAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var psqlPath = PostgresBinaryLocator.FindPsql();
        if (psqlPath == null)
        {
            throw new InvalidOperationException("psql executable not found. Please ensure PostgreSQL is installed.");
        }

        var arguments = $"-h \"{options.Host}\" -p {options.Port} -U \"{options.Username}\" -d postgres -c \"SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;\"";
        var environmentVariables = new Dictionary<string, string>
        {
            ["PGPASSWORD"] = options.Password ?? string.Empty
        };

        var result = await _processExecutor.ExecuteAsync(
            new ProcessExecutionOptions
            {
                FileName = psqlPath,
                Arguments = arguments,
                EnvironmentVariables = environmentVariables
            },
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to list databases: {result.StandardError}");
        }

        // Parse output to get database names
        var databases = new List<string>();
        var lines = result.StandardOutput.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            // Skip header lines and empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine) ||
                trimmedLine.StartsWith("datname") ||
                trimmedLine.StartsWith("---") ||
                trimmedLine.StartsWith("("))
            {
                continue;
            }

            // Remove trailing | and whitespace
            if (trimmedLine.EndsWith("|"))
            {
                trimmedLine = trimmedLine.Substring(0, trimmedLine.Length - 1).Trim();
            }

            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                databases.Add(trimmedLine);
            }
        }

        return databases;
    }

    /// <summary>
    /// Tests the connection to a PostgreSQL database.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with a boolean indicating success.</returns>
    public async Task<bool> TestConnectionAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var databases = await ListDatabasesAsync(options, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<DatabaseBackupResult> BackupSingleDatabaseAsync(
        DatabaseBackupOptions options,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        var pgDumpPath = PostgresBinaryLocator.FindPgDump();
        if (pgDumpPath == null)
        {
            return DatabaseBackupResult.FailureResult(
                "pg_dump executable not found. Please ensure PostgreSQL is installed.",
                duration: DateTime.UtcNow - startTime);
        }

        var outputPath = options.OutputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = $"{options.DatabaseName}.dump";
        }

        var arguments = BuildPgDumpArguments(options, outputPath);
        var environmentVariables = new Dictionary<string, string>
        {
            ["PGPASSWORD"] = options.Password ?? string.Empty
        };

        progress?.Report(OperationProgress.Create(0, 1, $"Backing up database '{options.DatabaseName}'..."));

        var result = await _processExecutor.ExecuteAsync(
            new ProcessExecutionOptions
            {
                FileName = pgDumpPath,
                Arguments = arguments,
                EnvironmentVariables = environmentVariables
            },
            progress,
            cancellationToken);

        if (!result.Success)
        {
            return DatabaseBackupResult.FailureResult(
                $"pg_dump failed: {result.StandardError}",
                duration: DateTime.UtcNow - startTime);
        }

        var fileInfo = new FileInfo(outputPath);

        progress?.Report(OperationProgress.Create(1, 1, "Backup completed"));

        return DatabaseBackupResult.SuccessResult(
            options.DatabaseName,
            Path.GetFullPath(outputPath),
            fileInfo.Length,
            0, // TablesBackedUp would need to be parsed from output
            DateTime.UtcNow - startTime);
    }

    private async Task<DatabaseBackupResult> BackupAllDatabasesAsync(
        DatabaseBackupOptions options,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        // List all databases
        var connectionOptions = new DatabaseConnectionOptions
        {
            Host = options.Host ?? "localhost",
            Port = options.Port ?? "5432",
            Username = options.Username,
            Password = options.Password
        };

        var databases = await ListDatabasesAsync(connectionOptions, cancellationToken);

        if (databases.Count == 0)
        {
            return DatabaseBackupResult.FailureResult(
                "No databases found",
                duration: DateTime.UtcNow - startTime);
        }

        // Determine output directory
        var outputDirectory = options.OutputPath;
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Backup each database
        var successCount = 0;
        var failureCount = 0;
        var totalSize = 0L;
        var currentStep = 0;

        foreach (var database in databases)
        {
            currentStep++;
            progress?.Report(OperationProgress.CreateFromSteps(
                currentStep,
                databases.Count,
                $"Backing up database '{database}'..."));

            try
            {
                var singleOptions = new DatabaseBackupOptions
                {
                    DatabaseName = database,
                    Host = options.Host,
                    Port = options.Port,
                    Username = options.Username,
                    Password = options.Password,
                    ExcludeTableData = options.ExcludeTableData,
                    OutputPath = Path.Combine(outputDirectory, $"{database}.dump"),
                    SchemaOnly = options.SchemaOnly,
                    CustomFormat = options.CustomFormat
                };

                var result = await BackupSingleDatabaseAsync(singleOptions, progress, cancellationToken);
                if (result.Success)
                {
                    successCount++;
                    totalSize += result.BackupFileSize;
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning($"Failed to backup database '{database}': {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogWarning($"Failed to backup database '{database}': {ex.Message}");
            }
        }

        if (failureCount > 0)
        {
            return DatabaseBackupResult.FailureResult(
                $"Backup completed with failures: {successCount} successful, {failureCount} failed",
                duration: DateTime.UtcNow - startTime);
        }

        return DatabaseBackupResult.SuccessResult(
            "All databases",
            outputDirectory,
            totalSize,
            successCount,
            DateTime.UtcNow - startTime);
    }

    private async Task CreateDatabaseIfNeededAsync(
        string host,
        string port,
        string username,
        string password,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var psqlPath = PostgresBinaryLocator.FindPsql();
        if (psqlPath == null)
        {
            throw new InvalidOperationException("psql executable not found. Please ensure PostgreSQL is installed.");
        }

        // Check if database exists
        var checkArguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'\"";
        var environmentVariables = new Dictionary<string, string>
        {
            ["PGPASSWORD"] = password
        };

        var result = await _processExecutor.ExecuteAsync(
            new ProcessExecutionOptions
            {
                FileName = psqlPath,
                Arguments = checkArguments,
                EnvironmentVariables = environmentVariables
            },
            cancellationToken: cancellationToken);

        // If database doesn't exist (exit code 3), create it
        if (result.ExitCode == 3)
        {
            var createArguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"CREATE DATABASE \\\"{databaseName}\\\"\"";
            var createResult = await _processExecutor.ExecuteAsync(
                new ProcessExecutionOptions
                {
                    FileName = psqlPath,
                    Arguments = createArguments,
                    EnvironmentVariables = environmentVariables
                },
                cancellationToken: cancellationToken);

            if (!createResult.Success)
            {
                throw new InvalidOperationException($"Failed to create database: {createResult.StandardError}");
            }
        }
    }

    private string BuildPgDumpArguments(DatabaseBackupOptions options, string outputPath)
    {
        var arguments = new StringBuilder();

        // Format option
        if (options.CustomFormat)
        {
            arguments.Append("-Fc ");
        }
        else
        {
            arguments.Append("-Fp ");
        }

        // Schema only
        if (options.SchemaOnly)
        {
            arguments.Append("--schema-only ");
        }

        // Connection options
        arguments.Append($"-h \"{options.Host ?? "localhost"}\" ");
        arguments.Append($"-p {options.Port ?? "5432"} ");
        arguments.Append($"-U \"{options.Username}\" ");
        arguments.Append($"-d \"{options.DatabaseName}\" ");
        arguments.Append($"-f \"{outputPath}\" ");

        // Exclude table data
        if (options.ExcludeTableData != null && options.ExcludeTableData.Length > 0)
        {
            foreach (var pattern in options.ExcludeTableData)
            {
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    arguments.Append($"--exclude-table-data \"{pattern}\" ");
                }
            }
        }

        return arguments.ToString();
    }

    private string BuildPgRestoreArguments(DatabaseRestoreOptions options, string inputPath)
    {
        var arguments = new StringBuilder();

        // Clean option
        if (options.Clean)
        {
            arguments.Append("--clean ");
        }

        // Connection options
        arguments.Append($"-h \"{options.Host ?? "localhost"}\" ");
        arguments.Append($"-p {options.Port ?? "5432"} ");
        arguments.Append($"-U \"{options.Username}\" ");
        arguments.Append($"-d \"{options.DatabaseName}\" ");
        arguments.Append($"\"{inputPath}\"");

        return arguments.ToString();
    }
}
