using System.CommandLine;

using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;
using FurLab.Core.Models;

using Npgsql;
using Spectre.Console;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides commands for managing FurLab settings and configured servers.
/// </summary>
public static class SettingsCommand
{
    /// <summary>
    /// Builds the settings command structure.
    /// </summary>
    public static Command Build()
    {
        var command = new Command("settings", "Manage FurLab settings and server configurations.");

        var dbServersCommand = new Command("db-servers", "Manage configured database servers.");

        var lsCommand = new Command("ls", "List all configured database servers.");
        lsCommand.SetAction(_ => ListServers());

        var addCommand = new Command("add", "Add a new database server.");
        var nameOption = new Option<string>("--name", "-n") { Description = "Server name (unique identifier)." };
        var hostOption = new Option<string>("--host", "-h") { Description = "Database host address." };
        var portOption = new Option<int>("--port", "-p") { Description = "Database port. Default: 5432." };
        var usernameOption = new Option<string>("--username", "-U") { Description = "Database username." };
        var databasesOption = new Option<string?>("--databases", "-d") { Description = "Comma-separated list of specific databases." };
        var sslModeOption = new Option<string?>("--ssl-mode") { Description = "SSL mode (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull)." };
        var timeoutOption = new Option<int?>("--timeout") { Description = "Connection timeout in seconds." };
        var commandTimeoutOption = new Option<int?>("--command-timeout") { Description = "Command timeout in seconds." };
        var maxParallelismOption = new Option<int?>("--max-parallelism") { Description = "Max degree of parallelism." };
        var fetchAllOption = new Option<bool>("--fetch-all") { Description = "Auto-discover all databases on server." };
        var excludePatternsOption = new Option<string?>("--exclude-patterns") { Description = "Comma-separated patterns to exclude from auto-discovery." };

        addCommand.Add(nameOption);
        addCommand.Add(hostOption);
        addCommand.Add(portOption);
        addCommand.Add(usernameOption);
        addCommand.Add(databasesOption);
        addCommand.Add(sslModeOption);
        addCommand.Add(timeoutOption);
        addCommand.Add(commandTimeoutOption);
        addCommand.Add(maxParallelismOption);
        addCommand.Add(fetchAllOption);
        addCommand.Add(excludePatternsOption);

        addCommand.SetAction(parseResult =>
        {
            var options = new AddServerCommandOptions
            {
                Name = parseResult.GetValue(nameOption),
                Host = parseResult.GetValue(hostOption),
                Port = parseResult.GetValue(portOption),
                Username = parseResult.GetValue(usernameOption),
                Databases = parseResult.GetValue(databasesOption),
                SslMode = parseResult.GetValue(sslModeOption),
                Timeout = parseResult.GetValue(timeoutOption),
                CommandTimeout = parseResult.GetValue(commandTimeoutOption),
                MaxParallelism = parseResult.GetValue(maxParallelismOption),
                FetchAllDatabases = parseResult.GetValue(fetchAllOption),
                ExcludePatterns = parseResult.GetValue(excludePatternsOption)
            };
            AddServer(options);
        });

        var rmCommand = new Command("rm", "Remove a configured database server.");
        var rmNameOption = new Option<string?>("--name", "-n") { Description = "Server name to remove." };

        rmCommand.Add(rmNameOption);

        rmCommand.SetAction(parseResult =>
        {
            var options = new RemoveServerCommandOptions
            {
                Name = parseResult.GetValue(rmNameOption)
            };
            RemoveServer(options);
        });

        var testCommand = new Command("test", "Test connection to a configured database server.");
        var testNameOption = new Option<string?>("--name", "-n") { Description = "Server name to test." };
        testCommand.Add(testNameOption);

        testCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(testNameOption);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = SelectServer("Select a server to test:");
                if (name == null)
                {
                    return 0;
                }
            }

