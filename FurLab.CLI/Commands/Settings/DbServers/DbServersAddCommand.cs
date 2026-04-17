using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Settings.DbServers;

/// <summary>
/// Adds a new database server configuration.
/// Enters interactive mode when <c>--name</c> or <c>--host</c> are not provided.
/// </summary>
public sealed class DbServersAddCommand : AsyncCommand<DbServersAddCommand.Settings>
{
    private readonly IUserConfigService _userConfigService;
    private readonly ICredentialService _credentialService;
    private readonly IPostgresPasswordHandler _postgresPasswordHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbServersAddCommand"/> class.
    /// </summary>
    /// <param name="userConfigService">The user configuration service.</param>
    /// <param name="credentialService">The credential service for encrypting passwords.</param>
    /// <param name="postgresPasswordHandler">The password handler for interactive password input.</param>
    public DbServersAddCommand(
        IUserConfigService userConfigService,
        ICredentialService credentialService,
        IPostgresPasswordHandler postgresPasswordHandler)
    {
        _userConfigService = userConfigService;
        _credentialService = credentialService;
        _postgresPasswordHandler = postgresPasswordHandler;
    }

    /// <summary>
    /// Settings for the db-servers add command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the server name (unique identifier).
        /// </summary>
        [CommandOption("-n|--name")]
        [System.ComponentModel.Description("Server name (unique identifier).")]
        public string? Name { get; init; }

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
        [System.ComponentModel.Description("Database port. Default: 5432.")]
        public int Port { get; init; } = 5432;

        /// <summary>
        /// Gets the database username.
        /// </summary>
        [CommandOption("-U|--username")]
        [System.ComponentModel.Description("Database username.")]
        public string? Username { get; init; }

        /// <summary>
        /// Gets the comma-separated list of specific databases.
        /// </summary>
        [CommandOption("-d|--databases")]
        [System.ComponentModel.Description("Comma-separated list of specific databases.")]
        public string? Databases { get; init; }

        /// <summary>
        /// Gets the SSL mode.
        /// </summary>
        [CommandOption("--ssl-mode")]
        [System.ComponentModel.Description("SSL mode (Disable, Allow, Prefer, Require, VerifyCA, VerifyFull).")]
        public string? SslMode { get; init; }

        /// <summary>
        /// Gets the connection timeout in seconds.
        /// </summary>
        [CommandOption("--timeout")]
        [System.ComponentModel.Description("Connection timeout in seconds.")]
        public int? Timeout { get; init; }

        /// <summary>
        /// Gets the command timeout in seconds.
        /// </summary>
        [CommandOption("--command-timeout")]
        [System.ComponentModel.Description("Command timeout in seconds.")]
        public int? CommandTimeout { get; init; }

        /// <summary>
        /// Gets the max degree of parallelism.
        /// </summary>
        [CommandOption("--max-parallelism")]
        [System.ComponentModel.Description("Max degree of parallelism.")]
        public int? MaxParallelism { get; init; }

        /// <summary>
        /// Gets a value indicating whether to auto-discover all databases on server.
        /// </summary>
        [CommandOption("--fetch-all")]
        [System.ComponentModel.Description("Auto-discover all databases on server.")]
        public bool FetchAllDatabases { get; init; }

