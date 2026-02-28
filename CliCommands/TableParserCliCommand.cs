using System.CommandLine;
using DevMaid.CommandOptions;
using DevMaid.Commands;

namespace DevMaid.CliCommands
{
    public static class TableParserCliCommand
    {
        public static Command Build()
        {
            var command = new Command("TableParser", "Convert a database table in C# propreties class");
            command.Aliases.Add("tableparser");

            var userOption = new Option<string>("--user", "-u", "--User")
            {
                Description = "Set database user."
            };
            var databaseOption = new Option<string>("--db", "-d")
            {
                Description = "Set source database.",
                Required = true
            };
            var passwordOption = new Option<string>("--password", "-p", "--Password")
            {
                Description = "Set user password."
            };
            var hostOption = new Option<string>("--host", "-H")
            {
                Description = "Set database host."
            };
            var outputOption = new Option<string>("--output", "-o")
            {
                Description = "Set output file"
            };
            var tableOption = new Option<string>("--table", "-t")
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

                TableParserCommand.Parser(options);
            });

            return command;
        }
    }
}
