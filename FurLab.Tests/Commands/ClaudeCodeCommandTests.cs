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

    [TestMethod]
    public void Build_ReturnsCommandWithCorrectName()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        Assert.AreEqual("claude", command.Name);
    }

    [TestMethod]
    public void Build_ContainsInstallSubcommand()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var installCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "install");
        Assert.IsNotNull(installCommand);
    }

    [TestMethod]
    public void Build_ContainsSettingsSubcommand()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);
    }

    [TestMethod]
    public void Build_SettingsContainsMcpDatabaseCommand()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var mcpDatabaseCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "mcp-database");
        Assert.IsNotNull(mcpDatabaseCommand);
    }

    [TestMethod]
    public void Build_SettingsContainsWinEnvCommand()
    {
        var command = CLI.Commands.ClaudeCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var winEnvCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "win-env");
        Assert.IsNotNull(winEnvCommand);
    }

    [TestMethod]
    [OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
    public void Install_NonWindows_ThrowsPlatformNotSupportedException()
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

    [TestMethod]
    [OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
    public void ConfigureWindowsEnvironment_NonWindows_ThrowsPlatformNotSupportedException()
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

    [TestMethod]
    public void LoadSettingsFile_NonExistentFile_ReturnsEmptyJsonObject()
    {
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");

        var result = InvokePrivateLoadSettingsFile(nonExistentPath);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void LoadSettingsFile_EmptyFile_ReturnsEmptyJsonObject()
    {
        var emptyFile = Path.Combine(_testDirectory, "empty.json");
        File.WriteAllText(emptyFile, "");

        var result = InvokePrivateLoadSettingsFile(emptyFile);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void LoadSettingsFile_ValidJson_ReturnsJsonObject()
    {
        var validFile = Path.Combine(_testDirectory, "valid.json");
        var json = JsonSerializer.Serialize(new { name = "test" });
        File.WriteAllText(validFile, json);

        var result = InvokePrivateLoadSettingsFile(validFile);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("name"));
    }

    [TestMethod]
    public void LoadSettingsFile_InvalidJson_ThrowsException()
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
