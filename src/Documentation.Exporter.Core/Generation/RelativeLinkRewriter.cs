using System.Text;
using System.Text.RegularExpressions;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Default implementation of <see cref="IRelativeLinkRewriter"/>.
/// </summary>
public sealed partial class RelativeLinkRewriter : IRelativeLinkRewriter
{
    /// <inheritdoc />
    public string AdjustLocalLinksForTypeDirectory(
        string rawMarkdown,
        string currentTypeDirectoryUrl,
        string? assemblyPrefix = null,
        string? unprefixedSelfUrl = null)
    {
        ArgumentNullException.ThrowIfNull(rawMarkdown);

        if (string.IsNullOrEmpty(currentTypeDirectoryUrl))
        {
            return rawMarkdown;
        }

        var currentSegments = currentTypeDirectoryUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return LocalMarkdownLinkRegex().Replace(rawMarkdown, match =>
        {
            var linkLabel = match.Groups[1].Value;
            var targetUrl = match.Groups[2].Value;
            var anchorFragment = match.Groups[3].Value;

            if (targetUrl.Contains("://", StringComparison.Ordinal) ||
                targetUrl.StartsWith('/') ||
                targetUrl.StartsWith('.'))
            {
                return match.Value;
            }

            if (!targetUrl.Contains('/', StringComparison.Ordinal) &&
                !string.Equals(targetUrl, "index.md", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            if (!string.IsNullOrEmpty(assemblyPrefix) &&
                (string.Equals(targetUrl, "index.md", StringComparison.OrdinalIgnoreCase) ||
                 (!string.IsNullOrEmpty(unprefixedSelfUrl) && string.Equals(targetUrl, unprefixedSelfUrl, StringComparison.Ordinal))))
            {
                targetUrl = $"{assemblyPrefix}/{targetUrl}";
            }

            var targetSegments = targetUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var targetDirectorySegments = targetSegments.Length > 1 ? targetSegments[..^1] : [];
            var targetFileName = targetSegments[^1];

            var commonPrefixCount = 0;
            while (commonPrefixCount < currentSegments.Length &&
                   commonPrefixCount < targetDirectorySegments.Length &&
                   string.Equals(currentSegments[commonPrefixCount], targetDirectorySegments[commonPrefixCount], StringComparison.Ordinal))
            {
                commonPrefixCount++;
            }

            List<string> relativeParts = [];
            for (var index = commonPrefixCount; index < currentSegments.Length; index++)
            {
                relativeParts.Add("..");
            }

            for (var index = commonPrefixCount; index < targetDirectorySegments.Length; index++)
            {
                relativeParts.Add(targetDirectorySegments[index]);
            }

            relativeParts.Add(targetFileName);
            var relativeTargetUrl = string.Join("/", relativeParts);
            return $"[{linkLabel}]({relativeTargetUrl}{anchorFragment})";
        });
    }

    /// <inheritdoc />
    public string NormalizeMarkdown(string rawMarkdown)
    {
        ArgumentNullException.ThrowIfNull(rawMarkdown);

        var normalized = ConsecutiveBlankLinesRegex().Replace(rawMarkdown, "$1$1");
        return normalized.TrimEnd() + Environment.NewLine;
    }

    /// <inheritdoc />
    public string BuildNamespaceSectionLinkTarget(string? namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return "index.md";
        }

        var rawHeading = $"{MarkdownGenerationResources.NamespaceLabel} {namespaceName.Trim()}";
        StringBuilder slugBuilder = new();
        foreach (var character in rawHeading.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                _ = slugBuilder.Append(character);
            }
            else if (character is ' ' or '-')
            {
                _ = slugBuilder.Append('-');
            }
        }

        return $"index.md#{slugBuilder}";
    }

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex ConsecutiveBlankLinesRegex();

    [GeneratedRegex(@"\[((?:[^\[\]]|\[[^\]]*\])+)\]\(([^)#]+\.md)(#[^)]+)?\)")]
    private static partial Regex LocalMarkdownLinkRegex();
}
