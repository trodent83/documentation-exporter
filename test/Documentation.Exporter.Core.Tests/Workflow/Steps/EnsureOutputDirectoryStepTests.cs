using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

[IntegrationTest]
public sealed class EnsureOutputDirectoryStepTests
{
    [Fact]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var step = new EnsureOutputDirectoryStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Assert
        _ = step.StepName.Should().Be("Ensure Output Directory");
    }

    [Fact]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var step = new EnsureOutputDirectoryStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsyncWhenDirectoryDoesNotExistShouldCreateDirectory()
    {
        // Arrange
        var step = new EnsureOutputDirectoryStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var tempDir = Path.Combine(Path.GetTempPath(), "EnsureDirTest_" + Guid.NewGuid().ToString("N"));
        var options = new ExporterOptions { OutputDirectoryPath = tempDir };
        var context = new ExportWorkflowContext(options);

        try
        {
            // Act
            var result = await step.ExecuteAsync(context);
            var existingResult = await step.ExecuteAsync(context);
            var emptyPathResult = await step.ExecuteAsync(new ExportWorkflowContext(new ExporterOptions { OutputDirectoryPath = "   " }));

            // Assert
            _ = result.Should().BeTrue();
            _ = existingResult.Should().BeTrue();
            _ = emptyPathResult.Should().BeFalse();
            _ = Directory.Exists(tempDir).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsyncWhenCreationFailsShouldReturnFalse()
    {
        // Arrange
        var step = new EnsureOutputDirectoryStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var options = new ExporterOptions
        {
            OutputDirectoryPath = "http://invalid*path/with?illegal<characters>"
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse("because directory creation with invalid characters should fail gracefully.");
    }

    [Fact]
    public async Task ExecuteAsyncWhenCleanOutputDirectoryIsTrueShouldDeleteExistingGeneratedFiles()
    {
        // Arrange
        var step = new EnsureOutputDirectoryStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var tempDirectory = Path.Combine(Path.GetTempPath(), "EnsureDirCleanTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            var staleMarkdownFile = Path.Combine(tempDirectory, "StalePage.md");
            var stalePlantUmlFile = Path.Combine(tempDirectory, "StaleDiagram.puml");
            var preservedNonGeneratedFile = Path.Combine(tempDirectory, "PreservedConfig.json");

            await File.WriteAllTextAsync(staleMarkdownFile, "# Stale Content");
            await File.WriteAllTextAsync(stalePlantUmlFile, "@startuml\n@enduml");
            await File.WriteAllTextAsync(preservedNonGeneratedFile, "{}");

            var options = new ExporterOptions
            {
                OutputDirectoryPath = tempDirectory,
                CleanOutputDirectory = true
            };
            var context = new ExportWorkflowContext(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue("because cleaning and ensuring output directory succeeded.");
            _ = File.Exists(staleMarkdownFile).Should().BeFalse("because .md files must be cleaned when CleanOutputDirectory is true.");
            _ = File.Exists(stalePlantUmlFile).Should().BeFalse("because .puml files must be cleaned when CleanOutputDirectory is true.");
            _ = File.Exists(preservedNonGeneratedFile).Should().BeTrue("because non-generated file extensions are preserved.");
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
    public async Task ExecuteAsyncWhenCleanOutputDirectoryIsTrueAndManifestExistsShouldOnlyDeleteManifestTrackedFilesAndPreserveHandWrittenMarkdown()
    {
        // Arrange
        var step = new EnsureOutputDirectoryStep(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var tempDirectory = Path.Combine(Path.GetTempPath(), "EnsureDirManifestCleanTest_" + Guid.NewGuid().ToString("N"));
        var generatedTypeDir = Path.Combine(tempDirectory, "GeneratedType");
        _ = Directory.CreateDirectory(generatedTypeDir);

        try
        {
            var generatedIndexFile = Path.Combine(generatedTypeDir, "index.md");
            var handWrittenGuideFile = Path.Combine(tempDirectory, "HandWrittenArchitecture.md");
            var manifestFile = Path.Combine(tempDirectory, EnsureOutputDirectoryStep.GeneratedFilesManifestFileName);

            await File.WriteAllTextAsync(generatedIndexFile, "# Generated Type");
            await File.WriteAllTextAsync(handWrittenGuideFile, "# Hand-Authored Portal Guide");
            await File.WriteAllTextAsync(manifestFile, "[\n  \"GeneratedType/index.md\",\n  \"../OutsideAttempt.md\"\n]" + Environment.NewLine);

            var options = new ExporterOptions
            {
                OutputDirectoryPath = tempDirectory,
                CleanOutputDirectory = true
            };
            var context = new ExportWorkflowContext(options);

            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue("because manifest-based cleaning must succeed.");
            _ = File.Exists(generatedIndexFile).Should().BeFalse("because files listed in .docexporter-manifest.json must be deleted.");
            _ = Directory.Exists(generatedTypeDir).Should().BeFalse("because emptied generated subdirectories must be cleaned up.");
            _ = File.Exists(handWrittenGuideFile).Should().BeTrue("because hand-written Markdown files not in the manifest must be preserved.");
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
