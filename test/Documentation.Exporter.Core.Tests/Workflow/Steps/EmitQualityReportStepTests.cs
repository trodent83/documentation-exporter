using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

public sealed class EmitQualityReportStepTests
{
    private sealed class ThrowingReportWriter : IDocumentationQualityReportWriter
    {
        public Task WriteReportAsync(
            DocumentationQualityReport report,
            string reportFilePath,
            QualityReportFormat format,
            CancellationToken cancellationToken = default) =>
            throw new IOException("Simulated disk write failure.");
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncWhenReportPathNotConfiguredShouldPopulateSnapshotWithoutWritingFile()
    {
        // Arrange
        var writer = new DocumentationQualityReportWriter([new JsonQualityReportFormatter()], new PhysicalFileSystem());
        var step = new EmitQualityReportStep(writer);
        var context = new ExportWorkflowContext(new ExporterOptions())
        {
            GeneratedFilesCount = 5,
            DocumentationCoveragePercentage = 90.0
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = FluentActions.Invoking(() => new EmitQualityReportStep(null!))
            .Should().Throw<ArgumentNullException>("because reportWriter cannot be null.");
        _ = await FluentActions.Awaiting(() => step.ExecuteAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>("because context cannot be null.");
        _ = step.StepName.Should().Be("Emit Documentation Quality Report");
        _ = result.Should().BeTrue();
        _ = context.QualityReport.Should().NotBeNull();
        _ = context.QualityReport!.GeneratedFilesCount.Should().Be(5);
        _ = context.IsQualityReportWritten.Should().BeFalse();
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenReportPathConfiguredShouldWriteReportToDisk()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_EmitReport_{Guid.NewGuid():N}");
        var reportPath = Path.Combine(tempDirectory, "report.sarif");
        var writer = new DocumentationQualityReportWriter([new SarifQualityReportFormatter()], new PhysicalFileSystem());
        var step = new EmitQualityReportStep(writer);
        var context = new ExportWorkflowContext(new ExporterOptions
        {
            ReportFilePath = reportPath,
            ReportFormat = QualityReportFormat.Sarif
        });
        context.Diagnostics.Add(new DocumentationDiagnostic
        {
            Code = "DOC001",
            Kind = DocumentationDiagnosticKind.MissingTypeSummary,
            Severity = DocumentationDiagnosticSeverity.Warning,
            SymbolName = "Demo.MissingType",
            Message = "Missing summary."
        });

        try
        {
            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue();
            _ = context.IsQualityReportWritten.Should().BeTrue();
            _ = File.Exists(reportPath).Should().BeTrue();
            var sarifContent = await File.ReadAllTextAsync(reportPath);
            _ = sarifContent.Should().Contain("2.1.0").And.Contain("DOC001");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncWhenWriterThrowsIOExceptionShouldMarkFailedAndReturnFalse()
    {
        // Arrange
        var step = new EmitQualityReportStep(new ThrowingReportWriter());
        var context = new ExportWorkflowContext(new ExporterOptions
        {
            ReportFilePath = "invalid/report.json",
            ReportFormat = QualityReportFormat.Json
        });

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse();
        _ = context.HasFailed.Should().BeTrue();
        _ = context.IsQualityReportWritten.Should().BeFalse();
    }
}
