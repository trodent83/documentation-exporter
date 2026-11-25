using System.Globalization;
using System.Resources;

namespace Documentation.Exporter.Core.Resources;

/// <summary>
/// Provides strongly-typed localized strings for console status output, workflow step names, and user-facing execution messages.
/// </summary>
public static class ExecutionMessagesResources
{
    /// <summary>
    /// Gets or sets the culture to use for resource lookups, overriding <see cref="CultureInfo.CurrentUICulture"/> when specified.
    /// </summary>
    public static CultureInfo? ResourceCulture { get; set; }

    /// <summary>
    /// Gets the underlying <see cref="System.Resources.ResourceManager"/> instance.
    /// </summary>
    public static ResourceManager ResourceManager { get; } =
        new("Documentation.Exporter.Core.Resources.ExecutionMessagesResources", typeof(ExecutionMessagesResources).Assembly);

    /// <summary>
    /// Gets the localized status message when initializing the CLI pipeline.
    /// </summary>
    public static string CliInitializingPipeline => GetResourceString(nameof(CliInitializingPipeline));

    /// <summary>
    /// Gets the localized status message when the CLI export completes successfully.
    /// </summary>
    public static string CliExportSuccess => GetResourceString(nameof(CliExportSuccess));

    /// <summary>
    /// Gets the localized error message when the CLI export fails.
    /// </summary>
    public static string CliExportError => GetResourceString(nameof(CliExportError));

    /// <summary>
    /// Gets the localized message template when starting the engine export.
    /// </summary>
    public static string EngineStartingExport => GetResourceString(nameof(EngineStartingExport));

    /// <summary>
    /// Gets the localized error message when the assembly file path is missing or empty.
    /// </summary>
    public static string ReflectorAssemblyFilePathMissing => GetResourceString(nameof(ReflectorAssemblyFilePathMissing));

    /// <summary>
    /// Gets the localized error message template when the assembly file is not found.
    /// </summary>
    public static string ReflectorAssemblyFileNotFound => GetResourceString(nameof(ReflectorAssemblyFileNotFound));

    /// <summary>
    /// Gets the localized status message template when loading the target assembly.
    /// </summary>
    public static string ReflectorLoadingAssembly => GetResourceString(nameof(ReflectorLoadingAssembly));

    /// <summary>
    /// Gets the localized status message template reporting the number of extracted public types.
    /// </summary>
    public static string ReflectorExtractedTypes => GetResourceString(nameof(ReflectorExtractedTypes));

    /// <summary>
    /// Gets the localized error message template when loading the target assembly fails.
    /// </summary>
    public static string ReflectorFailedLoadingAssembly => GetResourceString(nameof(ReflectorFailedLoadingAssembly));

    /// <summary>
    /// Gets the localized error message when the XML documentation file path is missing or empty.
    /// </summary>
    public static string XmlParserFilePathMissing => GetResourceString(nameof(XmlParserFilePathMissing));

    /// <summary>
    /// Gets the localized error message template when the XML documentation file is not found.
    /// </summary>
    public static string XmlParserFileNotFound => GetResourceString(nameof(XmlParserFileNotFound));

    /// <summary>
    /// Gets the localized status message template when loading the XML documentation file.
    /// </summary>
    public static string XmlParserLoadingFile => GetResourceString(nameof(XmlParserLoadingFile));

    /// <summary>
    /// Gets the localized warning message when the XML documentation file has no <c>&lt;members&gt;</c> root element.
    /// </summary>
    public static string XmlParserNoMembersRoot => GetResourceString(nameof(XmlParserNoMembersRoot));

    /// <summary>
    /// Gets the localized status message template reporting the number of indexed XML members.
    /// </summary>
    public static string XmlParserIndexedMembers => GetResourceString(nameof(XmlParserIndexedMembers));

    /// <summary>
    /// Gets the localized error message template when parsing the XML documentation file fails.
    /// </summary>
    public static string XmlParserFailedParsing => GetResourceString(nameof(XmlParserFailedParsing));

