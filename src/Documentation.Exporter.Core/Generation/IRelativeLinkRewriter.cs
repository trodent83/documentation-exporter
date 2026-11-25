namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Rewrites relative Markdown links between documentation subdirectories, constructs namespace section anchors,
/// and normalizes Markdown line endings.
/// </summary>
public interface IRelativeLinkRewriter
{
    /// <summary>
    /// Rewrites root-relative Markdown links inside <paramref name="rawMarkdown"/> so they resolve correctly from
    /// <paramref name="currentTypeDirectoryUrl"/>.
    /// </summary>
    /// <param name="rawMarkdown">The generated Markdown content containing root-relative links.</param>
    /// <param name="currentTypeDirectoryUrl">The directory path of the current Markdown document relative to the output root.</param>
    /// <param name="assemblyPrefix">Optional current assembly subdirectory prefix when exporting multiple assemblies.</param>
    /// <param name="unprefixedSelfUrl">Optional unprefixed URL to rewrite to the assembly-scoped target.</param>
    /// <returns>Markdown content with rewritten relative links.</returns>
    string AdjustLocalLinksForTypeDirectory(
        string rawMarkdown,
        string currentTypeDirectoryUrl,
        string? assemblyPrefix = null,
        string? unprefixedSelfUrl = null);

    /// <summary>
    /// Collapses excessive consecutive blank lines and guarantees a trailing newline.
    /// </summary>
    /// <param name="rawMarkdown">The Markdown content to normalize.</param>
    /// <returns>Normalized Markdown content.</returns>
    string NormalizeMarkdown(string rawMarkdown);

    /// <summary>
    /// Builds the root <c>index.md#anchor</c> link target pointing to the corresponding namespace subsection.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A relative link to <c>index.md</c> with the namespace section anchor.</returns>
    string BuildNamespaceSectionLinkTarget(string? namespaceName);
}
