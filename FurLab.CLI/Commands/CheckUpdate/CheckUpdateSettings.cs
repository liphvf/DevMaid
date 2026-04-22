using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.CheckUpdate;

/// <summary>
/// Settings for the check-update command.
/// </summary>
public class CheckUpdateSettings : CommandSettings
{
    /// <summary>
    /// Enable automatic update checking.
    /// </summary>
    [CommandOption("--enable")]
    public bool Enable { get; set; }

    /// <summary>
    /// Disable automatic update checking.
    /// </summary>
    [CommandOption("--disable")]
    public bool Disable { get; set; }
}
