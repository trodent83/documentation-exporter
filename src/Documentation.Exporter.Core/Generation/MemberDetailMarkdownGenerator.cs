using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Default implementation of <see cref="IMemberDetailMarkdownGenerator"/>.
/// </summary>
/// <param name="memberFileNameFormatter">The member detail file name and title formatter.</param>
/// <param name="linkRewriter">The relative link rewriter and Markdown normalizer.</param>
/// <param name="exceptionTableFormatter">The documented exceptions table formatter.</param>
/// <param name="referenceRegistry">The canonical Microsoft Learn reference URL registry.</param>
public sealed class MemberDetailMarkdownGenerator(
    IMemberFileNameFormatter memberFileNameFormatter,
    IRelativeLinkRewriter linkRewriter,
    IExceptionTableFormatter exceptionTableFormatter,
    IMicrosoftLearnReferenceRegistry referenceRegistry) : IMemberDetailMarkdownGenerator
{
    private readonly IMemberFileNameFormatter fileNameFormatter = ArgumentGuard.EnsureNotNull(memberFileNameFormatter);
    private readonly IRelativeLinkRewriter rewriter = ArgumentGuard.EnsureNotNull(linkRewriter);
    private readonly IExceptionTableFormatter exceptionFormatter = ArgumentGuard.EnsureNotNull(exceptionTableFormatter);
    private readonly IMicrosoftLearnReferenceRegistry learnRegistry = ArgumentGuard.EnsureNotNull(referenceRegistry);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GenerateMemberMarkdownFiles(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var typeDirectoryUrl = LocalProjectNamespaceHandler.GetTypeDirectoryUrlPath(typeModel, includeAssemblyDirectory);
        var unprefixedSelfUrl = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(typeModel, includeAssemblyDirectory: false);
        var assemblyPrefix = includeAssemblyDirectory && !string.IsNullOrWhiteSpace(typeModel.AssemblyName)
            ? typeModel.AssemblyName.Trim()
            : null;

        IEnumerable<KeyValuePair<string, string>> allMemberFiles =
        [
            .. BuildGroupMemberFiles(typeModel, typeModel.Constructors, isConstructorGroup: true, typeDirectoryUrl, assemblyPrefix, unprefixedSelfUrl),
            .. BuildGroupMemberFiles(typeModel, typeModel.Properties, isConstructorGroup: false, typeDirectoryUrl, assemblyPrefix, unprefixedSelfUrl),
            .. BuildGroupMemberFiles(typeModel, typeModel.Methods, isConstructorGroup: false, typeDirectoryUrl, assemblyPrefix, unprefixedSelfUrl),
            .. BuildGroupMemberFiles(typeModel, typeModel.Fields, isConstructorGroup: false, typeDirectoryUrl, assemblyPrefix, unprefixedSelfUrl),
            .. BuildGroupMemberFiles(typeModel, typeModel.Events, isConstructorGroup: false, typeDirectoryUrl, assemblyPrefix, unprefixedSelfUrl)
        ];

        Dictionary<string, string> memberFiles = [with(StringComparer.Ordinal)];
        foreach (var memberFilePair in allMemberFiles)
        {
            memberFiles[memberFilePair.Key] = memberFilePair.Value;
        }

        return memberFiles;
    }

    /// <inheritdoc />
    public string RenderMemberDetailMarkdown(
        TypeDocumentationModel declaringTypeModel,
        MemberDocumentationModel memberModel,
        bool isOverloaded)
    {
        ArgumentNullException.ThrowIfNull(declaringTypeModel);
        ArgumentNullException.ThrowIfNull(memberModel);

        StringBuilder outputBuilder = new();
        var displayHeader = fileNameFormatter.FormatMemberHeaderTitle(memberModel, isOverloaded);
        var declaringTypeUrl = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(declaringTypeModel);
        var namespaceTargetUrl = rewriter.BuildNamespaceSectionLinkTarget(declaringTypeModel.NamespaceName);

        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"# {declaringTypeModel.DisplayTypeName}.{displayHeader}");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{MarkdownGenerationResources.DeclaringTypeLabel}:** [{declaringTypeModel.DisplayTypeName}]({declaringTypeUrl})  ");
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{MarkdownGenerationResources.NamespaceLabel}:** [{declaringTypeModel.NamespaceName}]({namespaceTargetUrl})  ");
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{MarkdownGenerationResources.AssemblyLabel}:** [{declaringTypeModel.AssemblyName}](index.md)  ");
        _ = outputBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.TableVisibilityHeader}]({learnRegistry.AccessModifiersDocUrl}):** {learnRegistry.FormatMemberVisibility(memberModel)}  ");

        if (memberModel.IsPrimaryConstructor)
        {
            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.TableConstructorHeader}]({learnRegistry.ConstructorsDocUrl}):** [{MarkdownGenerationResources.PrimaryConstructorLabel}]({learnRegistry.PrimaryConstructorsDocUrl})  ");
        }

        if (memberModel.IsAsyncMethod)
        {
            var asyncDetails = memberModel.IsAsyncEnumerable
                ? $"[{MarkdownGenerationResources.BooleanYes}]({learnRegistry.AsyncKeywordDocUrl}) ([IAsyncEnumerable]({learnRegistry.AsyncEnumerableDocUrl}))"
                : $"[{MarkdownGenerationResources.BooleanYes}]({learnRegistry.AsyncKeywordDocUrl})";
            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.AsyncHeaderLabel}]({learnRegistry.AsyncProgrammingDocUrl}):** {asyncDetails}  ");
        }

        if (memberModel.AppliedAttributes.Count > 0)
        {
            var formattedAttributes = string.Join(", ", memberModel.AppliedAttributes.Select(learnRegistry.FormatAttributeItem));
            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.AttributesHeaderLabel}]({learnRegistry.AttributesDocUrl}):** {formattedAttributes}  ");
        }

        _ = outputBuilder.AppendLine();
        _ = outputBuilder.Append(learnRegistry.FormatObsoleteWarningBlock(
            memberModel.IsObsolete,
            memberModel.ObsoleteMessage,
            memberModel.IsObsoleteError,
            memberModel.ObsoleteDiagnosticId,
            memberModel.ObsoleteUrlFormat));
        _ = outputBuilder.Append(learnRegistry.FormatExperimentalWarningBlock(
            memberModel.IsExperimental,
            memberModel.ExperimentalDiagnosticId,
            memberModel.ExperimentalUrlFormat));

        if (!string.IsNullOrWhiteSpace(memberModel.SummaryDescription))
        {
            _ = outputBuilder.AppendLine(memberModel.SummaryDescription);
            _ = outputBuilder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(memberModel.MemberSignature))
        {
            _ = outputBuilder.AppendLine("```csharp");
            _ = outputBuilder.AppendLine(memberModel.MemberSignature);
            _ = outputBuilder.AppendLine("```");
            _ = outputBuilder.AppendLine();
        }

        _ = outputBuilder.Append(FormatGenericTypeParameters(memberModel.GenericTypeParameters));
        _ = outputBuilder.Append(FormatParameters(memberModel.MemberParameters));
        _ = outputBuilder.Append(FormatReturns(memberModel));
        _ = outputBuilder.Append(exceptionFormatter.FormatExceptionsTable(memberModel.DocumentedExceptions));
        _ = outputBuilder.Append(FormatInjectedSections(memberModel.InjectedDocumentationSections, SidecarAnchor.BeforeRemarks));
        _ = outputBuilder.Append(FormatRemarks(memberModel.RemarksDescription));
        _ = outputBuilder.Append(FormatInjectedSections(memberModel.InjectedDocumentationSections, SidecarAnchor.AfterRemarks));
        _ = outputBuilder.Append(FormatExamples(memberModel.CodeExamples));
        _ = outputBuilder.Append(FormatInjectedSections(memberModel.InjectedDocumentationSections, SidecarAnchor.AfterExamples));
        _ = outputBuilder.Append(FormatSeeAlso(memberModel.SeeAlsoReferences));
        _ = outputBuilder.Append(FormatInjectedSections(memberModel.InjectedDocumentationSections, SidecarAnchor.PageAppend));
        return outputBuilder.ToString();
    }

    private static string FormatInjectedSections(
        IReadOnlyList<DocumentationInjectionModel> injections,
        SidecarAnchor targetAnchor)
    {
        if (injections.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        foreach (var injection in injections.Where(candidate =>
                     candidate.TargetAnchor == targetAnchor &&
                     !string.IsNullOrWhiteSpace(candidate.CustomMarkdownContent)))
        {
            _ = sectionBuilder.AppendLine(injection.CustomMarkdownContent);
            _ = sectionBuilder.AppendLine();
        }

        return sectionBuilder.ToString();
    }

    private List<KeyValuePair<string, string>> BuildGroupMemberFiles(
        TypeDocumentationModel declaringTypeModel,
        IReadOnlyList<MemberDocumentationModel> members,
        bool isConstructorGroup,
        string typeDirectoryUrl,
        string? assemblyPrefix,
        string unprefixedSelfUrl) =>
    [
        .. members.Select(memberModel =>
        {
            var isOverloaded = isConstructorGroup
                ? members.Count > 1
                : members.Count(candidate => candidate.MemberName == memberModel.MemberName) > 1;

            var detailFileName = fileNameFormatter.GetMemberDetailFileName(memberModel, isOverloaded, isConstructorGroup);
            var rawMemberMarkdown = RenderMemberDetailMarkdown(declaringTypeModel, memberModel, isOverloaded || isConstructorGroup);
            var adjustedMarkdown = rewriter.AdjustLocalLinksForTypeDirectory(
                rawMemberMarkdown,
                typeDirectoryUrl,
                assemblyPrefix,
                unprefixedSelfUrl);
            return new KeyValuePair<string, string>(detailFileName, rewriter.NormalizeMarkdown(adjustedMarkdown));
        })
    ];

    private string FormatGenericTypeParameters(IReadOnlyList<ParameterDocumentationModel> genericTypeParameters)
    {
        if (genericTypeParameters.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"#### {MarkdownGenerationResources.TypeParametersHeading}");
        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableNameHeader} | {MarkdownGenerationResources.TableConstraintsHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
        _ = sectionBuilder.AppendLine("| :--- | :--- | :--- |");

        foreach (var typeParameter in genericTypeParameters)
        {
            var parameterDisplayName = string.IsNullOrWhiteSpace(typeParameter.ParameterModifier)
                ? typeParameter.ParameterName
                : $"{typeParameter.ParameterModifier} {typeParameter.ParameterName}";
            var constraintsCell = FormatTypeParameterConstraintsCell(typeParameter);
            var descriptionCell = FormatTableCellDescription(typeParameter.ParameterDescription);
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{parameterDisplayName}` | {constraintsCell} | {descriptionCell} |");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }

    private string FormatTypeParameterConstraintsCell(ParameterDocumentationModel typeParameter)
    {
        var constraintsCell = learnRegistry.FormatGenericConstraintsCell(typeParameter.ParameterConstraints);
        if (typeParameter.AppliedAttributes.Count == 0)
        {
            return constraintsCell;
        }

        var formattedAttributes = string.Join(
            ", ",
            typeParameter.AppliedAttributes.Select(attribute => EscapeMarkdownTableCell(learnRegistry.FormatAttributeItem(attribute))));

        return string.IsNullOrWhiteSpace(typeParameter.ParameterConstraints)
            ? formattedAttributes
            : $"{constraintsCell}, {formattedAttributes}";
    }

    private string FormatParameters(IReadOnlyList<ParameterDocumentationModel> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"#### {MarkdownGenerationResources.ParametersHeading}");
        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableNameHeader} | {MarkdownGenerationResources.TableTypeHeader} | {MarkdownGenerationResources.TableModifiersHeader} | {MarkdownGenerationResources.TableDefaultHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
        _ = sectionBuilder.AppendLine("| :--- | :--- | :--- | :--- | :--- |");

        foreach (var parameterModel in parameters)
        {
            var typeCell = !string.IsNullOrWhiteSpace(parameterModel.ResolvedTypeDocumentationLink)
                ? parameterModel.ResolvedTypeDocumentationLink
                : $"`{parameterModel.ParameterTypeName}`";
            var modifiersCell = FormatParameterModifiersCell(parameterModel);
            var defaultCell = FormatParameterDefaultValueCell(parameterModel);
            var descriptionCell = FormatParameterDescriptionCell(parameterModel);

            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{parameterModel.ParameterName}` | {typeCell} | {modifiersCell} | {defaultCell} | {descriptionCell} |");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }

    private string FormatParameterModifiersCell(ParameterDocumentationModel parameterModel)
    {
        List<string> modifiers = [];

        if (parameterModel.IsExtensionTarget)
        {
            modifiers.Add($"[this]({learnRegistry.ExtensionMethodsDocUrl})");
        }

        AppendByRefOrParamsModifier(modifiers, parameterModel);

        if (parameterModel.IsOptional)
        {
            modifiers.Add($"[{MarkdownGenerationResources.OptionalParameterBadge}]({learnRegistry.OptionalParametersDocUrl})");
        }

        if (parameterModel.IsNullable)
        {
            modifiers.Add($"[{MarkdownGenerationResources.NullableParameterBadge}]({learnRegistry.NullableReferencesDocUrl})");
        }

        if (!string.IsNullOrWhiteSpace(parameterModel.CallerInformationAttribute))
        {
            modifiers.Add($"[[{parameterModel.CallerInformationAttribute}]]({learnRegistry.CallerInformationDocUrl})");
        }

        if (parameterModel.IsEnumeratorCancellation)
        {
            modifiers.Add($"[[{MarkdownGenerationResources.EnumeratorCancellationBadge}]]({learnRegistry.EnumeratorCancellationDocUrl})");
        }
        else if (parameterModel.IsCancellationToken)
        {
            modifiers.Add($"[{MarkdownGenerationResources.AsyncCancellationBadge}]({learnRegistry.CancellationDocUrl})");
        }

        foreach (var appliedAttribute in parameterModel.AppliedAttributes)
        {
            modifiers.Add(EscapeMarkdownTableCell(learnRegistry.FormatAttributeItem(appliedAttribute)));
        }

        return modifiers.Count > 0
            ? string.Join(", ", modifiers)
            : $"`{MarkdownGenerationResources.NoConstraintsLabel}`";
    }

    private static string EscapeMarkdownTableCell(string cellContent) =>
        cellContent.Replace("|", "\\|", StringComparison.Ordinal);

    private void AppendByRefOrParamsModifier(List<string> modifiers, ParameterDocumentationModel parameterModel)
    {
        var modifierText = parameterModel.ParameterModifier?.Trim() ?? string.Empty;
        if (parameterModel.IsParams || string.Equals(modifierText, "params", StringComparison.OrdinalIgnoreCase))
        {
            modifiers.Add($"[params]({learnRegistry.ParamsModifierDocUrl})");
            return;
        }

        if (ContainsModifierToken(modifierText, "scoped"))
        {
            modifiers.Add($"[{MarkdownGenerationResources.ScopedModifierBadge}]({learnRegistry.ScopedModifierDocUrl})");
        }

        if (modifierText.Contains("ref readonly", StringComparison.Ordinal))
        {
            modifiers.Add($"[ref readonly]({learnRegistry.RefReadonlyModifierDocUrl})");
        }
        else if (ContainsModifierToken(modifierText, "out"))
        {
            modifiers.Add($"[out]({learnRegistry.OutModifierDocUrl})");
        }
        else if (ContainsModifierToken(modifierText, "in"))
        {
            modifiers.Add($"[in]({learnRegistry.InModifierDocUrl})");
        }
        else if (ContainsModifierToken(modifierText, "ref"))
        {
            modifiers.Add($"[ref]({learnRegistry.RefModifierDocUrl})");
        }
    }

    private static bool ContainsModifierToken(string modifierText, string expectedToken) =>
        modifierText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(expectedToken, StringComparer.Ordinal);

    private static string FormatParameterDefaultValueCell(ParameterDocumentationModel parameterModel) =>
        !string.IsNullOrWhiteSpace(parameterModel.DefaultValue)
            ? $"`{parameterModel.DefaultValue}`"
            : parameterModel.IsOptional
                ? "`default`"
                : "-";

    private string FormatParameterDescriptionCell(ParameterDocumentationModel parameterModel)
    {
        var baseDescription = FormatTableCellDescription(parameterModel.ParameterDescription);
        if (parameterModel.IsEnumeratorCancellation)
        {
            var badge = $"**[[{MarkdownGenerationResources.EnumeratorCancellationBadge}]]({learnRegistry.EnumeratorCancellationDocUrl}):**";
            return string.IsNullOrWhiteSpace(baseDescription) ? badge : $"{badge} {baseDescription}";
        }

        if (parameterModel.IsCancellationToken)
        {
            var badge = $"**[{MarkdownGenerationResources.AsyncCancellationBadge}]({learnRegistry.CancellationDocUrl}):**";
            return string.IsNullOrWhiteSpace(baseDescription) ? badge : $"{badge} {baseDescription}";
        }

        return baseDescription;
    }

    private static string FormatTableCellDescription(string? rawDescription) =>
        string.IsNullOrWhiteSpace(rawDescription)
            ? string.Empty
            : rawDescription
                .Replace("\r\n", "<br/>", StringComparison.Ordinal)
                .Replace("\n", "<br/>", StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("|", "\\|", StringComparison.Ordinal)
                .Trim();

    private string FormatReturns(MemberDocumentationModel memberModel)
    {
        if (string.IsNullOrWhiteSpace(memberModel.ReturnTypeName) &&
            string.IsNullOrWhiteSpace(memberModel.ReturnValueDescription) &&
            memberModel.ReturnAttributes.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"#### {MarkdownGenerationResources.ReturnsHeading}");
        _ = sectionBuilder.AppendLine();
        if (!string.IsNullOrWhiteSpace(memberModel.ResolvedReturnTypeDocumentationLink))
        {
            _ = sectionBuilder.AppendLine(memberModel.ResolvedReturnTypeDocumentationLink);
        }
        else if (!string.IsNullOrWhiteSpace(memberModel.ReturnTypeName))
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"`{memberModel.ReturnTypeName}`");
        }

        if (memberModel.ReturnAttributes.Count > 0)
        {
            var formattedReturnAttributes = string.Join(", ", memberModel.ReturnAttributes.Select(learnRegistry.FormatAttributeItem));
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.AttributesHeaderLabel}]({learnRegistry.AttributesDocUrl}):** {formattedReturnAttributes}  ");
        }

        if (!string.IsNullOrWhiteSpace(memberModel.ReturnValueDescription))
        {
            _ = sectionBuilder.AppendLine(memberModel.ReturnValueDescription);
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }

    private static string FormatRemarks(string remarksDescription)
    {
        if (string.IsNullOrWhiteSpace(remarksDescription))
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.RemarksHeading}");
        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.AppendLine(remarksDescription);
        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }

    private static string FormatExamples(IReadOnlyList<string> examples)
    {
        if (examples.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"#### {MarkdownGenerationResources.ExamplesHeading}");
        _ = sectionBuilder.AppendLine();
        foreach (var example in examples)
        {
            _ = sectionBuilder.AppendLine(example);
            _ = sectionBuilder.AppendLine();
        }

        return sectionBuilder.ToString();
    }

    private static string FormatSeeAlso(IReadOnlyList<string> seeAlsoReferences)
    {
        if (seeAlsoReferences.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.SeeAlsoHeading}");
        _ = sectionBuilder.AppendLine();
        foreach (var seeAlsoRef in seeAlsoReferences)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"* {seeAlsoRef}");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
