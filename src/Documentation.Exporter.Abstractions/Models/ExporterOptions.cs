using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Configuration options for the documentation export execution.
/// </summary>
public sealed class ExporterOptions
{
    private string assemblyFilePath = string.Empty;
    private string xmlDocumentationFilePath = string.Empty;

    /// <summary>
    /// Gets or sets the file path to the compiled target assembly (.dll).
    /// When <see cref="AssemblyFilePaths"/> or <see cref="SolutionFilePath"/> is configured instead, returns the primary resolved target path.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.AssemblyFilePathRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.AssemblyFilePathMinLength))]
    public string AssemblyFilePath
    {
        get => !string.IsNullOrWhiteSpace(assemblyFilePath)
            ? assemblyFilePath
            : AssemblyFilePaths.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
                ?? SolutionFilePath
                ?? string.Empty;
        set => assemblyFilePath = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the file path to the compiler-generated XML documentation file (.xml).
    /// When <see cref="XmlDocumentationFilePaths"/> or <see cref="SolutionFilePath"/> is configured instead, returns the primary resolved XML path.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.XmlDocumentationFilePathRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.XmlDocumentationFilePathMinLength))]
    public string XmlDocumentationFilePath
    {
        get => !string.IsNullOrWhiteSpace(xmlDocumentationFilePath)
            ? xmlDocumentationFilePath
            : XmlDocumentationFilePaths.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
                ?? TryInferSiblingXmlFilePath(assemblyFilePath)
                ?? TryInferSiblingXmlFilePath(AssemblyFilePaths.FirstOrDefault())
                ?? SolutionFilePath
                ?? string.Empty;
        set => xmlDocumentationFilePath = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the collection of compiled target assembly file paths (.dll) when exporting multiple assemblies together.
    /// </summary>
    public List<string> AssemblyFilePaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of compiler-generated XML documentation file paths (.xml) when exporting multiple assemblies together.
    /// </summary>
    public List<string> XmlDocumentationFilePaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional file path to a C# solution (<c>.sln</c> or <c>.slnx</c>) whose projects should be exported as a unified documentation set.
    /// </summary>
    public string? SolutionFilePath { get; set; }

    /// <summary>
    /// Gets or sets the build configuration (<c>"Debug"</c> or <c>"Release"</c>) used when discovering compiled project outputs for <see cref="SolutionFilePath"/>.
    /// </summary>
    public string SolutionConfiguration { get; set; } = "Debug";

    /// <summary>
    /// Gets or sets a value indicating whether test projects in <see cref="SolutionFilePath"/> should be included in the documentation export.
    /// </summary>
    public bool IncludeTestProjects { get; set; }

    /// <summary>
    /// Gets or sets the list of project/assembly names or wildcard patterns (such as <c>"*.Tests"</c> or <c>"*.Benchmarks"</c>) to exclude when exporting a solution or assembly collection.
    /// </summary>
    public List<string> ExcludedProjects { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of fully qualified or simple type names, or wildcard patterns (such as <c>"*Internal*"</c>), to exclude from the generated documentation.
    /// </summary>
    public List<string> ExcludedTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of namespace names or wildcard patterns (such as <c>"*.Migrations"</c> or <c>"*.Internal*"</c>) to exclude from the generated documentation.
    /// </summary>
    public List<string> ExcludedNamespaces { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether types marked with <c>[EditorBrowsable(EditorBrowsableState.Never)]</c> should be excluded from generated documentation. Defaults to <c>true</c>.
    /// </summary>
    public bool ExcludeEditorBrowsableNever { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to clean existing generated Markdown (.md) and PlantUML (.puml) files in <see cref="OutputDirectoryPath"/> prior to generation.
    /// </summary>
    public bool CleanOutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether export execution should fail if any included project in <see cref="SolutionFilePath"/> is missing its compiled assembly or XML documentation file.
    /// </summary>
    public bool FailOnMissingProjectArtifacts { get; set; }

    /// <summary>
    /// Gets or sets the minimum documentation coverage percentage (0.0 to 100.0) required. If documentation coverage is below this threshold, the export fails.
    /// </summary>
    [Range(0.0, 100.0, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.FailOnUndocumentedThresholdRange))]
    public double? FailOnUndocumentedThreshold { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to fail export execution when any relative markdown link in the generated documentation cannot be resolved to an existing file.
    /// </summary>
    public bool StrictLinkValidation { get; set; }

    /// <summary>
    /// Gets or sets the preferred diagram generator format (such as <see cref="DiagramFormat.Mermaid"/>, <see cref="DiagramFormat.PlantUml"/>,
    /// <see cref="DiagramFormat.None"/>, or a plugin-defined custom format; defaults to <see cref="DiagramFormat.Mermaid"/>)
    /// rendered on type and index pages, falling back to another registered generator only when the preferred generator does not support a diagram type.
    /// </summary>
    [ValidDiagramFormat(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DiagramFormatInvalid))]
    public DiagramFormat DiagramFormat { get; set; } = DiagramFormat.Mermaid;

    /// <summary>
    /// Gets or sets the target output directory path where generated Markdown files will be saved.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.OutputDirectoryPathRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.OutputDirectoryPathMinLength))]
    public string OutputDirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional directory path containing external code example snippets.
    /// </summary>
    public string? ExamplesDirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the optional directory path containing supplementary markdown sidecar override files.
    /// </summary>
    public string? OverridesDirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the base URL for Microsoft Learn API documentation links.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.MicrosoftDocsBaseUrlRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.MicrosoftDocsBaseUrlMinLength))]
    public string MicrosoftDocsBaseUrl { get; set; } = "https://learn.microsoft.com/en-us/dotnet/api/";

    /// <summary>
    /// Gets custom registered namespace handlers for third-party external types.
    /// </summary>
    [JsonIgnore]
    public List<INamespaceHandler> CustomNamespaceHandlers { get; set; } = [];

    /// <summary>
    /// Gets custom URL template link rules for external third-party namespaces.
    /// </summary>
    public List<ExternalDocLinkRule> ExternalDocLinkRules { get; set; } = [];

    /// <summary>
    /// Gets or sets the target static-site navigation generator format (<see cref="NavigationFormat.MkDocs"/> by default,
    /// <see cref="NavigationFormat.None"/> to disable navigation generation, or any plugin-defined format).
    /// </summary>
    [ValidNavigationFormat(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.NavigationFormatInvalid))]
    public NavigationFormat NavigationFormat { get; set; } = NavigationFormat.MkDocs;

