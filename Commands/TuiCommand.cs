using System;
using System.CommandLine;

using DevMaid.Tui;

namespace DevMaid.Commands;

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
