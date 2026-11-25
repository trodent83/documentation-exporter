using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Abstractions.Parsing.Tags;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Transforms C# XML documentation tags into GitHub Flavored Markdown syntax using ordered, DI-registered tag handlers.
/// </summary>
/// <param name="tagHandlers">The collection of XML tag handlers supplied via dependency injection.</param>
public sealed class XmlTagTransformer(IEnumerable<IXmlTagHandler> tagHandlers) : IXmlTagTransformer
{
    private readonly IReadOnlyList<IXmlTagHandler> handlers = [.. ArgumentGuard.EnsureNotNull(tagHandlers).OrderBy(handler => handler.Order)];

    /// <inheritdoc />
    public string TransformXmlToMarkdown(XNode? xmlNode)
    {
        if (xmlNode == null)
        {
            return string.Empty;
        }

        StringBuilder stringBuilder = new();
        ProcessXmlNode(xmlNode, stringBuilder);
        return stringBuilder.ToString().Trim();
    }

    private void ProcessXmlNode(XNode xmlNode, StringBuilder stringBuilder)
    {
        if (xmlNode is XText textNode)
        {
            _ = stringBuilder.Append(NormalizeTextWhitespace(textNode.Value));
            return;
        }

        if (xmlNode is not XElement xmlElement)
        {
            return;
        }

        var tagName = xmlElement.Name.LocalName;
        foreach (var handler in handlers)
        {
            if (handler.CanHandleTag(tagName))
            {
                handler.TransformTag(xmlElement, stringBuilder, ProcessChildNodes);
                return;
            }
        }
    }

    private static string NormalizeTextWhitespace(string text)
    {
        if (!text.Contains('\n'))
        {
            return text;
        }

        var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            line = lineIndex > 0 ? line.TrimStart() : line;
            line = lineIndex < lines.Length - 1 ? line.TrimEnd() : line;
            lines[lineIndex] = line;
        }

        return string.Join('\n', lines);
    }

    private void ProcessChildNodes(XElement xmlElement, StringBuilder stringBuilder)
    {
        foreach (var childNode in xmlElement.Nodes())
        {
            ProcessXmlNode(childNode, stringBuilder);
        }
    }
}
