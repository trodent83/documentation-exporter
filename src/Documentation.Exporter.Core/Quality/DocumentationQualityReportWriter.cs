using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Resources;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Default implementation of <see cref="IDocumentationQualityReportWriter"/> selecting a matching
/// <see cref="IQualityReportFormatter"/> from DI and persisting the report via <see cref="IFileSystem"/>.
/// </summary>
/// <param name="reportFormatters">The registered report formatters.</param>
/// <param name="fileSystem">The file system abstraction.</param>
public sealed class DocumentationQualityReportWriter(
    IEnumerable<IQualityReportFormatter> reportFormatters,
    IFileSystem fileSystem) : IDocumentationQualityReportWriter
{
    private readonly Dictionary<QualityReportFormat, IQualityReportFormatter> formattersByFormat =
        ArgumentGuard.EnsureNotNull(reportFormatters)
        .GroupBy(formatter => formatter.Format)
        .ToDictionary(group => group.Key, group => group.Last());

    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);

    /// <inheritdoc />
    public async Task WriteReportAsync(
        DocumentationQualityReport report,
        string reportFilePath,
        QualityReportFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportFilePath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!formattersByFormat.TryGetValue(format, out var formatter))
        {
            throw new NotSupportedException(ValidationMessagesResources.ReportFormatInvalid);
        }

        var fullReportPath = this.fileSystem.GetFullPath(reportFilePath);
        var parentDirectory = Path.GetDirectoryName(fullReportPath);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            this.fileSystem.CreateDirectory(parentDirectory);
        }

        var formattedContent = formatter.FormatReport(report);
        await this.fileSystem.WriteAllTextAsync(fullReportPath, formattedContent, cancellationToken).ConfigureAwait(false);
    }
}
