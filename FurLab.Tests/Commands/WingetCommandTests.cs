using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

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

    [TestMethod(DisplayName = "Build deve retornar comando com nome 'winget'")]
    [Description("Verifica que o comando principal é construído com o nome e descrição corretos.")]
    public void Build_ComandoPrincipal_RetornaNomeEDescricaoCorretos()
    {
        var command = CLI.Commands.WingetCommand.Build();

        Assert.AreEqual("winget", command.Name);
        Assert.AreEqual("Manage winget packages.", command.Description);
    }

    [TestMethod(DisplayName = "Build deve conter subcomandos 'backup' e 'restore'")]
    [Description("Verifica que o comando winget possui exatamente dois subcomandos: backup e restore.")]
    public void Build_ComandoPrincipal_ContemSubcomandosBackupERestore()
    {
        var command = CLI.Commands.WingetCommand.Build();

        Assert.AreEqual(2, command.Children.Count());

        var backupCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "backup");
        var restoreCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "restore");

        Assert.IsNotNull(backupCommand);
        Assert.IsNotNull(restoreCommand);
    }

    [TestMethod(DisplayName = "Backup deve conter opção de output")]
    [Description("Verifica que o subcomando backup possui uma opção --output/-o para especificar o arquivo de destino.")]
    public void Build_SubcomandoBackup_ContemOpcaoOutput()
    {
        var command = CLI.Commands.WingetCommand.Build();

        var backupCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "backup");
        Assert.IsNotNull(backupCommand);

        var outputOption = backupCommand!.Options.FirstOrDefault(o => o.Name == "output" || o.Aliases.Contains("-o") || o.Aliases.Contains("--output"));
        Assert.IsNotNull(outputOption, "Backup command should have an output option");
    }

    [TestMethod(DisplayName = "Restore deve conter opção de input")]
    [Description("Verifica que o subcomando restore possui uma opção --input/-i para especificar o arquivo de origem.")]
    public void Build_SubcomandoRestore_ContemOpcaoInput()
    {
        var command = CLI.Commands.WingetCommand.Build();

        var restoreCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "restore");
        Assert.IsNotNull(restoreCommand);

        var inputOption = restoreCommand!.Options.FirstOrDefault(o => o.Name == "input" || o.Aliases.Contains("-i") || o.Aliases.Contains("--input"));
        Assert.IsNotNull(inputOption, "Restore command should have an input option");
    }
}