        /// <summary>
        /// Gets the comma-separated patterns to exclude from auto-discovery.
        /// </summary>
        [CommandOption("--exclude-patterns")]
        [System.ComponentModel.Description("Comma-separated patterns to exclude from auto-discovery.")]
        public string? ExcludePatterns { get; init; }
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(settings.Name) || string.IsNullOrWhiteSpace(settings.Host))
        {
            return Task.FromResult(AddServerInteractive(settings));
        }

        return Task.FromResult(AddServerDirect(settings));
    }

    private int AddServerDirect(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            throw new ArgumentException("--name (-n) is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            throw new ArgumentException("--host (-h) is required.");
        }

        if (!SecurityUtils.IsValidHost(settings.Host))
        {
            throw new ArgumentException($"Invalid host: '{settings.Host}'.");
        }

        if (settings.Port is < 1 or > 65535)
        {
            throw new ArgumentException($"Invalid port: {settings.Port}. Must be between 1 and 65535.");
        }

        if (!string.IsNullOrWhiteSpace(settings.Username) && !SecurityUtils.IsValidUsername(settings.Username))
        {
            throw new ArgumentException($"Invalid username: '{settings.Username}'.");
        }

        var existing = _userConfigService.GetServer(settings.Name);
        if (existing != null)
        {
            throw new InvalidOperationException($"Server '{settings.Name}' already exists. Use a different name or remove the existing one first.");
        }

        var server = BuildServerFromSettings(settings);
        _userConfigService.AddOrUpdateServer(server);
        AnsiConsole.MarkupLine($"[green]Server '{server.Name.EscapeMarkup()}' added successfully.[/]");
        AnsiConsole.MarkupLine($"[dim]Use 'fur settings db-servers set-password {server.Name.EscapeMarkup()}' to set the password.[/]");
        return 0;
    }

    private int AddServerInteractive(Settings settings)
    {
        var name = AnsiConsole.Ask("Server name*: ", settings.Name ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[red]Server name is required.[/]");
            return 1;
        }

        var host = AnsiConsole.Ask("Host*: ", settings.Host ?? string.Empty);
        if (string.IsNullOrWhiteSpace(host))
        {
            AnsiConsole.MarkupLine("[red]Host is required.[/]");
            return 1;
        }

        if (!SecurityUtils.IsValidHost(host))
        {
            AnsiConsole.MarkupLine($"[red]Invalid host: '{host.EscapeMarkup()}'.[/]");
            return 1;
        }

        var port = AnsiConsole.Ask("Port [[5432]]: ", settings.Port > 0 ? settings.Port : 5432);
        if (port is < 1 or > 65535)
        {
            AnsiConsole.MarkupLine($"[red]Invalid port: {port}. Must be between 1 and 65535.[/]");
            return 1;
        }

        var defaultUsername = !string.IsNullOrWhiteSpace(settings.Username) ? settings.Username : "postgres";
        var username = AnsiConsole.Ask("Username [[postgres]]: ", defaultUsername);
        if (!SecurityUtils.IsValidUsername(username))
        {
            AnsiConsole.MarkupLine($"[red]Invalid username: '{username.EscapeMarkup()}'.[/]");
            return 1;
        }

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
                .AddChoices("Disable", "Allow", "Prefer", "Require", "VerifyCA", "VerifyFull"));

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

        var existing = _userConfigService.GetServer(name);
        if (existing != null)
        {
            throw new InvalidOperationException($"Server '{name}' already exists.");
        }

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Action:")
                .AddChoices("Save and set password", "Save and test connection", "Save without password", "Cancel"));

        if (action == "Cancel")
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 0;
        }

        _userConfigService.AddOrUpdateServer(server);

        if (action == "Save and set password")
        {
            SavePasswordForServer(name);
        }
        else if (action == "Save and test connection")
        {
            AnsiConsole.MarkupLine($"[green]Server '{name.EscapeMarkup()}' added successfully.[/]");
            AnsiConsole.MarkupLine($"[dim]Run 'fur settings db-servers test {name.EscapeMarkup()}' to test the connection.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Server '{name.EscapeMarkup()}' added successfully.[/]");
            AnsiConsole.MarkupLine($"[dim]Use 'fur settings db-servers set-password {name.EscapeMarkup()}' to set the password.[/]");
        }

        return 0;
    }

    private void SavePasswordForServer(string serverName)
    {
        var server = _userConfigService.GetServer(serverName);
        if (server == null)
        {
            throw new ArgumentException($"Server '{serverName}' not found.");
        }

        var password = _postgresPasswordHandler.ReadPasswordInteractively("Password: ");
        var encrypted = _credentialService.Encrypt(password);
        _userConfigService.SetEncryptedPassword(serverName, encrypted);
        AnsiConsole.MarkupLine($"[green]Password saved securely for '{serverName.EscapeMarkup()}'.[/]");
    }

    private static ServerConfigEntry BuildServerFromSettings(Settings settings)
    {
        var databases = ParseDatabases(settings.Databases);
        var excludePatterns = ParseExcludePatterns(settings.ExcludePatterns);

        return new ServerConfigEntry
        {
            Name = settings.Name ?? string.Empty,
            Host = settings.Host ?? string.Empty,
            Port = settings.Port,
            Username = settings.Username ?? string.Empty,
            Databases = databases,
            FetchAllDatabases = settings.FetchAllDatabases,
            ExcludePatterns = excludePatterns,
            SslMode = settings.SslMode ?? "Prefer",
            Timeout = settings.Timeout ?? 30,
            CommandTimeout = settings.CommandTimeout ?? 300,
            MaxParallelism = settings.MaxParallelism ?? 4
        };
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
}
