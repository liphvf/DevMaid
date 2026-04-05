
using Npgsql;

namespace DevMaid.CLI;

/// <summary>
/// Represents column information for a database table.
/// </summary>
/// <param name="ColumnName">The name of the column.</param>
/// <param name="DataType">The data type of the column.</param>
/// <param name="IsNullable">Whether the column allows null values.</param>
public sealed record TableColumnInfo(string ColumnName, string DataType, bool IsNullable);

/// <summary>
/// Provides database utility methods.
/// </summary>
public static class Database
{
    /// <summary>
    /// Retrieves column information for a given table from a PostgreSQL database.
    /// </summary>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <param name="tableName">The name of the table to inspect.</param>
    /// <returns>A list of <see cref="TableColumnInfo"/> describing the table's columns.</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string or table name is null or whitespace.</exception>
    public static List<TableColumnInfo> GetColumnsInfo(string connectionString, string? tableName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Miss Connections String.");
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Miss table name.");
        }

        const string sqlQuery = @"SELECT column_name, data_type, is_nullable
                                      FROM information_schema.columns
                                      WHERE table_name = @tableName;";

        var columns = new List<TableColumnInfo>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var command = new NpgsqlCommand(sqlQuery, conn);
        command.Parameters.AddWithValue("tableName", tableName);

        using var reader = command.ExecuteReader();
        var columnNameOrdinal = reader.GetOrdinal("column_name");
        var dataTypeOrdinal = reader.GetOrdinal("data_type");
        var isNullableOrdinal = reader.GetOrdinal("is_nullable");

        while (reader.Read())
        {
            var columnName = reader.IsDBNull(columnNameOrdinal) ? string.Empty : reader.GetString(columnNameOrdinal);
            var dataType = reader.IsDBNull(dataTypeOrdinal) ? string.Empty : reader.GetString(dataTypeOrdinal);
            var isNullableText = reader.IsDBNull(isNullableOrdinal) ? "NO" : reader.GetString(isNullableOrdinal);

            if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(dataType))
            {
                continue;
            }

            var isNullable = string.Equals(isNullableText, "YES", StringComparison.OrdinalIgnoreCase);
            columns.Add(new TableColumnInfo(columnName, dataType, isNullable));
        }

        return columns;
    }
}
