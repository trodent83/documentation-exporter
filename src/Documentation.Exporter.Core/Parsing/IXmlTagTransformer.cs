using System.Xml.Linq;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Transforms XML documentation comment fragments into formatted Markdown text.
/// </summary>
public interface IXmlTagTransformer
{
    /// <summary>
    /// Converts an <see cref="XNode"/> or XML documentation element into formatted Markdown text.
    /// </summary>
    /// <param name="xmlNode">The XML node to transform.</param>
    /// <returns>Formatted Markdown string.</returns>
    string TransformXmlToMarkdown(XNode? xmlNode);
}
