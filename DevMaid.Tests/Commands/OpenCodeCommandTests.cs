using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Command = System.CommandLine.Command;

namespace DevMaid.Tests.Commands;

[TestClass]
public class OpenCodeCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"OpenCodeCommandTests_{Guid.NewGuid():N}");
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
        var command = DevMaid.Commands.OpenCodeCommand.Build();

        Assert.AreEqual("opencode", command.Name);
    }

    [TestMethod]
    public void Build_ContainsSettingsSubcommand()
    {
        var command = DevMaid.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);
    }

    [TestMethod]
    public void Build_SettingsContainsMcpDatabaseCommand()
    {
        var command = DevMaid.Commands.OpenCodeCommand.Build();

        var settingsCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "settings");
        Assert.IsNotNull(settingsCommand);

        var mcpDatabaseCommand = settingsCommand!.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "mcp-database");
        Assert.IsNotNull(mcpDatabaseCommand);
    }
}
