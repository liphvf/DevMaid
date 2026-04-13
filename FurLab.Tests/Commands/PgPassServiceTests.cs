using System;
using System.IO;
using System.Linq;

using FurLab.Core.Interfaces;
using FurLab.Core.Models;
using FurLab.Core.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

/// <summary>
/// Testes unitários para PgPassService (US1, US2, US3).
/// Seguindo TDD: testes escritos antes da implementação.
/// </summary>
[TestClass]
public class PgPassServiceTests
{
    private IPgPassService _service = null!;
    private string _tempDir = null!;
    private string _tempFile = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _service = new PgPassService();
        _tempDir = Path.Combine(Path.GetTempPath(), $"pgpass_test_{Guid.NewGuid():N}");
        _tempFile = Path.Combine(_tempDir, "pgpass.conf");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // =========================================================================
    // US1 — AddEntry
    // =========================================================================

    [TestMethod(DisplayName = "AddEntry com diretório inexistente deve criar diretório automaticamente")]
    [Description("Verifica que o método AddEntry cria o diretório pai do arquivo pgpass.conf quando ele não existe.")]
    [TestCategory("US1")]
    public void AddEntry_DiretorioInexistente_CriaDiretorioAutomaticamente()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = "senha123"
        };

        var result = _service.AddEntry(entry, _tempFile);

        Assert.IsTrue(result.Success, result.Message);
        Assert.IsTrue(Directory.Exists(_tempDir), "Diretório deve ser criado automaticamente.");
        Assert.IsTrue(File.Exists(_tempFile), "Arquivo pgpass.conf deve ser criado.");
    }

    [TestMethod(DisplayName = "AddEntry deve gravar entrada no formato host:porta:banco:usuario:senha")]
    [Description("Verifica que a entrada é escrita no arquivo seguindo o formato padrão do pgpass.")]
    [TestCategory("US1")]
    public void AddEntry_EntradaValida_GravaFormatoCorreto()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = "senha123"
        };

        _service.AddEntry(entry, _tempFile);

        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("localhost:5432:meu_banco:postgres:senha123"),
            $"Formato esperado não encontrado. Conteúdo: {conteudo}");
    }

    [TestMethod(DisplayName = "AddEntry deve preservar entradas existentes ao adicionar nova")]
    [Description("Verifica que adicionar uma nova entrada não remove as entradas já presentes no arquivo.")]
    [TestCategory("US1")]
    public void AddEntry_EntradaNova_PreservaExistentes()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempFile, "localhost:5432:banco_existente:postgres:senha_antiga\n");

        var novaEntrada = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "banco_novo",
            Username = "postgres",
            Password = "senha_nova"
        };

        _service.AddEntry(novaEntrada, _tempFile);

        var linhas = File.ReadAllLines(_tempFile);
        Assert.IsTrue(linhas.Any(l => l.Contains("banco_existente")),
            "Entrada existente deve ser preservada.");
        Assert.IsTrue(linhas.Any(l => l.Contains("banco_novo")),
            "Nova entrada deve ser adicionada.");
    }

    [TestMethod(DisplayName = "AddEntry com entrada duplicada deve retornar IsDuplicate=true")]
    [Description("Verifica que ao adicionar uma entrada idêntica a uma existente, o resultado indica duplicata sem erro.")]
    [TestCategory("US1")]
    public void AddEntry_EntradaDuplicata_RetornaIsDuplicateTrue()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = "senha123"
        };

        _service.AddEntry(entry, _tempFile);
        var resultado = _service.AddEntry(entry, _tempFile);

        Assert.IsTrue(resultado.Success, "Duplicata não é erro — Success deve ser true.");
        Assert.IsTrue(resultado.IsDuplicate, "IsDuplicate deve ser true para entrada duplicada.");

        var linhas = File.ReadAllLines(_tempFile);
        Assert.AreEqual(1, linhas.Length, "Apenas uma linha deve existir no arquivo.");
    }

    [TestMethod(DisplayName = "AddEntry deve escapar dois-pontos na senha")]
    [Description("Verifica que caracteres ':' na senha são escapados com '\\' conforme o formato pgpass.")]
    [TestCategory("US1")]
    public void AddEntry_SenhaComDoisPontos_EscapaCorretamente()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = "senha:com:dois:pontos"
        };

        _service.AddEntry(entry, _tempFile);

        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains(@"senha\:com\:dois\:pontos"),
            $"Dois-pontos devem ser escapados. Conteúdo: {conteudo}");
    }

    [TestMethod(DisplayName = "AddEntry deve escapar barra invertida na senha")]
    [Description("Verifica que caracteres '\\' na senha são escapados com '\\' adicional conforme o formato pgpass.")]
    [TestCategory("US1")]
    public void AddEntry_SenhaComBarraInvertida_EscapaCorretamente()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = @"senha\invertida"
        };

        _service.AddEntry(entry, _tempFile);

        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains(@"senha\\invertida"),
            $"Barras invertidas devem ser escapadas. Conteúdo: {conteudo}");
    }

    [TestMethod(DisplayName = "AddEntry com senha vazia deve falhar sem criar arquivo")]
    [Description("Verifica que entradas com senha vazia são rejeitadas e nenhum arquivo é criado.")]
    [TestCategory("US1")]
    public void AddEntry_SenhaVazia_FalhaSemCriarArquivo()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = ""
        };

        var resultado = _service.AddEntry(entry, _tempFile);

        Assert.IsFalse(resultado.Success, "Senha vazia deve resultar em falha.");
        Assert.IsFalse(File.Exists(_tempFile), "Arquivo não deve ser criado com senha vazia.");
    }

    [TestMethod(DisplayName = "AddEntry com parâmetros omitidos deve aplicar valores padrão")]
    [Description("Verifica que campos não informados na entrada recebem os valores padrão do record PgPassEntry.")]
    [TestCategory("US1")]
    public void AddEntry_ParametrosOmitidos_AplicaValoresPadrao()
    {
        var entry = new PgPassEntry
        {
            Database = "meu_banco",
            Password = "senha123"
            // Hostname, Port e Username usando valores padrão do record
        };

        _service.AddEntry(entry, _tempFile);

        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("localhost:5432:meu_banco:postgres:senha123"),
            $"Padrões devem ser aplicados. Conteúdo: {conteudo}");
    }

    [TestMethod(DisplayName = "AddEntry com banco '*' deve gravar curinga literalmente")]
    [Description("Verifica que o caractere '*' no campo Database é escrito sem escape no arquivo pgpass.")]
    [TestCategory("US1")]
    public void AddEntry_BancoComCuringa_GravaLiteralmente()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "*",
            Username = "postgres",
            Password = "senha123"
        };

        _service.AddEntry(entry, _tempFile);

        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("localhost:5432:*:postgres:senha123"),
            $"Curinga '*' deve ser escrito literalmente. Conteúdo: {conteudo}");
    }

    // =========================================================================
    // US2 — ListEntries
    // =========================================================================

    [TestMethod(DisplayName = "ListEntries com arquivo inexistente deve retornar lista vazia")]
    [Description("Verifica que a listagem de entradas retorna coleção vazia quando o arquivo pgpass não existe.")]
    [TestCategory("US2")]
    public void ListEntries_ArquivoInexistente_RetornaListaVazia()
    {
        var entradas = _service.ListEntries(_tempFile).ToList();

        Assert.AreEqual(0, entradas.Count, "Deve retornar lista vazia quando arquivo não existe.");
    }

    [TestMethod(DisplayName = "ListEntries com arquivo vazio deve retornar lista vazia")]
    [Description("Verifica que a listagem de entradas retorna coleção vazia quando o arquivo pgpass existe mas está vazio.")]
    [TestCategory("US2")]
    public void ListEntries_ArquivoVazio_RetornaListaVazia()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempFile, string.Empty);

        var entradas = _service.ListEntries(_tempFile).ToList();

        Assert.AreEqual(0, entradas.Count, "Deve retornar lista vazia quando arquivo está vazio.");
    }

    [TestMethod(DisplayName = "ListEntries deve ignorar linhas de comentário")]
    [Description("Verifica que linhas iniciadas com '#' são filtradas durante o parsing do arquivo pgpass.")]
    [TestCategory("US2")]
    public void ListEntries_LinhasComentario_SaoIgnoradas()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllLines(_tempFile, new[]
        {
            "# Comentário de exemplo",
            "localhost:5432:meu_banco:postgres:senha123",
            "# Outro comentário"
        });

        var entradas = _service.ListEntries(_tempFile).ToList();

        Assert.AreEqual(1, entradas.Count, "Linhas de comentário devem ser ignoradas.");
    }

    [TestMethod(DisplayName = "ListEntries deve retornar todas as entradas corretamente parsadas")]
    [Description("Verifica que cada linha válida do arquivo é convertida em um PgPassEntry com os campos corretos.")]
    [TestCategory("US2")]
    public void ListEntries_MultiplasEntradas_RetornaTodasParsadas()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllLines(_tempFile, new[]
        {
            "localhost:5432:banco1:postgres:senha1",
            "db.empresa.com:5433:banco2:deploy:senha2"
        });

        var entradas = _service.ListEntries(_tempFile).ToList();

        Assert.AreEqual(2, entradas.Count);
        Assert.AreEqual("localhost", entradas[0].Hostname);
        Assert.AreEqual("5432", entradas[0].Port);
        Assert.AreEqual("banco1", entradas[0].Database);
        Assert.AreEqual("postgres", entradas[0].Username);
        Assert.AreEqual("senha1", entradas[0].Password);

        Assert.AreEqual("db.empresa.com", entradas[1].Hostname);
        Assert.AreEqual("5433", entradas[1].Port);
        Assert.AreEqual("banco2", entradas[1].Database);
        Assert.AreEqual("deploy", entradas[1].Username);
        Assert.AreEqual("senha2", entradas[1].Password);
    }

    [TestMethod(DisplayName = "ListEntries deve aplicar unescape na senha ao ler")]
    [Description("Verifica que sequências de escape '\\:' e '\\\\' são convertidas de volta para ':' e '\\' respectivamente.")]
    [TestCategory("US2")]
    public void ListEntries_SenhaComEscape_AplicaUnescape()
    {
        Directory.CreateDirectory(_tempDir);
        // Arquivo contém senhas com escape aplicado
        File.WriteAllLines(_tempFile, new[]
        {
            @"localhost:5432:meu_banco:postgres:senha\:com\:dois\:pontos",
            @"localhost:5432:outro_banco:postgres:senha\\invertida"
        });

        var entradas = _service.ListEntries(_tempFile).ToList();

        Assert.AreEqual("senha:com:dois:pontos", entradas[0].Password,
            "Deve aplicar unescape de \\: → :");
        Assert.AreEqual(@"senha\invertida", entradas[1].Password,
            @"Deve aplicar unescape de \\ → \");
    }

    // =========================================================================
    // US3 — RemoveEntry
    // =========================================================================

    [TestMethod(DisplayName = "RemoveEntry deve remover entrada correta e preservar demais")]
    [Description("Verifica que a remoção por chave (host:porta:banco:usuario) elimina apenas a entrada correspondente.")]
    [TestCategory("US3")]
    public void RemoveEntry_EntradaExistente_RemovePreservandoDemais()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllLines(_tempFile, new[]
        {
            "localhost:5432:banco1:postgres:senha1",
            "localhost:5432:banco2:postgres:senha2",
            "localhost:5432:banco3:postgres:senha3"
        });

        var chave = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "banco2",
            Username = "postgres",
            Password = "qualquer"
        };

        var resultado = _service.RemoveEntry(chave, _tempFile);

        Assert.IsTrue(resultado.Success, resultado.Message);
        var linhas = File.ReadAllLines(_tempFile);
        Assert.AreEqual(2, linhas.Length, "Apenas a entrada removida deve sumir.");
        Assert.IsFalse(linhas.Any(l => l.Contains("banco2")), "banco2 deve ser removido.");
        Assert.IsTrue(linhas.Any(l => l.Contains("banco1")), "banco1 deve ser preservado.");
        Assert.IsTrue(linhas.Any(l => l.Contains("banco3")), "banco3 deve ser preservado.");
    }

    [TestMethod(DisplayName = "RemoveEntry com entrada inexistente deve retornar resultado informativo")]
    [Description("Verifica que a tentativa de remover uma entrada que não existe retorna um resultado com Success=false e mensagem descritiva.")]
    [TestCategory("US3")]
    public void RemoveEntry_EntradaInexistente_RetornaResultadoInformativo()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllLines(_tempFile, new[]
        {
            "localhost:5432:banco1:postgres:senha1"
        });

        var chave = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "banco_inexistente",
            Username = "postgres",
            Password = "qualquer"
        };

        var resultado = _service.RemoveEntry(chave, _tempFile);

        Assert.IsFalse(resultado.Success, "Deve retornar falha informativa quando entrada não existe.");
        var linhas = File.ReadAllLines(_tempFile);
        Assert.AreEqual(1, linhas.Length, "Arquivo deve permanecer inalterado.");
    }

    [TestMethod(DisplayName = "RemoveEntry com entrada inexistente deve deixar arquivo inalterado")]
    [Description("Verifica que a tentativa de remover uma entrada que não existe não modifica o conteúdo do arquivo.")]
    [TestCategory("US3")]
    public void RemoveEntry_EntradaInexistente_ArquivoInalterado()
    {
        Directory.CreateDirectory(_tempDir);
        var conteudoOriginal = "localhost:5432:banco1:postgres:senha1\n";
        File.WriteAllText(_tempFile, conteudoOriginal);

        var chave = new PgPassEntry
        {
            Hostname = "remotehost",
            Port = "5432",
            Database = "banco1",
            Username = "postgres",
            Password = "qualquer"
        };

        _service.RemoveEntry(chave, _tempFile);

        var conteudoAtual = File.ReadAllText(_tempFile);
        Assert.AreEqual(conteudoOriginal, conteudoAtual,
            "Conteúdo do arquivo não deve ser alterado quando entrada não encontrada.");
    }

    // =========================================================================
    // Curingas pgpass — suporte a * em host, porta, banco e usuário
    // =========================================================================

    [TestMethod(DisplayName = "AddEntry com curinga no host deve gravar entrada correta")]
    [Description("Verifica que o caractere '*' no campo Hostname é aceito e gravado literalmente.")]
    [TestCategory("pgpass-wildcard")]
    public void AddEntry_CuringaNoHost_GravaEntradaCorreta()
    {
        var entry = new PgPassEntry
        {
            Hostname = "*",
            Port = "5432",
            Database = "meu_banco",
            Username = "postgres",
            Password = "senha123"
        };

        var result = _service.AddEntry(entry, _tempFile);

        Assert.IsTrue(result.Success, result.Message);
        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("*:5432:meu_banco:postgres:senha123"),
            "Entrada com curinga no host deve ser gravada corretamente.");
    }

    [TestMethod(DisplayName = "AddEntry com curinga na porta deve gravar entrada correta")]
    [Description("Verifica que o caractere '*' no campo Port é aceito e gravado literalmente.")]
    [TestCategory("pgpass-wildcard")]
    public void AddEntry_CuringaNaPorta_GravaEntradaCorreta()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "*",
            Database = "meu_banco",
            Username = "postgres",
            Password = "senha123"
        };

        var result = _service.AddEntry(entry, _tempFile);

        Assert.IsTrue(result.Success, result.Message);
        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("localhost:*:meu_banco:postgres:senha123"),
            "Entrada com curinga na porta deve ser gravada corretamente.");
    }

    [TestMethod(DisplayName = "AddEntry com curinga no usuário deve gravar entrada correta")]
    [Description("Verifica que o caractere '*' no campo Username é aceito e gravado literalmente.")]
    [TestCategory("pgpass-wildcard")]
    public void AddEntry_CuringaNoUsuario_GravaEntradaCorreta()
    {
        var entry = new PgPassEntry
        {
            Hostname = "localhost",
            Port = "5432",
            Database = "meu_banco",
            Username = "*",
            Password = "senha123"
        };

        var result = _service.AddEntry(entry, _tempFile);

        Assert.IsTrue(result.Success, result.Message);
        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("localhost:5432:meu_banco:*:senha123"),
            "Entrada com curinga no usuário deve ser gravada corretamente.");
    }

    [TestMethod(DisplayName = "AddEntry com todos os campos como curinga deve gravar entrada correta")]
    [Description("Verifica que múltiplos caracteres '*' em diferentes campos são aceitos e gravados literalmente.")]
    [TestCategory("pgpass-wildcard")]
    public void AddEntry_TodosCuringas_GravaEntradaCorreta()
    {
        var entry = new PgPassEntry
        {
            Hostname = "*",
            Port = "*",
            Database = "*",
            Username = "*",
            Password = "senha123"
        };

        var result = _service.AddEntry(entry, _tempFile);

        Assert.IsTrue(result.Success, result.Message);
        var conteudo = File.ReadAllText(_tempFile);
        Assert.IsTrue(conteudo.Contains("*:*:*:*:senha123"),
            "Entrada com todos os curingas deve ser gravada corretamente.");
    }
}
