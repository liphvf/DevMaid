using System.CommandLine;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;

using Npgsql;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides commands for executing SQL queries and exporting results to CSV.
/// </summary>
public static class QueryCommand
{
    /// <summary>
    /// Builds the query command structure.
    /// </summary>
    /// <returns>The configured <see cref="Command"/>.</returns>
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

        // Multi-server options
        var serversOption = new Option<bool>("--servers", "-s")
        {
            Description = "Execute the query on all servers configured in appsettings.json."
        };

        var serverFilterOption = new Option<string?>("--server-filter")
        {
            Description = "Filter servers by name pattern (supports * wildcard). Example: 'prod-*' or '*-primary'."
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
        runCommand.Add(serversOption);
        runCommand.Add(serverFilterOption);

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
                Exclude = parseResult.GetValue(excludeOption),
                Servers = parseResult.GetValue(serversOption),
                ServerFilter = parseResult.GetValue(serverFilterOption)
            };

            Run(options);
        });

        command.Add(runCommand);

        return command;
    }

    /// <summary>
    /// Executes a SQL query and exports the results to CSV.
    /// </summary>
    /// <param name="options">The query execution options.</param>
    /// <exception cref="ArgumentException">Thrown when required options are missing or invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the SQL input file does not exist.</exception>
    public static void Run(QueryCommandOptions options)
    {
        // Validate input file
        if (string.IsNullOrWhiteSpace(options.InputFile))
        {
            throw new ArgumentException("Input file path is required.");
        }

        // Validate for path traversal before normalizing the path
        if (!SecurityUtils.IsValidPath(options.InputFile))
        {
            throw new ArgumentException($"Invalid input path: '{options.InputFile}'. Path traversal not allowed.");
        }

        var inputFullPath = Path.GetFullPath(options.InputFile);
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

        // Check if --servers flag is set
        if (options.Servers)
        {
            RunOnAllServers(options, sqlQuery);
            return;
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

        // Validate for path traversal before normalizing the path
        if (!SecurityUtils.IsValidPath(options.OutputFile))
        {
            throw new ArgumentException($"Invalid output path: '{options.OutputFile}'. Path traversal not allowed.");
        }

        var outputDirectory = Path.GetFullPath(options.OutputFile);

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
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

        var data = new List<Dictionary<string, string>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount; i++)
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
        // Load default values from configuration
        var dbConfig = ConfigurationService.GetDatabaseConfig();
        var defaultHost = dbConfig.Host ?? "localhost";
        var defaultPort = dbConfig.Port ?? "5432";
        var defaultUsername = dbConfig.Username;
        var defaultPassword = dbConfig.Password;

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

        if (HasExplicitConnectionParameters(options))
        {
            var directConnection = ResolveExplicitConnection(options);
            LogConnectionContext(directConnection);
            return BuildConnectionStringForResolvedConnection(directConnection, options);
        }

        var serverConnection = ResolvePrimaryServerConnection(options);
        LogConnectionContext(serverConnection);
        return BuildConnectionStringForResolvedConnection(serverConnection, options);
    }

    private static bool HasExplicitConnectionParameters(QueryCommandOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Host)
            || !string.IsNullOrWhiteSpace(options.Port)
            || !string.IsNullOrWhiteSpace(options.Database)
            || !string.IsNullOrWhiteSpace(options.Username)
            || !string.IsNullOrWhiteSpace(options.Password);
    }

    private static ResolvedQueryConnection ResolveExplicitConnection(QueryCommandOptions options)
    {
        var dbConfig = ConfigurationService.GetDatabaseConfig();

        return CreateResolvedConnection(
            host: options.Host ?? dbConfig.Host ?? "localhost",
            port: options.Port ?? dbConfig.Port ?? "5432",
            database: options.Database,
            username: options.Username ?? dbConfig.Username,
            password: options.Password ?? dbConfig.Password,
            sslMode: options.SslMode,
            timeout: options.Timeout,
            commandTimeout: options.CommandTimeout,
            pooling: options.Pooling,
            minPoolSize: options.MinPoolSize,
            maxPoolSize: options.MaxPoolSize,
            keepalive: options.Keepalive,
            connectionLifetime: options.ConnectionLifetime,
            missingDatabaseMessage: "Database name is required when using direct connection parameters. Specify --database or use the Servers configuration.");
    }

    private static ResolvedQueryConnection ResolvePrimaryServerConnection(QueryCommandOptions options)
    {
        var primaryServerName = ConfigurationService.Configuration["Servers:PrimaryServer"];
        if (string.IsNullOrWhiteSpace(primaryServerName))
        {
            throw new ArgumentException("PrimaryServer is not configured in appsettings.json. Please set 'Servers:PrimaryServer'.");
        }

        var servers = LoadServersConfiguration(checkEnabled: false);
        if (servers == null || servers.Count == 0)
        {
            throw new ArgumentException("No servers configured in appsettings.json. Please add servers configuration under 'Servers:ServersList'.");
        }

        var primaryServer = servers.FirstOrDefault(s => s.Name.Equals(primaryServerName, StringComparison.OrdinalIgnoreCase));
        if (primaryServer == null)
        {
            throw new ArgumentException($"Primary server '{primaryServerName}' not found in ServersList. Available servers: {string.Join(", ", servers.Select(s => s.Name))}");
        }

        return CreateResolvedConnection(
            host: primaryServer.Host,
            port: primaryServer.Port,
            database: primaryServer.Database,
            username: primaryServer.Username,
            password: primaryServer.Password,
            sslMode: options.SslMode ?? primaryServer.SslMode,
            timeout: options.Timeout ?? primaryServer.Timeout,
            commandTimeout: options.CommandTimeout ?? primaryServer.CommandTimeout,
            pooling: options.Pooling ?? primaryServer.Pooling,
            minPoolSize: options.MinPoolSize ?? primaryServer.MinPoolSize,
            maxPoolSize: options.MaxPoolSize ?? primaryServer.MaxPoolSize,
            keepalive: options.Keepalive ?? primaryServer.Keepalive,
            connectionLifetime: options.ConnectionLifetime ?? primaryServer.ConnectionLifetime,
            primaryServerName: primaryServerName,
            missingDatabaseMessage: "Database name is required. Either specify --database, configure Database in the server config, or use --all.");
    }

    private static ResolvedQueryConnection CreateResolvedConnection(
        string? host,
        string? port,
        string? database,
        string? username,
        string? password,
        string? sslMode,
        int? timeout,
        int? commandTimeout,
        bool? pooling,
        int? minPoolSize,
        int? maxPoolSize,
        int? keepalive,
        int? connectionLifetime,
        string missingDatabaseMessage,
        string? primaryServerName = null)
    {
        ValidateConnectionParameters(host, port, username, database, missingDatabaseMessage);
        var resolvedPassword = PromptForPasswordIfMissing(password);

        return new ResolvedQueryConnection(
            host ?? string.Empty,
            port ?? string.Empty,
            database ?? string.Empty,
            username ?? string.Empty,
            resolvedPassword,
            sslMode,
            timeout,
            commandTimeout,
            pooling,
            minPoolSize,
            maxPoolSize,
            keepalive,
            connectionLifetime,
            primaryServerName);
    }

    private static void ValidateConnectionParameters(string? host, string? port, string? username, string? database, string missingDatabaseMessage)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host is required when not using --npgsql-connection-string.");
        }

        if (!SecurityUtils.IsValidHost(host))
        {
            throw new ArgumentException($"Invalid host: '{host}'");
        }

        if (string.IsNullOrWhiteSpace(port) || !SecurityUtils.IsValidPort(port))
        {
            throw new ArgumentException($"Invalid port: '{port}'. Port must be between 1 and 65535.");
        }

        if (!string.IsNullOrWhiteSpace(username) && !SecurityUtils.IsValidUsername(username))
        {
            throw new ArgumentException($"Invalid username: '{username}'");
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentException(missingDatabaseMessage);
        }
    }

    private static string PromptForPasswordIfMissing(string? password)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            return password;
        }

        Console.Write("Enter password: ");
        var enteredPassword = ReadPassword();
        Console.WriteLine();
        return enteredPassword;
    }

    private static void LogConnectionContext(ResolvedQueryConnection connection)
    {
        Console.WriteLine($"Connection: {connection.Host}:{connection.Port}/{connection.Database}");
        Console.WriteLine($"Username: {connection.Username}");

        if (!string.IsNullOrWhiteSpace(connection.PrimaryServerName))
        {
            Console.WriteLine($"Primary Server: {connection.PrimaryServerName}");
        }
    }

    private static string BuildConnectionStringForResolvedConnection(ResolvedQueryConnection connection, QueryCommandOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = int.Parse(connection.Port),
            Database = connection.Database,
            Username = connection.Username,
            Password = connection.Password
        };

        if (!string.IsNullOrWhiteSpace(connection.SslMode))
        {
            builder.SslMode = ParseSslMode(connection.SslMode);
        }

        if (connection.Timeout.HasValue)
        {
            builder.Timeout = connection.Timeout.Value;
        }

        if (connection.CommandTimeout.HasValue)
        {
            builder.CommandTimeout = connection.CommandTimeout.Value;
        }

        if (connection.Pooling.HasValue)
        {
            builder.Pooling = connection.Pooling.Value;
        }

        if (connection.MinPoolSize.HasValue)
        {
            builder.MinPoolSize = connection.MinPoolSize.Value;
        }

        if (connection.MaxPoolSize.HasValue)
        {
            builder.MaxPoolSize = connection.MaxPoolSize.Value;
        }

        if (connection.Keepalive.HasValue)
        {
            builder.KeepAlive = connection.Keepalive.Value;
        }

        if (connection.ConnectionLifetime.HasValue)
        {
            builder.ConnectionLifetime = connection.ConnectionLifetime.Value;
        }

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
        for (var i = 0; i < reader.FieldCount; i++)
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
        var rowCount = 0;
        while (reader.Read())
        {
            for (var i = 0; i < reader.FieldCount; i++)
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

    private static void RunOnAllServers(QueryCommandOptions options, string sqlQuery)
    {
        // Validate output directory for multi-server mode
        if (string.IsNullOrWhiteSpace(options.OutputFile))
        {
            throw new ArgumentException("Output directory path is required when using --servers.");
        }

        if (!SecurityUtils.IsValidPath(options.OutputFile))
        {
            throw new ArgumentException($"Invalid output path: '{options.OutputFile}'. Path traversal not allowed.");
        }

        var outputDirectory = Path.GetFullPath(options.OutputFile);

        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Load servers configuration from appsettings.json
        var servers = LoadServersConfiguration();

        if (servers == null || servers.Count == 0)
        {
            throw new ArgumentException("No servers configured in appsettings.json. Please add servers configuration under 'Servers:ServersList'.");
        }

        // Filter servers by pattern if provided
        var filteredServers = FilterServers(servers, options.ServerFilter);

        if (filteredServers.Count == 0)
        {
            throw new ArgumentException($"No servers found matching filter pattern: '{options.ServerFilter}'");
        }

        Console.WriteLine($"Found {filteredServers.Count} servers to process:");
        foreach (var server in filteredServers)
        {
            Console.WriteLine($"  - {server.Name} ({server.Host}:{server.Port})");
        }
        Console.WriteLine();
        Console.WriteLine($"Output directory: {Path.GetFullPath(outputDirectory)}");
        Console.WriteLine();

        // Execute query on all servers
        var successCount = 0;
        var failureCount = 0;
        var totalRowCount = 0;

        foreach (var server in filteredServers)
        {
            var serverOutputDir = Path.Combine(outputDirectory, server.Name);

            Console.WriteLine($"========================================");
            Console.WriteLine($"Processing server: {server.Name}");
            Console.WriteLine($"Host: {server.Host}:{server.Port}");
            Console.WriteLine($"========================================");
            Console.WriteLine();

            try
            {
                // Determine databases to query
                List<string> databasesToQuery;

                if (server.Databases != null && server.Databases.Count > 0)
                {
                    // Use specific databases from server config
                    databasesToQuery = server.Databases;
                    Console.WriteLine($"Using configured databases: {string.Join(", ", databasesToQuery)}");
                }
                else if (options.All)
                {
                    // Query all databases on the server
                    Console.WriteLine("Querying all databases on server...");
                    databasesToQuery = ListAllDatabases(server.Host, server.Port, server.Username, server.Password);

                    // Apply exclude filter if provided
                    if (!string.IsNullOrWhiteSpace(options.Exclude))
                    {
                        var excludedDatabases = new HashSet<string>(options.Exclude.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        databasesToQuery = databasesToQuery.FindAll(db => !excludedDatabases.Contains(db));
                    }
                }
                else
                {
                    // Use default database from server config
                    if (string.IsNullOrWhiteSpace(server.Database))
                    {
                        throw new ArgumentException($"No database specified for server '{server.Name}'. Either configure Database in the server config, use --all, or specify databases in the Databases list");
                    }
                    databasesToQuery = new List<string> { server.Database };
                    Console.WriteLine($"Using default database: {server.Database}");
                }

                Console.WriteLine();

                // Create server output directory
                if (!Directory.Exists(serverOutputDir))
                {
                    Directory.CreateDirectory(serverOutputDir);
                }

                // Execute query on each database
                var serverSuccessCount = 0;
                var serverFailureCount = 0;
                var serverRowCount = 0;

                foreach (var database in databasesToQuery)
                {
                    var outputFilePath = Path.Combine(serverOutputDir, $"{database}.csv");

                    Console.WriteLine($"  Processing database '{database}'...");

                    try
                    {
                        // Build connection string for this database
                        var connectionString = BuildConnectionStringForServer(server, database, options);

                        // Execute query and export to CSV
                        var rowCount = ExecuteQueryAndExportToCsv(connectionString, sqlQuery, outputFilePath, server.CommandTimeout ?? options.CommandTimeout);

                        serverRowCount += rowCount;
                        serverSuccessCount++;

                        Console.WriteLine($"    ✓ Results exported to: {database}.csv ({rowCount} rows)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    ✗ Failed: {ex.Message}");
                        serverFailureCount++;
                    }
                }

                totalRowCount += serverRowCount;
                successCount += serverSuccessCount;
                failureCount += serverFailureCount;

                Console.WriteLine();
                Console.WriteLine($"Server '{server.Name}' summary:");
                Console.WriteLine($"  Successful: {serverSuccessCount}");
                Console.WriteLine($"  Failed: {serverFailureCount}");
                Console.WriteLine($"  Total rows: {serverRowCount}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ Server '{server.Name}' failed: {ex.Message}");
                failureCount++;
                Console.WriteLine();
            }
        }

        // Overall summary
        Console.WriteLine("========================================");
        Console.WriteLine("Overall Execution Summary:");
        Console.WriteLine($"  Servers processed: {filteredServers.Count}");
        Console.WriteLine($"  Successful databases: {successCount}");
        Console.WriteLine($"  Failed databases: {failureCount}");
        Console.WriteLine($"  Total rows: {totalRowCount}");
        Console.WriteLine($"  Output directory: {Path.GetFullPath(outputDirectory)}");
        Console.WriteLine("========================================");
    }

    private static List<ServerConfig>? LoadServersConfiguration(bool checkEnabled = true)
    {
        try
        {
            // Check if servers configuration is enabled (only when explicitly requested)
            if (checkEnabled)
            {
                var enabledValue = ConfigurationService.Configuration["Servers:Enabled"];
                if (enabledValue == null || !bool.TryParse(enabledValue, out var enabled) || !enabled)
                {
                    throw new ArgumentException("Multi-server configuration is not enabled. Set 'Servers:Enabled' to true in appsettings.json");
                }
            }

            // Load servers list
            var serversSection = ConfigurationService.Configuration["Servers:ServersList"];
            if (string.IsNullOrWhiteSpace(serversSection))
            {
                return null;
            }

            // Parse JSON configuration
            var serversJson = $"{{\"Servers\":{serversSection}}}";
            var config = System.Text.Json.JsonSerializer.Deserialize<ServersConfig>(serversJson);

            return config?.Servers;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to load servers configuration from appsettings.json: {ex.Message}");
        }
    }

    private static List<ServerConfig> FilterServers(List<ServerConfig> servers, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return servers;
        }

        var filtered = new List<ServerConfig>();
        var pattern = filter.Replace("*", ".*");

        foreach (var server in servers)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(server.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                filtered.Add(server);
            }
        }

        return filtered;
    }

    private static string BuildConnectionStringForServer(ServerConfig server, string database, QueryCommandOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = server.Host,
            Port = int.Parse(server.Port),
            Database = database,
            Username = server.Username,
            Password = server.Password
        };

        // Use server-specific options or fall back to command options
        var sslMode = server.SslMode ?? options.SslMode;
        var timeout = server.Timeout ?? options.Timeout;
        var commandTimeout = server.CommandTimeout ?? options.CommandTimeout;
        var pooling = server.Pooling ?? options.Pooling;
        var minPoolSize = server.MinPoolSize ?? options.MinPoolSize;
        var maxPoolSize = server.MaxPoolSize ?? options.MaxPoolSize;
        var keepalive = server.Keepalive ?? options.Keepalive;
        var connectionLifetime = server.ConnectionLifetime ?? options.ConnectionLifetime;

        if (!string.IsNullOrWhiteSpace(sslMode))
        {
            builder.SslMode = ParseSslMode(sslMode);
        }

        if (timeout.HasValue)
        {
            builder.Timeout = timeout.Value;
        }

        if (commandTimeout.HasValue)
        {
            builder.CommandTimeout = commandTimeout.Value;
        }

        if (pooling.HasValue)
        {
            builder.Pooling = pooling.Value;
        }

        if (minPoolSize.HasValue)
        {
            builder.MinPoolSize = minPoolSize.Value;
        }

        if (maxPoolSize.HasValue)
        {
            builder.MaxPoolSize = maxPoolSize.Value;
        }

        if (keepalive.HasValue)
        {
            builder.KeepAlive = keepalive.Value;
        }

        if (connectionLifetime.HasValue)
        {
            builder.ConnectionLifetime = connectionLifetime.Value;
        }

        return builder.ConnectionString;
    }

    private sealed record ResolvedQueryConnection(
        string Host,
        string Port,
        string Database,
        string Username,
        string Password,
        string? SslMode,
        int? Timeout,
        int? CommandTimeout,
        bool? Pooling,
        int? MinPoolSize,
        int? MaxPoolSize,
        int? Keepalive,
        int? ConnectionLifetime,
        string? PrimaryServerName);
}

// Helper class for deserializing servers configuration
/// <summary>
/// Represents the top-level servers configuration for deserialization.
/// </summary>
public class ServersConfig
{
    /// <summary>Gets or sets the list of server configurations.</summary>
    public List<ServerConfig> Servers { get; set; } = new();
}
