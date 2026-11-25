using Documentation.Exporter.Abstractions.Workflow;
using System.Collections.ObjectModel;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow;

/// <summary>
/// Orchestrates the execution of sequential workflow steps and ensures quality diagnostics
/// are preserved and optionally persisted even when a gate step aborts execution.
/// </summary>
/// <param name="workflowSteps">The sequence of workflow steps.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
/// <param name="qualityReportWriter">Optional quality report writer used to persist diagnostics if an earlier step fails.</param>
public sealed partial class ExportWorkflow(
    IEnumerable<IExportWorkflowStep> workflowSteps,
    ILogger<ExportWorkflow>? logger = null,
    IDocumentationQualityReportWriter? qualityReportWriter = null) : IExportWorkflow
{
    private readonly ReadOnlyCollection<IExportWorkflowStep> steps =
        ArgumentGuard.EnsureNotNull(workflowSteps).ToList().AsReadOnly();
    private readonly ILogger<ExportWorkflow> logger = logger ?? NullLogger<ExportWorkflow>.Instance;
    private readonly IDocumentationQualityReportWriter? reportWriter = qualityReportWriter;

    [LoggerMessage(EventId = 801, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogWorkflowStarted(string statusMessage);

    [LoggerMessage(EventId = 802, Level = LogLevel.Debug, Message = "{StatusMessage}")]
    private partial void LogExecutingStep(string statusMessage);

    [LoggerMessage(EventId = 803, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogStepFailed(string statusMessage);

    [LoggerMessage(EventId = 804, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogWorkflowCompleted(string statusMessage);

    [LoggerMessage(EventId = 805, Level = LogLevel.Warning, Message = "Failed to persist quality report on workflow failure: {ErrorMessage}")]
    private partial void LogFailureReportPersistError(string errorMessage);

    /// <inheritdoc />
    public async Task<ExportResult> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startedMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.WorkflowStarted, steps.Count);
        LogWorkflowStarted(startedMessage);

        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var executingStepMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.WorkflowExecutingStep, step.StepName);
            LogExecutingStep(executingStepMessage);

            bool stepSuccess;
            try
            {
                stepSuccess = await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception stepException)
            {
                stepSuccess = false;
                LogStepFailed(ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.WorkflowStepFailed,
                    $"{step.StepName} ({stepException.Message})"));
            }

            if (!stepSuccess)
            {
                context.HasFailed = true;
                LogStepFailed(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.WorkflowStepFailed, step.StepName));

                var failureReport = context.QualityReport ?? context.BuildQualityReportSnapshot();
                context.QualityReport = failureReport;
                await TryPersistFailureReportAsync(context, failureReport, cancellationToken).ConfigureAwait(false);

                return ExportResult.Failure(
                    context.GeneratedFilesCount,
                    context.DocumentationCoveragePercentage,
                    failureReport);
            }
        }

        var finalReport = context.QualityReport ?? context.BuildQualityReportSnapshot();
        context.QualityReport = finalReport;

        LogWorkflowCompleted(ExecutionMessagesResources.WorkflowCompleted);
        return ExportResult.Success(context.GeneratedFilesCount, context.DocumentationCoveragePercentage, finalReport);
    }

    private async Task TryPersistFailureReportAsync(
        ExportWorkflowContext context,
        DocumentationQualityReport failureReport,
        CancellationToken cancellationToken)
    {
        if (reportWriter == null ||
            context.IsQualityReportWritten ||
            string.IsNullOrWhiteSpace(context.Options.ReportFilePath))
        {
            return;
        }

        try
        {
            await reportWriter.WriteReportAsync(
                failureReport,
                context.Options.ReportFilePath,
                context.Options.ReportFormat,
                cancellationToken).ConfigureAwait(false);
            context.IsQualityReportWritten = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            LogFailureReportPersistError(exception.Message);
        }
    }
}
