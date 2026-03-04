using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using DevMaid.CommandOptions;

using Npgsql;

namespace DevMaid.Commands;

public static class QueryCommand
{
    public static Command Build()
    {
        var command = new Command("query", "Execute SQL queries and export results to CSV.");

        var runCommand = new Command("run", "Run a SQL query and export the results to CSV.");

        // Input/Output options
        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "Path to the SQL input file."
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Path to the CSV output file (or directory when using --all)."
        };

        // Connection string option (takes precedence over individual parameters)
        var npgsqlConnectionStringOption = new Option<string?>("--npgsql-connection-string")
        {
            Description = "Complete Npgsql connection string (e.g., \"Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass\")."
        };

        // Individual connection parameters
        var hostOption = new Option<string?>("--host", "-h")
        {
            Description = "Database host address."
        };

        var portOption = new Option<string?>("--port", "-p")
        {
            Description = "Database port."
        };

        var databaseOption = new Option<string?>("--database", "-d")
        {
            Description = "Database name. Not required when using --all."
        };

        var usernameOption = new Option<string?>("--username", "-U")
        {
            Description = "Database username."
        };

        var passwordOption = new Option<string?>("--password", "-W")
        {
            Description = "Database password. If not provided, will be prompted interactively."
        };

        // Connection options
        var sslModeOption = new Option<string?>("--ssl-mode")
        {
            Description = "SSL mode (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull). Default: Prefer."
        };

        var timeoutOption = new Option<int?>("--timeout")
        {
            Description = "Connection timeout in seconds. Default: 30."
        };

        var commandTimeoutOption = new Option<int?>("--command-timeout")
        {
            Description = "Command timeout in seconds. Default: 300."
        };

        var poolingOption = new Option<bool?>("--pooling")
        {
            Description = "Enable connection pooling. Default: true."
        };

        var minPoolSizeOption = new Option<int?>("--min-pool-size")
        {
            Description = "Minimum pool size. Default: 1."
        };

        var maxPoolSizeOption = new Option<int?>("--max-pool-size")
        {
            Description = "Maximum pool size. Default: 100."
        };

        var keepaliveOption = new Option<int?>("--keepalive")
        {
            Description = "Keepalive interval in seconds. Default: 0."
        };

        var connectionLifetimeOption = new Option<int?>("--connection-lifetime")
        {
            Description = "Connection lifetime in seconds. Default: 0."
        };

        // Multi-database options
        var allOption = new Option<bool>("--all", "-a")
        {
            Description = "Execute the query on all databases on the server."
        };

        var separateFilesOption = new Option<bool>("--separate-files")
        {
            Description = "Generate separate CSV files for each database instead of a single consolidated file."
        };

        var excludeOption = new Option<string?>("--exclude")
        {
            Description = "Comma-separated list of database names to exclude (e.g., postgres,template0,template1)."
        };

        // Add all options to the command
        runCommand.Add(inputOption);
        runCommand.Add(outputOption);
        runCommand.Add(npgsqlConnectionStringOption);
        runCommand.Add(hostOption);
        runCommand.Add(portOption);
        runCommand.Add(databaseOption);
        runCommand.Add(usernameOption);
        runCommand.Add(passwordOption);
        runCommand.Add(sslModeOption);
        runCommand.Add(timeoutOption);
        runCommand.Add(commandTimeoutOption);
        runCommand.Add(poolingOption);
        runCommand.Add(minPoolSizeOption);
        runCommand.Add(maxPoolSizeOption);
        runCommand.Add(keepaliveOption);
        runCommand.Add(connectionLifetimeOption);
        runCommand.Add(allOption);
        runCommand.Add(separateFilesOption);
        runCommand.Add(excludeOption);

