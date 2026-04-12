using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides commands for managing OpenCode configuration.
/// </summary>
public static class OpenCodeCommand
{
    private static readonly string GlobalConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "opencode");

    /// <summary>
    /// Hook that overrides model fetching in tests. When non-null, replaces the
    /// real <c>opencode models</c> process call inside <see cref="GetAvailableModels"/>.
    /// </summary>
    internal static Func<IReadOnlyList<string>>? ModelsProvider { get; set; }

    /// <summary>
    /// Builds the opencode command structure.
    /// </summary>
    /// <returns>The configured <see cref="Command"/>.</returns>
    public static Command Build()
    {
        var command = new Command("opencode", "Comandos para OpenCode");

        command.SetAction(parseResult =>
        {
            var helpOption = new System.CommandLine.Help.HelpOption();
            ((System.CommandLine.Help.HelpAction)helpOption.Action!).Invoke(parseResult);
        });

        var settingsCommand = new Command("settings", "Configuracoes do OpenCode");

        var mcpDatabaseCommand = new Command("mcp-database", "Configura o MCP database no config.json do OpenCode");
        mcpDatabaseCommand.SetAction(_ =>
        {
            ConfigureMcpDatabase();
        });

        var defaultModelCommand = BuildDefaultModelCommand();

        settingsCommand.Add(mcpDatabaseCommand);
        settingsCommand.Add(defaultModelCommand);
        command.Add(settingsCommand);

        return command;
    }

    private static Command BuildDefaultModelCommand()
    {
        var modelIdArgument = new Argument<string?>("model-id")
        {
            Description = "Model ID to set as default. If omitted, displays interactive menu.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var globalFlag = new Option<bool>("--global")
        {
            Description = "Changes the global configuration (~/.config/opencode/opencode.jsonc) instead of local."
        };

        var defaultModelCommand = new Command("default-model", "Set the default OpenCode model")
        {
            modelIdArgument,
            globalFlag
        };

        defaultModelCommand.SetAction(parseResult =>
        {
            var modelId = parseResult.GetValue(modelIdArgument);
            var isGlobal = parseResult.GetValue(globalFlag);
            SetDefaultModel(modelId, isGlobal);
        });

        return defaultModelCommand;
    }

    /// <summary>
    /// Sets the default model in the OpenCode configuration file.
    /// </summary>
    /// <param name="modelId">The model ID to set, or null to use interactive selection.</param>
    /// <param name="global">If true, modifies the global config; otherwise modifies the local config.</param>
    public static void SetDefaultModel(string? modelId, bool global)
    {
        string selectedModel;

        if (modelId is not null)
        {
            // Model ID supplied explicitly: try to validate, but proceed anyway if opencode is not in PATH.
            try
            {
                var availableModels = GetAvailableModels();
                if (!availableModels.Contains(modelId, StringComparer.Ordinal))
                {
                    Console.Error.WriteLine($"Model '{modelId}' not found.");
                    Console.Error.WriteLine("Available models:");
                    foreach (var m in availableModels)
                    {
                        Console.Error.WriteLine($"  {m}");
                    }

                    Environment.Exit(1);
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // opencode not available in PATH (e.g. Desktop installer without PATH entry).
                // Skip validation and trust the provided model ID.
                AnsiConsole.MarkupLine(
                    "[yellow]Warning:[/] Could not validate model (opencode not found in PATH). Setting without validation.");
            }

            selectedModel = modelId;
        }
        else
        {
            // No model ID supplied: need opencode to list candidates for interactive selection.
            IReadOnlyList<string> availableModels;
            try
            {
                availableModels = GetAvailableModels();
            }
            catch (InvalidOperationException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                AnsiConsole.MarkupLine(
                    "[yellow]Tip:[/] Provide the model ID directly: FurLab opencode settings default-model [grey]<model-id>[/]");
                Environment.Exit(1);
                return;
            }

            var chosen = SelectModelInteractively(availableModels);
            if (chosen is null)
            {
                return;
            }

            selectedModel = chosen;
        }

        var configPath = ResolveConfigPath(global);
        var configDir = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException($"Could not determine configuration file directory: {configPath}");
        Directory.CreateDirectory(configDir);

        var config = LoadConfigFile(configPath);
        config["model"] = selectedModel;
        SaveConfigFile(configPath, config);

        AnsiConsole.MarkupLine($"[green]Model '{selectedModel}' set at: {configPath}[/]");
    }

    /// <summary>
    /// Resolves the config file path to use based on scope and existing files.
    /// Priority: opencode.jsonc > opencode.json > creates opencode.jsonc
    /// </summary>
    /// <param name="global">If true, resolves within the global OpenCode config directory.</param>
    /// <returns>The resolved file path.</returns>
    public static string ResolveConfigPath(bool global)
    {
        var directory = global ? GlobalConfigDir : Directory.GetCurrentDirectory();

        var jsonc = Path.Combine(directory, "opencode.jsonc");
        var json = Path.Combine(directory, "opencode.json");

        if (File.Exists(jsonc)) return jsonc;
        if (File.Exists(json)) return json;
        return jsonc;
    }

    /// <summary>
    /// Looks for the opencode executable in known installation locations.
    /// Falls back to a plain <c>"opencode"</c> name so the OS PATH is tried last.
    /// </summary>
    /// <returns>Full path to the executable (or script), or <c>"opencode"</c> if no known path was found.</returns>
    internal static string ResolveOpenCodeExecutable()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // 1. WinGet portable package: winget install SST.opencode
        var wingetPackages = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");
        if (Directory.Exists(wingetPackages))
        {
            foreach (var dir in Directory.GetDirectories(wingetPackages, "SST.opencode_*"))
            {
                var exe = Path.Combine(dir, "opencode.exe");
                if (File.Exists(exe)) return exe;
            }
        }

        // 2. npm global install: npm install -g opencode
        //    Installs a .ps1 wrapper to %APPDATA%\npm\opencode.ps1
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var npmPs1 = Path.Combine(appData, "npm", "opencode.ps1");
        if (File.Exists(npmPs1)) return npmPs1;

        // 3. Fall back to PATH lookup
        return "opencode";
    }

    /// <summary>
    /// Returns the list of available models from the `opencode models` command.
    /// </summary>
    /// <returns>A read-only list of model IDs.</returns>
    /// <exception cref="InvalidOperationException">Thrown when opencode is not found.</exception>
    public static IReadOnlyList<string> GetAvailableModels()
    {
        if (ModelsProvider is not null)
        {
            return ModelsProvider();
        }

        var executable = ResolveOpenCodeExecutable();

        try
        {
            using var process = new Process();
            process.StartInfo = executable.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)
                ? new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NonInteractive -NoProfile -File \"{executable}\" models",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
                : new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "models",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList()
                .AsReadOnly();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                """
                Could not find the OpenCode executable.
                Try one of the options below:
                  1. Install the CLI via WinGet:  winget install SST.opencode
                  2. Add the installation directory to PATH manually.
                  3. Provide the model-id directly:  FurLab opencode settings default-model <model-id>
                """, ex);
        }
    }

    /// <summary>
    /// Displays an interactive selection prompt for the user to choose a model.
    /// </summary>
    /// <param name="models">The list of available models.</param>
    /// <returns>The selected model ID, or null if the user cancelled.</returns>
    public static string? SelectModelInteractively(IReadOnlyList<string> models)
    {
        try
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the default model:")
                    .PageSize(15)
                    .MoreChoicesText("[grey](Move up and down to see more models)[/]")
                    .AddChoices(models));
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Configures the MCP database integration in the OpenCode config.json file.
    /// </summary>
    public static void ConfigureMcpDatabase()
    {
        var configPath = Path.Combine(GlobalConfigDir, "config.json");

        var configDir = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException($"Could not determine configuration file directory: {configPath}");
        Directory.CreateDirectory(configDir);

        var config = LoadConfigFile(configPath);

        if (config["mcp"] is not JsonObject mcpNode)
        {
            mcpNode = new JsonObject();
            config["mcp"] = mcpNode;
        }

        mcpNode["toolbox"] = new JsonObject
        {
            ["type"] = "remote",
            ["url"] = "http://127.0.0.1:5000/mcp",
            ["enabled"] = true
        };

        SaveConfigFile(configPath, config);
        Console.WriteLine($"Configuration file updated: {configPath}");
    }

    private static JsonObject LoadConfigFile(string path)
    {
        if (!File.Exists(path))
        {
            return new JsonObject();
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        var node = JsonNode.Parse(json, null, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip
        });

        if (node is JsonObject objectNode)
        {
            return objectNode;
        }

        throw new InvalidDataException($"The file '{path}' does not contain a valid JSON object.");
    }

    private static void SaveConfigFile(string path, JsonObject config)
    {
        var json = config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json + Environment.NewLine);
    }
}
