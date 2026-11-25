using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Abstractions.Resolution;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Handles cross-reference link tags (<c>&lt;see&gt;</c> and <c>&lt;seealso&gt;</c>) using an injected <see cref="ITypeLinkResolver"/>.
/// </summary>
/// <param name="typeLinkResolver">The type link resolver used for <c>cref</c> resolution.</param>
public sealed class SeeTagHandler(ITypeLinkResolver typeLinkResolver) : IXmlTagHandler
{
    private readonly ITypeLinkResolver linkResolver = ArgumentGuard.EnsureNotNull(typeLinkResolver);

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) =>
        string.Equals(tagName, "see", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(tagName, "seealso", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        var crefValue = xmlElement.Attribute("cref")?.Value;
        if (!string.IsNullOrWhiteSpace(crefValue))
        {
            var cleanedTypeName = StripTypePrefix(crefValue);
            var resolvedLink = linkResolver.ResolveLink(cleanedTypeName);
            _ = outputBuilder.Append(ApplyCustomLabelIfPresent(xmlElement, resolvedLink, processChildrenAction));
            return;
        }

        var hrefValue = xmlElement.Attribute("href")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(hrefValue))
        {
            var linkText = string.IsNullOrWhiteSpace(xmlElement.Value) ? hrefValue : xmlElement.Value.Trim();
            _ = outputBuilder.Append(CultureInfo.InvariantCulture, $"[{linkText}]({hrefValue})");
            return;
        }

        var langwordValue = xmlElement.Attribute("langword")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(langwordValue))
        {
            _ = outputBuilder.Append(CultureInfo.InvariantCulture, $"`{langwordValue}`");
            return;
        }

        if (!string.IsNullOrWhiteSpace(xmlElement.Value))
        {
            _ = outputBuilder.Append(xmlElement.Value.Trim());
        }
    }

    private static string ApplyCustomLabelIfPresent(
        XElement xmlElement,
        string resolvedLink,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        if (xmlElement.IsEmpty || !xmlElement.Nodes().Any())
        {
            return resolvedLink;
        }

        StringBuilder labelBuilder = new();
        processChildrenAction(xmlElement, labelBuilder);
        var customLabel = labelBuilder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(customLabel))
        {
            return resolvedLink;
        }

        var linkSeparatorIndex = resolvedLink.IndexOf("](", StringComparison.Ordinal);
        if (resolvedLink.StartsWith('[') &&
            resolvedLink.EndsWith(')') &&
            linkSeparatorIndex > 0)
        {
            var targetUrl = resolvedLink[(linkSeparatorIndex + 2)..^1];
            return $"[{customLabel}]({targetUrl})";
        }

        return customLabel;
    }

    private static string StripTypePrefix(string crefValue) =>
        crefValue.Length > 2 && crefValue[1] == ':' ? crefValue[2..] : crefValue;
}
