using System.ComponentModel;
using System.Diagnostics;

using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Claude.Settings.McpDatabase;

/// <summary>
/// Configures the MCP database integration for Claude Code.
/// </summary>
public sealed class ClaudeSettingsMcpDatabaseCommand : AsyncCommand<ClaudeSettingsMcpDatabaseSettings>
{
    private const string McpDatabaseArguments = "mcp add --transport sse toolbox http://127.0.0.1:5000/mcp/sse --scope user";

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, ClaudeSettingsMcpDatabaseSettings settings, CancellationToken cancellation)
    {
        var result = RunProcess("claude", McpDatabaseArguments);
        return Task.FromResult(result.ExitCode);
    }

    private static (int ExitCode, string Output, string Error) RunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine(error.TrimEnd());
            }

            return (process.ExitCode, output, error);
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Could not execute '{fileName}'. Please check if the command exists in PATH.", ex);
        }
    }
}
