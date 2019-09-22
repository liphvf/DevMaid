using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using Npgsql;

namespace DevMaid
{
    public static class Database
    {
        public static List<dynamic> GetColumnsInfo(string connectionString, string tableName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Miss Connections String.");
            }
            var sqlQuery = $@"SELECT column_name, data_type, is_nullable FROM information_schema.columns where table_name = '{tableName}';";
            // var connectionString = "Host=baasu.db.elephantsql.com;Username=wzemlogc;Password=Izzk4VtPDnkz0y5gdgWzH6WL6Vf6vyXc;Database=wzemlogc";

            var parametros = new List<NpgsqlParameter>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Connection = conn;
                    command.Parameters.AddRange(parametros.ToArray());
                    var lista = new List<dynamic>();
                    using (var reader = command.ExecuteReader())
                    {
                        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                        foreach (IDataRecord record in reader as IEnumerable)
                        {
                            var expando = new ExpandoObject() as IDictionary<string, object>;
                            int i = 0;
                            foreach (var name in names)
                            {
                                expando.Add(name, record.IsDBNull(i) ? null : record[name]);
                                i++;
                            }
                            lista.Add(expando);
                        }
                    }
                    return lista;
                }
            }
        }

    }
}