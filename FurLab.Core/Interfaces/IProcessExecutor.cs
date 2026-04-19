namespace FurLab.Core.Interfaces;

/// <summary>
/// Defines options for process execution.
/// </summary>
public record ProcessExecutionOptions
{
    /// <summary>
    /// Gets or sets the file name or path of the executable.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the command-line arguments.
    /// </summary>
    public string? Arguments { get; init; }

    /// <summary>
    /// Gets or sets the working directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets or sets environment variables for the process.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Gets or sets whether to redirect standard output.
    /// </summary>
    public bool RedirectStandardOutput { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to redirect standard error.
    /// </summary>
    public bool RedirectStandardError { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to create a new window.
    /// </summary>
    public bool CreateNoWindow { get; init; } = true;
}

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public record ProcessExecutionResult
{
    /// <summary>
    /// Gets or sets the exit code of the process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets or sets the standard output of the process.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the standard error of the process.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the process completed successfully.
    /// </summary>
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Defines a service for executing external processes.
/// </summary>
public interface IProcessExecutor
{
    /// <summary>
    /// Executes a process with the specified options.
    /// </summary>
    /// <param name="options">The execution options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the operation with the result.</returns>
    Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionOptions options,
        CancellationToken cancellationToken = default);
}
