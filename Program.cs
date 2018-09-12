using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using DevMaid.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace DevMaid
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instantiate the command line app
            var app = new CommandLineApplication();

            // This should be the name of the executable itself.
            // the help text line "Usage: ConsoleArgs" uses this
            app.Name = "DevMaid";
            app.Description = ".NET Core console app for developers helps";
            app.ExtendedHelpText = "Thanks!";

            // Set the arguments to display the description and help text
            app.HelpOption("-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            app.VersionOption("-v|--version", () =>
            {
                return string.Format("Version {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            });

            // When no commands are specified, this block will execute.
            // This is the main "command"
            app.OnExecute(async () =>
            {
                await Geral.TableToClass();

                if (app.Arguments.Count == 0)
                {
                    app.ShowHelp();
                }

                return 0;
            });

            /// https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-8.1-and-8/hh824822(v=win.10)
            app.Command("wfl", (command) =>
                {
                    //description and help text of the command.
                    command.Description = "Listar Windows Features";
                    command.ExtendedHelpText = "Listando";
                    command.HelpOption("-?|-h|--help");

                    command.OnExecute(() =>
                    {
                        Process cmd = new Process();
                        cmd.StartInfo.FileName = "CMD.exe";
                        cmd.StartInfo.Verb = "runas";
                        // cmd.StartInfo.RedirectStandardInput = true;
                        // cmd.StartInfo.RedirectStandardOutput = true;
                        // cmd.StartInfo.CreateNoWindow = true;
                        // cmd.StartInfo.UseShellExecute = false;
                        // cmd.StartInfo = new ProcessStartInfo();
                        cmd.StartInfo.Arguments = @"/K DISM /online /get-features /format:table | find ""Habilitado"" ";
                        cmd.Start();
                        // var resposta = Process.Start("CMD.exe", strCmdText);
                        // Console.WriteLine(resposta);
                        // Console.WriteLine("simple-command is executing");

                        //Do the command's work here, or via another object/method

                        // Console.WriteLine("simple-command has finished.");
                        return 0; //return 0 on a successful execution
                    });

                }
            );

            app.Command("csv-to-class", (command) =>
                {
                    //description and help text of the command.
                    command.Description = "Convert pre-format .csv file to .class";
                    command.ExtendedHelpText = "Convert arquivo.csv para .class";
                    command.HelpOption("-?|-h|--help");

                    var inputFileArgument = command.Argument("inputFile", "An Input Csv File.");

                    var inputFileOption = command.Option("-i|--input <value>",
                    "Input File",
                    CommandOptionType.SingleValue);

                    command.OnExecute(() =>
                    {
                        if (inputFileOption.HasValue())
                        {
                            Geral.CsvToClass(inputFileOption.Value());
                        }
                        else if (!string.IsNullOrWhiteSpace(inputFileArgument.Value))
                        {
                            Geral.CsvToClass(inputFileArgument.Value);
                        }
                        else
                        {
                            Console.WriteLine("Invalid Input File");
                        }
                        return 0; //return 0 on a successful execution
                    });

                }
            );


            app.Command("table-to-class", (command) =>
                {
                    //description and help text of the command.
                    command.Description = "Convert pre-format .csv file to .class";
                    command.ExtendedHelpText = "Convert arquivo.csv para .class";
                    command.HelpOption("-?|-h|--help");

                    var inputFileArgument = command.Argument("inputFile", "An Input Csv File.");

                    var dbUser = command.Option("-u|--user <value>",
                    "Database user",
                    CommandOptionType.SingleValue);

                    var dbUserPassword = command.Option("-p|--password <value>",
                    "Database user password",
                    CommandOptionType.SingleValue);

                    var db = command.Option("-d|--database <value>",
                    "Database name",
                    CommandOptionType.SingleValue);

                    command.OnExecute(() =>
                    {
                        // if (inputFileOption.HasValue())
                        // {
                        //     Geral.CsvToClass(inputFileOption.Value());
                        // }
                        // else if (!string.IsNullOrWhiteSpace(inputFileArgument.Value))
                        // {
                        //     Geral.CsvToClass(inputFileArgument.Value);
                        // }
                        // else
                        // {
                        //     Console.WriteLine("Invalid Input File");
                        // }
                        return 0; //return 0 on a successful execution
                    });

                }
            );



            // app.Command("complex-command", (command) =>
            // {
            //     // This is a command that has it's own options.
            //     command.ExtendedHelpText = "This is the extended help text for complex-command.";
            //     command.Description = "This is the description for complex-command.";
            //     command.HelpOption("-?|-h|--help");

            //     // There are 3 possible option types:
            //     // NoValue
            //     // SingleValue
            //     // MultipleValue

            //     // MultipleValue options can be supplied as one or multiple arguments
            //     // e.g. -m valueOne -m valueTwo -m valueThree
            //     var multipleValueOption = command.Option("-m|--multiple-option <value>",
            //         "A multiple-value option that can be specified multiple times",
            //         CommandOptionType.MultipleValue);

            //     // SingleValue: A basic Option with a single value
            //     // e.g. -s sampleValue
            //     var singleValueOption = command.Option("-s|--single-option <value>",
            //         "A basic single-value option",
            //         CommandOptionType.SingleValue);

            //     // NoValue are basically booleans: true if supplied, false otherwise
            //     var booleanOption = command.Option("-b|--boolean-option",
            //         "A true-false, no value option",
            //         CommandOptionType.NoValue);

            //     command.OnExecute(() =>
            //     {
            //         Console.WriteLine("complex-command is executing");

            //         // Do the command's work here, or via another object/method                    

            //         // Grab the values of the various options. when not specified, they will be null.

            //         // The NoValue type has no Value property, just the HasValue() method.
            //         bool booleanOptionValue = booleanOption.HasValue();

            //         // MultipleValue returns a List<string>
            //         List<string> multipleOptionValues = multipleValueOption.Values;

            //         // SingleValue returns a single string
            //         string singleOptionValue = singleValueOption.Value();

            //         // Check if the various options have values and display them.
            //         // Here we're checking HasValue() to see if there is a value before displaying the output.
            //         // Alternatively, you could just handle nulls from the Value properties
            //         if (booleanOption.HasValue())
            //         {
            //             Console.WriteLine("booleanOption option: {0}", booleanOptionValue.ToString());
            //         }

            //         if (multipleValueOption.HasValue())
            //         {
            //             Console.WriteLine("multipleValueOption option(s): {0}", string.Join(",", multipleOptionValues));
            //         }

            //         if (singleValueOption.HasValue())
            //         {
            //             Console.WriteLine("singleValueOption option: {0}", singleOptionValue ?? "null");
            //         }

            //         Console.WriteLine("complex-command has finished.");
            //         return 0; // return 0 on a successful execution
            //     });
            // });

            try
            {
                // This begins the actual execution of the application
                // Console.WriteLine("ConsoleArgs app executing...");
                app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                // You'll always want to catch this exception, otherwise it will generate a messy and confusing error for the end user.
                // the message will usually be something like:
                // "Unrecognized command or argument '<invalid-command>'"
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to execute application: {0}", ex.Message);
            }
        }
    }
}
