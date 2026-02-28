using System;
using System.Collections.Generic;
using Npgsql;

namespace DevMaid
{
    public sealed record TableColumnInfo(string ColumnName, string DataType, bool IsNullable);

    public static class Database
    {
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
}
