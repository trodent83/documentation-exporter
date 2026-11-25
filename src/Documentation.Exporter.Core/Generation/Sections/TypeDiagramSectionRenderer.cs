using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders at most one inline type hierarchy and relationship diagram using DI-registered
/// <see cref="ITypeDiagramGenerator"/> strategies and <see cref="IDiagramGeneratorSelector"/>.
/// </summary>
/// <param name="diagramGenerators">The registered type diagram generators.</param>
/// <param name="diagramGeneratorSelector">The preferred-with-fallback diagram generator selector.</param>
public sealed class TypeDiagramSectionRenderer(
    IEnumerable<ITypeDiagramGenerator> diagramGenerators,
    IDiagramGeneratorSelector diagramGeneratorSelector) : IMarkdownSectionRenderer
{
    private readonly IReadOnlyList<ITypeDiagramGenerator> generators = [.. ArgumentGuard.EnsureNotNull(diagramGenerators)];

    private readonly IDiagramGeneratorSelector diagramSelector = ArgumentGuard.EnsureNotNull(diagramGeneratorSelector);

    /// <inheritdoc />
    public int Order => 45;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.DiagramFormat == DiagramFormat.None ||
            string.IsNullOrWhiteSpace(typeModel.TypeName))
        {
            return string.Empty;
        }

        var selectedGenerator = diagramSelector.SelectGenerator(
            generators,
            typeModel.DiagramFormat,
            generator => generator.CanGenerate(typeModel));
        if (selectedGenerator == null)
        {
            return string.Empty;
        }

        var diagramBlock = selectedGenerator.GenerateTypeDiagram(typeModel);
        if (string.IsNullOrWhiteSpace(diagramBlock))
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.TypeDiagramHeading}");
        _ = sectionBuilder.AppendLine();
        _ = sectionBuilder.Append(diagramBlock);

        return sectionBuilder.ToString();
    }
}
