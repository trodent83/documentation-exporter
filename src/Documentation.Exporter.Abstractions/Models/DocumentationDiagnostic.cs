using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents an actionable documentation quality or Markdown validation finding recorded during export.
/// </summary>
public sealed record DocumentationDiagnostic
{
    /// <summary>
    /// Gets or sets the stable rule code identifying this diagnostic (for example <c>"DOC001"</c> through <c>"DOC007"</c>).
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DiagnosticCodeRequired))]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the structured diagnostic category.
    /// </summary>
    [EnumDataType(typeof(DocumentationDiagnosticKind), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DiagnosticKindInvalid))]
    public DocumentationDiagnosticKind Kind { get; init; }

    /// <summary>
    /// Gets or sets the diagnostic severity level.
    /// </summary>
    [EnumDataType(typeof(DocumentationDiagnosticSeverity), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DiagnosticSeverityInvalid))]
    public DocumentationDiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// Gets or sets the human-readable diagnostic description.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DiagnosticMessageRequired))]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the qualified API symbol name associated with this diagnostic when applicable.
    /// </summary>
    public string SymbolName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path to the Markdown file containing this diagnostic when applicable.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the 1-based line number within <see cref="FilePath"/> where this diagnostic occurs when known.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Gets or sets the unresolved target link or anchor reference when applicable.
    /// </summary>
    public string? TargetReference { get; init; }
}
