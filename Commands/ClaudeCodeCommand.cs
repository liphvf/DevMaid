using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevMaid.Commands
{
    public static class ClaudeCodeCommand
    {
        private const string McpDatabaseArguments = "mcp add --transport sse toolbox http://127.0.0.1:5000/mcp/sse --scope user";
        private const string WingetInstallArguments = "install --id Anthropic.ClaudeCode -e --accept-package-agreements --accept-source-agreements";

        public static void Install()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("O comando 'claude install' so eh suportado no Windows por usar winget.");
            }

            var result = RunProcess("winget", WingetInstallArguments);
            if (result.ExitCode != 0)
            {
                throw new Exception($"Falha ao instalar o Claude Code com winget. Codigo de saida: {result.ExitCode}.");
            }
        }

        public static void ConfigureMcpDatabase()
        {
            RunProcess("claude", McpDatabaseArguments);
        }

        public static void ConfigureWindowsEnvironment()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("O comando 'claude settings win-env' so eh suportado no Windows.");
            }

            var configPath = GetUserClaudeConfigPath();
            var config = LoadSettingsFile(configPath);

            config["shell"] = "pwsh.exe";
            config["permission"] = new JsonObject
            {
                ["edit"] = "allow",
                ["read"] = "allow",
                ["shell"] = "allow"
            };

            SaveSettingsFile(configPath, config);
            Console.WriteLine($"Arquivo de configuracao atualizado: {configPath}");
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
                throw new InvalidOperationException($"Nao foi possivel executar '{fileName}'. Verifique se o comando existe no PATH.", ex);
            }
        }

        private static string GetUserClaudeConfigPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude.json");
        }

        private static JsonObject LoadSettingsFile(string settingsPath)
        {
            if (!File.Exists(settingsPath))
            {
                return new JsonObject();
            }

            var json = File.ReadAllText(settingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new JsonObject();
            }

            var node = JsonNode.Parse(json);
            if (node is JsonObject objectNode)
            {
                return objectNode;
            }

            throw new InvalidDataException($"O arquivo '{settingsPath}' nao contem um JSON objeto valido.");
        }

        private static void SaveSettingsFile(string settingsPath, JsonObject settings)
        {
            var json = settings.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(settingsPath, json + Environment.NewLine);
        }

    }
}
