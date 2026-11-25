using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class ExporterOptionsTests
{
    [Fact]
    public void ExporterOptionsWhenPropertiesProvidedShouldPassValidation()
    {
        // Arrange
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "Assembly.dll"),
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "Assembly.xml"),
            OutputDirectoryPath = Path.Combine(Path.GetTempPath(), "Output"),
            MicrosoftDocsBaseUrl = "https://learn.microsoft.com/en-us/dotnet/api/"
        };
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeTrue("because all required options were properly configured.");
        _ = validationResults.Should().BeEmpty();
    }

    [Fact]
    public void ExporterOptionsWhenRequiredPropertiesEmptyShouldFailValidation()
    {
        // Arrange
        var options = new ExporterOptions
        {
            AssemblyFilePath = string.Empty,
            XmlDocumentationFilePath = string.Empty,
            OutputDirectoryPath = string.Empty,
            MicrosoftDocsBaseUrl = string.Empty
        };
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because required properties are empty strings.");
        _ = validationResults.Should().NotBeEmpty();
        _ = validationResults.Should().Contain(result =>
            result.MemberNames.Contains("AssemblyFilePath") &&
            result.ErrorMessage == ValidationMessagesResources.AssemblyFilePathRequired);
        _ = validationResults.Should().Contain(result =>
            result.MemberNames.Contains("XmlDocumentationFilePath") &&
            result.ErrorMessage == ValidationMessagesResources.XmlDocumentationFilePathRequired);
        _ = validationResults.Should().Contain(result =>
            result.MemberNames.Contains("OutputDirectoryPath") &&
            result.ErrorMessage == ValidationMessagesResources.OutputDirectoryPathRequired);
        _ = validationResults.Should().Contain(result =>
            result.MemberNames.Contains("MicrosoftDocsBaseUrl") &&
            result.ErrorMessage == ValidationMessagesResources.MicrosoftDocsBaseUrlRequired);
    }

    [Fact]
    public void ExporterOptionsWhenThresholdOutOfRangeOrDiagramFormatInvalidShouldEmitLocalizedErrors()
    {
        // Arrange
        var options = new ExporterOptions
        {
            AssemblyFilePath = "Sample.dll",
            XmlDocumentationFilePath = "Sample.xml",
            OutputDirectoryPath = "Output",
            FailOnUndocumentedThreshold = 150.0,
            DiagramFormat = default
        };
        ValidationContext validationContext = new(options);
        List<ValidationResult> validationResults = [];

        // Act
        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because FailOnUndocumentedThreshold is outside [0, 100] and DiagramFormat is undefined.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.FailOnUndocumentedThresholdRange);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.DiagramFormatInvalid);
    }

    [Fact]
    public void ExternalDocLinkRuleWhenRequiredPropertiesEmptyShouldEmitLocalizedErrors()
    {
        // Arrange
        var invalidRule = new ExternalDocLinkRule
        {
            NamespacePrefix = string.Empty,
            UrlTemplate = string.Empty
        };
        ValidationContext validationContext = new(invalidRule);
        List<ValidationResult> validationResults = [];

        // Act
        var isValid = Validator.TryValidateObject(invalidRule, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because NamespacePrefix and UrlTemplate are required.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.NamespacePrefixRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.UrlTemplateRequired);
    }
}
