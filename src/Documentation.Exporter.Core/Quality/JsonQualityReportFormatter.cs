using Documentation.Exporter.Abstractions.Quality;
using System.Text.Json;
using System.Text.Json.Serialization;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Formats <see cref="DocumentationQualityReport"/> instances as indented camelCase JSON.
/// </summary>
public sealed class JsonQualityReportFormatter : IQualityReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public QualityReportFormat Format => QualityReportFormat.Json;

    /// <inheritdoc />
    public string FormatReport(DocumentationQualityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine;
    }
}
