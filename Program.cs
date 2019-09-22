using System;
using System.IO;
using CommandLine;
using DevMaid.CommandOptions;
using DevMaid.Commands;
using Microsoft.Extensions.Configuration;

namespace DevMaid
{
    class Program
    {
        static public IConfigurationRoot AppSettings;
        static void Main(string[] args)
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

            Parser.Default.ParseArguments<TableParserOptions, FileCommandOptions>(args)
            .WithParsed<TableParserOptions>(opts =>
            {
                TableParserCommand.Parser(opts);
            })
            .WithParsed<FileCommandOptions>(opts =>
            {
                FileCommand.Combine(opts);
            });
        }
    }
}
