using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Quality;

/// <summary>
/// Strategy interface for formatting a <see cref="DocumentationQualityReport"/> into a target output representation.
/// </summary>
public interface IQualityReportFormatter
{
    /// <summary>
    /// Gets the report format handled by this formatter.
    /// </summary>
    QualityReportFormat Format { get; }

    /// <summary>
    /// Formats <paramref name="report"/> into a serialized string representation.
    /// </summary>
    /// <param name="report">The aggregated documentation quality report.</param>
    /// <returns>Formatted report content ready to write to disk or stdout.</returns>
    string FormatReport(DocumentationQualityReport report);
}
