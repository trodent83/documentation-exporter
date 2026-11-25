using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the page title header, assembly/namespace metadata links, obsolete banner, and summary description.
/// </summary>
/// <param name="referenceRegistry">The canonical Microsoft Learn reference registry.</param>
/// <param name="linkRewriter">The relative link and namespace anchor helper.</param>
public sealed class HeaderTitleSectionRenderer(
    IMicrosoftLearnReferenceRegistry referenceRegistry,
    IRelativeLinkRewriter linkRewriter) : IMarkdownSectionRenderer
{
    private readonly IMicrosoftLearnReferenceRegistry learnRegistry = ArgumentGuard.EnsureNotNull(referenceRegistry);
    private readonly IRelativeLinkRewriter rewriter = ArgumentGuard.EnsureNotNull(linkRewriter);

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        var categoryLink = learnRegistry.FormatTypeCategoryLink(typeModel.TypeCategory);
        var namespaceTargetUrl = rewriter.BuildNamespaceSectionLinkTarget(typeModel.NamespaceName);

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"# {typeModel.DisplayTypeName} {categoryLink}");
        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{MarkdownGenerationResources.NamespaceLabel}:** [{typeModel.NamespaceName}]({namespaceTargetUrl})  ");
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{MarkdownGenerationResources.AssemblyLabel}:** [{typeModel.AssemblyName}](index.md)  ");
        _ = sectionBuilder.AppendLine();

        _ = sectionBuilder.Append(learnRegistry.FormatObsoleteWarningBlock(
            typeModel.IsObsolete,
            typeModel.ObsoleteMessage,
            typeModel.IsObsoleteError,
            typeModel.ObsoleteDiagnosticId,
            typeModel.ObsoleteUrlFormat));
        _ = sectionBuilder.Append(learnRegistry.FormatExperimentalWarningBlock(typeModel.IsExperimental, typeModel.ExperimentalDiagnosticId, typeModel.ExperimentalUrlFormat));

        var replaceSummaryInjection = typeModel.InjectedDocumentationSections
            .FirstOrDefault(injection => injection.InjectionMode == InjectionMode.Replace &&
                                         injection.TargetAnchor == SidecarAnchor.ReplaceSummary);

        if (replaceSummaryInjection != null)
        {
            _ = sectionBuilder.AppendLine(replaceSummaryInjection.CustomMarkdownContent);
            _ = sectionBuilder.AppendLine();
        }
        else if (!string.IsNullOrWhiteSpace(typeModel.SummaryDescription))
        {
            _ = sectionBuilder.AppendLine(typeModel.SummaryDescription);
            _ = sectionBuilder.AppendLine();
        }

        return sectionBuilder.ToString();
    }
}
