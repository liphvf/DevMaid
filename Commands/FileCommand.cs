using System;
using System.IO;
using System.Linq;
using System.Text;
using DevMaid.CommandOptions;

namespace DevMaid.Commands
{
    public static class FileCommand
    {
        public static void Combine(FileCommandOptions Options)
        {
            var pattern = Path.GetFileName(Options.Input);
            var directory = Path.GetDirectoryName(Options.Input);
            var extension = Path.GetExtension(Options.Input);

            if (String.IsNullOrWhiteSpace(Options.Output))
            {
                Options.Output = Path.Join(directory, $"CombineFiles{extension}");
            }

            var GetAllFileText = string.Empty;
            var currentEncoding = Encoding.UTF8;

            var inputFilePaths = Directory.GetFiles(directory, pattern);
            Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);

            if (!inputFilePaths.Any())
            {
                throw new Exception("Files not Found");
            }

            foreach (var inputFilePath in inputFilePaths)
            {
                currentEncoding = Utils.GetCurrentFileEncoding(inputFilePath);
                GetAllFileText += File.ReadAllText(inputFilePath, currentEncoding);
                GetAllFileText += Environment.NewLine;

                Console.WriteLine("The file {0} has been processed.", inputFilePath);
            }

            File.WriteAllText(Options.Output, $"{GetAllFileText}", currentEncoding);

        }
    }
}