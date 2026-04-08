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
        var availableModels = GetAvailableModels();

        string selectedModel;
        if (modelId is not null)
        {
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

            selectedModel = modelId;
        }
        else
        {
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
    /// Returns the list of available models from the `opencode models` command.
    /// </summary>
    /// <returns>A read-only list of model IDs.</returns>
    /// <exception cref="InvalidOperationException">Thrown when opencode is not found in PATH.</exception>
    public static IReadOnlyList<string> GetAvailableModels()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "opencode",
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
                "Nao foi possivel executar 'opencode'. Verifique se o OpenCode esta instalado e disponivel no PATH.", ex);
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
