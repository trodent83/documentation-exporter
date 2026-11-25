using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Generates Mermaid package/class architecture diagrams for index pages.
/// </summary>
/// <param name="identifierFormatter">The diagram identifier formatter.</param>
public sealed class MermaidArchitectureDiagramGenerator(IDiagramIdentifierFormatter identifierFormatter) : IArchitectureDiagramGenerator
{
    private readonly IDiagramIdentifierFormatter formatter = PluginArgumentGuard.EnsureNotNull(identifierFormatter);

    /// <inheritdoc />
    public DiagramFormat Format => DiagramFormat.Mermaid;

    /// <inheritdoc />
    public string? StandaloneFileName => null;

    /// <inheritdoc />
    public bool CanGenerate(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory)
    {
        ArgumentNullException.ThrowIfNull(types);

        return types.Any(typeModel => !string.IsNullOrWhiteSpace(typeModel.TypeName));
    }

    /// <inheritdoc />
    public string GenerateEmbeddedMarkdown(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory)
    {
        ArgumentNullException.ThrowIfNull(types);

        var materializedTypes = types
            .Where(typeModel => !string.IsNullOrWhiteSpace(typeModel.TypeName))
            .ToList();

        if (materializedTypes.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder outputBuilder = new();
        _ = outputBuilder.AppendLine("```mermaid");
        _ = outputBuilder.AppendLine("classDiagram");

        foreach (var namespaceGroup in materializedTypes.GroupBy(typeModel => typeModel.NamespaceName).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var safeNamespace = formatter.SanitizeIdentifier(string.IsNullOrWhiteSpace(namespaceGroup.Key) ? "Global" : namespaceGroup.Key);
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"    namespace {safeNamespace} {{");
            foreach (var typeModel in namespaceGroup.OrderBy(typeModel => typeModel.DisplayTypeName, StringComparer.Ordinal))
            {
                var nodeNameSource = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
                    ? typeModel.DirectorySegmentName
                    : typeModel.TypeName;
                var safeType = formatter.SanitizeIdentifier(nodeNameSource);
                if (!string.IsNullOrEmpty(safeType))
                {
                    _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"        class {safeType}");
                }
            }

            _ = outputBuilder.AppendLine("    }");
        }

        foreach (var relationLine in formatter.CollectArchitectureRelationships(materializedTypes))
        {
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"    {relationLine}");
        }

        _ = outputBuilder.AppendLine("```");
        _ = outputBuilder.AppendLine();
        return outputBuilder.ToString();
    }

    /// <inheritdoc />
    public string GenerateStandaloneContent(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
        string.Empty;
}