        runCommand.SetAction(parseResult =>
        {
            var options = new QueryCommandOptions
            {
                InputFile = parseResult.GetValue(inputOption) ?? string.Empty,
                OutputFile = parseResult.GetValue(outputOption) ?? string.Empty,
                NpgsqlConnectionString = parseResult.GetValue(npgsqlConnectionStringOption),
                Host = parseResult.GetValue(hostOption),
                Port = parseResult.GetValue(portOption),
                Database = parseResult.GetValue(databaseOption),
                Username = parseResult.GetValue(usernameOption),
                Password = parseResult.GetValue(passwordOption),
                SslMode = parseResult.GetValue(sslModeOption),
                Timeout = parseResult.GetValue(timeoutOption),
                CommandTimeout = parseResult.GetValue(commandTimeoutOption),
                Pooling = parseResult.GetValue(poolingOption),
                MinPoolSize = parseResult.GetValue(minPoolSizeOption),
                MaxPoolSize = parseResult.GetValue(maxPoolSizeOption),
                Keepalive = parseResult.GetValue(keepaliveOption),
                ConnectionLifetime = parseResult.GetValue(connectionLifetimeOption),
                All = parseResult.GetValue(allOption),
                SeparateFiles = parseResult.GetValue(separateFilesOption),
                Exclude = parseResult.GetValue(excludeOption)
            };

            Run(options);
        });

        command.Add(runCommand);

