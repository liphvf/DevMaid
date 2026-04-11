using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class OpenCodeCommandTests
{
    private string _testDirectory = null!;
    private string _originalDirectory = null!;

    private static readonly System.Collections.Generic.IReadOnlyList<string> FakeModels =
        new System.Collections.Generic.List<string>
        {
            "github-copilot/gpt-4o",
            "github-copilot/gpt-4.1",
            "anthropic/claude-3-5-sonnet"
        }.AsReadOnly();

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"OpenCodeCommandTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        CLI.Commands.OpenCodeCommand.ModelsProvider = () => FakeModels;
    }

    [TestCleanup]
    public void Cleanup()
    {
        CLI.Commands.OpenCodeCommand.ModelsProvider = null;
        Directory.SetCurrentDirectory(_originalDirectory);

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Build_ReturnsCommandWithCorrectName()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        Assert.AreEqual("opencode", command.Name);
    }

    [TestMethod]
    public void Build_ContainsSettingsSubcommand()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);
    }

    [TestMethod]
    public void Build_SettingsContainsMcpDatabaseCommand()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var mcpDatabaseCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "mcp-database");
        Assert.IsNotNull(mcpDatabaseCommand);
    }

    [TestMethod]
    public void Build_SettingsContainsDefaultModelCommand()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var defaultModelCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "default-model");
        Assert.IsNotNull(defaultModelCommand);
    }

    // --- ResolveConfigPath (local scope) ---

    [TestMethod]
    public void ResolveConfigPath_Local_JsoncExists_ReturnsJsonc()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        var jsonc = Path.Combine(_testDirectory, "opencode.jsonc");
        File.WriteAllText(jsonc, "{}");

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.AreEqual(jsonc, result);
    }

    [TestMethod]
    public void ResolveConfigPath_Local_OnlyJsonExists_ReturnsJson()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        var json = Path.Combine(_testDirectory, "opencode.json");
        File.WriteAllText(json, "{}");

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.AreEqual(json, result);
    }

    [TestMethod]
    public void ResolveConfigPath_Local_NeitherExists_ReturnsJsonc()
    {
        Directory.SetCurrentDirectory(_testDirectory);

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.IsTrue(result.EndsWith("opencode.jsonc", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ResolveConfigPath_Local_BothExist_PrefersJsonc()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "opencode.jsonc"), "{}");
        File.WriteAllText(Path.Combine(_testDirectory, "opencode.json"), "{}");

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.IsTrue(result.EndsWith("opencode.jsonc", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ResolveConfigPath_Global_ReturnsPathInsideOpenCodeConfigDir()
    {
        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: true);

        var expectedDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "opencode");

        Assert.IsTrue(result.StartsWith(expectedDir, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(
            result.EndsWith("opencode.jsonc", StringComparison.OrdinalIgnoreCase) ||
            result.EndsWith("opencode.json", StringComparison.OrdinalIgnoreCase));
    }

    // --- LoadConfigFile: tolerancia a comentarios ---

    [TestMethod]
    public void SetDefaultModel_JsoncWithComments_DoesNotThrow()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        var jsonc = Path.Combine(_testDirectory, "opencode.jsonc");
        File.WriteAllText(jsonc, """
            {
              // comentario de linha
              "$schema": "https://opencode.ai/config.json",
              /* comentario de bloco */
              "model": "old/model"
            }
            """);

        // Deve ler sem lançar exceção e atualizar o campo model
        CLI.Commands.OpenCodeCommand.SetDefaultModel("github-copilot/gpt-4o", global: false);

        var content = File.ReadAllText(jsonc);
        var node = JsonNode.Parse(content);
        Assert.AreEqual("github-copilot/gpt-4o", node!["model"]!.GetValue<string>());
    }

    // --- Comportamento quando opencode nao esta no PATH ---

    [TestMethod]
    public void SetDefaultModel_OpenCodeUnavailable_WithExplicitModelId_WritesToFile()
    {
        // Simula opencode ausente no PATH
        CLI.Commands.OpenCodeCommand.ModelsProvider = () =>
            throw new InvalidOperationException("Nao foi possivel executar 'opencode'.");

        Directory.SetCurrentDirectory(_testDirectory);

        // Redireciona stdout para suprimir o aviso do AnsiConsole durante o teste
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
            // Deve gravar o modelo sem lançar exceção, mesmo sem opencode no PATH
            CLI.Commands.OpenCodeCommand.SetDefaultModel("github-copilot/gpt-4o", global: false);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var configPath = Path.Combine(_testDirectory, "opencode.jsonc");
        Assert.IsTrue(File.Exists(configPath), "O arquivo de config deve ter sido criado.");
        var node = JsonNode.Parse(File.ReadAllText(configPath));
        Assert.AreEqual("github-copilot/gpt-4o", node!["model"]!.GetValue<string>());
    }



    [TestMethod]
    public void GetAvailableModels_ReturnsNonEmptyList()
    {
        var models = CLI.Commands.OpenCodeCommand.GetAvailableModels();

        Assert.IsTrue(models.Count > 0, "A lista de modelos nao deve ser vazia.");
    }

    [TestMethod]
    public void SetDefaultModel_ValidModel_WritesToFile()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        var models = CLI.Commands.OpenCodeCommand.GetAvailableModels();
        var firstModel = models[0];

        CLI.Commands.OpenCodeCommand.SetDefaultModel(firstModel, global: false);

        var configPath = Path.Combine(_testDirectory, "opencode.jsonc");
        Assert.IsTrue(File.Exists(configPath));
        var node = JsonNode.Parse(File.ReadAllText(configPath));
        Assert.AreEqual(firstModel, node!["model"]!.GetValue<string>());
    }
}
