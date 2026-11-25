using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the See Also cross-references list.
/// </summary>
public sealed class SeeAlsoSectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.SeeAlsoReferences.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.SeeAlsoHeading}");
        _ = sectionBuilder.AppendLine();
        foreach (var seeAlsoRef in typeModel.SeeAlsoReferences)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"* {seeAlsoRef}");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
