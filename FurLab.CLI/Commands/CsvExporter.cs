using System.Globalization;
using System.Text;

namespace FurLab.CLI.Commands;

/// <summary>
/// Provides CSV export functionality for query results.
/// Extracted to allow unit testing without depending on file I/O side effects.
/// </summary>
internal static class CsvExporter
{
    /// <summary>
    /// Writes a consolidated CSV file containing results from multiple servers and databases.
    /// Columns: Server, Database, then all query result columns (union of all result sets).
    /// </summary>
    /// <param name="outputPath">Destination file path.</param>
    /// <param name="successResults">Query results with Status == "Success".</param>
    public static void WriteConsolidatedCsv(string outputPath, List<CsvRow> successResults)
    {
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
        WriteConsolidatedCsv(csv, successResults);
    }

    /// <summary>
    /// Writes a per-server CSV file containing results for a single server.
    /// Columns: Server, Database, then all query result columns.
    /// </summary>
    /// <param name="outputPath">Destination file path.</param>
    /// <param name="serverName">Name of the server (written into the Server column).</param>
    /// <param name="serverResults">Query results for this server.</param>
    public static void WriteServerCsv(string outputPath, string serverName, List<CsvRow> serverResults)
    {
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
        WriteServerCsv(csv, serverName, serverResults);
    }

    /// <summary>
    /// Writes consolidated CSV rows to an already-open CsvWriter.
    /// Used for testing without requiring file I/O.
    /// </summary>
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

    /// <summary>
    /// Writes per-server CSV rows to an already-open CsvWriter.
    /// Used for testing without requiring file I/O.
    /// </summary>
    internal static void WriteServerCsv(CsvHelper.CsvWriter csv, string serverName, List<CsvRow> serverResults)
    {
        var allColumnNames = BuildColumnList(serverResults);

        csv.WriteField("Server");
        csv.WriteField("Database");
        foreach (var columnName in allColumnNames)
        {
            csv.WriteField(columnName);
        }

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

    /// <summary>
    /// Builds an ordered, deduplicated list of column names from all result sets.
    /// Column order is determined by first appearance across results.
    /// </summary>
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
}
