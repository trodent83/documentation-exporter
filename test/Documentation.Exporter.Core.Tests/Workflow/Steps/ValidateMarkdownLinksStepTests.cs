using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

/// <summary>
/// Unit tests for <see cref="ValidateMarkdownLinksStep"/>.
/// </summary>
[IntegrationTest]
public sealed class ValidateMarkdownLinksStepTests
{
    [Fact]
    public void StepNameShouldMatchExpectedResourceString()
    {
        // Arrange
        var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act & Assert
        _ = step.StepName.Should().Be("Validate Markdown Links", "because the step name must match localized execution message resources.");
    }

    [Fact]
    public async Task ExecuteAsyncWithNullContextShouldThrowArgumentNullException()
    {
        // Arrange
        var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var nullAction = () => step.ExecuteAsync(null!);

        // Assert
        _ = await nullAction.Should().ThrowAsync<ArgumentNullException>("because context cannot be null.");
    }

    [Fact]
    public async Task ExecuteAsyncWhenOutputDirectoryDoesNotExistShouldReturnTrue()
    {
        // Arrange
        var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "DocExporter_NoDir_" + Guid.NewGuid());
        ExporterOptions options = new() { OutputDirectoryPath = nonExistentDir };
        ExportWorkflowContext context = new(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because a non-existent directory means no emitted files to validate.");
    }

    [Fact]
    public async Task ExecuteAsyncWithValidRelativeAndExternalLinksShouldPassValidation()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_ValidLinks_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            var targetFile = Path.Combine(tempDirectory, "Target.md");
            await File.WriteAllTextAsync(targetFile, "# Target Page Content");

            var indexFile = Path.Combine(tempDirectory, "index.md");
            var markdownWithValidLinks = """
                # Overview
                Link to [Target](Target.md)
                Link to [Section](#overview)
                External link to [Microsoft](https://learn.microsoft.com)
                Mailto link to [Support](mailto:support@example.com)
                """;
            await File.WriteAllTextAsync(indexFile, markdownWithValidLinks);

            var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
            ExporterOptions options = new()
            {
                OutputDirectoryPath = tempDirectory,
                StrictLinkValidation = true
            };
            ExportWorkflowContext context = new(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue("because all internal relative links point to existing files and external links are ignored.");
            _ = context.HasFailed.Should().BeFalse();
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
    public async Task ExecuteAsyncWithBrokenLinkInNonStrictModeShouldReturnTrue()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_BrokenNonStrict_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            var indexFile = Path.Combine(tempDirectory, "index.md");
            var markdownWithBrokenLink = "# Overview\nBroken [Missing Page](NonExistentPage.md)";
            await File.WriteAllTextAsync(indexFile, markdownWithBrokenLink);

            var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
            ExporterOptions options = new()
            {
                OutputDirectoryPath = tempDirectory,
                StrictLinkValidation = false
            };
            ExportWorkflowContext context = new(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue("because non-strict mode only logs broken links as warnings without failing the workflow.");
            _ = context.HasFailed.Should().BeFalse();
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
    public async Task ExecuteAsyncWithBrokenLinkInStrictModeShouldReturnFalseAndSetHasFailed()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_BrokenStrict_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            var indexFile = Path.Combine(tempDirectory, "index.md");
            var markdownWithBrokenLink = "# Overview\nBroken [Missing Page](NonExistentPage.md)";
            await File.WriteAllTextAsync(indexFile, markdownWithBrokenLink);

            var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
            ExporterOptions options = new()
            {
                OutputDirectoryPath = tempDirectory,
                StrictLinkValidation = true
            };
            ExportWorkflowContext context = new(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeFalse("because strict mode fails execution when broken relative links exist.");
            _ = context.HasFailed.Should().BeTrue("because failing validation must mark the context as failed.");
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
    public async Task ExecuteAsyncWithCanceledTokenShouldThrowOperationCanceledException()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_CancelledLinks_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDirectory, "index.md"), "# Index\n[Other](other.md)");
            var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
            ExporterOptions options = new() { OutputDirectoryPath = tempDirectory };
            ExportWorkflowContext context = new(options);
            using CancellationTokenSource cancellationTokenSource = new();
            await cancellationTokenSource.CancelAsync();

            // Act
            var executeAction = async () => await step.ExecuteAsync(context, cancellationTokenSource.Token);

            // Assert
            _ = await executeAction.Should().ThrowAsync<OperationCanceledException>(
                "because cancelled execution tokens must halt markdown link validation immediately.");
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
    public async Task ExecuteAsyncShouldIgnoreLinksInsideCodeBlocksAndInlineSpansWhileValidatingHeadingAnchors()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_AnchorCode_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "Target.md"),
                """
                # Overview
                # Overview
                ## Namespace: `Demo.Models`
                Details here.
                """);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "index.md"),
                """
                # Home
                See [Namespace Section](Target.md#namespace-demomodels), [Dup](#home), [Dup2](Target.md#overview-1), [Query](Target.md?v=1#overview), and [EmptyHash](#).
                Double-backtick inline example `` `[Fake Inline Link](MissingInline.md)` `` and `single` trailing `unclosed backtick are ignored.

                ```markdown
                [Fake Fenced Backtick Link](MissingFenced.md)
                ```

                ~~~markdown
                [Fake Tilde Fenced Link](MissingTilde.md)
                ~~~
                """);

            var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
            ExporterOptions options = new()
            {
                OutputDirectoryPath = tempDirectory,
                StrictLinkValidation = true
            };
            ExportWorkflowContext context = new(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue("because real file, query-stripped, and duplicate heading anchor links resolve cleanly while links inside code fences or inline code are ignored.");
            _ = context.VerifiedLinksCount.Should().Be(4);
            _ = context.Diagnostics.Should().BeEmpty();
            _ = context.HasFailed.Should().BeFalse();
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
    public async Task ExecuteAsyncWithBrokenAnchorInStrictModeShouldFailAndRecordExactLineNumberAndDiagnostic()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_BrokenAnchor_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDirectory, "Target.md"), "# Real Heading\nBody");
            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "index.md"),
                "# Home\nLine two ok.\nBroken [Section](Target.md#missing-section)\nBroken [Self](#missing-self)");

            var step = new ValidateMarkdownLinksStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
            ExporterOptions options = new()
            {
                OutputDirectoryPath = tempDirectory,
                StrictLinkValidation = true
            };
            ExportWorkflowContext context = new(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeFalse("because strict mode fails when target heading anchors do not exist.");
            _ = context.HasFailed.Should().BeTrue();
            _ = context.Diagnostics.Should().HaveCount(2);
            _ = context.Diagnostics.Should().Contain(diagnostic =>
                diagnostic.Code == "DOC007" &&
                diagnostic.Kind == DocumentationDiagnosticKind.BrokenMarkdownAnchor &&
                diagnostic.Severity == DocumentationDiagnosticSeverity.Error &&
                diagnostic.FilePath == "index.md" &&
                diagnostic.LineNumber == 3 &&
                diagnostic.TargetReference == "Target.md#missing-section");
            _ = context.Diagnostics.Should().Contain(diagnostic =>
                diagnostic.Code == "DOC007" &&
                diagnostic.LineNumber == 4 &&
                diagnostic.TargetReference == "#missing-self");
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
