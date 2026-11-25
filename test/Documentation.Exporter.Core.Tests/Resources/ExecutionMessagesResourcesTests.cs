using System.Globalization;
using Documentation.Exporter.Core.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resources;

[UnitTest]
public sealed class ExecutionMessagesResourcesTests
{
    [Fact]
    public void ResourceManagerShouldNotBeNull() =>
        _ = ExecutionMessagesResources.ResourceManager.Should().NotBeNull("because the embedded ExecutionMessagesResources manifest must be accessible.");

    [Fact]
    public void ResourceCultureWhenSetAndResetShouldPersistProperly()
    {
        try
        {
            // Arrange
            var targetCulture = new CultureInfo("en-US");

            // Act
            ExecutionMessagesResources.ResourceCulture = targetCulture;

            // Assert
            _ = ExecutionMessagesResources.ResourceCulture.Should().Be(targetCulture, "because ResourceCulture should store the assigned CultureInfo.");
            _ = ExecutionMessagesResources.CliInitializingPipeline.Should().Be("Initializing Documentation Exporter Pipeline...");
        }
        finally
        {
            ExecutionMessagesResources.ResourceCulture = null;
        }
    }

    [Fact]
    public void FormatMessageWhenTemplateOrArgumentsNullShouldThrowArgumentNullException()
    {
        // Act
        var actNullTemplate = () => ExecutionMessagesResources.FormatMessage(null!, "arg");
        var actNullArguments = () => ExecutionMessagesResources.FormatMessage("{0}", null!);

        // Assert
        _ = actNullTemplate.Should().Throw<ArgumentNullException>("because resourceTemplate cannot be null.");
        _ = actNullArguments.Should().Throw<ArgumentNullException>("because arguments cannot be null.");
    }

    [Fact]
    public void FormatMessageWhenValidTemplateAndArgumentsProvidedShouldReturnFormattedString()
    {
        // Act
        var formatted = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.EngineStartingExport, "Sample.dll");

