using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DevMaid.CommandOptions;

namespace DevMaid.Commands
{
    public static class TableParserCommand
    {
        public static void Parser(TableParserOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Password))
            {
                options.Password = Utils.SecureStringToString(Utils.GetConsoleSecurePassword());
            }

            var tableColumns = Database.GetColumnsInfo(options.ConnectionStringDatabase, options.Table);
            if (tableColumns.Count <= 0)
            {
                throw new System.ArgumentException("Erro ao obter informações da tabela.");
            }

            var tiposDoBanco = new Dictionary<string, string>
                {
                    { "bigint" , "long"  },
                    { "binary" , "byte[]"  },
                    { "bit" , "bool"  },
                    { "char" , "string"  },
                    { "date" , "DateTime"  },
                    { "datetime" , "DateTime"  },
                    { "datetime2" , "DateTime"  },
                    { "datetimeoffset" , "DateTimeOffset"  },
                    { "decimal" , "decimal"  },
                    { "float" , "float"  },
                    { "image" , "byte[]"  },
                    { "int" , "int"  },
                    { "money" , "decimal"  },
                    { "nchar" , "char"  },
                    { "ntext" , "string"  },
                    { "numeric" , "decimal"  },
                    { "nvarchar" , "string"  },
                    { "real" , "double"  },
                    { "smalldatetime" , "DateTime"  },
                    { "smallint" , "short"  },
                    { "smallmoney" , "decimal"  },
                    { "text" , "string"  },
                    { "time" , "TimeSpan"  },
                    { "timestamp" , "DateTime"  },
                    { "tinyint" , "byte"  },
                    { "uniqueidentifier" , "Guid"  },
                    { "\"character varying\"", "string" },
                    {"character varying", "string"},
                    { "character", "string" },
                    {"integer", "int"},
                    {"boolean", "bool"}
                };

            foreach (var tableColumn in tableColumns)
            {
                var strbuild = new StringBuilder();

                strbuild.Append($"[Column(\"{tableColumn.column_name}\")]");
                strbuild.Append("\n");

                var tipo = tiposDoBanco.GetValueOrDefault((tableColumn.data_type as string).ToLower());
                strbuild.Append($"public {tipo}");
                if (tipo != "string")
                {
                    var nulo = tableColumn.is_nullable == "YES" ? "?" : "";
                    strbuild.Append($"{ nulo }");
                }
                strbuild.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableColumn.column_name)} " + "{ get; set; }");
                strbuild.Append("\n");


                using StreamWriter file = new StreamWriter(@"./tabela.class", true);
                file.WriteLine(strbuild.ToString());
            }

        }
    }
}