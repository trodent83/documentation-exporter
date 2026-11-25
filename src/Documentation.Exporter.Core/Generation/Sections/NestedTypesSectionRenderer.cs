using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the list of public nested types declared inside the current type.
/// </summary>
public sealed class NestedTypesSectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 41;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.NestedTypes.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.NestedTypesHeading}");
        _ = sectionBuilder.AppendLine();
        foreach (var nestedTypeEntry in typeModel.NestedTypes)
        {
            var formattedEntry = nestedTypeEntry.Contains("](", StringComparison.Ordinal)
                ? nestedTypeEntry
                : $"`{nestedTypeEntry}`";
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"* {formattedEntry}");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
