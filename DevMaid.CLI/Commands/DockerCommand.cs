using System.CommandLine;
using DevMaid.CLI.Services;
using DevMaid.CLI.Services.Logging;

namespace DevMaid.CLI.Commands;

/// <summary>
/// Provides Docker utilities including PostgreSQL container management.
/// </summary>
public static class DockerCommand
{
    /// <summary>
    /// Builds the Docker command structure.
    /// </summary>
    /// <returns>The configured Command.</returns>
    public static Command Build()
    {
        var command = new Command("docker", "Docker utilities.")
        {
            BuildPostgresCommand()
        };

        return command;
    }

    private static Command BuildPostgresCommand()
    {
        var postgresCommand = new Command("postgres", "Start a PostgreSQL container for local development.");

        postgresCommand.SetAction(parseResult =>
        {
            RunPostgresCommand();
        });

        return postgresCommand;
    }

    private static void RunPostgresCommand()
    {
        var dockerStatus = DockerService.GetDockerStatus();

        if (dockerStatus == DockerStatus.NotInstalled)
        {
            Logger.LogError("Docker não encontrado. Instale o Docker Desktop: https://www.docker.com/products/docker-desktop");
            return;
        }

        if (dockerStatus == DockerStatus.NotRunning)
        {
            Logger.LogError("Docker está instalado mas não está em execução. Inicie o Docker Desktop e tente novamente.");
            return;
        }

        var containerName = DockerConstants.PostgresContainerName;
        var (exists, status) = DockerService.ContainerExists(containerName);

        if (exists)
        {
            Logger.LogInformation("Container '{ContainerName}' já existe (Status: {Status}). Iniciando...", containerName, status ?? "");
            DockerService.StartContainer(containerName);
            DisplayConnectionInfo();
        }
        else
        {
            Logger.LogInformation("Criando container PostgreSQL...");
            var containerId = DockerService.CreatePostgresContainer();
            Logger.LogInformation("Container criado com sucesso!");
            Logger.LogInformation("ID do container: {ContainerId}", containerId ?? "unknown");
            DisplayConnectionInfo();
        }
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
