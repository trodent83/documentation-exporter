using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the list of derived classes or known interface implementations discovered across the loaded assemblies.
/// </summary>
public sealed class DerivedTypesSectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 42;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum ||
            typeModel.DerivedTypes.Count == 0)
        {
            return string.Empty;
        }

        var headingText = typeModel.TypeCategory == TypeCategory.Interface
            ? MarkdownGenerationResources.KnownImplementationsHeading
            : MarkdownGenerationResources.DerivedTypesHeading;

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {headingText}");
        _ = sectionBuilder.AppendLine();
        foreach (var derivedTypeEntry in typeModel.DerivedTypes)
        {
            var formattedEntry = derivedTypeEntry.Contains("](", StringComparison.Ordinal)
                ? derivedTypeEntry
                : $"`{derivedTypeEntry}`";
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"* {formattedEntry}");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
