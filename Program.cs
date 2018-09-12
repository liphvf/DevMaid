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
            app.OnExecute(() =>
            {

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
                    command.Description = "Convert table to .cs";
                    command.ExtendedHelpText = "Convert table to .cs";
                    command.HelpOption("-?|-h|--help");

                    var inputFileArgument = command.Argument("inputFile", "An Input Csv File.");

                    var dbUser = command.Option("-u|--user <value>",
                    "Database user", CommandOptionType.SingleValue);

                    // var dbUserPassword = string.Empty;

                    var dbName = command.Option("-d|--database <value>",
                    "Database name", CommandOptionType.SingleValue);

                    var dbTable = command.Option("-t|--table <value>",
                    "Database name", CommandOptionType.SingleValue);

                    var dbHost = command.Option("-h|--host <value>",
                    "Database name", CommandOptionType.SingleValue);

                    command.OnExecute(async () =>
                    {
                        var dbUserPassword = Geral.GetConsoleSecurePassword();
                        var connectionString = Geral.GetConnectionString(dbHost.Value(), dbName.Value(), dbUser.Value(), dbUserPassword);

                        await Geral.TableToClass(connectionString, dbTable.Value());
                        return 0;
                    });

                }
            );

            app.Command("combine", (command) =>
                {
                    //description and help text of the command.
                    command.Description = "Combine any files in one.";
                    command.ExtendedHelpText = "Combine any files in one.";
                    command.HelpOption("-?|-h|--help");

                    var inputDirectoryPathWithPattern = command.Argument("inputDirectoryPathWithPattern","inputDirectoryPathWithPattern");
                    var outputFilePath = command.Argument("outputFilePath","outputFilePath");

                    command.OnExecute(() =>
                    {
                        Console.WriteLine(inputDirectoryPathWithPattern.Value);
                        Console.WriteLine(outputFilePath.Value);

                        Geral.CombineMultipleFilesIntoSingleFile(inputDirectoryPathWithPattern.Value, outputFilePath.Value);
                        return 0;
                    });

                }
            );

            try
            {
                // This begins the actual execution of the application
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
