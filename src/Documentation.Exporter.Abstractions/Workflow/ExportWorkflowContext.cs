using System.Collections.Concurrent;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Workflow;

/// <summary>
/// Mutable state bag passed through sequential workflow execution steps.
/// </summary>
public sealed class ExportWorkflowContext(ExporterOptions options)
{
    /// <summary>
    /// Gets the exporter options for this export run.
    /// </summary>
    public ExporterOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets or sets the dictionary of exported runtime types extracted from the target assembly.
    /// </summary>
    public IReadOnlyDictionary<string, Type>? ExportedTypesMap { get; set; }

    /// <summary>
    /// Gets or sets the resolved XML documentation (<c>.xml</c>) file paths cached for this workflow execution.
    /// </summary>
    public IReadOnlyList<string>? ResolvedXmlDocumentationFilePaths { get; set; }

    /// <summary>
    /// Gets or sets analyzed assembly reference metadata keyed by target assembly simple name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> AssemblyReferencesByAssemblyName { get; set; } =
        new Dictionary<string, IReadOnlyList<AssemblyReferenceModel>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets analyzed transitive non-framework assembly dependency edges keyed by root assembly simple name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<AssemblyDependencyEdgeModel>> AssemblyDependencyEdgesByAssemblyName { get; set; } =
        new Dictionary<string, IReadOnlyList<AssemblyDependencyEdgeModel>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets formatted custom attributes extracted per exported assembly simple name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AssemblyAttributesByAssemblyName { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the thread-safe collection of compiled type documentation models.
    /// </summary>
    public ConcurrentBag<TypeDocumentationModel> CompiledTypeModels { get; } = [];

    /// <summary>
    /// Gets or sets the total number of generated output files.
    /// </summary>
    public int GeneratedFilesCount { get; set; }

    /// <summary>
    /// Gets or sets the calculated documentation coverage percentage (0.0 to 100.0).
    /// </summary>
    public double DocumentationCoveragePercentage { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets whether the workflow has encountered a failure.
    /// </summary>
    public bool HasFailed { get; set; }

    /// <summary>
    /// Gets the thread-safe collection of documentation quality and link validation diagnostics recorded during execution.
    /// </summary>
    public ConcurrentBag<DocumentationDiagnostic> Diagnostics { get; } = [];

    /// <summary>
    /// Gets or sets the total number of public and protected types and members evaluated for summary coverage.
    /// </summary>
    public int TotalDocumentableSymbols { get; set; }

    /// <summary>
    /// Gets or sets the number of evaluated symbols carrying non-empty summary documentation.
    /// </summary>
    public int DocumentedSymbols { get; set; }

    /// <summary>
    /// Gets or sets the total number of relative links and anchors inspected during link validation.
    /// </summary>
    public int VerifiedLinksCount { get; set; }

    /// <summary>
    /// Gets or sets the finalized quality report snapshot built for this workflow execution.
    /// </summary>
    public DocumentationQualityReport? QualityReport { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the quality report file has already been written to disk.
    /// </summary>
    public bool IsQualityReportWritten { get; set; }

    /// <summary>
    /// Determines whether per-assembly subdirectories (<c>&lt;AssemblyName&gt;/</c>) should be used for output paths and relative links.
    /// </summary>
    /// <param name="distinctAssemblyCount">The number of distinct target assemblies present in the export set.</param>
    /// <returns><see langword="true"/> if multiple assemblies are exported or a solution file is configured; otherwise <see langword="false"/>.</returns>
    public bool ShouldUseAssemblySubdirectories(int distinctAssemblyCount) =>
        distinctAssemblyCount > 1 || !string.IsNullOrWhiteSpace(Options.SolutionFilePath);

    /// <summary>
    /// Builds a deterministic <see cref="DocumentationQualityReport"/> snapshot from the current workflow state.
    /// </summary>
    /// <returns>An aggregated <see cref="DocumentationQualityReport"/> with deterministically sorted diagnostics.</returns>
    public DocumentationQualityReport BuildQualityReportSnapshot()
    {
        List<DocumentationDiagnostic> orderedDiagnostics =
        [
            .. Diagnostics
                .OrderByDescending(diagnostic => diagnostic.Severity)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.FilePath ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.LineNumber ?? 0)
                .ThenBy(diagnostic => diagnostic.SymbolName, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
        ];

        var errorCount = orderedDiagnostics.Count(diagnostic => diagnostic.Severity == DocumentationDiagnosticSeverity.Error);
        var warningCount = orderedDiagnostics.Count(diagnostic => diagnostic.Severity == DocumentationDiagnosticSeverity.Warning);
        var infoCount = orderedDiagnostics.Count(diagnostic => diagnostic.Severity == DocumentationDiagnosticSeverity.Info);

        return new DocumentationQualityReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            DocumentationCoveragePercentage = DocumentationCoveragePercentage,
            TotalDocumentableSymbols = TotalDocumentableSymbols,
            DocumentedSymbols = DocumentedSymbols,
            GeneratedFilesCount = GeneratedFilesCount,
            VerifiedLinksCount = VerifiedLinksCount,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            InfoCount = infoCount,
            Diagnostics = orderedDiagnostics
        };
    }
}
