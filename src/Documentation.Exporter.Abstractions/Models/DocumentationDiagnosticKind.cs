using System.Text.Json.Serialization;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Categorizes a documentation quality or link validation finding.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentationDiagnosticKind
{
    /// <summary>
    /// Public or protected type is missing an XML <c>&lt;summary&gt;</c> description (<c>DOC001</c>).
    /// </summary>
    MissingTypeSummary = 0,

    /// <summary>
    /// Accessible member is missing an XML <c>&lt;summary&gt;</c> description (<c>DOC002</c>).
    /// </summary>
    MissingMemberSummary = 1,

    /// <summary>
    /// Method, constructor, or indexer parameter is missing an XML <c>&lt;param&gt;</c> description (<c>DOC003</c>).
    /// </summary>
    MissingParameterDocumentation = 2,

    /// <summary>
    /// Generic type or method type parameter is missing an XML <c>&lt;typeparam&gt;</c> description (<c>DOC004</c>).
    /// </summary>
    MissingTypeParameterDocumentation = 3,

    /// <summary>
    /// Non-void method return value is missing an XML <c>&lt;returns&gt;</c> description (<c>DOC005</c>).
    /// </summary>
    MissingReturnDocumentation = 4,

    /// <summary>
    /// Relative Markdown hyperlink references a missing file or directory target (<c>DOC006</c>).
    /// </summary>
    BrokenMarkdownLink = 5,

    /// <summary>
    /// Markdown hyperlink references an anchor heading slug that does not exist on the target page (<c>DOC007</c>).
    /// </summary>
    BrokenMarkdownAnchor = 6
}
