using System;
using System.IO;
using System.Linq;
using System.Text;

using FurLab.CLI.CommandOptions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class FileCommandTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileCommandTests_{Guid.NewGuid():N}");
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

    [TestMethod(DisplayName = "Build deve retornar comando com nome 'file'")]
    [Description("Verifica que o comando principal é construído com o nome e descrição corretos.")]
    public void Build_ComandoPrincipal_RetornaNomeEDescricaoCorretos()
    {
        var command = CLI.Commands.FileCommand.Build();

        Assert.AreEqual("file", command.Name);
        Assert.AreEqual("File utilities.", command.Description);
    }

    [TestMethod(DisplayName = "Build deve conter subcomando 'combine'")]
    [Description("Verifica que o subcomando de combinação de arquivos está registrado na árvore de comandos.")]
    public void Build_ComandoPrincipal_ContemSubcomandoCombine()
    {
        var command = CLI.Commands.FileCommand.Build();

        var combineCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "combine");
        Assert.IsNotNull(combineCommand);
    }

    [TestMethod(DisplayName = "Combine com padrão válido deve mesclar arquivos corretamente")]
    [Description("Verifica que múltiplos arquivos correspondentes ao pattern são combinados no arquivo de saída.")]
    public void Combine_PadraoValido_MesclaArquivosCorretamente()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");

        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(_testDirectory, "combined.txt")
        };

        CLI.Commands.FileCommand.Combine(options);

        Assert.IsTrue(File.Exists(options.Output));
        var content = File.ReadAllText(options.Output);
        Assert.Contains("Content 1", content);
        Assert.Contains("Content 2", content);
    }

    [TestMethod(DisplayName = "Combine com pattern vazio deve lançar ArgumentException")]
    [Description("Verifica a validação do parâmetro Input — string vazia deve ser rejeitada.")]
    public void Combine_PadraoVazio_LancaArgumentException()
    {
        var options = new FileCommandOptions
        {
            Input = "",
            Output = Path.Combine(_testDirectory, "output.txt")
        };

        try { CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Combine com output fora do diretório permitido deve lançar ArgumentException")]
    [Description("Verifica que caminhos de saída fora do diretório de trabalho são rejeitados por segurança.")]
    public void Combine_OutputForaDoDiretorioPermitido_LancaArgumentException()
    {
        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(Path.GetTempPath(), "outside", "output.txt")
        };

        try { CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Combine sem arquivos correspondentes deve lançar exceção")]
    [Description("Verifica que a ausência de arquivos correspondentes ao pattern resulta em erro.")]
    public void Combine_SemArquivosCorrespondentes_LancaExcecao()
    {
        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "nonexistent*.txt"),
            Output = Path.Combine(_testDirectory, "output.txt")
        };

        try { CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (Exception) { }
    }

    [TestMethod(DisplayName = "Combine com path traversal no Input deve lançar ArgumentException")]
    [Description("Verifica que caminhos de entrada que tentam escapar do diretório permitido são rejeitados.")]
    public void Combine_InputComPathTraversal_LancaArgumentException()
    {
        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "..", "*.txt"),
            Output = Path.Combine(_testDirectory, "output.txt")
        };

        try { CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Combine com path traversal no Output deve lançar ArgumentException")]
    [Description("Verifica que caminhos de saída que tentam escapar do diretório permitido são rejeitados.")]
    public void Combine_OutputComPathTraversal_LancaArgumentException()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        File.WriteAllText(file1, "Content");

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(_testDirectory, "..", "output.txt")
        };

        try { CLI.Commands.FileCommand.Combine(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Combine sem OutputFile especificado deve usar extensão padrão .sql")]
    [Description("Verifica que quando o arquivo de saída não é informado, o nome padrão 'CombineFiles.sql' é utilizado.")]
    public void Combine_OutputNaoEspecificado_UsaNomePadraoCombineFilesSql()
    {
        var file1 = Path.Combine(_testDirectory, "test1.sql");
        File.WriteAllText(file1, "SELECT 1;");

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.sql")
        };

        CLI.Commands.FileCommand.Combine(options);

        var expectedOutput = Path.Combine(_testDirectory, "CombineFiles.sql");
        Assert.IsTrue(File.Exists(expectedOutput));
    }

    [TestMethod(DisplayName = "Combine com arquivos codificados em UTF-8 deve preservar caracteres especiais")]
    [Description("Verifica que caracteres com acentuação e símbolos são preservados corretamente na mesclagem.")]
    public void Combine_ArquivosUtf8_PreservaCaracteresEspeciais()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");

        var content1 = "Content with special chars: áéíóú";
        var content2 = "More content: ãõû";

        File.WriteAllText(file1, content1, Encoding.UTF8);
        File.WriteAllText(file2, content2, Encoding.UTF8);

        var options = new FileCommandOptions
        {
            Input = Path.Combine(_testDirectory, "*.txt"),
            Output = Path.Combine(_testDirectory, "combined.txt")
        };

        CLI.Commands.FileCommand.Combine(options);

        var result = File.ReadAllText(options.Output, Encoding.UTF8);
        Assert.Contains(content1, result);
        Assert.Contains(content2, result);
    }
}
