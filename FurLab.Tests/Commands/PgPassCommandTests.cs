using System.CommandLine;

using FurLab.CLI.Commands;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

/// <summary>
/// Testes unitários para PgPassCommand (US1, US2, US3).
/// Verifica a estrutura do comando e comportamento dos subcomandos.
/// Seguindo TDD: testes escritos antes da implementação.
/// </summary>
[TestClass]
public class PgPassCommandTests
{
    // =========================================================================
    // US1 — Estrutura do subcomando add
    // =========================================================================

    [TestMethod]
    [TestCategory("US1")]
    public void Build_RetornaComandoComNomePgpass()
    {
        var command = PgPassCommand.Build();

        Assert.AreEqual("pgpass", command.Name);
    }

    [TestMethod]
    [TestCategory("US1")]
    public void Build_ContemSubcomandosAddListRemove()
    {
        var command = PgPassCommand.Build();
        var subcomandos = command.Children.OfType<Command>().ToList();
        var nomes = subcomandos.Select(c => c.Name).ToList();

        CollectionAssert.Contains(nomes, "add");
        CollectionAssert.Contains(nomes, "list");
        CollectionAssert.Contains(nomes, "remove");
    }

    [TestMethod]
    [TestCategory("US1")]
    public void SubcomandoAdd_ContemArgumentoBanco()
    {
        var command = PgPassCommand.Build();
        var addCommand = command.Children.OfType<Command>().First(c => c.Name == "add");
        var argumentos = addCommand.Arguments.Select(a => a.Name).ToList();

        CollectionAssert.Contains(argumentos, "banco");
    }

    [TestMethod]
    [TestCategory("US1")]
    public void SubcomandoAdd_ContemOpcaoPassword()
    {
        var command = PgPassCommand.Build();
        var addCommand = command.Children.OfType<Command>().First(c => c.Name == "add");
        var opcoes = addCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--password");
    }

    [TestMethod]
    [TestCategory("US1")]
    public void SubcomandoAdd_ContemOpcaoHost()
    {
        var command = PgPassCommand.Build();
        var addCommand = command.Children.OfType<Command>().First(c => c.Name == "add");
        var opcoes = addCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--host");
    }

    [TestMethod]
    [TestCategory("US1")]
    public void SubcomandoAdd_ContemOpcaoPort()
    {
        var command = PgPassCommand.Build();
        var addCommand = command.Children.OfType<Command>().First(c => c.Name == "add");
        var opcoes = addCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--port");
    }

    [TestMethod]
    [TestCategory("US1")]
    public void SubcomandoAdd_ContemOpcaoUsername()
    {
        var command = PgPassCommand.Build();
        var addCommand = command.Children.OfType<Command>().First(c => c.Name == "add");
        var opcoes = addCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--username");
    }

    // =========================================================================
    // US2 — Estrutura do subcomando list
    // =========================================================================

    [TestMethod]
    [TestCategory("US2")]
    public void SubcomandoList_NaoContemArgumentosNemOpcoes()
    {
        var command = PgPassCommand.Build();
        var listCommand = command.Children.OfType<Command>().First(c => c.Name == "list");

        Assert.AreEqual(0, listCommand.Arguments.Count(),
            "Subcomando list não deve ter argumentos.");
        Assert.AreEqual(0, listCommand.Options.Count(),
            "Subcomando list não deve ter opções.");
    }

    // =========================================================================
    // US3 — Estrutura do subcomando remove
    // =========================================================================

    [TestMethod]
    [TestCategory("US3")]
    public void SubcomandoRemove_ContemArgumentoBanco()
    {
        var command = PgPassCommand.Build();
        var removeCommand = command.Children.OfType<Command>().First(c => c.Name == "remove");
        var argumentos = removeCommand.Arguments.Select(a => a.Name).ToList();

        CollectionAssert.Contains(argumentos, "banco");
    }

    [TestMethod]
    [TestCategory("US3")]
    public void SubcomandoRemove_ContemOpcaoHost()
    {
        var command = PgPassCommand.Build();
        var removeCommand = command.Children.OfType<Command>().First(c => c.Name == "remove");
        var opcoes = removeCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--host");
    }

    [TestMethod]
    [TestCategory("US3")]
    public void SubcomandoRemove_ContemOpcaoPort()
    {
        var command = PgPassCommand.Build();
        var removeCommand = command.Children.OfType<Command>().First(c => c.Name == "remove");
        var opcoes = removeCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--port");
    }

    [TestMethod]
    [TestCategory("US3")]
    public void SubcomandoRemove_ContemOpcaoUsername()
    {
        var command = PgPassCommand.Build();
        var removeCommand = command.Children.OfType<Command>().First(c => c.Name == "remove");
        var opcoes = removeCommand.Options.Select(o => o.Name).ToList();

        CollectionAssert.Contains(opcoes, "--username");
    }

    // =========================================================================
    // DatabaseCommand — integração do pgpass como subcomando
    // =========================================================================

    [TestMethod]
    [TestCategory("US1")]
    public void DatabaseCommand_ContemSubcomandoPgpass()
    {
        var command = DatabaseCommand.Build();
        var subcomandos = command.Children.OfType<Command>().ToList();
        var nomes = subcomandos.Select(c => c.Name).ToList();

        CollectionAssert.Contains(nomes, "pgpass",
            "DatabaseCommand deve conter pgpass como subcomando.");
    }
}
