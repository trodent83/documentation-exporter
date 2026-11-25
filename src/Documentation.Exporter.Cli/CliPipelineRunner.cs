using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Default <see cref="ICliPipelineRunner"/> implementation executing <see cref="IDocumentationExporterEngine"/>
/// inside the unified DI container and updating <see cref="CliLoggingLevelSwitch"/> per invocation.
/// </summary>
/// <param name="engine">The documentation exporter engine.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="loggingLevelSwitch">The dynamic CLI minimum log level switch.</param>
public sealed partial class CliPipelineRunner(
    IDocumentationExporterEngine engine,
    ILogger<CliPipelineRunner> logger,
    CliLoggingLevelSwitch loggingLevelSwitch) : ICliPipelineRunner
{
    private readonly IDocumentationExporterEngine exporterEngine = ArgumentGuard.EnsureNotNull(engine);
    private readonly ILogger<CliPipelineRunner> cliLogger = ArgumentGuard.EnsureNotNull(logger);
    private readonly CliLoggingLevelSwitch levelSwitch = ArgumentGuard.EnsureNotNull(loggingLevelSwitch);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private static partial void LogInitializingPipeline(ILogger logger, string statusMessage);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private static partial void LogExportSuccess(ILogger logger, string statusMessage);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private static partial void LogExportError(ILogger logger, string statusMessage);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Export pipeline execution failed: {ErrorMessage}")]
    private static partial void LogExportPipelineException(ILogger logger, Exception exception, string errorMessage);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private static partial void LogQualityDiagnosticsSummary(ILogger logger, string statusMessage);

    /// <inheritdoc />
    public async Task<int> RunAsync(ExporterOptions exporterOptions, LogLevel logLevel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exporterOptions);

        levelSwitch.MinimumLevel = logLevel;

        try
        {
            LogInitializingPipeline(cliLogger, ExecutionMessagesResources.CliInitializingPipeline);
            var exportResult = await exporterEngine.ExportDocumentationAsync(exporterOptions, cancellationToken).ConfigureAwait(false);

            if (exportResult.QualityReport != null &&
                exportResult.Diagnostics.Count > 0 &&
                cliLogger.IsEnabled(LogLevel.Information))
            {
                var summaryMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.StepQualityDiagnosticsSummary,
                    exportResult.Diagnostics.Count,
                    exportResult.QualityReport.ErrorCount,
                    exportResult.QualityReport.WarningCount);
                LogQualityDiagnosticsSummary(cliLogger, summaryMessage);
            }

            if (exportResult.IsSuccess)
            {
                LogExportSuccess(cliLogger, ExecutionMessagesResources.CliExportSuccess);
                return 0;
            }

            LogExportError(cliLogger, ExecutionMessagesResources.CliExportError);
            return 1;
        }
        catch (OperationCanceledException canceledException)
        {
            LogExportPipelineException(cliLogger, canceledException, canceledException.Message);
            return 1;
        }
        catch (Exception exception)
        {
            LogExportPipelineException(cliLogger, exception, exception.Message);
            return 1;
        }
    }
}
