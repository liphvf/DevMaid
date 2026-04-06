using System.Diagnostics;

namespace DevMaid.CLI.Services;

/// <summary>
/// Service for Docker container operations.
/// </summary>
public static class DockerService
{
    /// <summary>
    /// Checks Docker availability, distinguishing between not installed and not running.
    /// </summary>
    /// <returns>A <see cref="DockerStatus"/> value indicating the Docker state.</returns>
    public static DockerStatus GetDockerStatus()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = DockerConstants.DockerExecutable,
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return DockerStatus.NotInstalled;
            }

            process.WaitForExit();
            return process.ExitCode == 0 ? DockerStatus.Running : DockerStatus.NotRunning;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Executable not found on PATH
            return DockerStatus.NotInstalled;
        }
        catch
        {
            return DockerStatus.NotInstalled;
        }
    }

    /// <summary>
    /// Checks if Docker is available on the system.
    /// </summary>
    /// <returns>True if Docker is available, false otherwise.</returns>
    public static bool IsDockerAvailable() => GetDockerStatus() == DockerStatus.Running;

    /// <summary>
    /// Checks if a container with the specified name exists.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>A tuple containing existence status and current status.</returns>
    public static (bool exists, string? status) ContainerExists(string containerName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = DockerConstants.DockerExecutable,
                Arguments = $"ps -a --filter name=^{containerName}$ --format \"{{{{.Status}}}}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, null);
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                return (true, output);
            }

            return (false, null);
        }
        catch
        {
            return (false, null);
        }
    }

    /// <summary>
    /// Starts an existing container.
    /// </summary>
    /// <param name="containerName">The name of the container to start.</param>
    /// <returns>True if successful.</returns>
    public static bool StartContainer(string containerName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = DockerConstants.DockerExecutable,
            Arguments = $"start {containerName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new DockerOperationException("Falha ao iniciar o processo docker.");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new DockerOperationException($"Falha ao iniciar container: {error}");
        }

        return true;
    }

    /// <summary>
    /// Creates a PostgreSQL container with the configured settings.
    /// </summary>
    /// <returns>The container ID.</returns>
    public static string CreatePostgresContainer()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = DockerConstants.DockerExecutable,
            Arguments = BuildPostgresRunArguments(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new DockerOperationException("Falha ao iniciar o processo docker.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new DockerOperationException($"Falha ao criar container: {error}");
        }

        return output.Trim();
    }

    /// <summary>
    /// Gets the container ID by name.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>The container ID or empty string if not found.</returns>
    public static string GetContainerId(string containerName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = DockerConstants.DockerExecutable,
            Arguments = $"ps -a --filter name=^{containerName}$ --format \"{{{{.ID}}}}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return string.Empty;
        }

        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return output;
    }

    private static string BuildPostgresRunArguments()
    {
        // PostgreSQL runtime parameters (log_statement, log_min_duration_statement) must be
        // passed as command arguments AFTER the image name, not as docker flags or POSTGRES_INITDB_ARGS.
        // POSTGRES_INITDB_ARGS only affects initdb (first-time cluster initialization).
        return string.Join(" ", new[]
        {
            "run",
            "-d",
            $"--name {DockerConstants.PostgresContainerName}",
            "--restart always",
            "-e POSTGRES_PASSWORD=dev",
            "-e LANG=pt_BR.UTF-8",
            "-e LC_ALL=pt_BR.UTF-8",
            $"-p {DockerConstants.PostgresPort}:{DockerConstants.PostgresPort}",
            $"-v {DockerConstants.PostgresVolumeName}:/var/lib/postgresql/data",
            DockerConstants.PostgresImage,
            "postgres",
            "-c log_statement=all",
            "-c log_min_duration_statement=0"
        });
    }
}

/// <summary>
/// Represents the Docker daemon availability state.
/// </summary>
public enum DockerStatus
{
    /// <summary>Docker is installed and the daemon is running.</summary>
    Running,

    /// <summary>Docker is installed but the daemon is not running.</summary>
    NotRunning,

    /// <summary>Docker executable was not found on the system PATH.</summary>
    NotInstalled
}
