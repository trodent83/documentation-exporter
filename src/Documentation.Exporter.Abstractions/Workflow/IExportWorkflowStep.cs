namespace Documentation.Exporter.Abstractions.Workflow;

/// <summary>
/// Defines an individual step within the documentation export workflow.
/// </summary>
public interface IExportWorkflowStep
{
    /// <summary>
    /// Gets the human-friendly name of this workflow step.
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Executes the workflow step asynchronously.
    /// </summary>
    /// <param name="context">The mutable workflow state context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the step succeeded and workflow should continue; false to abort the workflow.</returns>
    Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default);
}
