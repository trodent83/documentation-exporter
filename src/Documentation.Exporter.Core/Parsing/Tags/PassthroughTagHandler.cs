using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Fallback tag handler that processes child nodes transparently for standard container tags
/// (<c>&lt;summary&gt;</c>, <c>&lt;remarks&gt;</c>, <c>&lt;value&gt;</c>, <c>&lt;returns&gt;</c>, <c>&lt;example&gt;</c>)
/// and unknown custom XML elements.
/// </summary>
public sealed class PassthroughTagHandler : IXmlTagHandler
{
    /// <inheritdoc />
    public int Order => int.MaxValue;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) => true;

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        processChildrenAction(xmlElement, outputBuilder);
    }
}
