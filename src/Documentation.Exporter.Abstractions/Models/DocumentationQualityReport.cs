using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Aggregates documentation coverage statistics, link verification totals, and structured diagnostics for an export run.
/// </summary>
public sealed record DocumentationQualityReport
{
    /// <summary>
    /// Gets or sets the UTC timestamp when this report snapshot was constructed.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the documentation summary coverage percentage (0.0 to 100.0).
    /// </summary>
    [Range(0.0, 100.0, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.CoveragePercentageRange))]
    public double DocumentationCoveragePercentage { get; init; } = 100.0;

    /// <summary>
    /// Gets or sets the total number of public/protected types and members evaluated for summary coverage.
    /// </summary>
    public int TotalDocumentableSymbols { get; init; }

    /// <summary>
    /// Gets or sets the number of documentable symbols that have non-empty <c>&lt;summary&gt;</c> documentation.
    /// </summary>
    public int DocumentedSymbols { get; init; }

    /// <summary>
    /// Gets or sets the total number of Markdown and diagram files emitted during the export run.
    /// </summary>
    public int GeneratedFilesCount { get; init; }

    /// <summary>
    /// Gets or sets the total number of relative Markdown links and anchors checked during link validation.
    /// </summary>
    public int VerifiedLinksCount { get; init; }

    /// <summary>
    /// Gets or sets the count of diagnostics with <see cref="DocumentationDiagnosticSeverity.Error"/>.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets or sets the count of diagnostics with <see cref="DocumentationDiagnosticSeverity.Warning"/>.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Gets or sets the count of diagnostics with <see cref="DocumentationDiagnosticSeverity.Info"/>.
    /// </summary>
    public int InfoCount { get; init; }

    /// <summary>
    /// Gets or sets the deterministically ordered list of documentation quality and link validation diagnostics.
    /// </summary>
    public IReadOnlyList<DocumentationDiagnostic> Diagnostics { get; init; } = [];
}
