using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders member overview tables (Constructors, Properties, Methods, Extension Methods, Fields, Events,
/// and inline Enum value tables) in a type's <c>index.md</c> page.
/// </summary>
/// <param name="memberFileNameFormatter">The member detail filename and title formatter.</param>
/// <param name="referenceRegistry">The canonical Microsoft Learn reference URL registry.</param>
public sealed class MembersSectionRenderer(
    IMemberFileNameFormatter memberFileNameFormatter,
    IMicrosoftLearnReferenceRegistry referenceRegistry) : IMarkdownSectionRenderer
{
    private readonly IMemberFileNameFormatter fileNameFormatter = ArgumentGuard.EnsureNotNull(memberFileNameFormatter);
    private readonly IMicrosoftLearnReferenceRegistry learnRegistry = ArgumentGuard.EnsureNotNull(referenceRegistry);

    /// <inheritdoc />
    public int Order => 60;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        return typeModel.TypeCategory == TypeCategory.Enum
            ? string.Concat(
                FormatEnumValuesTable(MarkdownGenerationResources.ValuesHeading, typeModel.Fields),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.ExtensionMethodsHeading, MarkdownGenerationResources.TableReturnTypeHeader, typeModel.ExtensionMethods),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.ExtensionPropertiesHeading, MarkdownGenerationResources.TableTypeHeader, typeModel.ExtensionProperties))
            : string.Concat(
                FormatConstructorOverviewTable(MarkdownGenerationResources.ConstructorsHeading, typeModel.Constructors),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.PropertiesHeading, MarkdownGenerationResources.TableTypeHeader, typeModel.Properties),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.MethodsHeading, MarkdownGenerationResources.TableReturnTypeHeader, typeModel.Methods),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.ExtensionMethodsHeading, MarkdownGenerationResources.TableReturnTypeHeader, typeModel.ExtensionMethods),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.ExtensionPropertiesHeading, MarkdownGenerationResources.TableTypeHeader, typeModel.ExtensionProperties),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.FieldsHeading, MarkdownGenerationResources.TableTypeHeader, typeModel.Fields),
                FormatTypedMemberOverviewTable(MarkdownGenerationResources.EventsHeading, MarkdownGenerationResources.TableTypeHeader, typeModel.Events));
    }

    private string FormatConstructorOverviewTable(
        string groupTitle,
        IReadOnlyList<MemberDocumentationModel> constructors)
    {
        if (constructors.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder tableBuilder = new();
        _ = tableBuilder.AppendLine("---");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {groupTitle}");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableNameHeader} | {MarkdownGenerationResources.TableVisibilityHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
        _ = tableBuilder.AppendLine("| :--- | :--- | :--- |");

        var isOverloaded = constructors.Count > 1;
        foreach (var constructorModel in constructors)
        {
            var displayHeader = fileNameFormatter.FormatMemberHeaderTitle(constructorModel, isOverloaded: true);
            var detailFileName = fileNameFormatter.GetMemberDetailFileName(constructorModel, isOverloaded, isConstructor: true);
            var visibility = learnRegistry.FormatMemberVisibility(constructorModel);
            var visibilityCell = constructorModel.IsPrimaryConstructor
                ? $"{visibility} ([{MarkdownGenerationResources.PrimaryConstructorLabel}]({learnRegistry.PrimaryConstructorsDocUrl}))"
                : visibility;
            var summary = FormatMemberOverviewDescription(constructorModel);

            _ = tableBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| [{displayHeader}]({detailFileName}) | {visibilityCell} | {summary} |");
        }

        _ = tableBuilder.AppendLine();
        return tableBuilder.ToString();
    }

    private string FormatEnumValuesTable(
        string groupTitle,
        IReadOnlyList<MemberDocumentationModel> enumValues)
    {
        if (enumValues.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder tableBuilder = new();
        _ = tableBuilder.AppendLine("---");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {groupTitle}");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableNameHeader} | {MarkdownGenerationResources.TableValueHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
        _ = tableBuilder.AppendLine("| :--- | :--- | :--- |");

        foreach (var enumValueModel in enumValues)
        {
            var numericValue = string.IsNullOrWhiteSpace(enumValueModel.ReturnValueDescription)
                ? "0"
                : enumValueModel.ReturnValueDescription.Trim();
            var description = FormatMemberOverviewDescription(enumValueModel);

            _ = tableBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{enumValueModel.MemberName}` | `{numericValue}` | {description} |");
        }

        _ = tableBuilder.AppendLine();

        foreach (var enumValueModel in enumValues.Where(value =>
                     !string.IsNullOrWhiteSpace(value.RemarksDescription) || value.CodeExamples.Count > 0))
        {
            _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {enumValueModel.MemberName}");
            _ = tableBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(enumValueModel.RemarksDescription))
            {
                _ = tableBuilder.AppendLine(enumValueModel.RemarksDescription);
                _ = tableBuilder.AppendLine();
            }

            foreach (var example in enumValueModel.CodeExamples)
            {
                _ = tableBuilder.AppendLine(example);
                _ = tableBuilder.AppendLine();
            }
        }

        return tableBuilder.ToString();
    }

    private string FormatTypedMemberOverviewTable(
        string groupTitle,
        string typeColumnHeader,
        IReadOnlyList<MemberDocumentationModel> members)
    {
        if (members.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder tableBuilder = new();
        _ = tableBuilder.AppendLine("---");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {groupTitle}");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableNameHeader} | {typeColumnHeader} | {MarkdownGenerationResources.TableVisibilityHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
        _ = tableBuilder.AppendLine("| :--- | :--- | :--- | :--- |");

        foreach (var memberModel in members)
        {
            var isOverloaded = members.Count(candidate => candidate.MemberName == memberModel.MemberName) > 1;
            var displayHeader = fileNameFormatter.FormatMemberHeaderTitle(memberModel, isOverloaded);
            var detailFileName = !string.IsNullOrWhiteSpace(memberModel.ResolvedMemberDetailUrl)
                ? memberModel.ResolvedMemberDetailUrl
                : fileNameFormatter.GetMemberDetailFileName(memberModel, isOverloaded, isConstructor: false);
            var returnTypeCell = FormatReturnTypeCell(memberModel);
            var visibility = learnRegistry.FormatMemberVisibility(memberModel);
            var summary = FormatMemberOverviewDescription(memberModel);

            _ = tableBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| [{displayHeader}]({detailFileName}) | {returnTypeCell} | {visibility} | {summary} |");
        }

        _ = tableBuilder.AppendLine();
        return tableBuilder.ToString();
    }

    private static string FormatReturnTypeCell(MemberDocumentationModel memberModel) =>
        !string.IsNullOrWhiteSpace(memberModel.ResolvedReturnTypeDocumentationLink)
            ? memberModel.ResolvedReturnTypeDocumentationLink
            : !string.IsNullOrWhiteSpace(memberModel.ReturnTypeName)
                ? $"`{memberModel.ReturnTypeName}`"
                : "`void`";

    private string FormatMemberOverviewDescription(MemberDocumentationModel memberModel)
    {
        var cleanSummary = FormatSingleLineSummary(memberModel.SummaryDescription);
        var hasEnumeratorCancellation = memberModel.MemberParameters.Any(parameter => parameter.IsEnumeratorCancellation);
        var hasCancellationToken = memberModel.MemberParameters.Any(parameter => parameter.IsCancellationToken);

        var cancellationSuffix = hasEnumeratorCancellation
            ? $" *([[{MarkdownGenerationResources.EnumeratorCancellationBadge}]]({learnRegistry.EnumeratorCancellationDocUrl}))*"
            : hasCancellationToken
                ? $" *([{MarkdownGenerationResources.AsyncCancellationBadge}]({learnRegistry.CancellationDocUrl}))*"
                : string.Empty;

        var summaryWithCancellation = string.IsNullOrEmpty(cleanSummary)
            ? cancellationSuffix.TrimStart()
            : $"{cleanSummary}{cancellationSuffix}";

        var statusBanner = BuildStatusSummaryBanner(memberModel);
        var baseDescription = string.IsNullOrEmpty(summaryWithCancellation)
            ? statusBanner
            : string.IsNullOrEmpty(statusBanner)
                ? summaryWithCancellation
                : $"{statusBanner} \u2014 {summaryWithCancellation}";

        if (memberModel.AppliedAttributes.Count == 0)
        {
            return baseDescription;
        }

        var formattedAttributes = string.Join(
            ", ",
            memberModel.AppliedAttributes.Select(attribute => EscapeMarkdownTableCell(learnRegistry.FormatAttributeItem(attribute))));
        var attributesSuffix = $"**[{MarkdownGenerationResources.AttributesHeaderLabel}]({learnRegistry.AttributesDocUrl}):** {formattedAttributes}";

        return string.IsNullOrEmpty(baseDescription)
            ? attributesSuffix
            : $"{baseDescription}<br/>{attributesSuffix}";
    }

    private string BuildStatusSummaryBanner(MemberDocumentationModel memberModel)
    {
        List<string> badges = [];
        if (memberModel.IsObsolete)
        {
            var obsoleteDiagnosticBadge = learnRegistry.FormatDiagnosticBadge(memberModel.ObsoleteDiagnosticId, memberModel.ObsoleteUrlFormat);
            var obsoleteBadge = $"**[{MarkdownGenerationResources.ObsoleteWarningTitle}]({learnRegistry.ObsoleteAttributeDocUrl}){obsoleteDiagnosticBadge}:**";
            var obsoleteDetail = string.IsNullOrWhiteSpace(memberModel.ObsoleteMessage)
                ? MarkdownGenerationResources.ObsoleteDefaultMessage
                : FormatSingleLineSummary(memberModel.ObsoleteMessage);
            badges.Add($"{obsoleteBadge} {obsoleteDetail}");
        }

        if (memberModel.IsExperimental)
        {
            var diagnosticSuffix = string.IsNullOrWhiteSpace(memberModel.ExperimentalDiagnosticId)
                ? string.Empty
                : $" `{memberModel.ExperimentalDiagnosticId.Trim()}`";
            badges.Add($"**[{MarkdownGenerationResources.ExperimentalWarningTitle}]({learnRegistry.ExperimentalAttributeDocUrl}):**{diagnosticSuffix}");
        }

        return string.Join(" \u2014 ", badges);
    }

    private static string EscapeMarkdownTableCell(string cellContent) =>
        cellContent.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatSingleLineSummary(string rawSummary) =>
        string.IsNullOrWhiteSpace(rawSummary)
            ? string.Empty
            : rawSummary.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
}
