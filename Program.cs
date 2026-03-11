using System;
using System.CommandLine;

using DevMaid.Commands;
using DevMaid.Services.Logging;

namespace DevMaid;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Initialize logger
        Logger.SetLogger(new ConsoleLogger(useColors: true));

        var rootCommand = new RootCommand("DevMaid command line tools")
        {
            TableParserCommand.Build(),
            FileCommand.Build(),
            ClaudeCodeCommand.Build(),
            OpenCodeCommand.Build(),
            WingetCommand.Build(),
            TuiCommand.Build(),
            DatabaseCommand.Build(),
            QueryCommand.Build(),
            CleanCommand.Build(),
            WindowsFeaturesCommand.Build()
        };

        return rootCommand.Parse(args).Invoke();
    }
}
