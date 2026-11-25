using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Calculates XML documentation comment coverage across all compiled public types and members,
/// records actionable <c>DOC001</c>-<c>DOC005</c> quality diagnostics, and enforces optional coverage thresholds.
/// </summary>
/// <param name="qualityAnalyzer">The documentation quality analyzer evaluating summaries, parameters, type parameters, and returns.</param>
/// <param name="logger">Optional ILogger instance.</param>
public sealed partial class CalculateDocumentationCoverageStep(
    IDocumentationQualityAnalyzer qualityAnalyzer,
    ILogger<CalculateDocumentationCoverageStep>? logger = null) : IExportWorkflowStep
{
    private readonly IDocumentationQualityAnalyzer analyzer = ArgumentGuard.EnsureNotNull(qualityAnalyzer);
    private readonly ILogger<CalculateDocumentationCoverageStep> logger = logger ?? NullLogger<CalculateDocumentationCoverageStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameCalculateDocumentationCoverage;

    [LoggerMessage(EventId = 820, Level = LogLevel.Information, Message = "Documentation coverage: {CoveragePercentage:F1}% ({DocumentedCount}/{TotalCount} items documented across {TypeCount} types).")]
    private partial void LogCoverageCalculated(double coveragePercentage, int documentedCount, int totalCount, int typeCount);

    [LoggerMessage(EventId = 821, Level = LogLevel.Error, Message = "Documentation coverage {CoveragePercentage:F1}% is below required threshold {RequiredThreshold:F1}%. Documentation export aborted.")]
    private partial void LogCoverageBelowThreshold(double coveragePercentage, double requiredThreshold);

    [LoggerMessage(EventId = 822, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogQualityDiagnosticsSummary(string statusMessage);

    /// <inheritdoc />
    public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var analysisResult = analyzer.Analyze(context.CompiledTypeModels);

        context.TotalDocumentableSymbols = analysisResult.TotalDocumentableSymbols;
        context.DocumentedSymbols = analysisResult.DocumentedSymbols;
        context.DocumentationCoveragePercentage = analysisResult.CoveragePercentage;

        foreach (var diagnostic in analysisResult.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic);
        }

        LogCoverageCalculated(
            analysisResult.CoveragePercentage,
            analysisResult.DocumentedSymbols,
            analysisResult.TotalDocumentableSymbols,
            context.CompiledTypeModels.Count);

        if (analysisResult.Diagnostics.Count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            var errorCount = analysisResult.Diagnostics.Count(diagnostic =>
                diagnostic.Severity == DocumentationDiagnosticSeverity.Error);
            var warningCount = analysisResult.Diagnostics.Count(diagnostic =>
                diagnostic.Severity == DocumentationDiagnosticSeverity.Warning);

            var summaryMessage = ExecutionMessagesResources.FormatMessage(
                ExecutionMessagesResources.StepQualityDiagnosticsSummary,
                analysisResult.Diagnostics.Count,
                errorCount,
                warningCount);
            LogQualityDiagnosticsSummary(summaryMessage);
        }

        if (context.Options.FailOnUndocumentedThreshold.HasValue &&
            analysisResult.CoveragePercentage < context.Options.FailOnUndocumentedThreshold.Value)
        {
            LogCoverageBelowThreshold(analysisResult.CoveragePercentage, context.Options.FailOnUndocumentedThreshold.Value);
            context.HasFailed = true;
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
