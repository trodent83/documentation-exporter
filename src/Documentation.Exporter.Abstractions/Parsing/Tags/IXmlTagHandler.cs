using System.Text;
using System.Xml.Linq;

namespace Documentation.Exporter.Abstractions.Parsing.Tags;

/// <summary>
/// Defines a strategy for transforming specific XML documentation tags into Markdown text.
/// </summary>
public interface IXmlTagHandler
{
    /// <summary>
    /// Gets the evaluation priority order for this handler (lower values run before fallback handlers such as passthrough).
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Determines whether this handler can process the specified XML element tag name.
    /// </summary>
    /// <param name="tagName">The local name of the XML tag.</param>
    /// <returns><see langword="true"/> if the handler can process the tag; otherwise <see langword="false"/>.</returns>
    bool CanHandleTag(string tagName);

    /// <summary>
    /// Transforms the XML element into Markdown syntax and appends it to <paramref name="outputBuilder"/>.
    /// </summary>
    /// <param name="xmlElement">The XML element to transform.</param>
    /// <param name="outputBuilder">The output string builder.</param>
    /// <param name="processChildrenAction">Delegate used to process nested child nodes.</param>
    void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction);
}
