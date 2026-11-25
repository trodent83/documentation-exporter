using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Generates Mermaid <c>flowchart LR</c> assembly dependency tree diagrams with colored links
/// distinguishing strong-named vs. unsigned dependencies and version-linking edge labels.
/// </summary>
/// <param name="identifierFormatter">The diagram identifier and edge label formatter.</param>
public sealed class MermaidAssemblyDependencyDiagramGenerator(IDiagramIdentifierFormatter identifierFormatter) : IAssemblyDependencyDiagramGenerator
{
    private readonly IDiagramIdentifierFormatter formatter = PluginArgumentGuard.EnsureNotNull(identifierFormatter);

    /// <inheritdoc />
    public DiagramFormat Format => DiagramFormat.Mermaid;

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
        _ = outputBuilder.AppendLine("```mermaid");
        _ = outputBuilder.AppendLine("flowchart LR");

        foreach (var (nodeName, nodeIdentifier) in orderedNodes)
        {
            var safeDisplayName = nodeName.Replace("\"", "'", StringComparison.Ordinal);
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"    {nodeIdentifier}[\"{safeDisplayName}\"]");
        }

        List<int> strongLinkIndices = [];
        List<int> unsignedLinkIndices = [];

        for (var linkIndex = 0; linkIndex < dependencyEdges.Count; linkIndex++)
        {
            var dependencyEdge = dependencyEdges[linkIndex];
            if (!identifierByNodeName.TryGetValue(dependencyEdge.SourceAssemblyName, out var sourceIdentifier) ||
                !identifierByNodeName.TryGetValue(dependencyEdge.TargetAssemblyName, out var targetIdentifier))
            {
                continue;
            }

            var edgeLabel = formatter.FormatDependencyEdgeLabel(dependencyEdge);
            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    {sourceIdentifier} -->|\"{edgeLabel}\"| {targetIdentifier}");

            if (dependencyEdge.IsTargetStrongNamed)
            {
                strongLinkIndices.Add(linkIndex);
            }
            else
            {
                unsignedLinkIndices.Add(linkIndex);
            }
        }

        if (strongLinkIndices.Count > 0)
        {
            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    linkStyle {string.Join(",", strongLinkIndices)} stroke:#2E7D32,stroke-width:2px");
        }

        if (unsignedLinkIndices.Count > 0)
        {
            _ = outputBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    linkStyle {string.Join(",", unsignedLinkIndices)} stroke:#757575,stroke-width:2px,stroke-dasharray:5 5");
        }

        _ = outputBuilder.AppendLine("```");
        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"*{formatter.DependencyTreeLegend}*");
        _ = outputBuilder.AppendLine();
        return outputBuilder.ToString();
    }
}