        // Assert
        _ = formatted.Should().Be("Starting documentation export for target assembly: Sample.dll");
    }

    [Fact]
    public void AllExecutionMessageResourcePropertiesShouldReturnExpectedNeutralStrings()
    {
        // Act & Assert
        _ = ExecutionMessagesResources.CliInitializingPipeline.Should().Be("Initializing Documentation Exporter Pipeline...");
        _ = ExecutionMessagesResources.CliExportSuccess.Should().Be("Documentation export completed successfully.");
        _ = ExecutionMessagesResources.CliExportError.Should().Be("Documentation export failed. Please check the log messages above for details.");
        _ = ExecutionMessagesResources.EngineStartingExport.Should().Be("Starting documentation export for target assembly: {0}");
        _ = ExecutionMessagesResources.ReflectorAssemblyFilePathMissing.Should().Be("Assembly file path is missing or empty.");
        _ = ExecutionMessagesResources.ReflectorAssemblyFileNotFound.Should().Be("Assembly file not found at path: {0}");
        _ = ExecutionMessagesResources.ReflectorLoadingAssembly.Should().Be("Loading target assembly from path: {0}");
        _ = ExecutionMessagesResources.ReflectorExtractedTypes.Should().Be("Extracted {0} exported public types from assembly.");
        _ = ExecutionMessagesResources.ReflectorFailedLoadingAssembly.Should().Be("Failed to load target assembly from path: {0}");
        _ = ExecutionMessagesResources.XmlParserFilePathMissing.Should().Be("XML documentation file path is missing or empty.");
        _ = ExecutionMessagesResources.XmlParserFileNotFound.Should().Be("XML documentation file not found at path: {0}");
        _ = ExecutionMessagesResources.XmlParserLoadingFile.Should().Be("Loading XML documentation file from path: {0}");
        _ = ExecutionMessagesResources.XmlParserNoMembersRoot.Should().Be("XML documentation file contains no <members> root element.");
        _ = ExecutionMessagesResources.XmlParserIndexedMembers.Should().Be("Successfully indexed {0} XML documentation members.");
        _ = ExecutionMessagesResources.XmlParserFailedParsing.Should().Be("Failed to parse XML documentation file at path: {0}");
        _ = ExecutionMessagesResources.ExampleProviderLoadingMemberExample.Should().Be("Loading external code example snippet for member {0}.{1}");
        _ = ExecutionMessagesResources.ExampleProviderLoadingTypeExample.Should().Be("Loading external code example snippet for type {0}");
        _ = ExecutionMessagesResources.InjectorLoadingSidecar.Should().Be("Loading sidecar documentation override file: {0}");
        _ = ExecutionMessagesResources.WorkflowStarted.Should().Be("Starting documentation export workflow with {0} steps.");
        _ = ExecutionMessagesResources.WorkflowExecutingStep.Should().Be("Executing workflow step: {0}");
        _ = ExecutionMessagesResources.WorkflowStepFailed.Should().Be("Workflow step '{0}' reported failure. Aborting workflow.");
        _ = ExecutionMessagesResources.WorkflowCompleted.Should().Be("Documentation export workflow completed successfully.");
        _ = ExecutionMessagesResources.StepNameValidateOptions.Should().Be("Validate Exporter Options");
        _ = ExecutionMessagesResources.StepValidationError.Should().Be("ExporterOptions Validation Error: {0}");
        _ = ExecutionMessagesResources.StepNameLoadAssemblyTypes.Should().Be("Load Assembly Types");
        _ = ExecutionMessagesResources.StepNoTypesFound.Should().Be("No exported public types found in target assembly: {0}");
        _ = ExecutionMessagesResources.StepNameConfigureResolutionHandlers.Should().Be("Configure Resolution Handlers");
        _ = ExecutionMessagesResources.StepNameLoadXmlDocumentation.Should().Be("Load XML Documentation");
        _ = ExecutionMessagesResources.StepFailedParseXml.Should().Be("Failed to parse XML documentation file: {0}");
        _ = ExecutionMessagesResources.StepNameCompileTypeDocumentationModels.Should().Be("Compile Type Documentation Models");
        _ = ExecutionMessagesResources.StepNameEnsureOutputDirectory.Should().Be("Ensure Output Directory");
        _ = ExecutionMessagesResources.StepFailedCreateOutputDir.Should().Be("Failed to create output directory: {0}");
        _ = ExecutionMessagesResources.StepNameEmitMarkdownDocumentation.Should().Be("Emit Markdown Documentation");
        _ = ExecutionMessagesResources.StepNameValidateMarkdownLinks.Should().Be("Validate Markdown Links");
        _ = ExecutionMessagesResources.StepFilesGenerated.Should().Be("Generated {0} Markdown documentation files.");
        _ = ExecutionMessagesResources.StepNameEmitQualityReport.Should().Be("Emit Documentation Quality Report");
        _ = ExecutionMessagesResources.StepQualityDiagnosticsSummary.Should().Be(
            "Documentation quality analysis completed: {0} diagnostic(s) recorded ({1} error(s), {2} warning(s)).");
        _ = ExecutionMessagesResources.StepBrokenAnchorDetected.Should().Be(
            "Broken Markdown anchor in '{0}' (line {1}): target '{2}' does not contain heading '#{3}'.");
        _ = ExecutionMessagesResources.StepQualityReportEmitted.Should().Be(
            "Documentation quality report ({0}) written to: {1}");
        _ = ExecutionMessagesResources.StepQualityReportFailed.Should().Be(
            "Failed to write documentation quality report to '{0}': {1}");
        _ = ExecutionMessagesResources.PluginScannerScanningDirectory.Should().Be(
            "Scanning plugins directory: {0}");
        _ = ExecutionMessagesResources.PluginScannerDirectoryMissing.Should().Be(
            "Plugins directory does not exist; skipping plugin discovery: {0}");
        _ = ExecutionMessagesResources.PluginLoaderLoadedPlugin.Should().Be(
            "Loaded plugin '{0}' ({1}) from '{2}'.");
        _ = ExecutionMessagesResources.PluginLoaderAssemblyFailed.Should().Be(
            "Failed to load or inspect candidate plugin assembly '{0}': {1}");
        _ = ExecutionMessagesResources.PluginLoaderActivationFailed.Should().Be(
            "Failed to activate or configure plugin '{0}' from '{1}': {2}");
        _ = ExecutionMessagesResources.PluginLoaderSummary.Should().Be(
            "Loaded {0} plugin(s) from '{1}'.");
    }
}
