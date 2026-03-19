using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Text;

using DevMaid.CLI.CommandOptions;

namespace DevMaid.CLI.Commands;

public static class TableParserCommand
{
    private static readonly Dictionary<string, string> DatabaseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "bigint", "long" },
        { "binary", "byte[]" },
        { "bit", "bool" },
        { "char", "string" },
        { "date", "DateTime" },
        { "datetime", "DateTime" },
        { "datetime2", "DateTime" },
        { "datetimeoffset", "DateTimeOffset" },
        { "decimal", "decimal" },
        { "float", "float" },
        { "image", "byte[]" },
        { "int", "int" },
        { "money", "decimal" },
        { "nchar", "char" },
        { "ntext", "string" },
        { "numeric", "decimal" },
        { "nvarchar", "string" },
        { "real", "double" },
        { "smalldatetime", "DateTime" },
        { "smallint", "short" },
        { "smallmoney", "decimal" },
        { "text", "string" },
        { "time", "TimeSpan" },
        { "timestamp", "DateTime" },
        { "tinyint", "byte" },
        { "uniqueidentifier", "Guid" },
        { "\"character varying\"", "string" },
        { "character varying", "string" },
        { "character", "string" },
        { "integer", "int" },
        { "boolean", "bool" }
    };

    public static Command Build()
    {
        var command = new Command("table-parser", "Convert a database table in C# propreties class");
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

            Parse(options);
        });

        return command;
    }

    public static void Parse(TableParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Table))
        {
            throw new ArgumentException("Table name is required.");
        }

        // Validate table name to prevent SQL injection
        if (!SecurityUtils.IsValidPostgreSQLIdentifier(options.Table))
        {
            throw new ArgumentException($"Invalid table name: '{options.Table}'. Table name must contain only letters, numbers, and underscores, and must not start with a number.");
        }

        // Validate host
        if (!SecurityUtils.IsValidHost(options.Host))
        {
            throw new ArgumentException($"Invalid host: '{options.Host}'");
        }

        // Validate username
        if (!string.IsNullOrWhiteSpace(options.User) && !SecurityUtils.IsValidUsername(options.User))
        {
            throw new ArgumentException($"Invalid username: '{options.User}'");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            options.Password = Utils.SecureStringToString(Utils.GetConsoleSecurePassword());
        }

        var outputPath = string.IsNullOrWhiteSpace(options.Output) ? "./Table.class" : options.Output;
        
        // Validate output path to prevent path traversal before normalizing
        if (!SecurityUtils.IsValidPath(outputPath))
        {
            throw new ArgumentException($"Invalid output path: '{outputPath}'. Path traversal not allowed.");
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var tableColumns = Database.GetColumnsInfo(options.ConnectionStringDatabase, options.Table);
        if (tableColumns.Count <= 0)
        {
            throw new ArgumentException("Erro ao obter informações da tabela.");
        }

        using var file = new StreamWriter(fullOutputPath, append: false);

        foreach (var tableColumn in tableColumns)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append($"[Column(\"{tableColumn.ColumnName}\")]");
            stringBuilder.AppendLine();

            var mappedType = DatabaseTypes.GetValueOrDefault(tableColumn.DataType.ToLowerInvariant(), "object");
            stringBuilder.Append($"public {mappedType}");
            if (tableColumn.IsNullable && mappedType != "string")
            {
                stringBuilder.Append('?');
            }

            stringBuilder.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableColumn.ColumnName)} " + "{ get; set; }");
            stringBuilder.AppendLine();

            file.WriteLine(stringBuilder.ToString());
        }
    }
}
