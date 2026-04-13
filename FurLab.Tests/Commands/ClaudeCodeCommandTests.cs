using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class ClaudeCodeCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ClaudeCodeCommandTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod(DisplayName = "Build deve retornar comando com nome 'claude'")]
    [Description("Verifica que o comando principal é construído com o nome correto.")]
    public void Build_ComandoPrincipal_RetornaNomeClaude()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        Assert.AreEqual("claude", command.Name);
    }

    [TestMethod(DisplayName = "Build deve conter subcomando 'install'")]
    [Description("Verifica que o subcomando de instalação está registrado na árvore de comandos.")]
    public void Build_ComandoPrincipal_ContemSubcomandoInstall()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var installCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "install");
        Assert.IsNotNull(installCommand);
    }

    [TestMethod(DisplayName = "Build deve conter subcomando 'settings'")]
    [Description("Verifica que o subcomando de configurações está registrado na árvore de comandos.")]
    public void Build_ComandoPrincipal_ContemSubcomandoSettings()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);
    }

    [TestMethod(DisplayName = "Settings deve conter subcomando 'mcp-database'")]
    [Description("Verifica que 'claude settings mcp-database' está registrado como subcomando de settings.")]
    public void Build_SubcomandoSettings_ContemSubcomandoMcpDatabase()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var mcpDatabaseCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "mcp-database");
        Assert.IsNotNull(mcpDatabaseCommand);
    }

    [TestMethod(DisplayName = "Settings deve conter subcomando 'win-env'")]
    [Description("Verifica que 'claude settings win-env' está registrado como subcomando de settings.")]
    public void Build_SubcomandoSettings_ContemSubcomandoWinEnv()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var winEnvCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "win-env");
        Assert.IsNotNull(winEnvCommand);
    }

    [TestMethod(DisplayName = "Install em plataforma non-Windows deve lançar PlatformNotSupportedException")]
    [Description("Verifica que a instalação é bloqueada em sistemas que não são Windows.")]
    [OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
    public void Install_PlataformaNonWindows_LancaPlatformNotSupportedException()
    {
        try
        {
            CLI.Commands.ClaudeCodeCommand.Install();
            Assert.Fail("Expected exception was not thrown");
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    [TestMethod(DisplayName = "ConfigureWindowsEnvironment em plataforma non-Windows deve lançar PlatformNotSupportedException")]
    [Description("Verifica que a configuração do ambiente Windows é bloqueada em sistemas que não são Windows.")]
    [OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
    public void ConfigureWindowsEnvironment_PlataformaNonWindows_LancaPlatformNotSupportedException()
    {
        try
        {
            CLI.Commands.ClaudeCodeCommand.ConfigureWindowsEnvironment();
            Assert.Fail("Expected exception was not thrown");
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    [TestMethod(DisplayName = "LoadSettingsFile com arquivo inexistente deve retornar objeto JSON vazio")]
    [Description("Quando o arquivo de configurações não existe, o método deve retornar um JsonObject vazio em vez de lançar exceção.")]
    public void LoadSettingsFile_ArquivoInexistente_RetornaJsonObjectVazio()
    {
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        var result = InvokePrivateLoadSettingsFile(nonExistentPath);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod(DisplayName = "LoadSettingsFile com arquivo vazio deve retornar objeto JSON vazio")]
    [Description("Quando o arquivo de configurações existe mas está vazio, o método deve retornar um JsonObject vazio.")]
    public void LoadSettingsFile_ArquivoVazio_RetornaJsonObjectVazio()
    {
        var emptyFile = Path.Combine(_testDirectory, "empty.json");
        File.WriteAllText(emptyFile, "");

        var result = InvokePrivateLoadSettingsFile(emptyFile);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod(DisplayName = "LoadSettingsFile com JSON válido deve retornar objeto populado")]
    [Description("Quando o arquivo contém JSON válido, o método deve retornar um JsonObject com as chaves correspondentes.")]
    public void LoadSettingsFile_JsonValido_RetornaJsonObjectPopulado()
    {
        var validFile = Path.Combine(_testDirectory, "valid.json");
        var json = JsonSerializer.Serialize(new { name = "test" });
        File.WriteAllText(validFile, json);

        var result = InvokePrivateLoadSettingsFile(validFile);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("name"));
    }

    [TestMethod(DisplayName = "LoadSettingsFile com JSON inválido deve lançar exceção")]
    [Description("Quando o arquivo contém conteúdo que não é JSON válido, o método deve lançar uma exceção.")]
    public void LoadSettingsFile_JsonInvalido_LancaExcecao()
    {
        var invalidFile = Path.Combine(_testDirectory, "invalid.json");
        File.WriteAllText(invalidFile, "not valid json");

        try
        {
            InvokePrivateLoadSettingsFile(invalidFile);
            Assert.Fail("Expected exception was not thrown");
        }
        catch
        {
        }
    }

    private static JsonObject InvokePrivateLoadSettingsFile(string settingsPath)
    {
        var method = typeof(CLI.Commands.ClaudeCodeCommand).GetMethod("LoadSettingsFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        return (JsonObject)method!.Invoke(null, [settingsPath])!;
    }
}
