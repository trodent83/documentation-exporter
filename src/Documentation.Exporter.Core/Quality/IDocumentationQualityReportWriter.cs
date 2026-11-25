using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Writes formatted <see cref="DocumentationQualityReport"/> outputs to disk using DI-registered <see cref="IQualityReportFormatter"/> strategies.
/// </summary>
public interface IDocumentationQualityReportWriter
{
    /// <summary>
    /// Formats and writes <paramref name="report"/> to <paramref name="reportFilePath"/> using <paramref name="format"/>.
    /// </summary>
    /// <param name="report">The documentation quality report snapshot to write.</param>
    /// <param name="reportFilePath">The destination output file path.</param>
    /// <param name="format">The requested output serialization format.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task WriteReportAsync(
        DocumentationQualityReport report,
        string reportFilePath,
        QualityReportFormat format,
        CancellationToken cancellationToken = default);
}
