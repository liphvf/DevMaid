using System.CommandLine;
using System.Diagnostics;
using System.Text;

using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;
using FurLab.CLI.Services.Logging;

using Spectre.Console;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides database utilities including backup and restore functionality.
/// </summary>
public static class DatabaseCommand
{
    /// <summary>
    /// Builds the database command structure.
    /// </summary>
    /// <returns>The configured RootCommand.</returns>
    public static Command Build()
    {
        var command = new Command("database", "Database utilities.")
        {
            BuildBackupCommand(),
            BuildRestoreCommand(),
            PgPassCommand.Build()
        };

        return command;
    }

    private static Command BuildBackupCommand()
    {
        var backupCommand = new Command("backup", "Create a backup of a PostgreSQL database using pg_dump.");

        var databaseNameArgument = new Argument<string>("database")
        {
            Description = "Name of the database to backup. Not required when using --all.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var hostOption = new Option<string?>("--host", "-h")
        {
            Description = "Database host address."
        };

        var portOption = new Option<string?>("--port", "-p")
        {
            Description = "Database port."
        };

        var usernameOption = new Option<string?>("--username", "-U")
        {
            Description = "Database username."
        };

        var passwordOption = new Option<string?>("--password", "-W")
        {
            Description = "Database password. If not provided, will be prompted interactively."
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (for single database) or folder path (for --all). If not provided, uses current directory."
        };

        var allOption = new Option<bool>("--all", "-a")
        {
            Description = "Backup all databases on the server. Each database will have its own .dump file."
        };

        var excludeTableDataOption = new Option<string[]?>("--exclude-table-data")
        {
            Description = "Exclude table data matching the specified pattern(s). Can be specified multiple times. Example: --exclude-table-data 'log*'"
        };

        backupCommand.Add(allOption);
        backupCommand.Add(databaseNameArgument);
        backupCommand.Add(hostOption);
        backupCommand.Add(portOption);
        backupCommand.Add(usernameOption);
        backupCommand.Add(passwordOption);
        backupCommand.Add(outputOption);
        backupCommand.Add(excludeTableDataOption);

        backupCommand.SetAction(parseResult =>
        {
            var options = new DatabaseCommandOptions
            {
                DatabaseName = parseResult.GetValue(databaseNameArgument) ?? string.Empty,
                All = parseResult.GetValue(allOption),
                Host = parseResult.GetValue(hostOption),
                Port = parseResult.GetValue(portOption),
                Username = parseResult.GetValue(usernameOption),
                Password = parseResult.GetValue(passwordOption),
                OutputPath = parseResult.GetValue(outputOption),
                ExcludeTableData = parseResult.GetValue(excludeTableDataOption)
            };

            Backup(options);
        });

        return backupCommand;
    }

    private static Command BuildRestoreCommand()
    {
        var restoreCommand = new Command("restore", "Restore a PostgreSQL database using pg_restore.");

        var restoreDatabaseNameArgument = new Argument<string>("database")
        {
            Description = "Name of the database to restore. Not required when using --all.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var restoreFileArgument = new Argument<string?>("file")
        {
            Description = "Path to the dump file to restore. If not provided, looks for <database>.dump in the current directory.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var restoreAllOption = new Option<bool>("--all", "-a")
        {
            Description = "Restore all databases from dump files in the specified directory."
        };

        var restoreDirectoryOption = new Option<string?>("--directory", "-d")
        {
            Description = "Directory containing .dump files (for --all). If not provided, uses current directory."
        };

        var restoreHostOption = new Option<string?>("--host", "-h")
        {
            Description = "Database host address."
        };

        var restorePortOption = new Option<string?>("--port", "-p")
        {
            Description = "Database port."
        };

        var restoreUsernameOption = new Option<string?>("--username", "-U")
        {
            Description = "Database username."
        };

        var restorePasswordOption = new Option<string?>("--password", "-W")
        {
            Description = "Database password. If not provided, will be prompted interactively."
        };

        restoreCommand.Add(restoreAllOption);
        restoreCommand.Add(restoreDirectoryOption);
        restoreCommand.Add(restoreDatabaseNameArgument);
        restoreCommand.Add(restoreFileArgument);
        restoreCommand.Add(restoreHostOption);
        restoreCommand.Add(restorePortOption);
        restoreCommand.Add(restoreUsernameOption);
        restoreCommand.Add(restorePasswordOption);

        restoreCommand.SetAction(parseResult =>
        {
            var options = new DatabaseCommandOptions
            {
                DatabaseName = parseResult.GetValue(restoreDatabaseNameArgument) ?? string.Empty,
                All = parseResult.GetValue(restoreAllOption),
                Host = parseResult.GetValue(restoreHostOption),
                Port = parseResult.GetValue(restorePortOption),
                Username = parseResult.GetValue(restoreUsernameOption),
                Password = parseResult.GetValue(restorePasswordOption),
                OutputPath = parseResult.GetValue(restoreDirectoryOption),
                InputFile = parseResult.GetValue(restoreFileArgument)
            };

            Restore(options);
        });

        return restoreCommand;
    }

    /// <summary>
    /// Creates a backup of a PostgreSQL database.
    /// </summary>
    /// <param name="options">The backup options.</param>
    public static void Backup(DatabaseCommandOptions options)
    {
        var config = LoadAndValidateBackupConfiguration(options);

        if (config.BackupAll)
        {
            BackupAllDatabases(config);
        }
        else
        {
            BackupSingleDatabase(config);
        }
    }

    /// <summary>
    /// Restores a PostgreSQL database from a backup.
    /// </summary>
    /// <param name="options">The restore options.</param>
    public static void Restore(DatabaseCommandOptions options)
    {
        var config = LoadAndValidateRestoreConfiguration(options);

        if (config.RestoreAll)
        {
            RestoreAllDatabases(config);
        }
        else
        {
            RestoreSingleDatabase(config);
        }
    }

    private static DatabaseBackupConfig LoadAndValidateBackupConfiguration(DatabaseCommandOptions options)
    {
        var dbConfig = ConfigurationService.GetDatabaseConfig();

        var host = options.Host ?? dbConfig.Host ?? FurLabConstants.DefaultHost;
        var port = options.Port ?? dbConfig.Port ?? FurLabConstants.DefaultPort;
        var username = options.Username ?? dbConfig.Username;
        var password = options.Password ?? dbConfig.Password;

        ValidateConnectionParameters(host, port, username);

        var config = new DatabaseBackupConfig
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            DatabaseName = options.DatabaseName,
            BackupAll = options.All,
            ExcludeTableData = options.ExcludeTableData,
            OutputPath = options.OutputPath
        };

        if (!config.BackupAll && string.IsNullOrWhiteSpace(config.DatabaseName))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Database name is required when --all is not specified.");
            Environment.Exit(2);
            throw new InvalidOperationException(); // unreachable but required for compilation
        }

        return config;
    }

    private static DatabaseRestoreConfig LoadAndValidateRestoreConfiguration(DatabaseCommandOptions options)
    {
        var dbConfig = ConfigurationService.GetDatabaseConfig();

        var host = options.Host ?? dbConfig.Host ?? FurLabConstants.DefaultHost;
        var port = options.Port ?? dbConfig.Port ?? FurLabConstants.DefaultPort;
        var username = options.Username ?? dbConfig.Username;
        var password = options.Password ?? dbConfig.Password;

        ValidateConnectionParameters(host, port, username);

        var config = new DatabaseRestoreConfig
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            DatabaseName = options.DatabaseName,
            RestoreAll = options.All,
            InputFile = options.InputFile,
            OutputPath = options.OutputPath
        };

        if (!config.RestoreAll && string.IsNullOrWhiteSpace(config.DatabaseName))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Database name is required when --all is not specified.");
            Environment.Exit(2);
            throw new InvalidOperationException(); // unreachable but required for compilation
        }

