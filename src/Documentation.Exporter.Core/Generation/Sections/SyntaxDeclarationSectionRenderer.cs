using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders a structured list of the type's modifiers and characteristics (Access, Constructor kind, Abstract, Sealed, Static, Generic) with links to Microsoft Learn documentation.
/// </summary>
/// <param name="referenceRegistry">The canonical Microsoft Learn reference URL registry.</param>
public sealed class SyntaxDeclarationSectionRenderer(IMicrosoftLearnReferenceRegistry referenceRegistry) : IMarkdownSectionRenderer
{
    private readonly IMicrosoftLearnReferenceRegistry learnRegistry = ArgumentGuard.EnsureNotNull(referenceRegistry);

    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (string.IsNullOrWhiteSpace(typeModel.CSharpDeclarationSignature) &&
            string.IsNullOrWhiteSpace(typeModel.TypeName))
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        if (!string.IsNullOrWhiteSpace(typeModel.CSharpDeclarationSignature))
        {
            _ = sectionBuilder.AppendLine("```csharp");
            _ = sectionBuilder.AppendLine(typeModel.CSharpDeclarationSignature);
            _ = sectionBuilder.AppendLine("```");
            _ = sectionBuilder.AppendLine();
        }

        var accessModifier = string.IsNullOrWhiteSpace(typeModel.AccessModifier) ? "public" : typeModel.AccessModifier.Trim();
        var formattedAccessValue = learnRegistry.FormatAccessModifierLink(accessModifier);

        if (typeModel.TypeCategory == TypeCategory.Enum)
        {
            var baseTypeDisplay = FormatEnumBaseTypeLink(typeModel);
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.TableAccessHeader}]({learnRegistry.AccessModifiersDocUrl}):** {formattedAccessValue}  ");
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.EnumBaseTypeHeader}]({learnRegistry.EnumBaseTypeDocUrl}):** {baseTypeDisplay}  ");
            AppendAttributesLine(sectionBuilder, typeModel.AppliedAttributes);
            _ = sectionBuilder.AppendLine();
            return sectionBuilder.ToString();
        }

        if (typeModel.TypeCategory == TypeCategory.Interface)
        {
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.TableAccessHeader}]({learnRegistry.AccessModifiersDocUrl}):** {formattedAccessValue}  ");

            AppendAttributesLine(sectionBuilder, typeModel.AppliedAttributes);

            _ = sectionBuilder.AppendLine();
            _ = sectionBuilder.Append(FormatGenericTypeParametersTable(typeModel));
            return sectionBuilder.ToString();
        }

        var rawConstructorKind = string.IsNullOrWhiteSpace(typeModel.ConstructorKind) ? "Default" : typeModel.ConstructorKind.Trim();
        var formattedConstructorValue = learnRegistry.FormatConstructorKindLink(rawConstructorKind);

        var abstractValue = typeModel.IsAbstractType ? MarkdownGenerationResources.BooleanYes : MarkdownGenerationResources.BooleanNo;
        var sealedValue = typeModel.IsSealedType ? MarkdownGenerationResources.BooleanYes : MarkdownGenerationResources.BooleanNo;
        var staticValue = typeModel.IsStaticType ? MarkdownGenerationResources.BooleanYes : MarkdownGenerationResources.BooleanNo;

        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.TableAccessHeader}]({learnRegistry.AccessModifiersDocUrl}):** {formattedAccessValue}  ");
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.TableConstructorHeader}]({learnRegistry.ConstructorsDocUrl}):** {formattedConstructorValue}  ");
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.TableAbstractHeader}]({learnRegistry.AbstractKeywordDocUrl}):** `{abstractValue}`  ");
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.TableSealedHeader}]({learnRegistry.SealedKeywordDocUrl}):** `{sealedValue}`  ");
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.TableStaticHeader}]({learnRegistry.StaticKeywordDocUrl}):** `{staticValue}`  ");

        if (typeModel.IsRecordType)
        {
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.TableRecordHeader}]({learnRegistry.RecordKeywordDocUrl}):** `{MarkdownGenerationResources.BooleanYes}`  ");
        }

        if (typeModel.IsReadOnlyType)
        {
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.TableReadOnlyHeader}]({learnRegistry.ReadOnlyKeywordDocUrl}):** `{MarkdownGenerationResources.BooleanYes}`  ");
        }

        if (typeModel.IsRefStructType)
        {
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"**[{MarkdownGenerationResources.TableRefStructHeader}]({learnRegistry.RefStructKeywordDocUrl}):** `{MarkdownGenerationResources.BooleanYes}`  ");
        }

        AppendAttributesLine(sectionBuilder, typeModel.AppliedAttributes);

        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.Append(FormatGenericTypeParametersTable(typeModel));
        return sectionBuilder.ToString();
    }

    private void AppendAttributesLine(StringBuilder sectionBuilder, IReadOnlyList<string> appliedAttributes)
    {
        if (appliedAttributes.Count == 0)
        {
            return;
        }

        var formattedAttributes = string.Join(", ", appliedAttributes.Select(learnRegistry.FormatAttributeItem));
        _ = sectionBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"**[{MarkdownGenerationResources.AttributesHeaderLabel}]({learnRegistry.AttributesDocUrl}):** {formattedAttributes}  ");
    }

    private string FormatGenericTypeParametersTable(TypeDocumentationModel typeModel)
    {
        if (!typeModel.IsGenericType || typeModel.GenericTypeParameterDetails.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder tableBuilder = new();
        _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"#### {MarkdownGenerationResources.TypeParametersHeading}");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"| {MarkdownGenerationResources.TableNameHeader} | {MarkdownGenerationResources.TableConstraintsHeader} | {MarkdownGenerationResources.TableDescriptionHeader} |");
        _ = tableBuilder.AppendLine("| :--- | :--- | :--- |");

        foreach (var typeParameter in typeModel.GenericTypeParameterDetails)
        {
            var parameterDisplayName = string.IsNullOrWhiteSpace(typeParameter.ParameterModifier)
                ? typeParameter.ParameterName
                : $"{typeParameter.ParameterModifier} {typeParameter.ParameterName}";
            var constraintsCell = FormatTypeParameterConstraintsCell(typeParameter);
            var descriptionCell = FormatTableCellDescription(typeParameter.ParameterDescription);
            _ = tableBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"| `{parameterDisplayName}` | {constraintsCell} | {descriptionCell} |");
        }

        _ = tableBuilder.AppendLine();
        return tableBuilder.ToString();
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

    private static string EscapeMarkdownTableCell(string cellContent) =>
        cellContent.Replace("|", "\\|", StringComparison.Ordinal);

    private string FormatEnumBaseTypeLink(TypeDocumentationModel typeModel) =>
        !string.IsNullOrWhiteSpace(typeModel.EnumUnderlyingTypeLink)
            ? typeModel.EnumUnderlyingTypeLink.Contains("](", StringComparison.Ordinal)
                ? typeModel.EnumUnderlyingTypeLink
                : learnRegistry.FormatEnumBaseTypeLink(typeModel.EnumUnderlyingTypeLink)
            : typeModel.Fields.Count > 0 && !string.IsNullOrWhiteSpace(typeModel.Fields[0].ResolvedReturnTypeDocumentationLink)
                ? typeModel.Fields[0].ResolvedReturnTypeDocumentationLink
                : $"[Int32]({learnRegistry.DefaultInt32DocUrl})";
}
