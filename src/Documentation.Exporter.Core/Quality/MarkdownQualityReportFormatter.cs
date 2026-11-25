using Documentation.Exporter.Abstractions.Quality;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Formats <see cref="DocumentationQualityReport"/> instances as GitHub Flavored Markdown
/// suitable for pull-request comments and CI workflow summaries.
/// </summary>
public sealed partial class MarkdownQualityReportFormatter : IQualityReportFormatter
{
    /// <inheritdoc />
    public QualityReportFormat Format => QualityReportFormat.Markdown;

    /// <inheritdoc />
    public string FormatReport(DocumentationQualityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        StringBuilder markdownBuilder = new();
        _ = markdownBuilder.AppendLine(CultureInfo.InvariantCulture, $"# {MarkdownGenerationResources.QualityReportTitle}");
        _ = markdownBuilder.AppendLine();
        _ = markdownBuilder.AppendLine("| Metric | Value |");
        _ = markdownBuilder.AppendLine("| :--- | :--- |");
        _ = markdownBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| Documentation Coverage | {report.DocumentationCoveragePercentage:F2}% ({report.DocumentedSymbols}/{report.TotalDocumentableSymbols}) |");
        _ = markdownBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Generated Files | {report.GeneratedFilesCount} |");
        _ = markdownBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Verified Links | {report.VerifiedLinksCount} |");
        _ = markdownBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Errors | {report.ErrorCount} |");
        _ = markdownBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Warnings | {report.WarningCount} |");
        _ = markdownBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Info | {report.InfoCount} |");
        _ = markdownBuilder.AppendLine();

        if (report.Diagnostics.Count == 0)
        {
            _ = markdownBuilder.AppendLine("No documentation quality or link validation diagnostics recorded.");
            return markdownBuilder.ToString();
        }

        _ = markdownBuilder.AppendLine("## Diagnostics");
        _ = markdownBuilder.AppendLine();
        _ = markdownBuilder.AppendLine("| Code | Severity | Symbol / File | Line | Target | Message |");
        _ = markdownBuilder.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");

        foreach (var diagnostic in report.Diagnostics)
        {
            var locationDisplay = !string.IsNullOrWhiteSpace(diagnostic.FilePath)
                ? EscapeCell(diagnostic.FilePath)
                : !string.IsNullOrWhiteSpace(diagnostic.SymbolName)
                    ? EscapeCell(diagnostic.SymbolName)
                    : "-";
            var lineDisplay = diagnostic.LineNumber.HasValue
                ? diagnostic.LineNumber.Value.ToString(CultureInfo.InvariantCulture)
                : "-";
            var targetDisplay = !string.IsNullOrWhiteSpace(diagnostic.TargetReference)
                ? EscapeCell(diagnostic.TargetReference)
                : "-";

            _ = markdownBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{EscapeCell(diagnostic.Code)}` | {diagnostic.Severity} | `{locationDisplay}` | {lineDisplay} | {targetDisplay} | {EscapeCell(diagnostic.Message)} |");
        }

        return markdownBuilder.ToString();
    }

    private static string EscapeCell(string rawValue) =>
        LineBreakRegex().Replace(rawValue.Trim(), " ")
            .Replace("|", "\\|", StringComparison.Ordinal);

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex LineBreakRegex();
}
