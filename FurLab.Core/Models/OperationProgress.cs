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

    /// <summary>
    /// Creates a new operation progress instance.
    /// </summary>
    /// <param name="currentStep">The current step.</param>
    /// <param name="totalSteps">The total steps.</param>
    /// <param name="currentOperation">The current operation description.</param>
    /// <param name="percentage">The percentage complete.</param>
    /// <param name="details">Additional details.</param>
    /// <returns>A new operation progress instance.</returns>
    public static OperationProgress Create(
        int currentStep,
        int totalSteps,
        string? currentOperation = null,
        double percentage = 0,
        string? details = null)
    {
        return new OperationProgress
        {
            CurrentStep = currentStep,
            TotalSteps = totalSteps,
            CurrentOperation = currentOperation,
            Percentage = percentage,
            Details = details
        };
    }

    /// <summary>
    /// Creates a progress instance with percentage calculated from steps.
    /// </summary>
    /// <param name="currentStep">The current step.</param>
    /// <param name="totalSteps">The total steps.</param>
    /// <param name="currentOperation">The current operation description.</param>
    /// <param name="details">Additional details.</param>
    /// <returns>A new operation progress instance.</returns>
    public static OperationProgress CreateFromSteps(
        int currentStep,
        int totalSteps,
        string? currentOperation = null,
        string? details = null)
    {
        var percentage = totalSteps > 0 ? (currentStep / (double)totalSteps) * 100 : 0;
        return new OperationProgress
        {
            CurrentStep = currentStep,
            TotalSteps = totalSteps,
            CurrentOperation = currentOperation,
            Percentage = percentage,
            Details = details
        };
    }
}
