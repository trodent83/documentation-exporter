using System.Text.Json;
using Documentation.Exporter.Core.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Quality;

public sealed class QualityReportFormattersAndWriterTests
{
    private static DocumentationQualityReport BuildSampleReport(bool includeDiagnostics = true) =>
        new()
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero),
            DocumentationCoveragePercentage = includeDiagnostics ? 85.5 : 100.0,
            TotalDocumentableSymbols = 20,
            DocumentedSymbols = includeDiagnostics ? 17 : 20,
            GeneratedFilesCount = 42,
            VerifiedLinksCount = 128,
            ErrorCount = includeDiagnostics ? 1 : 0,
            WarningCount = includeDiagnostics ? 1 : 0,
            InfoCount = 0,
            Diagnostics = includeDiagnostics
                ?
                [
                    new DocumentationDiagnostic
                    {
                        Code = "DOC006",
                        Kind = DocumentationDiagnosticKind.BrokenMarkdownLink,
                        Severity = DocumentationDiagnosticSeverity.Error,
                        FilePath = "Services/OrderService/index.md",
                        LineNumber = 14,
                        TargetReference = "Missing.md",
                        Message = "Broken relative link."
                    },
                    new DocumentationDiagnostic
                    {
                        Code = "DOC001",
                        Kind = DocumentationDiagnosticKind.MissingTypeSummary,
                        Severity = DocumentationDiagnosticSeverity.Warning,
                        SymbolName = "Demo.Services.LegacyAdapter",
                        Message = "Type missing <summary>."
                    },
                    new DocumentationDiagnostic
                    {
                        Code = "DOC007",
                        Kind = DocumentationDiagnosticKind.BrokenMarkdownAnchor,
                        Severity = DocumentationDiagnosticSeverity.Info,
                        FilePath = "Services/OrderService/Notes.md",
                        Message = "Informational anchor notice without line number."
                    },
                    new DocumentationDiagnostic
                    {
                        Code = "DOC002",
                        Kind = DocumentationDiagnosticKind.MissingMemberSummary,
                        Severity = (DocumentationDiagnosticSeverity)999,
                        Message = "Diagnostic without file or symbol location."
                    }
                ]
                : []
        };

    [Fact]
    [UnitTest]
    public void JsonFormatterShouldProduceValidIndentedCamelCaseJson()
    {
        // Arrange
        var formatter = new JsonQualityReportFormatter();
        var report = BuildSampleReport();

        // Act
        var json = formatter.FormatReport(report);
        using var document = JsonDocument.Parse(json);

        // Assert
        _ = formatter.Format.Should().Be(QualityReportFormat.Json);
        _ = document.RootElement.GetProperty("documentationCoveragePercentage").GetDouble().Should().Be(85.5);
        _ = document.RootElement.GetProperty("errorCount").GetInt32().Should().Be(1);
        _ = document.RootElement.GetProperty("diagnostics").GetArrayLength().Should().Be(4);
        _ = FluentActions.Invoking(() => formatter.FormatReport(null!))
            .Should().Throw<ArgumentNullException>("because report cannot be null.");
    }

    [Fact]
    [UnitTest]
    public void MarkdownFormatterShouldRenderMetricsAndDiagnosticsTablesAndEmptyState()
    {
        // Arrange
        var formatter = new MarkdownQualityReportFormatter();

        // Act
        var markdownWithDiagnostics = formatter.FormatReport(BuildSampleReport(includeDiagnostics: true));
        var markdownClean = formatter.FormatReport(BuildSampleReport(includeDiagnostics: false));

        // Assert
        _ = formatter.Format.Should().Be(QualityReportFormat.Markdown);
        _ = markdownWithDiagnostics.Should().Contain("# Documentation Quality Report")
            .And.Contain("85.50% (17/20)")
            .And.Contain("| `DOC006` | Error | `Services/OrderService/index.md` | 14 | Missing.md | Broken relative link. |")
            .And.Contain("| `DOC001` | Warning | `Demo.Services.LegacyAdapter` | - | - | Type missing <summary>. |")
            .And.Contain("| `DOC002` | 999 | `-` | - | - | Diagnostic without file or symbol location. |");
        _ = markdownClean.Should().Contain("No documentation quality or link validation diagnostics recorded.");
        _ = FluentActions.Invoking(() => formatter.FormatReport(null!))
            .Should().Throw<ArgumentNullException>("because report cannot be null.");
    }

    [Fact]
    [UnitTest]
    public void SarifFormatterShouldProduceValidSarifTwoOneSchemaWithPhysicalAndLogicalLocations()
    {
        // Arrange
        var formatter = new SarifQualityReportFormatter();
        var report = BuildSampleReport();

        // Act
        var sarif = formatter.FormatReport(report);
        using var document = JsonDocument.Parse(sarif);

        // Assert
        _ = formatter.Format.Should().Be(QualityReportFormat.Sarif);
        _ = document.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");
        var firstRun = document.RootElement.GetProperty("runs")[0];
        _ = firstRun.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString().Should().Be("Documentation.Exporter");
        _ = firstRun.GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength().Should().Be(7);

        var results = firstRun.GetProperty("results");
        _ = results.GetArrayLength().Should().Be(4);
        _ = results[0].GetProperty("ruleId").GetString().Should().Be("DOC006");
        _ = results[0].GetProperty("ruleIndex").GetInt32().Should().Be(5);
        _ = results[0].GetProperty("partialFingerprints").GetProperty("primaryLocationLineHash").GetString().Should().NotBeNullOrWhiteSpace();
        _ = results[0].GetProperty("level").GetString().Should().Be("error");
        _ = results[0].GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString()
            .Should().Be("Services/OrderService/index.md");
        _ = results[0].GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("region").GetProperty("startLine").GetInt32()
            .Should().Be(14);
        _ = results[1].GetProperty("locations")[0].GetProperty("logicalLocations")[0].GetProperty("fullyQualifiedName").GetString()
            .Should().Be("Demo.Services.LegacyAdapter");
        _ = results[2].GetProperty("level").GetString().Should().Be("note");
        _ = results[2].GetProperty("locations")[0].GetProperty("physicalLocation").TryGetProperty("region", out _).Should().BeFalse();
        _ = results[3].TryGetProperty("locations", out _).Should().BeFalse();
        _ = FluentActions.Invoking(() => formatter.FormatReport(null!))
            .Should().Throw<ArgumentNullException>("because report cannot be null.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ReportWriterShouldWriteReportToDiskAndValidateArguments()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_ReportWriter_{Guid.NewGuid():N}");
        var reportPath = Path.Combine(tempDirectory, "nested", "report.md");
        var writer = new DocumentationQualityReportWriter(
            [new JsonQualityReportFormatter(), new MarkdownQualityReportFormatter()],
            new PhysicalFileSystem());
        var report = BuildSampleReport();

        try
        {
            // Act & Assert
            _ = FluentActions.Invoking(() => new DocumentationQualityReportWriter(null!, new PhysicalFileSystem()))
                .Should().Throw<ArgumentNullException>("because reportFormatters cannot be null.");
            _ = FluentActions.Invoking(() => new DocumentationQualityReportWriter([], null!))
                .Should().Throw<ArgumentNullException>("because fileSystem cannot be null.");
            _ = await FluentActions.Awaiting(() => writer.WriteReportAsync(null!, reportPath, QualityReportFormat.Markdown))
                .Should().ThrowAsync<ArgumentNullException>("because report cannot be null.");
            _ = await FluentActions.Awaiting(() => writer.WriteReportAsync(report, "   ", QualityReportFormat.Markdown))
                .Should().ThrowAsync<ArgumentException>("because reportFilePath cannot be whitespace.");
            _ = await FluentActions.Awaiting(() => writer.WriteReportAsync(report, reportPath, QualityReportFormat.Sarif))
                .Should().ThrowAsync<NotSupportedException>("because Sarif formatter was not registered in this writer instance.");

            await writer.WriteReportAsync(report, reportPath, QualityReportFormat.Markdown);

            _ = File.Exists(reportPath).Should().BeTrue("because the report writer must create parent directories and write the report file.");
            var content = await File.ReadAllTextAsync(reportPath);
            _ = content.Should().Contain("# Documentation Quality Report");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
