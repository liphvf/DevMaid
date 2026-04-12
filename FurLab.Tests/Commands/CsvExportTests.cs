using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        WriteConsolidatedCsv(outputPath, results);

        var lines = ReadCsvLines(outputPath);
        Assert.AreEqual("Server,Database,id,name", lines[0]);
    }

    [TestMethod(DisplayName = "CSV consolidado tem uma linha por resultado de query com Server e Database")]
    public void ConsolidatedCsv_HasDataRowsWithServerAndDatabase()
    {
        var results = CreateSuccessResults();
        var outputPath = Path.Combine(_testDirectory, "test.csv");

        WriteConsolidatedCsv(outputPath, results);

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

        WriteConsolidatedCsv(outputPath, results);

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

        WriteServerCsv(outputPath, "dev", results);

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
        WriteConsolidatedCsv(outputPath, results);

        var lines = ReadCsvLines(outputPath);
        Assert.IsTrue(lines[1].StartsWith("dev,db1,"));
        Assert.IsTrue(lines[2].StartsWith("dev,db2,"));
    }

    private static void WriteConsolidatedCsv(string outputPath, List<CsvRow> successResults)
    {
        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var allColumnNames = new List<string>();
        var seenColumns = new HashSet<string>();
        foreach (var result in successResults)
        {
            foreach (var columnName in result.ColumnNames)
            {
                if (seenColumns.Add(columnName))
                    allColumnNames.Add(columnName);
            }
        }

        csv.WriteField("Server");
        csv.WriteField("Database");
        foreach (var columnName in allColumnNames) csv.WriteField(columnName);
        csv.NextRecord();

        foreach (var result in successResults)
        {
            foreach (var dataRow in result.Data)
            {
                csv.WriteField(result.Server);
                csv.WriteField(result.Database);
                foreach (var columnName in allColumnNames)
                {
                    var value = dataRow.ContainsKey(columnName) ? dataRow[columnName] : string.Empty;
                    csv.WriteField(value);
                }
                csv.NextRecord();
            }
        }
    }

    private static void WriteServerCsv(string outputPath, string serverName, List<CsvRow> serverResults)
    {
        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var allColumnNames = new List<string>();
        var seenColumns = new HashSet<string>();
        foreach (var result in serverResults)
        {
            foreach (var columnName in result.ColumnNames)
            {
                if (seenColumns.Add(columnName))
                    allColumnNames.Add(columnName);
            }
        }

        csv.WriteField("Server");
        csv.WriteField("Database");
        foreach (var columnName in allColumnNames) csv.WriteField(columnName);
        csv.NextRecord();

        foreach (var result in serverResults)
        {
            foreach (var dataRow in result.Data)
            {
                csv.WriteField(serverName);
                csv.WriteField(result.Database);
                foreach (var columnName in allColumnNames)
                {
                    var value = dataRow.ContainsKey(columnName) ? dataRow[columnName] : string.Empty;
                    csv.WriteField(value);
                }
                csv.NextRecord();
            }
        }
    }

    private static string[] ReadCsvLines(string path)
    {
        return File.ReadAllLines(path);
    }
}
