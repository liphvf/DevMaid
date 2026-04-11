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

    [TestMethod(DisplayName = "Build deve retornar comando com nome 'opencode'")]
    [Description("Verifica que o comando principal é construído com o nome correto.")]
    public void Build_ComandoPrincipal_RetornaNomeOpencode()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        Assert.AreEqual("opencode", command.Name);
    }

    [TestMethod(DisplayName = "Build deve conter subcomando 'settings'")]
    [Description("Verifica que o comando 'opencode settings' está registrado na árvore de comandos.")]
    public void Build_ComandoPrincipal_ContemSubcomandoSettings()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);
    }

    [TestMethod(DisplayName = "Settings deve conter subcomando 'mcp-database'")]
    [Description("Verifica que 'opencode settings mcp-database' está registrado como subcomando de settings.")]
    public void Build_SubcomandoSettings_ContemSubcomandoMcpDatabase()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var mcpDatabaseCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "mcp-database");
        Assert.IsNotNull(mcpDatabaseCommand);
    }

    [TestMethod(DisplayName = "Settings deve conter subcomando 'default-model'")]
    [Description("Verifica que 'opencode settings default-model' está registrado como subcomando de settings.")]
    public void Build_SubcomandoSettings_ContemSubcomandoDefaultModel()
    {
        var command = CLI.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var defaultModelCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "default-model");
        Assert.IsNotNull(defaultModelCommand);
    }

    // --- ResolveConfigPath (escopo local) ---

    [TestMethod(DisplayName = "ResolveConfigPath local com .jsonc existente deve retornar jsonc")]
    [Description("Quando opencode.jsonc existe no diretório local, deve ser retornado com prioridade sobre .json.")]
    public void ResolveConfigPath_EscopoLocal_ComJsoncExistente_RetornaJsonc()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        var jsonc = Path.Combine(_testDirectory, "opencode.jsonc");
        File.WriteAllText(jsonc, "{}");

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.AreEqual(jsonc, result);
    }

    [TestMethod(DisplayName = "ResolveConfigPath local apenas com .json deve retornar json")]
    [Description("Quando apenas opencode.json existe (sem jsonc), deve retornar o json.")]
    public void ResolveConfigPath_EscopoLocal_ApenasComJson_RetornaJson()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        var json = Path.Combine(_testDirectory, "opencode.json");
        File.WriteAllText(json, "{}");

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.AreEqual(json, result);
    }

    [TestMethod(DisplayName = "ResolveConfigPath local sem arquivos deve retornar jsonc como padrão")]
    [Description("Quando nenhum arquivo de configuração existe localmente, o padrão deve ser opencode.jsonc.")]
    public void ResolveConfigPath_EscopoLocal_SemArquivos_RetornaJsoncPadrao()
    {
        Directory.SetCurrentDirectory(_testDirectory);

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.IsTrue(result.EndsWith("opencode.jsonc", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod(DisplayName = "ResolveConfigPath local com ambos existentes deve preferir jsonc")]
    [Description("Quando .json e .jsonc coexistem, jsonc tem prioridade.")]
    public void ResolveConfigPath_EscopoLocal_ComAmbosExistentes_PrefereJsonc()
    {
        Directory.SetCurrentDirectory(_testDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "opencode.jsonc"), "{}");
        File.WriteAllText(Path.Combine(_testDirectory, "opencode.json"), "{}");

        var result = CLI.Commands.OpenCodeCommand.ResolveConfigPath(global: false);

        Assert.IsTrue(result.EndsWith("opencode.jsonc", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod(DisplayName = "ResolveConfigPath global deve retornar caminho dentro de .config/opencode")]
    [Description("No escopo global, o arquivo de configuração deve residir em %USERPROFILE%/.config/opencode/.")]
    public void ResolveConfigPath_EscopoGlobal_RetornaCaminhoConfigDir()
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

    // --- SetDefaultModel: tolerância a comentários ---

    [TestMethod(DisplayName = "SetDefaultModel com jsonc contendo comentários não deve lançar exceção")]
    [Description("Arquivos .jsonc com comentários de linha e bloco devem ser lidos e atualizados corretamente.")]
    public void SetDefaultModel_JsoncComComentarios_NaoLancaExcecao()
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

        CLI.Commands.OpenCodeCommand.SetDefaultModel("github-copilot/gpt-4o", global: false);

        var content = File.ReadAllText(jsonc);
        var node = JsonNode.Parse(content);
        Assert.AreEqual("github-copilot/gpt-4o", node!["model"]!.GetValue<string>());
    }

    // --- Comportamento quando opencode não está no PATH ---

    [TestMethod(DisplayName = "SetDefaultModel com opencode indisponível e modelo explícito deve gravar no arquivo")]
    [Description("Mesmo sem o executável 'opencode' no PATH, o modelo informado deve ser gravado no arquivo de configuração.")]
    public void SetDefaultModel_OpenCodeIndisponivel_ComModeloExplicito_GravaNoArquivo()
    {
        CLI.Commands.OpenCodeCommand.ModelsProvider = () =>
            throw new InvalidOperationException("Nao foi possivel executar 'opencode'.");

        Directory.SetCurrentDirectory(_testDirectory);

        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
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

    [TestMethod(DisplayName = "GetAvailableModels deve retornar lista não vazia")]
    [Description("Verifica que pelo menos um modelo está disponível quando o opencode está instalado.")]
    public void GetAvailableModels_QuandoOpencodeDisponivel_RetornaListaNaoVazia()
    {
        var models = CLI.Commands.OpenCodeCommand.GetAvailableModels();

        Assert.IsTrue(models.Count > 0, "A lista de modelos nao deve ser vazia.");
    }

    [TestMethod(DisplayName = "SetDefaultModel com modelo válido deve gravar no arquivo de configuração")]
    [Description("Ao informar um modelo válido, o arquivo opencode.jsonc local deve ser criado com o model correto.")]
    public void SetDefaultModel_ModeloValido_GravaNoArquivoDeConfiguracao()
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
