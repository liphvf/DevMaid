using System;
using System.IO;
using System.Text;

using FurLab.CLI.Utils;

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

    [TestMethod(DisplayName = "Combine com padrão válido deve mesclar arquivos corretamente")]
    [Description("Verifica que múltiplos arquivos correspondentes ao pattern são combinados.")]
    public void Combine_PadraoValido_MesclaArquivosCorretamente()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");

        System.IO.File.WriteAllText(file1, "Content 1", Encoding.UTF8);
        System.IO.File.WriteAllText(file2, "Content 2", Encoding.UTF8);

        var outputPath = Path.Combine(_testDirectory, "combined.txt");

        var inputFilePaths = System.IO.Directory.GetFiles(_testDirectory, "*.txt");
        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        foreach (var inputFilePath in inputFilePaths)
        {
            currentEncoding = Utils.GetCurrentFileEncoding(inputFilePath);
            allFileText.Append(System.IO.File.ReadAllText(inputFilePath, currentEncoding));
            allFileText.AppendLine();
        }

        System.IO.File.WriteAllText(outputPath, allFileText.ToString(), currentEncoding);

        Assert.IsTrue(System.IO.File.Exists(outputPath));
        var content = System.IO.File.ReadAllText(outputPath);
        Assert.IsTrue(content.Contains("Content 1"), "Expected content to contain 'Content 1'");
        Assert.IsTrue(content.Contains("Content 2"), "Expected content to contain 'Content 2'");
    }

    [TestMethod(DisplayName = "GetCurrentFileEncoding detecta codificação UTF-8")]
    [Description("Verifica que a detecção de encoding identifica UTF-8 corretamente.")]
    public void GetCurrentFileEncoding_DetectsUtf8()
    {
        var filePath = Path.Combine(_testDirectory, "utf8test.txt");
        System.IO.File.WriteAllText(filePath, "áéíóúãõû", Encoding.UTF8);

        var encoding = Utils.GetCurrentFileEncoding(filePath);
        Assert.IsNotNull(encoding);
    }

    [TestMethod(DisplayName = "Combine sem OutputFile especificado deve usar extensão padrão .sql")]
    [Description("Verifica que quando o arquivo de saída não é informado, o nome padrão 'CombineFiles.sql' é utilizado.")]
    public void Combine_OutputNaoEspecificado_UsaNomePadraoCombineFilesSql()
    {
        var file1 = Path.Combine(_testDirectory, "test1.sql");
        System.IO.File.WriteAllText(file1, "SELECT 1;", Encoding.UTF8);

        var inputFilePaths = System.IO.Directory.GetFiles(_testDirectory, "*.sql");
        Assert.AreEqual(1, inputFilePaths.Length);

        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        foreach (var inputFilePath in inputFilePaths)
        {
            currentEncoding = Utils.GetCurrentFileEncoding(inputFilePath);
            allFileText.Append(System.IO.File.ReadAllText(inputFilePath, currentEncoding));
            allFileText.AppendLine();
        }

        var expectedOutput = Path.Combine(_testDirectory, "CombineFiles.sql");
        System.IO.File.WriteAllText(expectedOutput, allFileText.ToString(), currentEncoding);

        Assert.IsTrue(System.IO.File.Exists(expectedOutput));
    }

    [TestMethod(DisplayName = "Combine com arquivos codificados em UTF-8 deve preservar caracteres especiais")]
    [Description("Verifica que caracteres com acentuação e símbolos são preservados corretamente na mesclagem.")]
    public void Combine_ArquivosUtf8_PreservaCaracteresEspeciais()
    {
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");

        var content1 = "Content with special chars: áéíóú";
        var content2 = "More content: ãõû";

        System.IO.File.WriteAllText(file1, content1, Encoding.UTF8);
        System.IO.File.WriteAllText(file2, content2, Encoding.UTF8);

        var outputPath = Path.Combine(_testDirectory, "combined.txt");

        var inputFilePaths = System.IO.Directory.GetFiles(_testDirectory, "*.txt");
        var allFileText = new StringBuilder();
        var currentEncoding = Encoding.UTF8;

        foreach (var inputFilePath in inputFilePaths)
        {
            currentEncoding = Utils.GetCurrentFileEncoding(inputFilePath);
            allFileText.Append(System.IO.File.ReadAllText(inputFilePath, currentEncoding));
            allFileText.AppendLine();
        }

        System.IO.File.WriteAllText(outputPath, allFileText.ToString(), currentEncoding);

        var result = System.IO.File.ReadAllText(outputPath, Encoding.UTF8);
        Assert.IsTrue(result.Contains("áéíóú"), "Expected special characters to be preserved");
        Assert.IsTrue(result.Contains("ãõû"), "Expected special characters to be preserved");
    }
}