    /// <summary>
    /// Gets the localized status message template when loading a member code example snippet.
    /// </summary>
    public static string ExampleProviderLoadingMemberExample => GetResourceString(nameof(ExampleProviderLoadingMemberExample));

    /// <summary>
    /// Gets the localized status message template when loading a type code example snippet.
    /// </summary>
    public static string ExampleProviderLoadingTypeExample => GetResourceString(nameof(ExampleProviderLoadingTypeExample));

    /// <summary>
    /// Gets the localized status message template when loading a sidecar markdown override file.
    /// </summary>
    public static string InjectorLoadingSidecar => GetResourceString(nameof(InjectorLoadingSidecar));

    /// <summary>
    /// Gets the localized status message template when starting the export workflow.
    /// </summary>
    public static string WorkflowStarted => GetResourceString(nameof(WorkflowStarted));

    /// <summary>
    /// Gets the localized debug message template when executing a workflow step.
    /// </summary>
    public static string WorkflowExecutingStep => GetResourceString(nameof(WorkflowExecutingStep));

    /// <summary>
    /// Gets the localized error message template when a workflow step fails.
    /// </summary>
    public static string WorkflowStepFailed => GetResourceString(nameof(WorkflowStepFailed));

    /// <summary>
    /// Gets the localized status message when the export workflow completes successfully.
    /// </summary>
    public static string WorkflowCompleted => GetResourceString(nameof(WorkflowCompleted));

    /// <summary>
    /// Gets the localized display name for the Validate Options workflow step.
    /// </summary>
    public static string StepNameValidateOptions => GetResourceString(nameof(StepNameValidateOptions));

    /// <summary>
    /// Gets the localized error message template for an <c>ExporterOptions</c> validation error.
    /// </summary>
    public static string StepValidationError => GetResourceString(nameof(StepValidationError));

    /// <summary>
    /// Gets the localized display name for the Load Assembly Types workflow step.
    /// </summary>
    public static string StepNameLoadAssemblyTypes => GetResourceString(nameof(StepNameLoadAssemblyTypes));

    /// <summary>
    /// Gets the localized warning message template when no exported public types are found.
    /// </summary>
    public static string StepNoTypesFound => GetResourceString(nameof(StepNoTypesFound));

    /// <summary>
    /// Gets the localized display name for the Configure Resolution Handlers workflow step.
    /// </summary>
    public static string StepNameConfigureResolutionHandlers => GetResourceString(nameof(StepNameConfigureResolutionHandlers));

    /// <summary>
    /// Gets the localized display name for the Load XML Documentation workflow step.
    /// </summary>
    public static string StepNameLoadXmlDocumentation => GetResourceString(nameof(StepNameLoadXmlDocumentation));

    /// <summary>
    /// Gets the localized error message template when the Load XML Documentation step fails.
    /// </summary>
    public static string StepFailedParseXml => GetResourceString(nameof(StepFailedParseXml));

    /// <summary>
    /// Gets the localized display name for the Compile Type Documentation Models workflow step.
    /// </summary>
    public static string StepNameCompileTypeDocumentationModels => GetResourceString(nameof(StepNameCompileTypeDocumentationModels));

    /// <summary>
    /// Gets the localized display name for the Ensure Output Directory workflow step.
    /// </summary>
    public static string StepNameEnsureOutputDirectory => GetResourceString(nameof(StepNameEnsureOutputDirectory));

    /// <summary>
    /// Gets the localized error message template when creating the output directory fails.
    /// </summary>
    public static string StepFailedCreateOutputDir => GetResourceString(nameof(StepFailedCreateOutputDir));

    /// <summary>
    /// Gets the localized display name for the Emit Markdown Documentation workflow step.
    /// </summary>
    public static string StepNameEmitMarkdownDocumentation => GetResourceString(nameof(StepNameEmitMarkdownDocumentation));

