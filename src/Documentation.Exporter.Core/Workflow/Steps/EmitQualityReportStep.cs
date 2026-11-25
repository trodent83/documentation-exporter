using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Builds the finalized <see cref="Documentation.Exporter.Abstractions.Models.DocumentationQualityReport"/> snapshot on the workflow context
/// and persists it to disk when <see cref="Documentation.Exporter.Abstractions.Models.ExporterOptions.ReportFilePath"/> is configured.
/// </summary>
/// <param name="reportWriter">The quality report writer service.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class EmitQualityReportStep(
    IDocumentationQualityReportWriter reportWriter,
    ILogger<EmitQualityReportStep>? logger = null) : IExportWorkflowStep
{
    private readonly IDocumentationQualityReportWriter writer = ArgumentGuard.EnsureNotNull(reportWriter);
    private readonly ILogger<EmitQualityReportStep> logger = logger ?? NullLogger<EmitQualityReportStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameEmitQualityReport;

    [LoggerMessage(EventId = 840, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogReportEmitted(string statusMessage);

    [LoggerMessage(EventId = 841, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogReportWriteFailed(string statusMessage);

    /// <inheritdoc />
    public async Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var reportSnapshot = context.BuildQualityReportSnapshot();
        context.QualityReport = reportSnapshot;

        var reportFilePath = context.Options.ReportFilePath;
        if (string.IsNullOrWhiteSpace(reportFilePath))
        {
            return true;
        }

        try
        {
            await writer.WriteReportAsync(
                reportSnapshot,
                reportFilePath,
                context.Options.ReportFormat,
                cancellationToken).ConfigureAwait(false);
            context.IsQualityReportWritten = true;

            if (logger.IsEnabled(LogLevel.Information))
            {
                var emittedMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.StepQualityReportEmitted,
                    context.Options.ReportFormat,
                    reportFilePath);
                LogReportEmitted(emittedMessage);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                var failedMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.StepQualityReportFailed,
                    reportFilePath,
                    exception.Message);
                LogReportWriteFailed(failedMessage);
            }

            context.HasFailed = true;
            return false;
        }
    }
}
