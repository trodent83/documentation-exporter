using Documentation.Exporter.Abstractions.Quality;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Formats <see cref="DocumentationQualityReport"/> instances according to the OASIS SARIF v2.1.0 specification.
/// </summary>
public sealed class SarifQualityReportFormatter : IQualityReportFormatter
{
    private const string SarifSchemaUrl = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json";
    private const string SarifVersion = "2.1.0";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly SarifRuleDescriptor[] RuleCatalog =
    [
        new("DOC001", "MissingTypeSummary", "Accessible type is missing XML <summary> documentation.", "warning"),
        new("DOC002", "MissingMemberSummary", "Accessible member is missing XML <summary> documentation.", "warning"),
        new("DOC003", "MissingParameterDocumentation", "Parameter is missing XML <param> documentation.", "warning"),
        new("DOC004", "MissingTypeParameterDocumentation", "Generic type parameter is missing XML <typeparam> documentation.", "warning"),
        new("DOC005", "MissingReturnDocumentation", "Non-void method is missing XML <returns> documentation.", "warning"),
        new("DOC006", "BrokenMarkdownLink", "Relative Markdown link points to a missing file or directory.", "warning"),
        new("DOC007", "BrokenMarkdownAnchor", "Markdown heading anchor fragment points to a missing section heading.", "warning")
    ];

    /// <inheritdoc />
    public QualityReportFormat Format => QualityReportFormat.Sarif;

    /// <inheritdoc />
    public string FormatReport(DocumentationQualityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var toolVersion = typeof(SarifQualityReportFormatter).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        SarifDocument document = new(
            SarifSchemaUrl,
            SarifVersion,
            [
                new SarifRun(
                    new SarifTool(
                        new SarifToolDriver(
                            "Documentation.Exporter",
                            toolVersion,
                            [.. RuleCatalog.Select(rule => new SarifReportingDescriptor(
                                rule.Id,
                                rule.Name,
                                new SarifMessage(rule.ShortDescription),
                                new SarifDefaultConfiguration(rule.DefaultLevel)))])),
                    [.. report.Diagnostics.Select(CreateSarifResult)])
            ]);

        return JsonSerializer.Serialize(document, SerializerOptions) + Environment.NewLine;
    }

    private static SarifResult CreateSarifResult(DocumentationDiagnostic diagnostic)
    {
        List<SarifLocation> locations = [];
        var hasFilePath = !string.IsNullOrWhiteSpace(diagnostic.FilePath);
        var hasSymbolName = !string.IsNullOrWhiteSpace(diagnostic.SymbolName);

        if (hasFilePath || hasSymbolName)
        {
            var physicalLocation = hasFilePath
                ? new SarifPhysicalLocation(
                    new SarifArtifactLocation(diagnostic.FilePath!.Replace('\\', '/')),
                    diagnostic.LineNumber is > 0 ? new SarifRegion(diagnostic.LineNumber.Value) : null)
                : null;

            List<SarifLogicalLocation>? logicalLocations = hasSymbolName
                ? [new SarifLogicalLocation(diagnostic.SymbolName!, diagnostic.SymbolName!, "member")]
                : null;

            locations.Add(new SarifLocation(physicalLocation, logicalLocations));
        }

        var ruleIndex = Array.FindIndex(RuleCatalog, rule => string.Equals(rule.Id, diagnostic.Code, StringComparison.Ordinal));
        var fingerprintInput = $"{diagnostic.Code}|{diagnostic.SymbolName ?? diagnostic.FilePath}|{diagnostic.TargetReference}";
        var fingerprintHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintInput)))[..16];

        return new SarifResult(
            diagnostic.Code,
            ruleIndex >= 0 ? ruleIndex : null,
            MapSeverityToSarifLevel(diagnostic.Severity),
            new SarifMessage(diagnostic.Message),
            locations.Count > 0 ? locations : null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["primaryLocationLineHash"] = fingerprintHash
            });
    }

    private static string MapSeverityToSarifLevel(DocumentationDiagnosticSeverity severity) =>
        severity switch
        {
            DocumentationDiagnosticSeverity.Error => "error",
            DocumentationDiagnosticSeverity.Warning => "warning",
            DocumentationDiagnosticSeverity.Info => "note",
            _ => "note"
        };

    private sealed record SarifRuleDescriptor(string Id, string Name, string ShortDescription, string DefaultLevel);

    private sealed record SarifDocument(
        [property: JsonPropertyName("$schema")] string Schema,
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("runs")] IReadOnlyList<SarifRun> Runs);

    private sealed record SarifRun(
        [property: JsonPropertyName("tool")] SarifTool Tool,
        [property: JsonPropertyName("results")] IReadOnlyList<SarifResult> Results);

    private sealed record SarifTool([property: JsonPropertyName("driver")] SarifToolDriver Driver);

    private sealed record SarifToolDriver(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("semanticVersion")] string SemanticVersion,
        [property: JsonPropertyName("rules")] IReadOnlyList<SarifReportingDescriptor> Rules);

    private sealed record SarifReportingDescriptor(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("shortDescription")] SarifMessage ShortDescription,
        [property: JsonPropertyName("defaultConfiguration")] SarifDefaultConfiguration DefaultConfiguration);

    private sealed record SarifDefaultConfiguration([property: JsonPropertyName("level")] string Level);

    private sealed record SarifMessage([property: JsonPropertyName("text")] string Text);

    private sealed record SarifResult(
        [property: JsonPropertyName("ruleId")] string RuleId,
        [property: JsonPropertyName("ruleIndex")] int? RuleIndex,
        [property: JsonPropertyName("level")] string Level,
        [property: JsonPropertyName("message")] SarifMessage Message,
        [property: JsonPropertyName("locations")] IReadOnlyList<SarifLocation>? Locations,
        [property: JsonPropertyName("partialFingerprints")] IReadOnlyDictionary<string, string>? PartialFingerprints);

    private sealed record SarifLocation(
        [property: JsonPropertyName("physicalLocation")] SarifPhysicalLocation? PhysicalLocation,
        [property: JsonPropertyName("logicalLocations")] IReadOnlyList<SarifLogicalLocation>? LogicalLocations);

    private sealed record SarifPhysicalLocation(
        [property: JsonPropertyName("artifactLocation")] SarifArtifactLocation ArtifactLocation,
        [property: JsonPropertyName("region")] SarifRegion? Region);

    private sealed record SarifArtifactLocation([property: JsonPropertyName("uri")] string Uri);

    private sealed record SarifRegion([property: JsonPropertyName("startLine")] int StartLine);

    private sealed record SarifLogicalLocation(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("fullyQualifiedName")] string FullyQualifiedName,
        [property: JsonPropertyName("kind")] string Kind);
}
