using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Default implementation of <see cref="IIndexPageMarkdownGenerator"/> using injected
/// <see cref="IArchitectureDiagramGenerator"/>, <see cref="IAssemblyDependencyDiagramGenerator"/>,
/// and <see cref="IDiagramGeneratorSelector"/> strategies.
/// </summary>
/// <param name="architectureDiagramGenerators">The registered architecture diagram generators.</param>
/// <param name="assemblyDependencyDiagramGenerators">The registered assembly dependency tree diagram generators.</param>
/// <param name="diagramGeneratorSelector">The preferred-with-fallback diagram generator selector.</param>
/// <param name="linkRewriter">The relative link rewriter and Markdown normalizer.</param>
/// <param name="referenceRegistry">The canonical Microsoft Learn reference URL registry.</param>
public sealed class IndexPageMarkdownGenerator(
    IEnumerable<IArchitectureDiagramGenerator> architectureDiagramGenerators,
    IEnumerable<IAssemblyDependencyDiagramGenerator> assemblyDependencyDiagramGenerators,
    IDiagramGeneratorSelector diagramGeneratorSelector,
    IRelativeLinkRewriter linkRewriter,
    IMicrosoftLearnReferenceRegistry referenceRegistry) : IIndexPageMarkdownGenerator
{
    private readonly IReadOnlyList<IArchitectureDiagramGenerator> diagramGenerators = [.. ArgumentGuard.EnsureNotNull(architectureDiagramGenerators)];

    private readonly IReadOnlyList<IAssemblyDependencyDiagramGenerator> dependencyDiagramGenerators = [.. ArgumentGuard.EnsureNotNull(assemblyDependencyDiagramGenerators)];

    private readonly IDiagramGeneratorSelector diagramSelector = ArgumentGuard.EnsureNotNull(diagramGeneratorSelector);
    private readonly IRelativeLinkRewriter rewriter = ArgumentGuard.EnsureNotNull(linkRewriter);
    private readonly IMicrosoftLearnReferenceRegistry learnRegistry = ArgumentGuard.EnsureNotNull(referenceRegistry);

    /// <inheritdoc />
    public string GenerateIndexMarkdown(
        IEnumerable<TypeDocumentationModel> allTypes,
        bool includeAssemblyDirectory = false,
        DiagramFormat? diagramFormat = null,
        bool isAssemblySubdirectoryIndex = false,
        IReadOnlyList<AssemblyReferenceModel>? assemblyReferences = null,
        IReadOnlySet<string>? analyzedAssemblyNames = null,
        IReadOnlyList<AssemblyDependencyEdgeModel>? assemblyDependencyEdges = null,
        IReadOnlyList<string>? assemblyAttributes = null)
    {
        ArgumentNullException.ThrowIfNull(allTypes);

        var effectiveDiagramFormat = diagramFormat ?? DiagramFormat.None;
        var materializedTypes = allTypes.ToList();
        StringBuilder outputBuilder = new();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"# {MarkdownGenerationResources.IndexPageTitle}");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(MarkdownGenerationResources.IndexWelcomeMessage);
        _ = outputBuilder.AppendLine();

        if (includeAssemblyDirectory)
        {
            AppendAssemblyDirectoryLinks(outputBuilder, materializedTypes);
            AppendIndexArchitectureDiagram(outputBuilder, materializedTypes, includeAssemblyDirectory: true, effectiveDiagramFormat);
            return rewriter.NormalizeMarkdown(outputBuilder.ToString());
        }

        var currentAssemblyName = materializedTypes
            .Select(typeModel => typeModel.AssemblyName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        AppendAssemblyAttributesLine(outputBuilder, assemblyAttributes, currentAssemblyName, isAssemblySubdirectoryIndex);

        var groupedByNamespace = materializedTypes
            .GroupBy(typeModel => typeModel.NamespaceName)
            .OrderBy(group => group.Key);

        foreach (var namespaceGroup in groupedByNamespace)
        {
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.NamespaceLabel}: `{namespaceGroup.Key}`");
            _ = outputBuilder.AppendLine();
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {MarkdownGenerationResources.TableTypeHeader} | {MarkdownGenerationResources.TableCategoryHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
            _ = outputBuilder.AppendLine("| :--- | :--- | :--- |");

            foreach (var typeModel in namespaceGroup
                         .OrderBy(typeModel => typeModel.TypeName, StringComparer.Ordinal)
                         .ThenBy(typeModel => typeModel.GenericTypeParameters.Count))
            {
                var cleanSummary = AdjustAssemblyIndexLinks(
                    typeModel.SummaryDescription.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim(),
                    typeModel.AssemblyName,
                    isAssemblySubdirectoryIndex);

                var relativeUrlPath = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(typeModel, includeAssemblyDirectory);
                var categoryLink = learnRegistry.FormatTypeCategoryLink(typeModel.TypeCategory);
                var descriptionCell = FormatTypeTableDescription(typeModel, cleanSummary);

                descriptionCell = AppendTypeAttributesToDescription(descriptionCell, typeModel, isAssemblySubdirectoryIndex);
                _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"| [{typeModel.DisplayTypeName}]({relativeUrlPath}) | {categoryLink} | {descriptionCell} |");
            }

            _ = outputBuilder.AppendLine();
        }

        AppendAssemblyReferencesTable(outputBuilder, currentAssemblyName, isAssemblySubdirectoryIndex, assemblyReferences, analyzedAssemblyNames);
        AppendAssemblyDependencyTreeDiagram(outputBuilder, currentAssemblyName, effectiveDiagramFormat, assemblyDependencyEdges);
        AppendIndexArchitectureDiagram(outputBuilder, materializedTypes, includeAssemblyDirectory: false, effectiveDiagramFormat);
        return rewriter.NormalizeMarkdown(outputBuilder.ToString());
    }

    private void AppendAssemblyAttributesLine(
        StringBuilder outputBuilder,
        IReadOnlyList<string>? assemblyAttributes,
        string? currentAssemblyName,
        bool isAssemblySubdirectoryIndex)
    {
        if (assemblyAttributes == null || assemblyAttributes.Count == 0)
        {
            return;
        }

        var formattedAttributes = string.Join(", ", assemblyAttributes.Select(learnRegistry.FormatAttributeItem));
        var adjustedAttributes = AdjustAssemblyIndexLinks(formattedAttributes, currentAssemblyName, isAssemblySubdirectoryIndex);
        _ = outputBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.AttributesHeaderLabel}]({learnRegistry.AttributesDocUrl}):** {adjustedAttributes}");
        _ = outputBuilder.AppendLine();
    }

    private string FormatTypeTableDescription(TypeDocumentationModel typeModel, string cleanSummary)
    {
        if (typeModel.IsObsolete)
        {
            var obsoleteDiagnosticBadge = learnRegistry.FormatDiagnosticBadge(typeModel.ObsoleteDiagnosticId, typeModel.ObsoleteUrlFormat);
            var obsoletePrefix = $"**[{MarkdownGenerationResources.ObsoleteWarningTitle}]({learnRegistry.ObsoleteAttributeDocUrl}){obsoleteDiagnosticBadge}:** {(string.IsNullOrWhiteSpace(typeModel.ObsoleteMessage) ? cleanSummary : typeModel.ObsoleteMessage)}";
            if (!typeModel.IsExperimental)
            {
                return obsoletePrefix;
            }

            var experimentalBadge = FormatExperimentalTableBadge(typeModel.ExperimentalDiagnosticId);
            return $"{obsoletePrefix}<br/>{experimentalBadge}";
        }

        if (!typeModel.IsExperimental)
        {
            return cleanSummary;
        }

        var badge = FormatExperimentalTableBadge(typeModel.ExperimentalDiagnosticId);
        return string.IsNullOrWhiteSpace(cleanSummary)
            ? badge
            : $"{badge} \u2014 {cleanSummary}";
    }

    private string FormatExperimentalTableBadge(string diagnosticId) =>
        string.IsNullOrWhiteSpace(diagnosticId)
            ? $"**[{MarkdownGenerationResources.ExperimentalWarningTitle}]({learnRegistry.ExperimentalAttributeDocUrl})**"
            : $"**[{MarkdownGenerationResources.ExperimentalWarningTitle}]({learnRegistry.ExperimentalAttributeDocUrl})** (`{diagnosticId.Trim()}`)";

    private string AppendTypeAttributesToDescription(
        string descriptionCell,
        TypeDocumentationModel typeModel,
        bool isAssemblySubdirectoryIndex)
    {
        if (typeModel.AppliedAttributes.Count == 0)
        {
            return descriptionCell;
        }

        var rawAttributes = string.Join(", ", typeModel.AppliedAttributes.Select(learnRegistry.FormatAttributeItem));
        var adjustedAttributes = AdjustAssemblyIndexLinks(rawAttributes, typeModel.AssemblyName, isAssemblySubdirectoryIndex);
        var escapedAttributes = EscapeMarkdownTableCell(adjustedAttributes);
        var attributesSection = $"**[{MarkdownGenerationResources.AttributesHeaderLabel}]({learnRegistry.AttributesDocUrl}):** {escapedAttributes}";

        return string.IsNullOrEmpty(descriptionCell)
            ? attributesSection
            : $"{descriptionCell}<br/>{attributesSection}";
    }

    private string AdjustAssemblyIndexLinks(string content, string? assemblyName, bool isAssemblySubdirectoryIndex) =>
        !string.IsNullOrWhiteSpace(assemblyName) &&
        (isAssemblySubdirectoryIndex || content.Contains($"]({assemblyName}/", StringComparison.Ordinal))
            ? rewriter.AdjustLocalLinksForTypeDirectory(content, assemblyName.Trim())
            : content;

    private static string EscapeMarkdownTableCell(string cellContent) =>
        cellContent.Replace("|", "\\|", StringComparison.Ordinal);

    private static void AppendAssemblyDirectoryLinks(
        StringBuilder outputBuilder,
        List<TypeDocumentationModel> materializedTypes)
    {
        var distinctAssemblies = materializedTypes
            .Select(typeModel => typeModel.AssemblyName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (distinctAssemblies.Count == 0)
        {
            return;
        }

        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.AssemblyLabel}");
        _ = outputBuilder.AppendLine();
        foreach (var assemblyName in distinctAssemblies)
        {
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"* [{assemblyName}]({assemblyName}/index.md)");
        }

        _ = outputBuilder.AppendLine();
    }

    private void AppendIndexArchitectureDiagram(
        StringBuilder outputBuilder,
        List<TypeDocumentationModel> materializedTypes,
        bool includeAssemblyDirectory,
        DiagramFormat diagramFormat)
    {
        if (diagramFormat == DiagramFormat.None || materializedTypes.Count == 0)
        {
            return;
        }

        var selectedGenerator = diagramSelector.SelectGenerator(
            diagramGenerators,
            diagramFormat,
            generator => generator.CanGenerate(materializedTypes, includeAssemblyDirectory));
        if (selectedGenerator == null)
        {
            return;
        }

        var diagramBlock = selectedGenerator.GenerateEmbeddedMarkdown(materializedTypes, includeAssemblyDirectory);
        if (string.IsNullOrWhiteSpace(diagramBlock))
        {
            return;
        }

        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.ArchitectureDiagramHeading}");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.Append(diagramBlock);
    }

    private static void AppendAssemblyReferencesTable(
        StringBuilder outputBuilder,
        string? currentAssemblyName,
        bool isAssemblySubdirectoryIndex,
        IReadOnlyList<AssemblyReferenceModel>? assemblyReferences,
        IReadOnlySet<string>? analyzedAssemblyNames)
    {
        if (assemblyReferences == null || assemblyReferences.Count == 0)
        {
            return;
        }

        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.AssemblyReferencesHeading}");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableReferenceHeader} | {MarkdownGenerationResources.TableKindHeader} | {MarkdownGenerationResources.TableVersionHeader} | {MarkdownGenerationResources.TableVersionLinkingHeader} | {MarkdownGenerationResources.TableStrongNamedHeader} |");
        _ = outputBuilder.AppendLine("| :--- | :--- | :--- | :--- | :--- |");

        foreach (var referenceModel in assemblyReferences)
        {
            var referenceCell = FormatReferenceCell(referenceModel, currentAssemblyName, isAssemblySubdirectoryIndex, analyzedAssemblyNames);
            var kindCell = FormatReferenceKindCell(referenceModel.ReferenceKind);
            var versionCell = FormatReferenceVersionCell(referenceModel);
            var linkingCell = FormatVersionLinkingCell(referenceModel);
            var strongNamedCell = FormatStrongNamedCell(referenceModel);

            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| {referenceCell} | {kindCell} | {versionCell} | {linkingCell} | {strongNamedCell} |");
        }

        _ = outputBuilder.AppendLine();
    }

    private void AppendAssemblyDependencyTreeDiagram(
        StringBuilder outputBuilder,
        string? currentAssemblyName,
        DiagramFormat diagramFormat,
        IReadOnlyList<AssemblyDependencyEdgeModel>? assemblyDependencyEdges)
    {
        if (diagramFormat == DiagramFormat.None ||
            string.IsNullOrWhiteSpace(currentAssemblyName) ||
            assemblyDependencyEdges == null ||
            assemblyDependencyEdges.Count == 0)
        {
            return;
        }

        var selectedGenerator = diagramSelector.SelectGenerator(
            dependencyDiagramGenerators,
            diagramFormat,
            generator => generator.CanGenerate(currentAssemblyName, assemblyDependencyEdges));
        if (selectedGenerator == null)
        {
            return;
        }

        var diagramBlock = selectedGenerator.GenerateDependencyTreeMarkdown(currentAssemblyName, assemblyDependencyEdges);
        if (string.IsNullOrWhiteSpace(diagramBlock))
        {
            return;
        }

        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.AssemblyDependencyTreeHeading}");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.Append(diagramBlock);
    }

    private static string FormatReferenceCell(
        AssemblyReferenceModel referenceModel,
        string? currentAssemblyName,
        bool isAssemblySubdirectoryIndex,
        IReadOnlySet<string>? analyzedAssemblyNames) =>
        isAssemblySubdirectoryIndex &&
        analyzedAssemblyNames != null &&
        analyzedAssemblyNames.Contains(referenceModel.ReferenceName) &&
        !string.Equals(referenceModel.ReferenceName, currentAssemblyName, StringComparison.OrdinalIgnoreCase)
            ? $"[{referenceModel.ReferenceName}](../{referenceModel.ReferenceName}/index.md)"
            : $"`{referenceModel.ReferenceName}`";

    private static string FormatReferenceKindCell(AssemblyReferenceKind referenceKind) =>
        referenceKind switch
        {
            AssemblyReferenceKind.Project => MarkdownGenerationResources.ReferenceKindProject,
            AssemblyReferenceKind.Package => MarkdownGenerationResources.ReferenceKindPackage,
            AssemblyReferenceKind.Assembly => MarkdownGenerationResources.ReferenceKindAssembly,
            AssemblyReferenceKind.Framework => MarkdownGenerationResources.ReferenceKindFramework,
            _ => MarkdownGenerationResources.ReferenceKindAssembly
        };

    private static string FormatReferenceVersionCell(AssemblyReferenceModel referenceModel)
    {
        var hasPackageVersion = !string.IsNullOrWhiteSpace(referenceModel.PackageVersion);
        var hasAssemblyVersion = !string.IsNullOrWhiteSpace(referenceModel.AssemblyVersion);

        return hasPackageVersion && hasAssemblyVersion &&
               !string.Equals(referenceModel.PackageVersion, referenceModel.AssemblyVersion, StringComparison.OrdinalIgnoreCase)
            ? $"`{referenceModel.PackageVersion}` (`{referenceModel.AssemblyVersion}`)"
            : hasPackageVersion
                ? $"`{referenceModel.PackageVersion}`"
                : hasAssemblyVersion ? $"`{referenceModel.AssemblyVersion}`" : "`-`";
    }

    private static string FormatVersionLinkingCell(AssemblyReferenceModel referenceModel)
    {
        var kindLabel = referenceModel.VersionLinkingKind switch
        {
            AssemblyVersionLinkingKind.Compiled => MarkdownGenerationResources.VersionLinkingCompiled,
            AssemblyVersionLinkingKind.ProjectLinked => MarkdownGenerationResources.VersionLinkingProjectLinked,
            AssemblyVersionLinkingKind.Fixed => MarkdownGenerationResources.VersionLinkingFixed,
            AssemblyVersionLinkingKind.Minimum => MarkdownGenerationResources.VersionLinkingMinimum,
            AssemblyVersionLinkingKind.Range => MarkdownGenerationResources.VersionLinkingRange,
            AssemblyVersionLinkingKind.Floating => MarkdownGenerationResources.VersionLinkingFloating,
            AssemblyVersionLinkingKind.Framework => MarkdownGenerationResources.VersionLinkingFramework,
            _ => MarkdownGenerationResources.VersionLinkingCompiled
        };

        return string.IsNullOrWhiteSpace(referenceModel.VersionConstraintExpression) ||
               referenceModel.VersionLinkingKind == AssemblyVersionLinkingKind.ProjectLinked
            ? kindLabel
            : $"{kindLabel} (`{referenceModel.VersionConstraintExpression}`)";
    }

    private static string FormatStrongNamedCell(AssemblyReferenceModel referenceModel) =>
        !referenceModel.IsStrongNamed
            ? MarkdownGenerationResources.BooleanNo
            : string.IsNullOrWhiteSpace(referenceModel.PublicKeyToken)
                ? MarkdownGenerationResources.BooleanYes
                : $"{MarkdownGenerationResources.BooleanYes} (`{referenceModel.PublicKeyToken}`)";
}
