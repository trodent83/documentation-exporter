using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Workflow;

/// <summary>
/// Defines the runner contract for executing the documentation export workflow.
/// </summary>
public interface IExportWorkflow
{
    /// <summary>
    /// Executes the sequence of workflow steps on the provided context.
    /// </summary>
    /// <param name="context">The export workflow context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final <see cref="ExportResult"/>.</returns>
    Task<ExportResult> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default);
}
