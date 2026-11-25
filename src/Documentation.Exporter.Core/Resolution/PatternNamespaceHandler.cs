using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Resolves external third-party types matching a namespace prefix into Markdown links using a configurable URL template.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PatternNamespaceHandler"/> class.
/// </remarks>
/// <param name="namespacePrefix">The namespace prefix handled by this instance (for example <c>"Newtonsoft.Json"</c>).</param>
/// <param name="urlTemplate">
/// The URL template containing placeholders (<c>{fullName}</c>, <c>{fullNameLower}</c>, <c>{fullNameUnderscore}</c>,
/// <c>{fullNameSlash}</c>, <c>{namespace}</c>, <c>{namespaceLower}</c>, <c>{namespaceSlash}</c>, <c>{name}</c>, <c>{nameLower}</c>),
/// or a base URL prefix.
/// </param>
public sealed class PatternNamespaceHandler(string namespacePrefix, string urlTemplate) : INamespaceHandler
{
    private readonly string prefix = ValidateNonEmpty(namespacePrefix, nameof(namespacePrefix));
    private readonly string template = ValidateNonEmpty(urlTemplate, nameof(urlTemplate));

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternNamespaceHandler"/> class from an <see cref="ExternalDocLinkRule"/>.
    /// </summary>
    /// <param name="rule">The external documentation link rule.</param>
    public PatternNamespaceHandler(ExternalDocLinkRule rule)
        : this(
            ArgumentGuard.EnsureNotNull(rule).NamespacePrefix,
            rule.UrlTemplate)
    {
    }

    /// <inheritdoc />
    public bool CanHandleNamespace(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return false;
        }

        var cleanedName = CleanTypeName(fullTypeName);
        return string.Equals(cleanedName, prefix, StringComparison.OrdinalIgnoreCase) ||
               cleanedName.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string ResolveTypeLink(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return string.Empty;
        }

        var cleanedName = CleanTypeName(fullTypeName);
        var lastDotIndex = cleanedName.LastIndexOf('.');
        var namespacePart = lastDotIndex > 0 ? cleanedName[..lastDotIndex] : string.Empty;
        var simpleName = lastDotIndex < 0 ? cleanedName : cleanedName[(lastDotIndex + 1)..];

        var resolvedUrl = FormatUrlFromTemplate(template, cleanedName, namespacePart, simpleName);
        return $"[{simpleName}]({resolvedUrl})";
    }

    private static string FormatUrlFromTemplate(
        string urlTemplate,
        string fullTypeName,
        string namespaceName,
        string simpleTypeName)
    {
        if (!urlTemplate.Contains('{', StringComparison.Ordinal))
        {
            var normalizedBaseUrl = urlTemplate.EndsWith('/') ? urlTemplate : urlTemplate + "/";
            return $"{normalizedBaseUrl}{fullTypeName.ToLowerInvariant()}";
        }

        return urlTemplate
            .Replace("{fullName}", fullTypeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{fullNameLower}", fullTypeName.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
            .Replace("{fullNameUnderscore}", fullTypeName.Replace('.', '_'), StringComparison.OrdinalIgnoreCase)
            .Replace("{fullNameSlash}", fullTypeName.Replace('.', '/'), StringComparison.OrdinalIgnoreCase)
            .Replace("{namespace}", namespaceName, StringComparison.OrdinalIgnoreCase)
            .Replace("{namespaceLower}", namespaceName.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
            .Replace("{namespaceSlash}", namespaceName.Replace('.', '/'), StringComparison.OrdinalIgnoreCase)
            .Replace("{name}", simpleTypeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{nameLower}", simpleTypeName.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanTypeName(string rawTypeName)
    {
        var cleaned = rawTypeName.Trim();
        if (cleaned.Length > 2 && cleaned[1] == ':')
        {
            cleaned = cleaned[2..];
        }

        var bracketIndex = cleaned.IndexOf("[[", StringComparison.Ordinal);
        if (bracketIndex >= 0)
        {
            cleaned = cleaned[..bracketIndex];
        }

        var parenIndex = cleaned.IndexOf('(');
        if (parenIndex >= 0)
        {
            cleaned = cleaned[..parenIndex];
        }

        var backtickIndex = cleaned.IndexOf('`');
        if (backtickIndex >= 0)
        {
            cleaned = cleaned[..backtickIndex];
        }

        var angleIndex = cleaned.IndexOf('<');
        if (angleIndex >= 0)
        {
            cleaned = cleaned[..angleIndex];
        }

        var curlyIndex = cleaned.IndexOf('{');
        return curlyIndex >= 0 ? cleaned[..curlyIndex] : cleaned;
    }

    private static string ValidateNonEmpty(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value.Trim();
    }
}
