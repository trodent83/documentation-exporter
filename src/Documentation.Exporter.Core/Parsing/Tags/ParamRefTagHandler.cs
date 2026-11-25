using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Handles parameter reference tags (<c>&lt;paramref&gt;</c> and <c>&lt;typeparamref&gt;</c>).
/// </summary>
public sealed class ParamRefTagHandler : IXmlTagHandler
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) =>
        string.Equals(tagName, "paramref", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(tagName, "typeparamref", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        var paramName = xmlElement.Attribute("name")?.Value ?? string.Empty;
        _ = outputBuilder.Append(CultureInfo.InvariantCulture, $"`{paramName}`");
    }
}
