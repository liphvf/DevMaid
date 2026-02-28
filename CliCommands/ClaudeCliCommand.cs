using System.CommandLine;
using DevMaid.Commands;

namespace DevMaid.CliCommands
{
    public static class ClaudeCliCommand
    {
        public static Command Build()
        {
            var command = new Command("claude", "Comandos para Claude Code");

            var installCommand = new Command("install", "Instala o Claude Code usando winget");
            installCommand.SetAction(parseResult =>
            {
                ClaudeCodeCommand.Install();
            });

            var settingsCommand = new Command("settings", "Configuracoes do Claude Code");

            var mcpDatabaseCommand = new Command("mcp-database", "Executa o comando de cadastro do MCP database no Claude");
            mcpDatabaseCommand.SetAction(parseResult =>
            {
                ClaudeCodeCommand.ConfigureMcpDatabase();
            });

            var winEnvCommand = new Command("win-env", "Configura o ~/.claude.json para usar pwsh e liberar edit/read/shell");
            winEnvCommand.SetAction(parseResult =>
            {
                ClaudeCodeCommand.ConfigureWindowsEnvironment();
            });

            settingsCommand.Add(mcpDatabaseCommand);
            settingsCommand.Add(winEnvCommand);

            command.Add(installCommand);
            command.Add(settingsCommand);

            return command;
        }
    }
}
