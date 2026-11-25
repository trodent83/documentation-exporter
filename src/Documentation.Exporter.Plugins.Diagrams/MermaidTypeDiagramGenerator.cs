using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Generates Mermaid <c>classDiagram</c> blocks for single-type documentation pages.
/// </summary>
/// <param name="identifierFormatter">The diagram identifier formatter.</param>
public sealed class MermaidTypeDiagramGenerator(IDiagramIdentifierFormatter identifierFormatter) : ITypeDiagramGenerator
{
    private readonly IDiagramIdentifierFormatter formatter = PluginArgumentGuard.EnsureNotNull(identifierFormatter);

    /// <inheritdoc />
    public DiagramFormat Format => DiagramFormat.Mermaid;

    /// <inheritdoc />
    public bool CanGenerate(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum ||
            string.IsNullOrWhiteSpace(typeModel.TypeName))
        {
            return false;
        }

        var (baseNames, interfaceNames, derivedNames) = formatter.ExtractTypeDiagramRelationships(typeModel);
        return baseNames.Count > 0 || interfaceNames.Count > 0 || derivedNames.Count > 0;
    }

    /// <inheritdoc />
    public string GenerateTypeDiagram(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum ||
            string.IsNullOrWhiteSpace(typeModel.TypeName))
        {
            return string.Empty;
        }

        var (baseNames, interfaceNames, derivedNames) = formatter.ExtractTypeDiagramRelationships(typeModel);
        if (baseNames.Count == 0 && interfaceNames.Count == 0 && derivedNames.Count == 0)
        {
            return string.Empty;
        }

        var nodeNameSource = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
            ? typeModel.DirectorySegmentName
            : typeModel.TypeName;
        var currentIdentifier = formatter.SanitizeIdentifier(nodeNameSource);
        var isCurrentInterface = typeModel.TypeCategory == TypeCategory.Interface;

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine("```mermaid");
        _ = sectionBuilder.AppendLine("classDiagram");

        if (isCurrentInterface)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"    class {currentIdentifier} {{");
            _ = sectionBuilder.AppendLine("        <<interface>>");
            _ = sectionBuilder.AppendLine("    }");
        }

        for (var index = 0; index < baseNames.Count; index++)
        {
            var parentNode = baseNames[index];
            var childNode = index + 1 < baseNames.Count ? baseNames[index + 1] : currentIdentifier;
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"    {parentNode} <|-- {childNode}");
        }

        foreach (var interfaceNode in interfaceNames)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"    {interfaceNode} <|.. {currentIdentifier}");
        }

        var arrowToken = isCurrentInterface ? "<|.." : "<|--";
        foreach (var derivedNode in derivedNames)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"    {currentIdentifier} {arrowToken} {derivedNode}");
        }

        _ = sectionBuilder.AppendLine("```");
        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
