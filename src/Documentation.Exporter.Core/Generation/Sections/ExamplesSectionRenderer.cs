using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders code examples for the type.
/// </summary>
public sealed class ExamplesSectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 90;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.CodeExamples.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.ExamplesHeading}");
        _ = sectionBuilder.AppendLine();
        foreach (var codeExample in typeModel.CodeExamples)
        {
            _ = sectionBuilder.AppendLine(codeExample);
            _ = sectionBuilder.AppendLine();
        }

        return sectionBuilder.ToString();
    }
}
