using System;
using System.CommandLine;

namespace DevMaid.Commands;

public static class TuiCommand
{
    public static Command Build()
    {
        var command = new Command("tui", "Launch DevMaid in interactive TUI mode.");

        command.SetAction(_ =>
        {
            TuiApp.Run();
        });

        return command;
    }
}
