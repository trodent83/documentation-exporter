using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

[UnitTest]
public sealed class ValidateOptionsStepTests
{
    [Fact]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var step = new ValidateOptionsStep();

        // Assert
        _ = step.StepName.Should().Be("Validate Exporter Options");
    }

    [Fact]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var step = new ValidateOptionsStep();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsyncWhenOptionsValidShouldReturnTrue()
    {
        // Arrange
        var step = new ValidateOptionsStep();
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.dll"),
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.xml"),
            OutputDirectoryPath = Path.Combine(Path.GetTempPath(), "Path", "Output")
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsyncWhenOptionsInvalidShouldReturnFalse()
    {
        // Arrange
        var step = new ValidateOptionsStep();
        var options = new ExporterOptions(); // Missing required properties
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsyncWithInvalidExternalDocLinkRulesThresholdOrDiagramFormatShouldReturnFalse()
    {
        // Arrange
        var step = new ValidateOptionsStep();
        ExporterOptions invalidRuleOptions = new()
        {
            AssemblyFilePath = "Sample.dll",
            XmlDocumentationFilePath = "Sample.xml",
            OutputDirectoryPath = "Output",
            ExternalDocLinkRules =
            [
                new ExternalDocLinkRule { NamespacePrefix = string.Empty, UrlTemplate = string.Empty },
                null!
            ]
        };
        ExporterOptions invalidRangeOptions = new()
        {
            AssemblyFilePath = "Sample.dll",
            XmlDocumentationFilePath = "Sample.xml",
            OutputDirectoryPath = "Output",
            FailOnUndocumentedThreshold = -1.0,
            DiagramFormat = default
        };
        ExporterOptions validRuleOptions = new()
        {
            AssemblyFilePath = "Sample.dll",
            XmlDocumentationFilePath = "Sample.xml",
            OutputDirectoryPath = "Output",
            ExternalDocLinkRules =
            [
                new ExternalDocLinkRule { NamespacePrefix = "Serilog*", UrlTemplate = "https://serilog.net/{name}" }
            ]
        };

        // Act
        var invalidRuleResult = await step.ExecuteAsync(new ExportWorkflowContext(invalidRuleOptions));
        var invalidRangeResult = await step.ExecuteAsync(new ExportWorkflowContext(invalidRangeOptions));
        var validRuleResult = await step.ExecuteAsync(new ExportWorkflowContext(validRuleOptions));

        // Assert
        _ = invalidRuleResult.Should().BeFalse("because ExternalDocLinkRules contains null and blank entries.");
        _ = invalidRangeResult.Should().BeFalse("because FailOnUndocumentedThreshold is negative and DiagramFormat is invalid.");
        _ = validRuleResult.Should().BeTrue("because all options and ExternalDocLinkRules entries are valid.");

        ExporterOptions invalidPlaceholderOptions = new()
        {
            AssemblyFilePath = "Sample.dll",
            XmlDocumentationFilePath = "Sample.xml",
            OutputDirectoryPath = "Output",
            ExternalDocLinkRules =
            [
                new ExternalDocLinkRule { NamespacePrefix = "Serilog", UrlTemplate = "https://serilog.net/{unknownPlaceholder}" }
            ]
        };
        var invalidPlaceholderResult = await step.ExecuteAsync(new ExportWorkflowContext(invalidPlaceholderOptions));
        _ = invalidPlaceholderResult.Should().BeFalse("because ExternalDocLinkRules contains an unknown URL placeholder.");
    }
}
