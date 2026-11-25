using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Defines a custom URL template rule for resolving external third-party namespace types into documentation links.
/// </summary>
public sealed record ExternalDocLinkRule
{
    /// <summary>
    /// Gets the namespace prefix matched by this rule (for example <c>"Newtonsoft.Json"</c>).
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.NamespacePrefixRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.NamespacePrefixMinLength))]
    public string NamespacePrefix { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL template or base URL used to generate documentation links for matched types.
    /// Supports placeholders: <c>{fullName}</c>, <c>{fullNameLower}</c>, <c>{fullNameUnderscore}</c>, <c>{fullNameSlash}</c>,
    /// <c>{namespace}</c>, <c>{namespaceLower}</c>, <c>{namespaceSlash}</c>, <c>{name}</c>, and <c>{nameLower}</c>.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.UrlTemplateRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.UrlTemplateMinLength))]
    public string UrlTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Parses a <c>"NamespacePrefix=UrlTemplate"</c> mapping string into an <see cref="ExternalDocLinkRule"/>.
    /// </summary>
    /// <param name="mappingExpression">The raw mapping expression.</param>
    /// <param name="parsedRule">The resulting rule when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the expression was valid; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? mappingExpression, out ExternalDocLinkRule? parsedRule)
    {
        parsedRule = null;
        if (string.IsNullOrWhiteSpace(mappingExpression))
        {
            return false;
        }

        var separatorIndex = mappingExpression.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= mappingExpression.Length - 1)
        {
            return false;
        }

        var namespacePrefix = mappingExpression[..separatorIndex].Trim();
        var urlTemplate = mappingExpression[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(namespacePrefix) || string.IsNullOrWhiteSpace(urlTemplate))
        {
            return false;
        }

        parsedRule = new ExternalDocLinkRule
        {
            NamespacePrefix = namespacePrefix,
            UrlTemplate = urlTemplate
        };
        return true;
    }
}
