using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Represents the output of evaluating documentation coverage and actionable quality diagnostics across compiled types.
/// </summary>
public sealed record DocumentationQualityAnalysisResult
{
    /// <summary>
    /// Gets or sets the total count of evaluated public/protected types and members.
    /// </summary>
    public int TotalDocumentableSymbols { get; init; }

    /// <summary>
    /// Gets or sets the count of evaluated symbols containing non-empty <c>&lt;summary&gt;</c> documentation.
    /// </summary>
    public int DocumentedSymbols { get; init; }

    /// <summary>
    /// Gets or sets the computed documentation summary coverage percentage (0.0 to 100.0).
    /// </summary>
    [Range(0.0, 100.0, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.CoveragePercentageRange))]
    public double CoveragePercentage { get; init; } = 100.0;

    /// <summary>
    /// Gets or sets the list of documentation quality diagnostics discovered during analysis.
    /// </summary>
    public IReadOnlyList<DocumentationDiagnostic> Diagnostics { get; init; } = [];
}
