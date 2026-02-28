using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Text;
using DevMaid.CommandOptions;

namespace DevMaid.Commands
{
    public static class TableParserCommand
    {
        private static readonly Dictionary<string, string> DatabaseTypes = new(StringComparer.OrdinalIgnoreCase)
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
            { "character varying", "string" },
            { "character", "string" },
            { "integer", "int" },
            { "boolean", "bool" }
        };

        public static Command Build()
        {
            var command = new Command("TableParser", "Convert a database table in C# propreties class");
            command.Aliases.Add("tableparser");

            var userOption = new Option<string?>("--user", "-u", "--User")
            {
                Description = "Set database user."
            };
            var databaseOption = new Option<string>("--db", "-d")
            {
                Description = "Set source database.",
                Required = true
            };
            var passwordOption = new Option<string?>("--password", "-p", "--Password")
            {
                Description = "Set user password."
            };
            var hostOption = new Option<string?>("--host", "-H")
            {
                Description = "Set database host."
            };
            var outputOption = new Option<string?>("--output", "-o")
            {
                Description = "Set output file"
            };
            var tableOption = new Option<string?>("--table", "-t")
            {
                Description = "Table Name"
            };

            command.Add(userOption);
            command.Add(databaseOption);
            command.Add(passwordOption);
            command.Add(hostOption);
            command.Add(outputOption);
            command.Add(tableOption);

            command.SetAction(parseResult =>
            {
                var options = new TableParserOptions
                {
                    User = parseResult.GetValue(userOption) ?? "postgres",
                    Database = parseResult.GetRequiredValue(databaseOption),
                    Password = parseResult.GetValue(passwordOption),
                    Host = parseResult.GetValue(hostOption) ?? "localhost",
                    Output = parseResult.GetValue(outputOption) ?? "./Table.class",
                    Table = parseResult.GetValue(tableOption)
                };

                Parser(options);
            });

            return command;
        }

        public static void Parser(TableParserOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.Table))
            {
                throw new ArgumentException("Table name is required.");
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                options.Password = Utils.SecureStringToString(Utils.GetConsoleSecurePassword());
            }

            var tableColumns = Database.GetColumnsInfo(options.ConnectionStringDatabase, options.Table);
            if (tableColumns.Count <= 0)
            {
                throw new System.ArgumentException("Erro ao obter informações da tabela.");
            }

            var outputPath = string.IsNullOrWhiteSpace(options.Output) ? "./Table.class" : options.Output;
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using var file = new StreamWriter(outputPath, append: false);

            foreach (var tableColumn in tableColumns)
            {
                var strbuild = new StringBuilder();

                strbuild.Append($"[Column(\"{tableColumn.ColumnName}\")]");
                strbuild.AppendLine();

                var tipo = DatabaseTypes.GetValueOrDefault(tableColumn.DataType.ToLowerInvariant(), "object");
                strbuild.Append($"public {tipo}");
                if (tableColumn.IsNullable && tipo != "string")
                {
                    strbuild.Append('?');
                }

                strbuild.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableColumn.ColumnName)} " + "{ get; set; }");
                strbuild.AppendLine();

                file.WriteLine(strbuild.ToString());
            }
        }
    }
}
