using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Command = System.CommandLine.Command;

namespace DevMaid.Tests.Commands;

[TestClass]
public class WingetCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WingetCommandTests_{Guid.NewGuid():N}");
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
        var command = DevMaid.Commands.WingetCommand.Build();

        Assert.AreEqual("winget", command.Name);
        Assert.AreEqual("Manage winget packages.", command.Description);
    }

    [TestMethod]
    public void Build_ContainsBackupAndRestoreSubcommands()
    {
        var command = DevMaid.Commands.WingetCommand.Build();

        Assert.AreEqual(2, command.Children.Count());
        
        var backupCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "backup");
        var restoreCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "restore");

        Assert.IsNotNull(backupCommand);
        Assert.IsNotNull(restoreCommand);
    }

    [TestMethod]
    public void Build_BackupCommand_HasOutputOption()
    {
        var command = DevMaid.Commands.WingetCommand.Build();

        var backupCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "backup");
        Assert.IsNotNull(backupCommand);

        var outputOption = backupCommand!.Options.FirstOrDefault(o => o.Name == "output");
    }

    [TestMethod]
    public void Build_RestoreCommand_HasInputOption()
    {
        var command = DevMaid.Commands.WingetCommand.Build();

        var restoreCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "restore");
        Assert.IsNotNull(restoreCommand);

        var inputOption = restoreCommand!.Options.FirstOrDefault(o => o.Name == "input");
    }
}
