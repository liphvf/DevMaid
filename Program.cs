using System;
using System.CommandLine;
using System.IO;
using DevMaid.Commands;
using Microsoft.Extensions.Configuration;

namespace DevMaid
{
    internal static class Program
    {
        public static IConfigurationRoot AppSettings { get; private set; } = null!;

        private static int Main(string[] args)
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

            var rootCommand = new RootCommand("DevMaid command line tools")
            {
                TableParserCommand.Build(),
                FileCommand.Build(),
                ClaudeCodeCommand.Build()
            };

            return rootCommand.Parse(args).Invoke();
        }
    }
}
