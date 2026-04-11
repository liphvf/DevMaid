using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class CleanCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CleanCommandTests_{Guid.NewGuid():N}");
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

    [TestMethod(DisplayName = "Clean com diretório válido deve remover pastas bin e obj")]
    [Description("Verifica que o comando Clean remove recursivamente as pastas bin e obj do diretório do projeto.")]
    public void Clean_DiretorioValido_RemoveBinEObj()
    {
        var projectDir = Path.Combine(_testDirectory, "Project1");
        var binDir = Path.Combine(projectDir, "bin");
        var objDir = Path.Combine(projectDir, "obj");

        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);

        var srcFile = Path.Combine(binDir, "test.dll");
        File.WriteAllText(srcFile, "test");

        CLI.Commands.CleanCommand.Clean(projectDir);

        Assert.IsFalse(Directory.Exists(binDir), "bin folder should be removed");
        Assert.IsFalse(Directory.Exists(objDir), "obj folder should be removed");
        Assert.IsTrue(Directory.Exists(projectDir), "project folder should remain");
    }

    [TestMethod(DisplayName = "Clean com caminho de arquivo deve usar diretório pai")]
    [Description("Quando um caminho de arquivo .csproj é informado, o Clean deve operar no diretório pai do arquivo.")]
    public void Clean_CaminhoArquivo_UsaDiretorioPai()
    {
        var projectDir = Path.Combine(_testDirectory, "Project2");
        var binDir = Path.Combine(projectDir, "bin");
        Directory.CreateDirectory(binDir);

        var projectFile = Path.Combine(projectDir, "Project2.csproj");
        File.WriteAllText(projectFile, "<Project />");

        CLI.Commands.CleanCommand.Clean(projectFile);

        Assert.IsFalse(Directory.Exists(binDir), "bin folder should be removed");
    }

    [TestMethod(DisplayName = "Clean com diretório inexistente deve lançar DirectoryNotFoundException")]
    [Description("Verifica que a limpeza falha corretamente quando o diretório alvo não existe.")]
    public void Clean_DiretorioInexistente_LancaDirectoryNotFoundException()
    {
        var nonExistentDir = Path.Combine(_testDirectory, "NonExistent");

        try { CLI.Commands.CleanCommand.Clean(nonExistentDir); Assert.Fail(); } catch (DirectoryNotFoundException) { }
    }

    [TestMethod(DisplayName = "Clean com diretórios aninhados deve remover todos os bin e obj")]
    [Description("Verifica que a limpeza recursiva remove pastas bin e obj de todos os projetos dentro da solution.")]
    public void Clean_DiretoriosAninhados_RemoveTodosBinEObj()
    {
        var rootDir = Path.Combine(_testDirectory, "Solution");
        var project1Dir = Path.Combine(rootDir, "Project1", "bin");
        var project2Dir = Path.Combine(rootDir, "Project2", "obj");
        var srcDir = Path.Combine(rootDir, "Project1", "src");

        Directory.CreateDirectory(project1Dir);
        Directory.CreateDirectory(project2Dir);
        Directory.CreateDirectory(srcDir);

        CLI.Commands.CleanCommand.Clean(rootDir);

        Assert.IsFalse(Directory.Exists(Path.Combine(rootDir, "Project1", "bin")));
        Assert.IsFalse(Directory.Exists(Path.Combine(rootDir, "Project2", "obj")));
        Assert.IsTrue(Directory.Exists(srcDir), "src folder should remain");
    }

    [TestMethod(DisplayName = "Clean com diretório contendo apenas bin e obj deve remover ambos")]
    [Description("Verifica que quando o projeto contém apenas pastas bin e obj, ambas são removidas.")]
    public void Clean_DiretorioApenasBinEObj_RemoveAmbos()
    {
        var projectDir = Path.Combine(_testDirectory, "EmptyProject");
        var binDir = Path.Combine(projectDir, "bin");
        var objDir = Path.Combine(projectDir, "obj");

        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);

        CLI.Commands.CleanCommand.Clean(projectDir);

        Assert.IsFalse(Directory.Exists(binDir));
        Assert.IsFalse(Directory.Exists(objDir));
    }

    [TestMethod(DisplayName = "Build deve retornar comando com nome 'clean'")]
    [Description("Verifica que o comando principal é construído com o nome e descrição corretos.")]
    public void Build_ComandoPrincipal_RetornaNomeEDescricaoCorretos()
    {
        var command = CLI.Commands.CleanCommand.Build();

        Assert.AreEqual("clean", command.Name);
        Assert.AreEqual("Remove bin and obj folders from solution.", command.Description);
    }

    [TestMethod(DisplayName = "Build deve conter exatamente um argumento 'directory'")]
    [Description("Verifica que o comando clean possui um único argumento obrigatório chamado 'directory'.")]
    public void Build_ComandoPrincipal_ContemArgumentoDirectory()
    {
        var command = CLI.Commands.CleanCommand.Build();

        Assert.HasCount(1, command.Arguments);
        Assert.AreEqual("directory", command.Arguments[0].Name);
    }
}
