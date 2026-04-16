namespace FurLab.CLI.CommandOptions;

/// <summary>
    /// Options for the settings db-servers rm command.
    /// </summary>
    public class RemoveServerCommandOptions
    {
        /// <summary>Server name to remove.</summary>
        public string? Name { get; set; }
    }
