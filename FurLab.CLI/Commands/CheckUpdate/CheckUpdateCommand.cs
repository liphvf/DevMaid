using FurLab.Core.Interfaces;
using FurLab.Core.Models;

using Spectre.Console;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.CheckUpdate;

/// <summary>
/// Command to check for FurLab updates synchronously.
/// </summary>
public class CheckUpdateCommand : AsyncCommand<CheckUpdateSettings>
{
    private readonly IUserConfigService _configService;
    private readonly IUpdateCheckService _updateCheckService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckUpdateCommand"/> class.
    /// </summary>
    public CheckUpdateCommand(IUserConfigService configService, IUpdateCheckService updateCheckService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _updateCheckService = updateCheckService ?? throw new ArgumentNullException(nameof(updateCheckService));
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, CheckUpdateSettings settings, CancellationToken cancellationToken)
    {
        // Handle enable/disable flags first
        if (settings.Enable)
        {
            return EnableUpdateCheck();
        }

        if (settings.Disable)
        {
            return DisableUpdateCheck();
        }

        // Perform synchronous update check
        return await PerformUpdateCheckAsync(cancellationToken);
    }

    private int EnableUpdateCheck()
    {
        var config = _configService.GetUpdateCheckConfig();
        config.Enabled = true;
        _configService.SaveUpdateCheckConfig(config);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓[/] Verificação automática de atualizações habilitada.");
        AnsiConsole.MarkupLine("[grey]Frequência: 1 vez ao dia[/]");
        AnsiConsole.MarkupLine("[grey]Próxima verificação: após o próximo comando[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private int DisableUpdateCheck()
    {
        var config = _configService.GetUpdateCheckConfig();
        config.Enabled = false;
        _configService.SaveUpdateCheckConfig(config);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]✓[/] Verificação automática de atualizações desabilitada.");
        AnsiConsole.MarkupLine("[grey]Use 'fur check-update --enable' para reabilitar.[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private async Task<int> PerformUpdateCheckAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Verificando atualizações...[/]");
        AnsiConsole.WriteLine();

        try
        {
            // Get current installation method
            var config = _configService.GetUpdateCheckConfig();
            var method = config.InstallationMethod ?? "desconhecido";

            AnsiConsole.MarkupLine($"[grey]Método de instalação:[/] {GetMethodDisplayName(method)}");

            // Check for updates
            var cache = await _updateCheckService.CheckForUpdateAsync(cancellationToken);

            if (cache == null)
            {
                AnsiConsole.MarkupLine("[red]✗[/] Não foi possível verificar atualizações.");
                AnsiConsole.MarkupLine("[grey]Verifique sua conexão com a internet e tente novamente.[/]");
                AnsiConsole.WriteLine();
                return 1;
            }

            AnsiConsole.MarkupLine($"[grey]Versão atual:[/] {cache.CurrentVersion}");
            AnsiConsole.MarkupLine($"[grey]Última versão:[/] {cache.LatestVersion}");
            AnsiConsole.WriteLine();

            if (cache.UpdateAvailable)
            {
                DisplayUpdateBanner(cache);
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Você está na versão mais recente ([blue]{cache.CurrentVersion}[/]).");
            }

            AnsiConsole.WriteLine();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Erro ao verificar atualizações: {ex.Message.EscapeMarkup()}");
            AnsiConsole.WriteLine();
            return 1;
        }
    }

    private void DisplayUpdateBanner(UpdateCache cache)
    {
        var method = cache.InstallationMethod;
        var updateCommand = method switch
        {
            "winget" => "winget upgrade FurLab.CLI",
            "dotnet-tool" => "dotnet tool update -g FurLab",
            _ => null
        };

        AnsiConsole.Write(
            new Panel(
                new Markup($"""
                    [yellow]📦 Nova versão disponível![/]

                    Instalada: [grey]{cache.CurrentVersion}[/]
                    Disponível: [green]{cache.LatestVersion}[/]
                    """ +
                    (updateCommand != null ? $"\n\nPara atualizar:\n  [blue]{updateCommand}[/]" : $"\n\nBaixe em:\n  [blue]{cache.ReleaseUrl}[/]")
                )
            )
            {
                Header = new PanelHeader("[yellow]Atualização Disponível[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1)
            }
        );
    }

    private static string GetMethodDisplayName(string method)
    {
        return method.ToLowerInvariant() switch
        {
            "winget" => "Windows Package Manager (winget)",
            "dotnet-tool" => ".NET Global Tool",
            "manual" => "Instalação Manual",
            _ => method
        };
    }
}
