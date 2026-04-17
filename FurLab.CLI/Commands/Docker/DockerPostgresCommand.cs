using Spectre.Console.Cli;

using FurLab.Core.Interfaces;
using FurLab.Core.Services.Docker;

namespace FurLab.CLI.Commands.Docker;

/// <summary>
/// Creates or starts a PostgreSQL Docker container for local development.
/// </summary>
public sealed class DockerPostgresCommand : AsyncCommand<DockerPostgresCommand.Settings>
{
    private readonly IDockerService _dockerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerPostgresCommand"/> class.
    /// </summary>
    /// <param name="dockerService">The Docker service.</param>
    public DockerPostgresCommand(IDockerService dockerService)
    {
        _dockerService = dockerService;
    }

    /// <summary>
    /// Settings for the Docker PostgreSQL command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var dockerStatus = _dockerService.GetDockerStatus();

        if (dockerStatus == DockerStatus.NotInstalled)
        {
            Console.WriteLine("Error: Docker não encontrado. Instale o Docker Desktop: https://www.docker.com/products/docker-desktop");
            return Task.FromResult(1);
        }

        if (dockerStatus == DockerStatus.NotRunning)
        {
            Console.WriteLine("Error: Docker está instalado mas não está em execução. Inicie o Docker Desktop e tente novamente.");
            return Task.FromResult(1);
        }

        var containerName = DockerConstants.PostgresContainerName;
        var (exists, status) = _dockerService.ContainerExists(containerName);

        if (exists)
        {
            Console.WriteLine($"Container '{containerName}' já existe (Status: {status ?? ""}). Iniciando...");
            _dockerService.StartContainer(containerName);
            DisplayConnectionInfo();
        }
        else
        {
            Console.WriteLine("Criando container PostgreSQL...");
            var containerId = _dockerService.CreatePostgresContainer();
            Console.WriteLine("Container criado com sucesso!");
            Console.WriteLine($"ID do container: {containerId ?? "unknown"}");
            DisplayConnectionInfo();
        }

        return Task.FromResult(0);
    }

    private static void DisplayConnectionInfo()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("PostgreSQL está pronto!");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("String de conexão:");
        Console.WriteLine($"  Host=localhost;Port={DockerConstants.PostgresPort};Database={DockerConstants.PostgresDatabase};Username={DockerConstants.PostgresUsername};Password={DockerConstants.PostgresPassword}");
        Console.WriteLine();
        Console.WriteLine("Ou via psql:");
        Console.WriteLine($"  psql -h localhost -p {DockerConstants.PostgresPort} -U {DockerConstants.PostgresUsername} -d {DockerConstants.PostgresDatabase}");
        Console.WriteLine();
    }
}