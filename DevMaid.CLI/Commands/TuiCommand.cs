using System;
using System.CommandLine;

using DevMaid.CLI.Tui;

namespace DevMaid.CLI.Commands;

public static class TuiCommand
{
    public static Command Build()
    {
        var command = new Command("tui", "Launch DevMaid in interactive TUI mode. (Experimental)");

        command.SetAction(_ =>
        {
            TuiApp.Run();
        });

        return command;
    }
}
