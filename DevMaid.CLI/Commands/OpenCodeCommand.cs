using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace DevMaid.CLI.Commands;

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
            Description = "ID do modelo a definir como padrao. Se omitido, exibe menu interativo.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var globalFlag = new Option<bool>("--global")
        {
            Description = "Altera a configuracao global (~/.config/opencode/opencode.jsonc) em vez da local."
        };

        var defaultModelCommand = new Command("default-model", "Define o modelo padrao do OpenCode")
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
                    Console.Error.WriteLine($"Modelo '{modelId}' nao encontrado.");
                    Console.Error.WriteLine("Modelos disponiveis:");
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
                    "[yellow]Aviso:[/] Nao foi possivel validar o modelo (opencode nao encontrado no PATH). Definindo sem validacao.");
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
                AnsiConsole.MarkupLine($"[red]Erro:[/] {ex.Message}");
                AnsiConsole.MarkupLine(
                    "[yellow]Dica:[/] Forneça o ID do modelo diretamente: devmaid opencode settings default-model [grey]<model-id>[/]");
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
            ?? throw new InvalidOperationException($"Nao foi possivel determinar o diretorio do arquivo de configuracao: {configPath}");
        Directory.CreateDirectory(configDir);

        var config = LoadConfigFile(configPath);
        config["model"] = selectedModel;
        SaveConfigFile(configPath, config);

        AnsiConsole.MarkupLine($"[green]Modelo '{selectedModel}' definido em: {configPath}[/]");
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
    /// <returns>Full path to the executable, or <c>"opencode"</c> if no known path was found.</returns>
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

        // 2. Tauri Desktop installer: winget install SST.OpenCodeDesktop
        //    Installs to %LocalAppData%\OpenCode\ with main binary OpenCode.exe
        var desktopExe = Path.Combine(localAppData, "OpenCode", "OpenCode.exe");
        if (File.Exists(desktopExe)) return desktopExe;

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
            process.StartInfo = new ProcessStartInfo
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
                Nao foi possivel encontrar o executavel do OpenCode.
                Tente uma das opcoes abaixo:
                  1. Instalar o CLI via WinGet:  winget install SST.opencode
                  2. Adicionar o diretorio de instalacao ao PATH manualmente.
                  3. Fornecer o model-id diretamente:  devmaid opencode settings default-model <model-id>
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
                    .Title("Selecione o modelo padrao:")
                    .PageSize(15)
                    .MoreChoicesText("[grey](Mova para cima e para baixo para ver mais modelos)[/]")
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
            ?? throw new InvalidOperationException($"Nao foi possivel determinar o diretorio do arquivo de configuracao: {configPath}");
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
        Console.WriteLine($"Arquivo de configuracao atualizado: {configPath}");
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

        throw new InvalidDataException($"O arquivo '{path}' nao contem um JSON objeto valido.");
    }

    private static void SaveConfigFile(string path, JsonObject config)
    {
        var json = config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json + Environment.NewLine);
    }
}
