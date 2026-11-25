using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the type remarks description section.
/// </summary>
public sealed class RemarksSectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 70;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (string.IsNullOrWhiteSpace(typeModel.RemarksDescription))
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.RemarksHeading}");
        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.AppendLine(typeModel.RemarksDescription);
        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
