using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow;
using FluentAssertions;
using Moq;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Workflow;

[UnitTest]
public sealed class ExportWorkflowTests
{
    private sealed class StubStep(bool shouldSucceed, string stepName, Action<ExportWorkflowContext>? onExecute = null) : IExportWorkflowStep
    {
        public string StepName => stepName;

        public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
        {
            onExecute?.Invoke(context);
            if (shouldSucceed)
            {
                context.GeneratedFilesCount++;
            }

            return Task.FromResult(shouldSucceed);
        }
    }

    [Fact]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var workflow = new ExportWorkflow([]);

        // Act
        var nullContextAction = async () => await workflow.ExecuteAsync(null!);

        // Assert
        _ = FluentActions.Invoking(() => new ExportWorkflow(null!))
            .Should().Throw<ArgumentNullException>("because workflow steps collection cannot be null.");
        _ = await nullContextAction.Should().ThrowAsync<ArgumentNullException>("because context cannot be null.");
    }

    [Fact]
    public async Task ExecuteAsyncWhenAllStepsSucceedShouldReturnSuccessResultAndAttachQualityReport()
    {
        // Arrange
        var firstStep = new StubStep(shouldSucceed: true, "StepOne");
        var secondStep = new StubStep(shouldSucceed: true, "StepTwo");
        var workflow = new ExportWorkflow([firstStep, secondStep]);
        var options = new ExporterOptions();
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await workflow.ExecuteAsync(context);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.QualityReport.Should().NotBeNull("because every completed export attaches a quality report snapshot.");
        _ = result.QualityReport!.GeneratedFilesCount.Should().Be(2);
        _ = context.GeneratedFilesCount.Should().Be(2);
        _ = context.HasFailed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsyncWhenStepFailsShouldShortCircuitReturnFailureAndPersistConfiguredQualityReport()
    {
        // Arrange
        const string virtualReportPath = "virtual-out/quality.json";
        var fullVirtualReportPath = Path.GetFullPath(virtualReportPath);
        string? writtenReportPayload = null;

        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fileSystem => fileSystem.GetFullPath(virtualReportPath))
            .Returns(fullVirtualReportPath);
        _ = mockFileSystem.Setup(fileSystem => fileSystem.WriteAllTextAsync(fullVirtualReportPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, content, _) => writtenReportPayload = content)
            .Returns(Task.CompletedTask);

        var failingStep = new StubStep(
            shouldSucceed: false,
            "FailingStep",
            context =>
            {
                context.DocumentationCoveragePercentage = 42.5;
                context.Diagnostics.Add(new DocumentationDiagnostic
                {
                    Code = "DOC006",
                    Kind = DocumentationDiagnosticKind.BrokenMarkdownLink,
                    Severity = DocumentationDiagnosticSeverity.Error,
                    Message = "Broken link detected."
                });
            });
        var secondStep = new StubStep(shouldSucceed: true, "StepTwo");
        var reportWriter = new DocumentationQualityReportWriter([new JsonQualityReportFormatter()], mockFileSystem.Object);
        var workflow = new ExportWorkflow([failingStep, secondStep], qualityReportWriter: reportWriter);
        var options = new ExporterOptions
        {
            ReportFilePath = virtualReportPath,
            ReportFormat = QualityReportFormat.Json
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await workflow.ExecuteAsync(context);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.DocumentationCoveragePercentage.Should().Be(42.5);
        _ = result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "DOC006");
        _ = context.HasFailed.Should().BeTrue();
        _ = context.GeneratedFilesCount.Should().Be(0);
        _ = context.IsQualityReportWritten.Should().BeTrue("because failure diagnostics must still be flushed when ReportFilePath is set.");
        _ = writtenReportPayload.Should().NotBeNullOrWhiteSpace("because the report writer must serialize failure diagnostics via IFileSystem.")
            .And.Contain("DOC006");
        mockFileSystem.Verify(
            fileSystem => fileSystem.CreateDirectory(Path.GetDirectoryName(fullVirtualReportPath)!),
            Times.Once);
        mockFileSystem.Verify(
            fileSystem => fileSystem.WriteAllTextAsync(fullVirtualReportPath, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