            TestServerConnection(name);
            return 0;
        });

        var setPasswordCommand = new Command("set-password", "Define ou redefine a senha encriptada de um servidor.");
        var setPasswordNameArg = new Argument<string?>("name") { Description = "Nome do servidor. Se omitido, exibe seleção interativa.", Arity = ArgumentArity.ZeroOrOne };
        setPasswordCommand.Add(setPasswordNameArg);

        setPasswordCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(setPasswordNameArg);
            SetPassword(name);
            return 0;
        });

        dbServersCommand.Add(lsCommand);
        dbServersCommand.Add(addCommand);
        dbServersCommand.Add(rmCommand);
        dbServersCommand.Add(testCommand);
        dbServersCommand.Add(setPasswordCommand);

        command.Add(dbServersCommand);

        return command;
    }

    /// <summary>
    /// Lists all configured database servers.
    /// </summary>
    public static void ListServers()
    {
        var servers = UserConfigService.GetServers();

        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers configured. Use 'fur settings db-servers add' to add one.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Host");
        table.AddColumn("Port");
        table.AddColumn("Username");
        table.AddColumn("Databases");
        table.AddColumn("Auto-DB");

        foreach (var server in servers)
        {
            var dbInfo = server.FetchAllDatabases
                ? "[green](auto)[/]"
                : (server.Databases.Count > 0 ? string.Join(", ", server.Databases) : "[dim](default)[/]");

            table.AddRow(
                server.Name.EscapeMarkup(),
                server.Host.EscapeMarkup(),
                server.Port.ToString(),
                server.Username.EscapeMarkup(),
                dbInfo,
                server.FetchAllDatabases ? "[green]Yes[/]" : "[dim]No[/]");
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Adds a new server configuration.
    /// Enters interactive mode automatically when --name or --host are not provided.
    /// </summary>
    public static void AddServer(AddServerCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name) || string.IsNullOrWhiteSpace(options.Host))
        {
            AddServerInteractive(options);
            return;
        }

        AddServerDirect(options);
    }

    private static void AddServerDirect(AddServerCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new ArgumentException("--name (-n) is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new ArgumentException("--host (-h) is required.");
        }

        var existing = UserConfigService.GetServer(options.Name);
        if (existing != null)
        {
            throw new InvalidOperationException($"Server '{options.Name}' already exists. Use a different name or remove the existing one first.");
        }

        var server = BuildServerFromOptions(options);
        UserConfigService.AddOrUpdateServer(server);
        AnsiConsole.MarkupLine($"[green]Server '{server.Name}' added successfully.[/]");
        AnsiConsole.MarkupLine($"[dim]Use 'fur settings db-servers set-password {server.Name.EscapeMarkup()}' to set the password.[/]");
    }

    private static void AddServerInteractive(AddServerCommandOptions options)
    {
        var name = AnsiConsole.Ask("Server name*: ", options.Name ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[red]Server name is required.[/]");
            return;
        }

        var host = AnsiConsole.Ask("Host*: ", options.Host ?? string.Empty);
        if (string.IsNullOrWhiteSpace(host))
        {
            AnsiConsole.MarkupLine("[red]Host is required.[/]");
            return;
        }

        var port = AnsiConsole.Ask("Port [[5432]]: ", options.Port > 0 ? options.Port : 5432);
        var username = AnsiConsole.Ask("Username [[postgres]]: ", !string.IsNullOrWhiteSpace(options.Username) ? options.Username : "postgres");

        var databasesInput = AnsiConsole.Ask<string?>("Databases (comma-separated, optional): ", string.Empty);
        var databases = ParseDatabases(databasesInput);

        var fetchAll = AnsiConsole.Confirm("Auto-discover databases? ", false);
        List<string> excludePatterns = ["template*", "postgres"];
        if (fetchAll)
        {
            var patternsInput = AnsiConsole.Ask<string?>("Exclude patterns (comma-separated) [[template*,postgres]]: ", "template*,postgres");
            excludePatterns = ParseExcludePatterns(patternsInput);
        }

        var sslMode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("SSL Mode [[Prefer]]: ")
                .AddChoices("Disable", "Allow", "Prefer", "Require", "VerifyCA", "VerifyFull")
                .UseConverter(s => s));

        var timeout = AnsiConsole.Ask("Connection timeout [[30s]]: ", 30);
        var commandTimeout = AnsiConsole.Ask("Command timeout [[300s]]: ", 300);
        var maxParallelism = AnsiConsole.Ask("Max Parallelism [[4]]: ", 4);

        var server = new ServerConfigEntry
        {
            Name = name,
            Host = host,
            Port = port,
            Username = username,
            Databases = databases,
            FetchAllDatabases = fetchAll,
            ExcludePatterns = excludePatterns,
            SslMode = sslMode,
            Timeout = timeout,
            CommandTimeout = commandTimeout,
            MaxParallelism = maxParallelism
        };

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Action:")
                .AddChoices("Save and set password", "Save and test connection", "Save without password", "Cancel"));

        if (action == "Cancel")
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return;
        }

        var existing = UserConfigService.GetServer(name);
        if (existing != null)
        {
            throw new InvalidOperationException($"Server '{name}' already exists.");
        }

        UserConfigService.AddOrUpdateServer(server);

        if (action == "Save and set password")
        {
            SavePasswordForServer(name);
        }
        else if (action == "Save and test connection")
        {
            TestServerConnection(name);
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Server '{name.EscapeMarkup()}' added successfully.[/]");
            AnsiConsole.MarkupLine($"[dim]Use 'fur settings db-servers set-password {name.EscapeMarkup()}' to set the password.[/]");
        }
    }

    /// <summary>
    /// Removes a server configuration.
    /// Uses interactive selection when --name is not provided.
    /// </summary>
    public static void RemoveServer(RemoveServerCommandOptions options)
    {
        var servers = UserConfigService.GetServers().ToList();

        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers configured to remove.[/]");
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.Name))
        {
            var removed = UserConfigService.RemoveServer(options.Name);
            if (!removed)
            {
                throw new ArgumentException($"Server '{options.Name}' not found.");
            }

            AnsiConsole.MarkupLine($"[green]Server '{options.Name.EscapeMarkup()}' removed successfully.[/]");
            return;
        }

        // No --name provided: use interactive selection
        var selection = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select servers to remove:")
                .PageSize(10)
                .AddChoices(servers.Select(s => s.Name)));

        if (selection.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers selected. Operation cancelled.[/]");
            return;
        }

        foreach (var name in selection)
        {
            UserConfigService.RemoveServer(name);
            AnsiConsole.MarkupLine($"[green]Server '{name.EscapeMarkup()}' removed.[/]");
        }
    }

    /// <summary>
    /// Tests connection to a configured server.
    /// </summary>
    public static void TestServerConnection(string name)
    {
        var server = UserConfigService.GetServer(name);
        if (server == null)
        {
            throw new ArgumentException($"Server '{name}' not found.");
        }

        AnsiConsole.MarkupLine($"[cyan]Testing connection to {server.Name.EscapeMarkup()} ({server.Host.EscapeMarkup()}:{server.Port})...[/]");

        var password = CredentialService.TryDecrypt(server.EncryptedPassword);
        if (password == null)
        {
            AnsiConsole.MarkupLine($"[yellow]No password found for '{server.Name.EscapeMarkup()}'. Type to test (will not be saved):[/]");
            password = ReadPassword();
        }

        try
        {
            var connectionString = BuildConnectionString(server, "postgres", password);
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            AnsiConsole.MarkupLine($"[green]✓ Connection to {server.Host.EscapeMarkup()}:{server.Port} successful[/]");
            AnsiConsole.MarkupLine($"[green]✓ Authenticated as {server.Username.EscapeMarkup()}[/]");

            try
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT datname FROM pg_database WHERE datistemplate = false AND datallowconn = true ORDER BY datname;",
                    connection);
                using var reader = cmd.ExecuteReader();
                var databases = new List<string>();
                while (reader.Read())
                {
                    databases.Add(reader.GetString(0));
                }

                if (databases.Count > 0)
                {
                    AnsiConsole.MarkupLine($"[green]✓ Databases found: {string.Join(", ", databases)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]! No accessible databases found[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]! Could not list databases: {ex.Message.EscapeMarkup()}[/]");
            }
        }
        catch (NpgsqlException ex)
        {
            if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Authentication failed: {ex.Message}", ex);
            }

            throw new InvalidOperationException($"Connection failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            throw new InvalidOperationException($"Connection failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Defines or redefines the encrypted password for a server.
    /// </summary>
    public static void SetPassword(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = SelectServer("Select a server to set the password:");
            if (name == null)
            {
                return;
            }
        }

        SavePasswordForServer(name);
    }

    /// <summary>
    /// Shared helper: prompts for password, encrypts, and saves for the given server.
    /// </summary>
    private static void SavePasswordForServer(string serverName)
    {
        var server = UserConfigService.GetServer(serverName);
        if (server == null)
        {
            throw new ArgumentException($"Server '{serverName}' not found.");
        }

        var password = ReadPassword();
        var encrypted = CredentialService.Encrypt(password);
        UserConfigService.SetEncryptedPassword(serverName, encrypted);
        AnsiConsole.MarkupLine($"[green]Password saved securely for '{serverName.EscapeMarkup()}'.[/]");
    }

    /// <summary>
    /// Displays an interactive server selection prompt and returns the selected server name.
    /// Returns <see langword="null"/> if no servers are configured or user cancels.
    /// </summary>
    private static string? SelectServer(string prompt)
    {
        var servers = UserConfigService.GetServers();

        if (servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers configured. Use 'fur settings db-servers add' to add one.[/]");
            return null;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(10)
                .AddChoices(servers.Select(s => s.Name)));
    }

    private static ServerConfigEntry BuildServerFromOptions(AddServerCommandOptions options)
    {
        var databases = ParseDatabases(options.Databases);
        var excludePatterns = ParseExcludePatterns(options.ExcludePatterns);

        return new ServerConfigEntry
        {
            Name = options.Name ?? string.Empty,
            Host = options.Host ?? string.Empty,
            Port = options.Port,
            Username = options.Username ?? string.Empty,
            Databases = databases,
            FetchAllDatabases = options.FetchAllDatabases,
            ExcludePatterns = excludePatterns,
            SslMode = options.SslMode ?? "Prefer",
            Timeout = options.Timeout ?? 30,
            CommandTimeout = options.CommandTimeout ?? 300,
            MaxParallelism = options.MaxParallelism ?? 4
        };
    }

    private static string BuildConnectionString(ServerConfigEntry server, string database, string password)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = server.Host,
            Port = server.Port,
            Username = server.Username,
            Password = password,
            Database = database,
            SslMode = Enum.TryParse<SslMode>(server.SslMode, true, out var sslMode) ? sslMode : SslMode.Prefer,
            Timeout = server.Timeout,
            CommandTimeout = server.CommandTimeout
        };
        return csb.ConnectionString;
    }

    private static List<string> ParseDatabases(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(db => !string.IsNullOrWhiteSpace(db))
            .ToList();
    }

    private static List<string> ParseExcludePatterns(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ["template*", "postgres"];
        }

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    private static string ReadPassword()
    {
        AnsiConsole.MarkupLine("[dim]Password: [/]");
        var password = new System.Security.SecureString();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.RemoveAt(password.Length - 1);
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                password.AppendChar(key.KeyChar);
                Console.Write("*");
            }
        }

        var ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(password);
        try
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr) ?? string.Empty;
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
        }
    }
}