    /// <summary>
    /// Gets or sets the optional file path to an existing or target navigation manifest file (such as <c>mkdocs.yml</c> or <c>mkdocs.nav.yml</c>) to generate or extend.
    /// When omitted, defaults to <c>&lt;OutputDirectoryPath&gt;/&lt;DefaultFileName&gt;</c> supplied by the active <c>INavigationGenerator</c>.
    /// </summary>
    public string? NavigationFilePath { get; set; }

    /// <summary>
    /// Gets or sets the optional MkDocs navigation entry point section path (for example <c>"API Reference"</c> or <c>"Developer Guide/API Reference"</c>)
    /// or comment placeholder (for example <c>"# API-NAV"</c>) where generated API navigation entries should be inserted or updated.
    /// </summary>
    public string? NavigationEntryPoint { get; set; }

    /// <summary>
    /// Gets or sets the optional relative URL prefix prepended to generated Markdown links inside the MkDocs navigation structure (for example <c>"api"</c>).
    /// </summary>
    public string? NavigationUrlPrefix { get; set; }

    /// <summary>
    /// Gets or sets the optional output file path for the structured documentation quality and diagnostics report.
    /// </summary>
    public string? ReportFilePath { get; set; }

    /// <summary>
    /// Gets or sets the serialization format for <see cref="ReportFilePath"/> (<see cref="QualityReportFormat.Json"/>,
    /// <see cref="QualityReportFormat.Markdown"/>, <see cref="QualityReportFormat.Sarif"/>, or a plugin-defined custom format; defaults to <see cref="QualityReportFormat.Json"/>).
    /// </summary>
    [ValidQualityReportFormat(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReportFormatInvalid))]
    public QualityReportFormat ReportFormat { get; set; } = QualityReportFormat.Json;

    internal bool HasExplicitAssemblyFilePath => !string.IsNullOrWhiteSpace(assemblyFilePath);

    internal bool HasExplicitXmlDocumentationFilePath => !string.IsNullOrWhiteSpace(xmlDocumentationFilePath);

    private static string? TryInferSiblingXmlFilePath(string? candidateAssemblyPath) =>
        string.IsNullOrWhiteSpace(candidateAssemblyPath) || !candidateAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? null
            : Path.ChangeExtension(candidateAssemblyPath, ".xml");
}
