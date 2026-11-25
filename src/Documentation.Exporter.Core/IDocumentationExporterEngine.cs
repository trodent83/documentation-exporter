using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core;

/// <summary>
/// Service interface for orchestrating the documentation export process.
/// </summary>
public interface IDocumentationExporterEngine
{
    /// <summary>
    /// Executes the documentation export process asynchronously.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An <see cref="ExportResult"/> detailing success or failure.</returns>
    Task<ExportResult> ExportDocumentationAsync(ExporterOptions options, CancellationToken cancellationToken = default);
}