        return config;
    }

    private static void ValidateConnectionParameters(string host, string port, string? username)
    {
        if (!SecurityUtils.IsValidHost(host))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid host: '{host}'");
            Environment.Exit(2);
        }

        if (!SecurityUtils.IsValidPort(port))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid port: '{port}'. Port must be between 1 and 65535.");
            Environment.Exit(2);
        }

        if (!string.IsNullOrWhiteSpace(username) && !SecurityUtils.IsValidUsername(username))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid username: '{username}'");
            Environment.Exit(2);
        }
    }

    private static void BackupSingleDatabase(DatabaseBackupConfig config)
    {
        var password = GetPassword(config.Password);

        var outputPath = config.OutputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = $"{config.DatabaseName}{FurLabConstants.DumpExtension}";
        }

        Logger.LogInformation("Creating backup of database '{DatabaseName}'...", config.DatabaseName);
        Logger.LogInformation("Host: {Host}:{Port}", config.Host, config.Port);
        Logger.LogInformation("Username: {Username}", config.Username ?? "N/A");
        Logger.LogInformation("Output: {Output}", Path.GetFullPath(outputPath ?? "unknown"));

        var pgDumpPath = Core.Services.PostgresBinaryLocator.FindPgDump();
        if (pgDumpPath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgDumpExecutable);
        }

        var arguments = BuildPgDumpArguments(config, outputPath ?? string.Empty);

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
                throw new Exception("Failed to start pg_dump process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new BackupFailedException($"pg_dump failed with exit code {process.ExitCode}. Error: {error}");
            }

            Logger.LogInformation("Backup created successfully at: {FullPath}", Path.GetFullPath(outputPath ?? "unknown"));

            if (!string.IsNullOrWhiteSpace(output))
            {
                Logger.LogDebug(output);
            }
        }
        finally
        {
            // Clear password from environment
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static void BackupAllDatabases(DatabaseBackupConfig config)
    {
        var password = GetPassword(config.Password);

        Logger.LogInformation("Listing all databases...");
        Logger.LogInformation("Host: {Host}:{Port}", config.Host, config.Port);
        Logger.LogInformation("Username: {Username}", config.Username ?? "N/A");

        var databases = PostgresDatabaseLister.ListAllDatabases(config.Host, config.Port, config.Username ?? string.Empty, password);

        if (databases.Count == 0)
        {
            Logger.LogWarning("No databases found.");
            return;
        }

        Logger.LogInformation("Found {Count} databases:", databases.Count);
        foreach (var db in databases)
        {
            Logger.LogInformation("  - {Database}", db);
        }
        Logger.LogInformation(string.Empty);

        var outputDirectory = config.OutputPath;
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Directory.GetCurrentDirectory();
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            Logger.LogInformation("Created output directory: {Directory}", outputDirectory);
        }

        Logger.LogInformation("Output directory: {FullPath}", Path.GetFullPath(outputDirectory));
        Logger.LogInformation(string.Empty);

        var successCount = 0;
        var failureCount = 0;

        foreach (var database in databases)
        {
            var dumpPath = Path.Combine(outputDirectory, $"{database}{FurLabConstants.DumpExtension}");

            Logger.LogInformation("Backing up database '{Database}'...", database);

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

                BackupSingleDatabaseInternal(singleConfig);
                Logger.LogInformation("✓ Backup created successfully: {File}", $"{database}{FurLabConstants.DumpExtension}");
                successCount++;
            }
            catch (Exception ex)
            {
                Logger.LogError("✗ Failed to backup database '{Database}': {Message}", ex, database, ex.Message);
                failureCount++;
            }

            Logger.LogInformation(string.Empty);
        }

        PrintBackupSummary(successCount, failureCount, databases.Count, outputDirectory);
    }

    private static void BackupSingleDatabaseInternal(DatabaseBackupConfig config)
    {
        var pgDumpPath = Core.Services.PostgresBinaryLocator.FindPgDump();
        if (pgDumpPath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgDumpExecutable);
        }

        var arguments = BuildPgDumpArguments(config, config.OutputPath ?? string.Empty);

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
                throw new Exception("Failed to start pg_dump process.");
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
                Logger.LogDebug(output);
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static string BuildPgDumpArguments(DatabaseBackupConfig config, string outputPath)
    {
        var arguments = new StringBuilder($"-Fc -h \"{config.Host}\" -p {config.Port} -U \"{config.Username}\" -d \"{config.DatabaseName}\" -f \"{outputPath}\"");

        if (config.ExcludeTableData != null && config.ExcludeTableData.Length > 0)
        {
            foreach (var pattern in config.ExcludeTableData)
            {
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    arguments.Append($" --exclude-table-data \"{pattern}\"");
                    Logger.LogInformation("Excluding table data matching pattern: {Pattern}", pattern);
                }
            }
        }

        return arguments.ToString();
    }

    private static void RestoreSingleDatabase(DatabaseRestoreConfig config)
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

        if (!File.Exists(fullPath))
        {
            throw new FurLabFileNotFoundException(fullPath);
        }

        Logger.LogInformation("Restoring database '{DatabaseName}'...", config.DatabaseName);
        Logger.LogInformation("Host: {Host}:{Port}", config.Host, config.Port);
        Logger.LogInformation("Username: {Username}", config.Username ?? "N/A");
        Logger.LogInformation("Input: {FullPath}", Path.GetFullPath(fullPath));

        var pgRestorePath = Core.Services.PostgresBinaryLocator.FindPgRestore();
        if (pgRestorePath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgRestoreExecutable);
        }

        CreateDatabaseIfNeeded(config.Host, config.Port, config.Username ?? string.Empty, password, config.DatabaseName);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgRestorePath,
            Arguments = $"-h \"{config.Host}\" -p {config.Port} -U \"{config.Username}\" -d \"{config.DatabaseName}\" -c \"{fullPath}\"",
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
                throw new Exception("Failed to start pg_restore process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new RestoreFailedException($"pg_restore failed with exit code {process.ExitCode}. Error: {error}");
            }

            Logger.LogInformation("Database '{DatabaseName}' restored successfully from: {FullPath}", config.DatabaseName, Path.GetFullPath(fullPath));

            if (!string.IsNullOrWhiteSpace(output))
            {
                Logger.LogDebug(output);
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static void RestoreAllDatabases(DatabaseRestoreConfig config)
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

        Logger.LogInformation("Scanning for {Extension} files in: {FullPath}", FurLabConstants.DumpExtension, Path.GetFullPath(inputDirectory));

        var dumpFiles = Directory.GetFiles(inputDirectory, $"*{FurLabConstants.DumpExtension}", SearchOption.TopDirectoryOnly);

        if (dumpFiles.Length == 0)
        {
            Logger.LogWarning("No {Extension} files found in the directory.", FurLabConstants.DumpExtension);
            return;
        }

        Logger.LogInformation("Found {Count} {Extension} files:", dumpFiles.Length, FurLabConstants.DumpExtension);
        foreach (var file in dumpFiles)
        {
            Logger.LogInformation("  - {File}", Path.GetFileName(file));
        }
        Logger.LogInformation(string.Empty);

        var successCount = 0;
        var failureCount = 0;

        foreach (var dumpFile in dumpFiles)
        {
            var databaseName = Path.GetFileNameWithoutExtension(dumpFile);

            if (!SecurityUtils.IsValidPostgreSQLIdentifier(databaseName))
            {
                Logger.LogWarning("✗ Skipping invalid database name: '{Database}'", databaseName);
                failureCount++;
                continue;
            }

            Logger.LogInformation("Restoring database '{Database}'...", databaseName);

            try
            {
                var singleConfig = new DatabaseRestoreConfig
                {
                    Host = config.Host,
                    Port = config.Port,
                    Username = config.Username,
                    Password = password,
                    DatabaseName = databaseName,
                    InputFile = dumpFile
                };

                RestoreSingleDatabaseInternal(singleConfig);
                Logger.LogInformation("✓ Database restored successfully: {Database}", databaseName);
                successCount++;
            }
            catch (Exception ex)
            {
                Logger.LogError("✗ Failed to restore database '{Database}': {Message}", ex, databaseName, ex.Message);
                failureCount++;
            }

            Logger.LogInformation(string.Empty);
        }

        PrintRestoreSummary(successCount, failureCount, dumpFiles.Length, inputDirectory);
    }

    private static void RestoreSingleDatabaseInternal(DatabaseRestoreConfig config)
    {
        var pgRestorePath = Core.Services.PostgresBinaryLocator.FindPgRestore();
        if (pgRestorePath == null)
        {
            throw new PostgresBinaryNotFoundException(FurLabConstants.PgRestoreExecutable);
        }

        CreateDatabaseIfNeeded(config.Host, config.Port, config.Username ?? string.Empty, config.Password ?? string.Empty, config.DatabaseName);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgRestorePath,
            Arguments = $"-h \"{config.Host}\" -p {config.Port} -U \"{config.Username}\" -d \"{config.DatabaseName}\" -c \"{config.InputFile}\"",
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
                Logger.LogDebug(output);
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static void CreateDatabaseIfNeeded(string host, string port, string username, string password, string databaseName)
    {
        if (!SecurityUtils.IsValidPostgreSQLIdentifier(databaseName))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid database name: '{databaseName}'");
            Environment.Exit(2);
        }

        var psqlPath = Core.Services.PostgresBinaryLocator.FindPsql();
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

            // If database doesn't exist (exit code 3), create it
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

    private static string GetPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Enter password: ");
            password = PostgresPasswordHandler.ReadPasswordInteractively();
            Console.WriteLine();
        }

        return password;
    }

    private static void PrintBackupSummary(int successCount, int failureCount, int totalCount, string outputDirectory)
    {
        Logger.LogInformation("========================================");
        Logger.LogInformation("Backup Summary:");
        Logger.LogInformation("  Successful: {Success}", successCount);
        Logger.LogInformation("  Failed: {Failure}", failureCount);
        Logger.LogInformation("  Total: {Total}", totalCount);
        Logger.LogInformation("  Output directory: {Directory}", Path.GetFullPath(outputDirectory));
        Logger.LogInformation("========================================");
    }

    private static void PrintRestoreSummary(int successCount, int failureCount, int totalCount, string inputDirectory)
    {
        Logger.LogInformation("========================================");
        Logger.LogInformation("Restore Summary:");
        Logger.LogInformation("  Successful: {Success}", successCount);
        Logger.LogInformation("  Failed: {Failure}", failureCount);
        Logger.LogInformation("  Total: {Total}", totalCount);
        Logger.LogInformation("  Input directory: {Directory}", Path.GetFullPath(inputDirectory));
        Logger.LogInformation("========================================");
    }
}
