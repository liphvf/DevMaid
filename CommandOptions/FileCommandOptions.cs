using CommandLine;

namespace DevMaid.CommandOptions
{
    [Verb("File", HelpText = "Copy dashboards between databases.")]
    public class FileCommandOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input Directory.")]
        public string Input { get; set; } = "postgres";

        [Option('o', "output", Required = false, HelpText = "Input Directory.")]
        public string Output { get; set; }
    }
}