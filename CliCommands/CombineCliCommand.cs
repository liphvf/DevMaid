using System.CommandLine;
using DevMaid.CommandOptions;
using DevMaid.Commands;

namespace DevMaid.CliCommands
{
    public static class CombineCliCommand
    {
        public static Command Build()
        {
            var command = new Command("Combine", "Copy dashboards between databases.");
            command.Aliases.Add("combine");

            var inputOption = new Option<string>("--input", "-i")
            {
                Description = "Input Directory.",
                Required = true
            };
            var outputOption = new Option<string>("--output", "-o")
            {
                Description = "Input Directory."
            };

            command.Add(inputOption);
            command.Add(outputOption);

            command.SetAction(parseResult =>
            {
                var options = new FileCommandOptions
                {
                    Input = parseResult.GetRequiredValue(inputOption),
                    Output = parseResult.GetValue(outputOption)
                };

                FileCommand.Combine(options);
            });

            return command;
        }
    }
}