        return command;
    }

    public static void Run(QueryCommandOptions options)
    {
        // Validate input file
        if (string.IsNullOrWhiteSpace(options.InputFile))
        {
            throw new ArgumentException("Input file path is required.");
        }

        var inputFullPath = Path.GetFullPath(options.InputFile);
        if (!SecurityUtils.IsValidPath(inputFullPath))
        {
            throw new ArgumentException($"Invalid input path: '{options.InputFile}'. Path traversal not allowed.");
        }

        if (!File.Exists(inputFullPath))
        {
            throw new FileNotFoundException($"SQL input file not found: {inputFullPath}");
        }

        // Read SQL query
        var sqlQuery = File.ReadAllText(inputFullPath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new ArgumentException("SQL input file is empty.");
        }

        // Check if --all flag is set
        if (options.All)
        {
            RunOnAllDatabases(options, sqlQuery);
            return;
        }

        // Validate output file for single database mode
        if (string.IsNullOrWhiteSpace(options.OutputFile))
        {
            throw new ArgumentException("Output file path is required when not using --all.");
        }

        var outputFullPath = Path.GetFullPath(options.OutputFile);
        if (!SecurityUtils.IsValidPath(outputFullPath))
        {
            throw new ArgumentException($"Invalid output path: '{options.OutputFile}'. Path traversal not allowed.");
        }

        // Create output directory if needed
        var outputDirectory = Path.GetDirectoryName(outputFullPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Build connection string
        var connectionString = BuildConnectionString(options);

        Console.WriteLine("Executing query...");
        Console.WriteLine($"Input: {inputFullPath}");
        Console.WriteLine($"Output: {outputFullPath}");
        Console.WriteLine();

        // Execute query and export to CSV
        try
        {
            ExecuteQueryAndExportToCsv(connectionString, sqlQuery, outputFullPath, options.CommandTimeout);
            Console.WriteLine();
            Console.WriteLine("✓ Query executed successfully!");
            Console.WriteLine($"✓ Results exported to: {outputFullPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"✗ Error executing query: {ex.Message}");
            throw;
        }
    }

    private static void RunOnAllDatabases(QueryCommandOptions options, string sqlQuery)
    {
        // Validate output path for multi-database mode
        if (string.IsNullOrWhiteSpace(options.OutputFile))
        {
            throw new ArgumentException("Output directory path is required when using --all.");
        }

        var outputDirectory = Path.GetFullPath(options.OutputFile);
        if (!SecurityUtils.IsValidPath(outputDirectory))
        {
            throw new ArgumentException($"Invalid output path: '{options.OutputFile}'. Path traversal not allowed.");
        }

        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Get connection parameters
        var (host, port, username, password) = GetConnectionParameters(options);

        Console.WriteLine("Listing all databases...");
        Console.WriteLine($"Host: {host}:{port}");
        Console.WriteLine($"Username: {username}");
        Console.WriteLine();

        // Get list of all databases
        var databases = ListAllDatabases(host, port, username, password);

        // Parse exclude list
        var excludedDatabases = new HashSet<string>();
        if (!string.IsNullOrWhiteSpace(options.Exclude))
        {
            var excludeList = options.Exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var db in excludeList)
            {
                excludedDatabases.Add(db);
            }
        }

        // Filter out excluded databases
        var filteredDatabases = databases.FindAll(db => !excludedDatabases.Contains(db));

        if (filteredDatabases.Count == 0)
        {
            Console.WriteLine("No databases found after filtering.");
            return;
        }

        Console.WriteLine($"Found {filteredDatabases.Count} databases to process:");
        foreach (var db in filteredDatabases)
        {
            var marker = excludedDatabases.Contains(db) ? " (excluded)" : "";
            Console.WriteLine($"  - {db}{marker}");
        }
        Console.WriteLine();

        Console.WriteLine($"Output directory: {Path.GetFullPath(outputDirectory)}");
        Console.WriteLine($"Output mode: {(options.SeparateFiles ? "Separate files per database" : "Consolidated file")}");
        Console.WriteLine();

        // Execute query on all databases
        if (options.SeparateFiles)
        {
            ExecuteOnAllDatabasesSeparateFiles(host, port, username, password, filteredDatabases, sqlQuery, outputDirectory, options);
        }
        else
        {
            ExecuteOnAllDatabasesConsolidated(host, port, username, password, filteredDatabases, sqlQuery, outputDirectory, options);
        }
    }

    private static void ExecuteOnAllDatabasesConsolidated(string host, string port, string username, string password, List<string> databases, string sqlQuery, string outputDirectory, QueryCommandOptions options)
    {
        var outputFilePath = Path.Combine(outputDirectory, "all_databases.csv");
        var successCount = 0;
        var failureCount = 0;
        var totalRowCount = 0;
        var allResults = new List<(string DatabaseName, List<string> ColumnNames, List<Dictionary<string, string>> Data)>();

        Console.WriteLine("Executing query on all databases (consolidated mode)...");
        Console.WriteLine();

        foreach (var database in databases)
        {
            Console.WriteLine($"Processing database '{database}'...");

            try
            {
                var connectionString = BuildConnectionStringForDatabase(host, port, username, password, database, options);
                var (columnNames, data) = ExecuteQuery(connectionString, sqlQuery, options.CommandTimeout);

                allResults.Add((database, columnNames, data));
                totalRowCount += data.Count;
                successCount++;

                Console.WriteLine($"  ✓ Retrieved {data.Count} rows");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed: {ex.Message}");
                failureCount++;
            }

            Console.WriteLine();
        }

        // Write consolidated results to CSV
        if (allResults.Count > 0)
        {
            Console.WriteLine("Writing consolidated results...");
            WriteConsolidatedCsv(outputFilePath, allResults);
            Console.WriteLine($"✓ Results exported to: {outputFilePath}");
        }

        // Summary
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("Execution Summary:");
        Console.WriteLine($"  Successful: {successCount}");
        Console.WriteLine($"  Failed: {failureCount}");
        Console.WriteLine($"  Total rows: {totalRowCount}");
        Console.WriteLine($"  Output file: {Path.GetFullPath(outputFilePath)}");
        Console.WriteLine("========================================");
    }

    private static void ExecuteOnAllDatabasesSeparateFiles(string host, string port, string username, string password, List<string> databases, string sqlQuery, string outputDirectory, QueryCommandOptions options)
    {
        var successCount = 0;
        var failureCount = 0;
        var totalRowCount = 0;

        Console.WriteLine("Executing query on all databases (separate files mode)...");
        Console.WriteLine();

        foreach (var database in databases)
        {
            var outputFilePath = Path.Combine(outputDirectory, $"{database}.csv");

            Console.WriteLine($"Processing database '{database}'...");

            try
            {
                var connectionString = BuildConnectionStringForDatabase(host, port, username, password, database, options);
                var rowCount = ExecuteQueryAndExportToCsv(connectionString, sqlQuery, outputFilePath, options.CommandTimeout);

                totalRowCount += rowCount;
                successCount++;

                Console.WriteLine($"  ✓ Results exported to: {database}.csv ({rowCount} rows)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed: {ex.Message}");
                failureCount++;
            }

            Console.WriteLine();
        }

        // Summary
        Console.WriteLine("========================================");
        Console.WriteLine("Execution Summary:");
        Console.WriteLine($"  Successful: {successCount}");
        Console.WriteLine($"  Failed: {failureCount}");
        Console.WriteLine($"  Total rows: {totalRowCount}");
        Console.WriteLine($"  Output directory: {Path.GetFullPath(outputDirectory)}");
        Console.WriteLine("========================================");
    }

    private static (List<string> ColumnNames, List<Dictionary<string, string>> Data) ExecuteQuery(string connectionString, string sqlQuery, int? commandTimeout)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sqlQuery, connection);

        if (commandTimeout.HasValue)
        {
            command.CommandTimeout = commandTimeout.Value;
        }

        using var reader = command.ExecuteReader();

        var columnNames = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

        var data = new List<Dictionary<string, string>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                row[columnNames[i]] = value;
            }
            data.Add(row);
        }

        return (columnNames, data);
    }

    private static void WriteConsolidatedCsv(string outputPath, List<(string DatabaseName, List<string> ColumnNames, List<Dictionary<string, string>> Data)> allResults)
    {
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

        // Get all unique column names across all databases
        var allColumnNames = new HashSet<string>();
        foreach (var result in allResults)
        {
            foreach (var columnName in result.ColumnNames)
            {
                allColumnNames.Add(columnName);
            }
        }

        // Add database name column
        var columns = new List<string> { "_database_name" };
        columns.AddRange(allColumnNames);

        // Write header
        foreach (var columnName in columns)
        {
            csv.WriteField(columnName);
        }
        csv.NextRecord();

        // Write data
        foreach (var result in allResults)
        {
            foreach (var row in result.Data)
            {
                csv.WriteField(result.DatabaseName);

                foreach (var columnName in allColumnNames)
                {
                    var value = row.ContainsKey(columnName) ? row[columnName] : string.Empty;
                    csv.WriteField(value);
                }

                csv.NextRecord();
            }
        }
    }

    private static (string Host, string Port, string Username, string Password) GetConnectionParameters(QueryCommandOptions options)
    {
        // Load default values from appsettings.json
        var defaultHost = Program.AppSettings["Database:Host"] ?? "localhost";
        var defaultPort = Program.AppSettings["Database:Port"] ?? "5432";
        var defaultUsername = Program.AppSettings["Database:Username"];
        var defaultPassword = Program.AppSettings["Database:Password"];

        // Use provided values or fall back to defaults
        var host = options.Host ?? defaultHost;
        var port = options.Port ?? defaultPort;
        var username = options.Username ?? defaultUsername;
        var password = options.Password ?? defaultPassword;

        // Validate required parameters
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host is required when not using --npgsql-connection-string.");
        }

        if (!SecurityUtils.IsValidHost(host))
        {
            throw new ArgumentException($"Invalid host: '{host}'");
        }

        if (!SecurityUtils.IsValidPort(port))
        {
            throw new ArgumentException($"Invalid port: '{port}'. Port must be between 1 and 65535.");
        }

        if (!string.IsNullOrWhiteSpace(username) && !SecurityUtils.IsValidUsername(username))
        {
            throw new ArgumentException($"Invalid username: '{username}'");
        }

        // Prompt for password if not provided
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Enter password: ");
            password = ReadPassword();
            Console.WriteLine();
        }

        return (host, port, username ?? string.Empty, password ?? string.Empty);
    }

    private static string BuildConnectionStringForDatabase(string host, string port, string username, string password, string database, QueryCommandOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Database = database,
            Username = username,
            Password = password
        };

        // Add optional connection parameters
        if (!string.IsNullOrWhiteSpace(options.SslMode))
        {
            builder.SslMode = ParseSslMode(options.SslMode);
        }

        if (options.Timeout.HasValue)
        {
            builder.Timeout = options.Timeout.Value;
        }

        if (options.CommandTimeout.HasValue)
        {
            builder.CommandTimeout = options.CommandTimeout.Value;
        }

        if (options.Pooling.HasValue)
        {
            builder.Pooling = options.Pooling.Value;
        }

        if (options.MinPoolSize.HasValue)
        {
            builder.MinPoolSize = options.MinPoolSize.Value;
        }

        if (options.MaxPoolSize.HasValue)
        {
            builder.MaxPoolSize = options.MaxPoolSize.Value;
        }

        if (options.Keepalive.HasValue)
        {
            builder.KeepAlive = options.Keepalive.Value;
        }

        if (options.ConnectionLifetime.HasValue)
        {
            builder.ConnectionLifetime = options.ConnectionLifetime.Value;
        }

        return builder.ConnectionString;
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

    private static string BuildConnectionString(QueryCommandOptions options)
    {
        // If complete connection string is provided, use it
        if (!string.IsNullOrWhiteSpace(options.NpgsqlConnectionString))
        {
            Console.WriteLine("Using provided Npgsql connection string.");
            var cleanedConnectionString = CleanConnectionString(options.NpgsqlConnectionString);
            return cleanedConnectionString;
        }

        // Load default values from appsettings.json
        var defaultHost = Program.AppSettings["Database:Host"] ?? "localhost";
        var defaultPort = Program.AppSettings["Database:Port"] ?? "5432";
        var defaultUsername = Program.AppSettings["Database:Username"];
        var defaultPassword = Program.AppSettings["Database:Password"];
        var defaultDatabase = Program.AppSettings["Database:Database"];

        // Use provided values or fall back to defaults
        var host = options.Host ?? defaultHost;
        var port = options.Port ?? defaultPort;
        var database = options.Database ?? defaultDatabase;
        var username = options.Username ?? defaultUsername;
        var password = options.Password ?? defaultPassword;

        // Validate required parameters
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host is required when not using --npgsql-connection-string.");
        }

        if (!SecurityUtils.IsValidHost(host))
        {
            throw new ArgumentException($"Invalid host: '{host}'");
        }

        if (!SecurityUtils.IsValidPort(port))
        {
            throw new ArgumentException($"Invalid port: '{port}'. Port must be between 1 and 65535.");
        }

        if (!string.IsNullOrWhiteSpace(username) && !SecurityUtils.IsValidUsername(username))
        {
            throw new ArgumentException($"Invalid username: '{username}'");
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentException("Database name is required when not using --npgsql-connection-string.");
        }

        // Prompt for password if not provided
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Enter password: ");
            password = ReadPassword();
            Console.WriteLine();
        }

        // Build connection string
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Database = database,
            Username = username,
            Password = password
        };

        // Add optional connection parameters
        if (!string.IsNullOrWhiteSpace(options.SslMode))
        {
            builder.SslMode = ParseSslMode(options.SslMode);
        }

        if (options.Timeout.HasValue)
        {
            builder.Timeout = options.Timeout.Value;
        }

        if (options.CommandTimeout.HasValue)
        {
            builder.CommandTimeout = options.CommandTimeout.Value;
        }

        if (options.Pooling.HasValue)
        {
            builder.Pooling = options.Pooling.Value;
        }

        if (options.MinPoolSize.HasValue)
        {
            builder.MinPoolSize = options.MinPoolSize.Value;
        }

        if (options.MaxPoolSize.HasValue)
        {
            builder.MaxPoolSize = options.MaxPoolSize.Value;
        }

        if (options.Keepalive.HasValue)
        {
            builder.KeepAlive = options.Keepalive.Value;
        }

        if (options.ConnectionLifetime.HasValue)
        {
            builder.ConnectionLifetime = options.ConnectionLifetime.Value;
        }

        Console.WriteLine($"Connection: {host}:{port}/{database}");
        Console.WriteLine($"Username: {username}");

        return builder.ConnectionString;
    }

    private static SslMode ParseSslMode(string sslMode)
    {
        if (Enum.TryParse<SslMode>(sslMode, true, out var result))
        {
            return result;
        }

        throw new ArgumentException($"Invalid SSL mode: '{sslMode}'. Valid values are: Disable, Allow, Prefer, Require, VerifyCA, VerifyFull.");
    }

    private static string CleanConnectionString(string connectionString)
    {
        // Remove surrounding quotes if present
        if (connectionString.StartsWith('"') && connectionString.EndsWith('"'))
        {
            return connectionString.Substring(1, connectionString.Length - 2);
        }

        if (connectionString.StartsWith('\'') && connectionString.EndsWith('\''))
        {
            return connectionString.Substring(1, connectionString.Length - 2);
        }

        return connectionString;
    }

    private static int ExecuteQueryAndExportToCsv(string connectionString, string sqlQuery, string outputPath, int? commandTimeout)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sqlQuery, connection);

        if (commandTimeout.HasValue)
        {
            command.CommandTimeout = commandTimeout.Value;
        }

        using var reader = command.ExecuteReader();

        // Get column names
        var columnNames = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

        // Write to CSV
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write header
        foreach (var columnName in columnNames)
        {
            csv.WriteField(columnName);
        }
        csv.NextRecord();

        // Write data
        int rowCount = 0;
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                csv.WriteField(value);
            }
            csv.NextRecord();
            rowCount++;

            // Show progress every 1000 rows
            if (rowCount % 1000 == 0)
            {
                Console.Write($"\rRows processed: {rowCount}");
            }
        }

        if (rowCount > 0)
        {
            Console.Write($"\rRows processed: {rowCount}");
        }

        return rowCount;
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
