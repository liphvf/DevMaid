using System.Globalization;
using System.Text;
using FurLab.Core.Models;

namespace FurLab.Core.Services;

/// <summary>
/// Provides CSV export functionality for query results.
/// Supports progressive append per server and consolidated merge at the end of execution.
/// </summary>
public class CsvExporterService
{
    /// <summary>
    /// Writes a consolidated CSV file by merging all success results from multiple servers and databases.
    /// </summary>
    public void WriteConsolidatedCsv(string outputPath, List<CsvRow> successResults)
    {
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
        WriteConsolidatedCsv(csv, successResults);
    }

    /// <summary>
    /// Writes consolidated CSV rows to an already-open CsvWriter.
    /// Used for testing without requiring file I/O.
    /// </summary>
    public void WriteConsolidatedCsv(CsvHelper.CsvWriter csv, List<CsvRow> successResults)
    {
        var allColumnNames = BuildColumnList(successResults);

        csv.WriteField("Server");
        csv.WriteField("Database");
        foreach (var columnName in allColumnNames)
        {
            csv.WriteField(columnName);
        }
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

    /// <summary>
    /// Appends a query result row to the per-server CSV file.
    /// </summary>
    public void AppendToServerCsv(string outputPath, CsvRow row)
    {
        var fileExists = File.Exists(outputPath);
        using var writer = new StreamWriter(outputPath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

        if (!fileExists)
        {
            csv.WriteField("Server");
            csv.WriteField("Database");
            foreach (var columnName in row.ColumnNames)
            {
                csv.WriteField(columnName);
            }
            csv.NextRecord();
        }

        foreach (var dataRow in row.Data)
        {
            csv.WriteField(row.Server);
            csv.WriteField(row.Database);
            foreach (var columnName in row.ColumnNames)
            {
                var value = dataRow.ContainsKey(columnName) ? dataRow[columnName] : string.Empty;
                csv.WriteField(value);
            }
            csv.NextRecord();
        }
    }

    /// <summary>
    /// Appends a query failure entry to the errors CSV file.
    /// </summary>
    public void WriteErrorEntry(string outputPath, string server, string database, DateTime executedAt, string error)
    {
        var fileExists = File.Exists(outputPath);
        using var writer = new StreamWriter(outputPath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

        if (!fileExists)
        {
            csv.WriteField("Server");
            csv.WriteField("Database");
            csv.WriteField("ExecutedAt");
            csv.WriteField("Error");
            csv.NextRecord();
        }

        csv.WriteField(server);
        csv.WriteField(database);
        csv.WriteField(executedAt.ToString("O", CultureInfo.InvariantCulture));
        csv.WriteField(error);
        csv.NextRecord();
    }

    /// <summary>
    /// Appends an execution log entry to the execution log CSV file.
    /// </summary>
    public void WriteLogEntry(string outputPath, ExecutionLogEntry entry)
    {
        var fileExists = File.Exists(outputPath);
        using var writer = new StreamWriter(outputPath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

        if (!fileExists)
        {
            csv.WriteField("Server");
            csv.WriteField("Database");
            csv.WriteField("ExecutedAt");
            csv.WriteField("Status");
            csv.WriteField("RowCount");
            csv.WriteField("DurationMs");
            csv.WriteField("Error");
            csv.NextRecord();
        }

        csv.WriteField(entry.Server);
        csv.WriteField(entry.Database);
        csv.WriteField(entry.ExecutedAt.ToString("O", CultureInfo.InvariantCulture));
        csv.WriteField(entry.Status);
        csv.WriteField(entry.RowCount);
        csv.WriteField(entry.DurationMs);
        csv.WriteField(entry.Error);
        csv.NextRecord();
    }

    /// <summary>
    /// Builds an ordered, deduplicated list of column names from all result sets.
    /// </summary>
    public List<string> BuildColumnList(List<CsvRow> results)
    {
        var allColumnNames = new List<string>();
        var seenColumns = new HashSet<string>();
        foreach (var result in results)
        {
            foreach (var columnName in result.ColumnNames)
            {
                if (seenColumns.Add(columnName))
                {
                    allColumnNames.Add(columnName);
                }
            }
        }
        return allColumnNames;
    }

    private static readonly char[] WindowsInvalidFileNameChars =
        ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'];

    /// <summary>
    /// Sanitizes a server name for use as a filename.
    /// </summary>
    public string SanitizeFilename(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(WindowsInvalidFileNameChars.Contains(c) ? '_' : c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Merges per-server partial CSV files into a single consolidated CSV with a unified header.
    /// </summary>
    public void MergeServerCsvsToConsolidated(string outputDirectory, string timestamp, List<string> serverNames)
    {
        var allResults = new List<CsvRow>();
        foreach (var serverName in serverNames)
        {
            var serverFile = Path.Combine(outputDirectory, $"{SanitizeFilename(serverName)}_{timestamp}.csv");
            if (!File.Exists(serverFile)) continue;

            var rows = ReadServerCsv(serverFile);
            allResults.AddRange(rows);
        }

        if (allResults.Count == 0) return;

        var consolidatedPath = Path.Combine(outputDirectory, $"consolidated_{timestamp}.csv");
        WriteConsolidatedCsv(consolidatedPath, allResults);
    }

    private List<CsvRow> ReadServerCsv(string filePath)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using var csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<dynamic>().ToList();
        if (records.Count == 0) return [];

        var header = csv.HeaderRecord;
        if (header == null) return [];

        var queryColumns = header
            .Where(h => h != "Server" && h != "Database")
            .ToList();

        var columnNames = queryColumns;
        var results = new List<CsvRow>();

        using var reader2 = new StreamReader(filePath, Encoding.UTF8);
        using var csv2 = new CsvHelper.CsvReader(reader2, CultureInfo.InvariantCulture);
        csv2.Read();
        csv2.ReadHeader();

        while (csv2.Read())
        {
            var server = csv2.GetField("Server");
            var database = csv2.GetField("Database");
            var dataRow = new Dictionary<string, string>();
            foreach (var col in queryColumns)
            {
                dataRow[col] = csv2.TryGetField<string>(col, out var val) ? val ?? string.Empty : string.Empty;
            }
            results.Add(new CsvRow(server ?? string.Empty, database ?? string.Empty, DateTime.MinValue, "Success", 0, string.Empty, 0, columnNames, [dataRow]));
        }

        return results;
    }
}
