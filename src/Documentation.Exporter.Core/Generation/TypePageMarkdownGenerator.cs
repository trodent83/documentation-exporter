using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Default implementation of <see cref="ITypePageMarkdownGenerator"/> assembling ordered
/// <see cref="IMarkdownSectionRenderer"/> outputs and rewriting root-relative local links.
/// </summary>
/// <param name="sectionRenderers">The collection of section renderers supplied via dependency injection.</param>
/// <param name="linkRewriter">The relative link rewriter and Markdown normalizer.</param>
public sealed class TypePageMarkdownGenerator(
    IEnumerable<IMarkdownSectionRenderer> sectionRenderers,
    IRelativeLinkRewriter linkRewriter) : ITypePageMarkdownGenerator
{
    private readonly IReadOnlyList<IMarkdownSectionRenderer> renderers = [.. ArgumentGuard.EnsureNotNull(sectionRenderers).OrderBy(renderer => renderer.Order)];

    private readonly IRelativeLinkRewriter rewriter = ArgumentGuard.EnsureNotNull(linkRewriter);

    /// <inheritdoc />
    public string GenerateTypeMarkdown(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        var combinedMarkdown = string.Concat(renderers.Select(renderer => renderer.RenderSection(typeModel)));
        var typeDirectoryUrl = LocalProjectNamespaceHandler.GetTypeDirectoryUrlPath(typeModel, includeAssemblyDirectory);
        var unprefixedSelfUrl = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(typeModel, includeAssemblyDirectory: false);
        var assemblyPrefix = includeAssemblyDirectory && !string.IsNullOrWhiteSpace(typeModel.AssemblyName)
            ? typeModel.AssemblyName.Trim()
            : null;

        var renderedMarkdown = rewriter.AdjustLocalLinksForTypeDirectory(
            combinedMarkdown,
            typeDirectoryUrl,
            assemblyPrefix,
            unprefixedSelfUrl);
        return rewriter.NormalizeMarkdown(renderedMarkdown);
    }
}
