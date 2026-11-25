using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Generates PlantUML <c>@startuml ... @enduml</c> assembly dependency tree component diagrams
/// with colored links distinguishing strong-named vs. unsigned dependencies and version-linking labels.
/// </summary>
/// <param name="identifierFormatter">The diagram identifier and edge label formatter.</param>
public sealed class PlantUmlAssemblyDependencyDiagramGenerator(IDiagramIdentifierFormatter identifierFormatter) : IAssemblyDependencyDiagramGenerator
{
    private readonly IDiagramIdentifierFormatter formatter = PluginArgumentGuard.EnsureNotNull(identifierFormatter);

    /// <inheritdoc />
    public DiagramFormat Format => DiagramFormat.PlantUml;

    /// <inheritdoc />
    public bool CanGenerate(string rootAssemblyName, IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges)
    {
        ArgumentNullException.ThrowIfNull(dependencyEdges);

        return !string.IsNullOrWhiteSpace(rootAssemblyName) && dependencyEdges.Count > 0;
    }

    /// <inheritdoc />
    public string GenerateDependencyTreeMarkdown(string rootAssemblyName, IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges)
    {
        ArgumentNullException.ThrowIfNull(dependencyEdges);

        if (!CanGenerate(rootAssemblyName, dependencyEdges))
        {
            return string.Empty;
        }

        var (orderedNodes, identifierByNodeName) = formatter.BuildUniqueNodeIdentifiers(rootAssemblyName, dependencyEdges);
        StringBuilder outputBuilder = new();
        _ = outputBuilder.AppendLine("```plantuml");
        _ = outputBuilder.AppendLine("@startuml");
        _ = outputBuilder.AppendLine("left to right direction");
        _ = outputBuilder.AppendLine("skinparam componentStyle rectangle");

        foreach (var (nodeName, nodeIdentifier) in orderedNodes)
        {
            var safeDisplayName = nodeName.Replace("\"", "'", StringComparison.Ordinal);
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"component \"{safeDisplayName}\" as {nodeIdentifier}");
        }

        foreach (var dependencyEdge in dependencyEdges)
        {
            if (!identifierByNodeName.TryGetValue(dependencyEdge.SourceAssemblyName, out var sourceIdentifier) ||
                !identifierByNodeName.TryGetValue(dependencyEdge.TargetAssemblyName, out var targetIdentifier))
            {
                continue;
            }

            var edgeLabel = formatter.FormatDependencyEdgeLabel(dependencyEdge);
            var arrowStyle = dependencyEdge.IsTargetStrongNamed
                ? "-[#2E7D32,bold]->"
                : "-[#757575,dashed]->";

            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"{sourceIdentifier} {arrowStyle} {targetIdentifier} : \"{edgeLabel}\"");
        }

        _ = outputBuilder.AppendLine("@enduml");
        _ = outputBuilder.AppendLine("```");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"*{formatter.DependencyTreeLegend}*");
        _ = outputBuilder.AppendLine();
        return outputBuilder.ToString();
    }
}
