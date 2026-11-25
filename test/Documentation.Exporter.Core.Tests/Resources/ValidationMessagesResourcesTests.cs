using System.Globalization;
using Documentation.Exporter.Abstractions.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resources;

[UnitTest]
public sealed class ValidationMessagesResourcesTests
{
    [Fact]
    public void ResourceManagerShouldNotBeNull() =>
        _ = ValidationMessagesResources.ResourceManager.Should().NotBeNull("because the embedded ValidationMessagesResources manifest must be accessible.");

    [Fact]
    public void ResourceCultureWhenSetAndResetShouldPersistProperly()
    {
        try
        {
            // Arrange
            var targetCulture = new CultureInfo("en-US");

            // Act
            ValidationMessagesResources.ResourceCulture = targetCulture;

            // Assert
            _ = ValidationMessagesResources.ResourceCulture.Should().Be(targetCulture, "because ResourceCulture should store the assigned CultureInfo.");
            _ = ValidationMessagesResources.AssemblyFilePathRequired.Should().Be("AssemblyFilePath is required.");
        }
        finally
        {
            ValidationMessagesResources.ResourceCulture = null;
        }
    }

    [Fact]
    public void AllValidationMessageResourcePropertiesShouldReturnExpectedNeutralStrings()
    {
        // Act & Assert
        _ = ValidationMessagesResources.AssemblyFilePathRequired.Should().Be("AssemblyFilePath is required.");
        _ = ValidationMessagesResources.AssemblyFilePathMinLength.Should().Be("AssemblyFilePath cannot be empty.");
        _ = ValidationMessagesResources.XmlDocumentationFilePathRequired.Should().Be("XmlDocumentationFilePath is required.");
        _ = ValidationMessagesResources.XmlDocumentationFilePathMinLength.Should().Be("XmlDocumentationFilePath cannot be empty.");
        _ = ValidationMessagesResources.OutputDirectoryPathRequired.Should().Be("OutputDirectoryPath is required.");
        _ = ValidationMessagesResources.OutputDirectoryPathMinLength.Should().Be("OutputDirectoryPath cannot be empty.");
        _ = ValidationMessagesResources.MicrosoftDocsBaseUrlRequired.Should().Be("MicrosoftDocsBaseUrl is required.");
        _ = ValidationMessagesResources.MicrosoftDocsBaseUrlMinLength.Should().Be("MicrosoftDocsBaseUrl cannot be empty.");
        _ = ValidationMessagesResources.TargetNameRequired.Should().Be("TargetName is required.");
        _ = ValidationMessagesResources.TargetNameMinLength.Should().Be("TargetName cannot be empty.");
        _ = ValidationMessagesResources.CustomMarkdownContentRequired.Should().Be("CustomMarkdownContent is required.");
        _ = ValidationMessagesResources.MemberNameRequired.Should().Be("Member name is required.");
        _ = ValidationMessagesResources.MemberNameMinLength.Should().Be("Member name cannot be empty.");
        _ = ValidationMessagesResources.ParameterNameRequired.Should().Be("Parameter name is required.");
        _ = ValidationMessagesResources.ParameterNameMinLength.Should().Be("Parameter name cannot be empty.");
        _ = ValidationMessagesResources.ParameterTypeNameRequired.Should().Be("Parameter type name is required.");
        _ = ValidationMessagesResources.TypeNameRequired.Should().Be("TypeName is required.");
        _ = ValidationMessagesResources.TypeNameMinLength.Should().Be("TypeName cannot be empty.");
        _ = ValidationMessagesResources.NamespaceNameRequired.Should().Be("NamespaceName is required.");
        _ = ValidationMessagesResources.NamespacePrefixRequired.Should().Be("NamespacePrefix is required.");
        _ = ValidationMessagesResources.NamespacePrefixMinLength.Should().Be("NamespacePrefix cannot be empty.");
        _ = ValidationMessagesResources.UrlTemplateRequired.Should().Be("UrlTemplate is required.");
        _ = ValidationMessagesResources.UrlTemplateMinLength.Should().Be("UrlTemplate cannot be empty.");
        _ = ValidationMessagesResources.FailOnUndocumentedThresholdRange.Should().Be("FailOnUndocumentedThreshold must be between 0 and 100.");
        _ = ValidationMessagesResources.DiagramFormatInvalid.Should().Be("DiagramFormat must be a valid DiagramFormat value.");
        _ = ValidationMessagesResources.TypeCategoryInvalid.Should().Be("TypeCategory must be a valid TypeCategory value.");
        _ = ValidationMessagesResources.TargetAnchorInvalid.Should().Be("TargetAnchor must be a valid SidecarAnchor value.");
        _ = ValidationMessagesResources.InjectionModeInvalid.Should().Be("InjectionMode must be a valid InjectionMode value.");
        _ = ValidationMessagesResources.ReferenceNameRequired.Should().Be("ReferenceName is required.");
        _ = ValidationMessagesResources.ReferenceNameMinLength.Should().Be("ReferenceName cannot be empty.");
        _ = ValidationMessagesResources.ReferenceKindInvalid.Should().Be("ReferenceKind must be a valid AssemblyReferenceKind value.");
        _ = ValidationMessagesResources.VersionLinkingKindInvalid.Should().Be("VersionLinkingKind must be a valid AssemblyVersionLinkingKind value.");
        _ = ValidationMessagesResources.DependencySourceNameRequired.Should().Be("SourceAssemblyName is required.");
        _ = ValidationMessagesResources.DependencySourceNameMinLength.Should().Be("SourceAssemblyName must contain at least 1 character.");
        _ = ValidationMessagesResources.ReportFormatInvalid.Should().Be("ReportFormat must be a valid QualityReportFormat value.");
        _ = ValidationMessagesResources.NavigationFormatInvalid.Should().Be("NavigationFormat must be a valid NavigationFormat value.");
        _ = ValidationMessagesResources.DiagnosticCodeRequired.Should().Be("Diagnostic code is required.");
        _ = ValidationMessagesResources.DiagnosticMessageRequired.Should().Be("Diagnostic message is required.");
        _ = ValidationMessagesResources.DiagnosticSeverityInvalid.Should().Be("Diagnostic severity must be a valid DocumentationDiagnosticSeverity value.");
        _ = ValidationMessagesResources.DiagnosticKindInvalid.Should().Be("Diagnostic kind must be a valid DocumentationDiagnosticKind value.");
        _ = ValidationMessagesResources.CoveragePercentageRange.Should().Be("CoveragePercentage must be between 0 and 100.");
        _ = ValidationMessagesResources.PluginNameRequired.Should().Be("PluginName is required.");
        _ = ValidationMessagesResources.PluginAssemblyPathRequired.Should().Be("AssemblyPath is required.");
        _ = ValidationMessagesResources.PluginTypeFullNameRequired.Should().Be("PluginTypeFullName is required.");
    }
}
