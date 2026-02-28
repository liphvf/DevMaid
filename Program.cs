using System;
using System.CommandLine;
using System.IO;
using DevMaid.CommandOptions;
using DevMaid.Commands;
using Microsoft.Extensions.Configuration;

namespace DevMaid
{
    class Program
    {
        static public IConfigurationRoot AppSettings;
        static int Main(string[] args)
        {

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var neoveroCLIConfigurationFolder = Path.Combine(localAppData, "DevMaid");
            Directory.CreateDirectory(neoveroCLIConfigurationFolder);
            var builder = new ConfigurationBuilder()
                           .SetBasePath(neoveroCLIConfigurationFolder)
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           //.AddUserSecrets<Program>() // Habilitar caso queira usar o user secrets
                           .AddEnvironmentVariables();

            AppSettings = builder.Build();

            var rootCommand = new RootCommand("DevMaid command line tools");

            var tableParserCommand = new Command("TableParser", "Convert a database table in C# propreties class");
            tableParserCommand.Aliases.Add("tableparser");

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

            tableParserCommand.Add(userOption);
            tableParserCommand.Add(databaseOption);
            tableParserCommand.Add(passwordOption);
            tableParserCommand.Add(hostOption);
            tableParserCommand.Add(outputOption);
            tableParserCommand.Add(tableOption);

            tableParserCommand.SetAction(parseResult =>
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

            var combineCommand = new Command("Combine", "Copy dashboards between databases.");
            combineCommand.Aliases.Add("combine");

            var inputOption = new Option<string>("--input", "-i")
            {
                Description = "Input Directory.",
                Required = true
            };
            var combineOutputOption = new Option<string>("--output", "-o")
            {
                Description = "Input Directory."
            };

            combineCommand.Add(inputOption);
            combineCommand.Add(combineOutputOption);

            combineCommand.SetAction(parseResult =>
            {
                var options = new FileCommandOptions
                {
                    Input = parseResult.GetRequiredValue(inputOption),
                    Output = parseResult.GetValue(combineOutputOption)
                };

                FileCommand.Combine(options);
            });

            rootCommand.Add(tableParserCommand);
            rootCommand.Add(combineCommand);

            return rootCommand.Parse(args).Invoke();
        }
    }
}
