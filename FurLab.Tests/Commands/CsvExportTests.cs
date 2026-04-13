#pragma warning disable IDE0005 // These usings are required when the project is built standalone (no solution-level ImplicitUsings)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
#pragma warning restore IDE0005
using CsvHelper;
using FurLab.CLI.Commands;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class CsvExportTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CsvExportTests_{Guid.NewGuid():N}");
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

    private static List<CsvRow> CreateSuccessResults()
    {
        return
        [
            new CsvRow("dev", "db1", DateTime.UtcNow, "Success", 2, string.Empty, ["id", "name"],
            [
                new Dictionary<string, string> { ["id"] = "1", ["name"] = "Alice" },
                new Dictionary<string, string> { ["id"] = "2", ["name"] = "Bob" }
            ]),
            new CsvRow("dev2", "db1", DateTime.UtcNow, "Success", 1, string.Empty, ["id", "name"],
            [
                new Dictionary<string, string> { ["id"] = "1", ["name"] = "Alice" }
            ])
        ];
    }

    [TestMethod(DisplayName = "CSV consolidado tem cabeçalho Server, Database, <query cols>")]
    public void ConsolidatedCsv_HasCorrectHeader()
    {
        var results = CreateSuccessResults();
        var outputPath = Path.Combine(_testDirectory, "test.csv");

        CsvExporter.WriteConsolidatedCsv(outputPath, results);

        var lines = ReadCsvLines(outputPath);
        Assert.AreEqual("Server,Database,id,name", lines[0]);
    }

    [TestMethod(DisplayName = "CSV consolidado tem uma linha por resultado de query com Server e Database")]
    public void ConsolidatedCsv_HasDataRowsWithServerAndDatabase()
    {
        var results = CreateSuccessResults();
        var outputPath = Path.Combine(_testDirectory, "test.csv");

        CsvExporter.WriteConsolidatedCsv(outputPath, results);

        var lines = ReadCsvLines(outputPath);
        Assert.AreEqual(4, lines.Length);
        Assert.IsTrue(lines[1].StartsWith("dev,db1,"));
        Assert.IsTrue(lines[2].StartsWith("dev,db1,"));
        Assert.IsTrue(lines[3].StartsWith("dev2,db1,"));
    }

    [TestMethod(DisplayName = "CSV consolidado não inclui colunas de metadados de execução")]
    public void ConsolidatedCsv_DoesNotIncludeExecutionMetadata()
    {
        var results = CreateSuccessResults();
        var outputPath = Path.Combine(_testDirectory, "test.csv");

        CsvExporter.WriteConsolidatedCsv(outputPath, results);

        var header = ReadCsvLines(outputPath)[0];
        Assert.IsFalse(header.Contains("ExecutedAt"));
        Assert.IsFalse(header.Contains("Status"));
        Assert.IsFalse(header.Contains("RowCount"));
        Assert.IsFalse(header.Contains("Error"));
    }

    [TestMethod(DisplayName = "CSV por servidor tem mesmo formato que consolidado")]
    public void ServerCsv_HasSameFormatAsConsolidated()
    {
        var results = CreateSuccessResults().Where(r => r.Server == "dev").ToList();
        var outputPath = Path.Combine(_testDirectory, "server_dev.csv");

        CsvExporter.WriteServerCsv(outputPath, "dev", results);

        var lines = ReadCsvLines(outputPath);
        Assert.AreEqual("Server,Database,id,name", lines[0]);
        Assert.AreEqual(3, lines.Length);
        Assert.IsTrue(lines[1].StartsWith("dev,db1,"));
    }

    [TestMethod(DisplayName = "CSV consolidado com múltiplas databases preserva identificação")]
    public void ConsolidatedCsv_MultipleDatabases_PreservesIdentification()
    {
        var results = new List<CsvRow>
        {
            new("dev", "db1", DateTime.UtcNow, "Success", 1, string.Empty, ["id"],
            [new Dictionary<string, string> { ["id"] = "1" }]),
            new("dev", "db2", DateTime.UtcNow, "Success", 1, string.Empty, ["id"],
            [new Dictionary<string, string> { ["id"] = "10" }]),
        };

        var outputPath = Path.Combine(_testDirectory, "test.csv");
        CsvExporter.WriteConsolidatedCsv(outputPath, results);

        var lines = ReadCsvLines(outputPath);
        Assert.IsTrue(lines[1].StartsWith("dev,db1,"));
        Assert.IsTrue(lines[2].StartsWith("dev,db2,"));
    }

    [TestMethod(DisplayName = "BuildColumnList retorna colunas em ordem de primeira aparição")]
    public void BuildColumnList_ReturnsColumnsInFirstAppearanceOrder()
    {
        var results = new List<CsvRow>
        {
            new("s1", "db1", DateTime.UtcNow, "Success", 1, string.Empty, ["b", "a"], []),
            new("s1", "db2", DateTime.UtcNow, "Success", 1, string.Empty, ["a", "c"], []),
        };

        var columns = CsvExporter.BuildColumnList(results);

        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, columns);
    }

    [TestMethod(DisplayName = "BuildColumnList deduplica colunas entre result sets")]
    public void BuildColumnList_DeduplicatesColumns()
    {
        var results = new List<CsvRow>
        {
            new("s1", "db1", DateTime.UtcNow, "Success", 1, string.Empty, ["id", "name"], []),
            new("s1", "db2", DateTime.UtcNow, "Success", 1, string.Empty, ["id", "email"], []),
        };

        var columns = CsvExporter.BuildColumnList(results);

        CollectionAssert.AreEqual(new[] { "id", "name", "email" }, columns);
    }

    [TestMethod(DisplayName = "CSV consolidado preenche coluna ausente com string vazia")]
    public void ConsolidatedCsv_MissingColumnFilledWithEmpty()
    {
        var results = new List<CsvRow>
        {
            new("s1", "db1", DateTime.UtcNow, "Success", 1, string.Empty, ["id", "name"],
            [new Dictionary<string, string> { ["id"] = "1", ["name"] = "Alice" }]),
            new("s1", "db2", DateTime.UtcNow, "Success", 1, string.Empty, ["id"],
            [new Dictionary<string, string> { ["id"] = "2" }]),
        };

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
        CsvExporter.WriteConsolidatedCsv(csv, results);
        csv.Flush();

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // header: Server,Database,id,name
        // row1:   s1,db1,1,Alice
        // row2:   s1,db2,2,  (empty name — trailing comma)
        var lastDataLine = lines[2].TrimEnd('\r');
        Assert.IsTrue(lastDataLine.EndsWith(","), $"Expected trailing comma for empty name column but got: {lastDataLine}");
    }

    private static string[] ReadCsvLines(string path)
    {
        return File.ReadAllLines(path);
    }
}
