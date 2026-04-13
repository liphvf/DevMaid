using System.Globalization;
using System.Text;

namespace FurLab.CLI.Commands;

internal static class CsvExporter
{
    public static void WriteConsolidatedCsv(string outputPath, List<CsvRow> successResults)
    {
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
        WriteConsolidatedCsv(csv, successResults);
    }

    internal static void WriteConsolidatedCsv(CsvHelper.CsvWriter csv, List<CsvRow> successResults)
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

    public static void AppendToServerCsv(string outputPath, CsvRow row)
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

    public static void WriteErrorEntry(string outputPath, string server, string database, DateTime executedAt, string error)
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

    public static void WriteLogEntry(string outputPath, ExecutionLogEntry entry)
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

    internal static List<string> BuildColumnList(List<CsvRow> results)
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

    internal static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(invalid.Contains(c) ? '_' : c);
        }
        return sb.ToString();
    }

    public static void MergeServerCsvsToConsolidated(string outputDirectory, string timestamp, List<string> serverNames)
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

    private static List<CsvRow> ReadServerCsv(string filePath)
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
