namespace FurLab.Core.Models;

/// <summary>
/// Represents the progress of an ongoing operation.
/// </summary>
public record OperationProgress
{
    /// <summary>
    /// Gets or sets the current step number.
    /// </summary>
    public int CurrentStep { get; init; }

    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps { get; init; }

    /// <summary>
    /// Gets or sets a description of the current operation.
    /// </summary>
    public string? CurrentOperation { get; init; }

    /// <summary>
    /// Gets or sets the percentage complete (0-100).
    /// </summary>
    public double Percentage { get; init; }

    /// <summary>
    /// Gets or sets additional progress information.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets or sets whether the operation is complete.
    /// </summary>
    public bool IsComplete => TotalSteps > 0 && CurrentStep >= TotalSteps;
}
