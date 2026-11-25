using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Handles paragraph break tags (<c>&lt;para&gt;</c>).
/// </summary>
public sealed class ParagraphTagHandler : IXmlTagHandler
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) =>
        string.Equals(tagName, "para", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine();
        processChildrenAction(xmlElement, outputBuilder);
        _ = outputBuilder.AppendLine();
    }
}
