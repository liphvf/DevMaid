using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;

using DevMaid.CommandOptions;

namespace DevMaid.Commands;

public static class DatabaseCommand
{
    public static Command Build()
    {
        var command = new Command("database", "Database utilities.");

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

        backupCommand.Add(allOption);
        backupCommand.Add(databaseNameArgument);
        backupCommand.Add(hostOption);
        backupCommand.Add(portOption);
        backupCommand.Add(usernameOption);
        backupCommand.Add(passwordOption);
        backupCommand.Add(outputOption);

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
                OutputPath = parseResult.GetValue(outputOption)
            };

            Backup(options);
        });

        command.Add(backupCommand);

        return command;
    }

    public static void Backup(DatabaseCommandOptions options)
    {
        // Load default values from appsettings.json
        var defaultHost = Program.AppSettings["Database:Host"];
        var defaultPort = Program.AppSettings["Database:Port"];
        var defaultUsername = Program.AppSettings["Database:Username"];
        var defaultPassword = Program.AppSettings["Database:Password"];

        // Use provided values or fall back to defaults
        var host = options.Host ?? defaultHost ?? "localhost";
        var port = options.Port ?? defaultPort ?? "5432";
        var username = options.Username ?? defaultUsername;
        var password = options.Password ?? defaultPassword;

        // Check if --all flag is set
        if (options.All)
        {
            BackupAllDatabases(host, port, username ?? string.Empty, password ?? string.Empty, options.OutputPath);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            throw new ArgumentException("Database name is required when --all is not specified.");
        }

        // Prompt for password if not provided
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Enter password: ");
            password = ReadPassword();
            Console.WriteLine();
        }

        // Set output path
        var outputPath = options.OutputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = $"{options.DatabaseName}.backup";
        }

        Console.WriteLine($"Creating backup of database '{options.DatabaseName}'...");
        Console.WriteLine($"Host: {host}:{port}");
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Output: {outputPath}");

        // Build pg_dump command
        var pgDumpPath = FindPgDump();
        if (pgDumpPath == null)
        {
            throw new Exception("pg_dump not found. Please ensure PostgreSQL is installed and pg_dump is in your PATH.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = $"-Fc -h \"{host}\" -p {port} -U \"{username}\" -d \"{options.DatabaseName}\" -f \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set PGPASSWORD environment variable
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
                throw new Exception($"pg_dump failed with exit code {process.ExitCode}. Error: {error}");
            }

            Console.WriteLine($"Backup created successfully at: {Path.GetFullPath(outputPath)}");

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }
        }
        finally
        {
            // Clear password from environment
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static void BackupAllDatabases(string host, string port, string username, string password, string? outputPath)
    {
        // Prompt for password if not provided
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Enter password: ");
            password = ReadPassword();
            Console.WriteLine();
        }

        Console.WriteLine("Listing all databases...");
        Console.WriteLine($"Host: {host}:{port}");
        Console.WriteLine($"Username: {username}");

        // Get list of all databases
        var databases = ListAllDatabases(host, port, username, password);

        if (databases.Count == 0)
        {
            Console.WriteLine("No databases found.");
            return;
        }

        Console.WriteLine($"Found {databases.Count} databases:");
        foreach (var db in databases)
        {
            Console.WriteLine($"  - {db}");
        }
        Console.WriteLine();

        // Set output directory
        var outputDirectory = outputPath;
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Directory.GetCurrentDirectory();
        }

        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            Console.WriteLine($"Created output directory: {outputDirectory}");
        }

        Console.WriteLine($"Output directory: {Path.GetFullPath(outputDirectory)}");
        Console.WriteLine();

        // Backup each database
        var successCount = 0;
        var failureCount = 0;

        foreach (var database in databases)
        {
            var dumpPath = Path.Combine(outputDirectory, $"{database}.dump");
            
            Console.WriteLine($"Backing up database '{database}'...");
            
            try
            {
                BackupSingleDatabase(host, port, username, password, database, dumpPath);
                Console.WriteLine($"✓ Backup created successfully: {database}.dump");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to backup database '{database}': {ex.Message}");
                failureCount++;
            }
            
            Console.WriteLine();
        }

        // Summary
        Console.WriteLine("========================================");
        Console.WriteLine("Backup Summary:");
        Console.WriteLine($"  Successful: {successCount}");
        Console.WriteLine($"  Failed: {failureCount}");
        Console.WriteLine($"  Total: {databases.Count}");
        Console.WriteLine($"  Output directory: {Path.GetFullPath(outputDirectory)}");
        Console.WriteLine("========================================");
    }

    private static void BackupSingleDatabase(string host, string port, string username, string password, string databaseName, string outputPath)
    {
        var pgDumpPath = FindPgDump();
        if (pgDumpPath == null)
        {
            throw new Exception("pg_dump not found. Please ensure PostgreSQL is installed and pg_dump is in your PATH.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = $"-Fc -h \"{host}\" -p {port} -U \"{username}\" -d \"{databaseName}\" -f \"{outputPath}\"",
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
                throw new Exception($"pg_dump failed with exit code {process.ExitCode}. Error: {error}");
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.Write(output);
            }
        }
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static string? FindPgDump()
    {
        // Try to find pg_dump in PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();

        foreach (var path in paths)
        {
            var pgDumpPath = Path.Combine(path, "pg_dump.exe");
            if (File.Exists(pgDumpPath))
            {
                return pgDumpPath;
            }

            pgDumpPath = Path.Combine(path, "pg_dump");
            if (File.Exists(pgDumpPath))
            {
                return pgDumpPath;
            }
        }

        // Try common PostgreSQL installation paths on Windows
        var commonPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\*\bin\pg_dump.exe",
            @"C:\PostgreSQL\*\bin\pg_dump.exe"
        };

        foreach (var pattern in commonPaths)
        {
            var directory = Path.GetDirectoryName(pattern);
            if (directory != null && Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "pg_dump.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }
        }

        return null;
    }

    private static string? FindPsql()
    {
        // Try to find psql in PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();

        foreach (var path in paths)
        {
            var psqlPath = Path.Combine(path, "psql.exe");
            if (File.Exists(psqlPath))
            {
                return psqlPath;
            }

            psqlPath = Path.Combine(path, "psql");
            if (File.Exists(psqlPath))
            {
                return psqlPath;
            }
        }

        // Try common PostgreSQL installation paths on Windows
        var commonPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\*\bin\psql.exe",
            @"C:\PostgreSQL\*\bin\psql.exe"
        };

        foreach (var pattern in commonPaths)
        {
            var directory = Path.GetDirectoryName(pattern);
            if (directory != null && Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "psql.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }
        }

        return null;
    }

    private static List<string> ListAllDatabases(string host, string port, string username, string password)
    {
        var psqlPath = FindPsql();
        if (psqlPath == null)
        {
            throw new Exception("psql not found. Please ensure PostgreSQL is installed and psql is in your PATH.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = psqlPath,
            Arguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;\"",
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

            if (process.ExitCode != 0)
            {
                throw new Exception($"psql failed with exit code {process.ExitCode}. Error: {error}");
            }

            // Parse output to get database names
            var databases = new List<string>();
            var lines = output.Split('\n');
            
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
        finally
        {
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        var consoleKeyInfo = Console.ReadKey(true);

        while (consoleKeyInfo.Key != ConsoleKey.Enter)
        {
            if (consoleKeyInfo.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                }
            }
            else if (!char.IsControl(consoleKeyInfo.KeyChar))
            {
                password += consoleKeyInfo.KeyChar;
            }

            consoleKeyInfo = Console.ReadKey(true);
        }

        return password;
    }
}
