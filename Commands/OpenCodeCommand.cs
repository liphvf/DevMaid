using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevMaid.Commands;

public static class OpenCodeCommand
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "opencode", "config.json");

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

        settingsCommand.Add(mcpDatabaseCommand);
        command.Add(settingsCommand);

        return command;
    }

    public static void ConfigureMcpDatabase()
    {
        var configDir = Path.GetDirectoryName(ConfigPath)
            ?? throw new InvalidOperationException($"Nao foi possivel determinar o diretorio do arquivo de configuracao: {ConfigPath}");
        Directory.CreateDirectory(configDir);

        var config = LoadConfigFile(ConfigPath);

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

        SaveConfigFile(ConfigPath, config);
        Console.WriteLine($"Arquivo de configuracao atualizado: {ConfigPath}");
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

        var node = JsonNode.Parse(json);
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