    /// <summary>
    /// Gets the localized display name for the Calculate Documentation Coverage workflow step.
    /// </summary>
    public static string StepNameCalculateDocumentationCoverage => GetResourceString(nameof(StepNameCalculateDocumentationCoverage));

    /// <summary>
    /// Gets the localized display name for the Validate Markdown Links workflow step.
    /// </summary>
    public static string StepNameValidateMarkdownLinks => GetResourceString(nameof(StepNameValidateMarkdownLinks));

    /// <summary>
    /// Gets the localized status message template reporting the number of generated Markdown files.
    /// </summary>
    public static string StepFilesGenerated => GetResourceString(nameof(StepFilesGenerated));

    /// <summary>
    /// Gets the localized warning message template when no navigation generator matches the configured format.
    /// </summary>
    public static string NavigationGeneratorNotFoundWarning => GetResourceString(nameof(NavigationGeneratorNotFoundWarning));

    /// <summary>
    /// Gets the localized display name for the Emit Documentation Quality Report workflow step.
    /// </summary>
    public static string StepNameEmitQualityReport => GetResourceString(nameof(StepNameEmitQualityReport));

    /// <summary>
    /// Gets the localized status message template reporting quality diagnostic totals.
    /// </summary>
    public static string StepQualityDiagnosticsSummary => GetResourceString(nameof(StepQualityDiagnosticsSummary));

    /// <summary>
    /// Gets the localized warning message template when a Markdown heading anchor target is missing.
    /// </summary>
    public static string StepBrokenAnchorDetected => GetResourceString(nameof(StepBrokenAnchorDetected));

    /// <summary>
    /// Gets the localized status message template when a documentation quality report is written.
    /// </summary>
    public static string StepQualityReportEmitted => GetResourceString(nameof(StepQualityReportEmitted));

    /// <summary>
    /// Gets the localized error message template when writing a documentation quality report fails.
    /// </summary>
    public static string StepQualityReportFailed => GetResourceString(nameof(StepQualityReportFailed));

    /// <summary>
    /// Gets the localized debug message template when scanning the configured plugins directory.
    /// </summary>
    public static string PluginScannerScanningDirectory => GetResourceString(nameof(PluginScannerScanningDirectory));

    /// <summary>
    /// Gets the localized debug message template when the configured plugins directory does not exist.
    /// </summary>
    public static string PluginScannerDirectoryMissing => GetResourceString(nameof(PluginScannerDirectoryMissing));

    /// <summary>
    /// Gets the localized status message template when a plugin is loaded and configured.
    /// </summary>
    public static string PluginLoaderLoadedPlugin => GetResourceString(nameof(PluginLoaderLoadedPlugin));

    /// <summary>
    /// Gets the localized warning message template when a candidate plugin assembly fails to load or inspect.
    /// </summary>
    public static string PluginLoaderAssemblyFailed => GetResourceString(nameof(PluginLoaderAssemblyFailed));

    /// <summary>
    /// Gets the localized warning message template when instantiating or configuring a plugin fails.
    /// </summary>
    public static string PluginLoaderActivationFailed => GetResourceString(nameof(PluginLoaderActivationFailed));

    /// <summary>
    /// Gets the localized status message template summarizing total plugins loaded from a directory.
    /// </summary>
    public static string PluginLoaderSummary => GetResourceString(nameof(PluginLoaderSummary));

    /// <summary>
    /// Formats a localized resource string template using the current or overridden culture.
    /// </summary>
    /// <param name="resourceTemplate">The composite format template retrieved from resources.</param>
    /// <param name="arguments">The arguments to format into the template.</param>
    /// <returns>The formatted localized message.</returns>
    public static string FormatMessage(string resourceTemplate, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(resourceTemplate);
        ArgumentNullException.ThrowIfNull(arguments);

        return string.Format(ResourceCulture ?? CultureInfo.CurrentCulture, resourceTemplate, arguments);
    }

    private static string GetResourceString(string resourceKey) =>
        ResourceManager.GetString(resourceKey, ResourceCulture ?? CultureInfo.CurrentUICulture)!;
}
