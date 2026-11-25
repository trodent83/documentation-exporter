using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Diagrams;

/// <summary>
/// Normalizes type names, Markdown links, relationship graphs, and display labels for diagram generator plugins.
/// </summary>
public interface IDiagramIdentifierFormatter
{
    /// <summary>
    /// Gets the localized legend caption displayed beneath rendered assembly dependency tree diagrams.
    /// </summary>
    string DependencyTreeLegend { get; }

    /// <summary>
    /// Strips unsupported characters from a raw type or namespace identifier so it forms a valid diagram node identifier.
    /// </summary>
    /// <param name="rawName">The raw type or namespace name.</param>
    /// <returns>Sanitized identifier composed solely of letters, digits, and underscores.</returns>
    string SanitizeIdentifier(string rawName);

    /// <summary>
    /// Extracts the simple sanitized node identifier from a Markdown link, qualified name, or generic signature.
    /// </summary>
    /// <param name="rawEntry">The raw type entry string.</param>
    /// <returns>The sanitized simple node identifier.</returns>
    string ExtractSimpleTypeIdentifier(string rawEntry);

    /// <summary>
    /// Extracts the human-readable type display label from a Markdown link or raw type name.
    /// </summary>
    /// <param name="rawTypeLink">The Markdown type link or plain type name.</param>
    /// <returns>The clean display label.</returns>
    string ExtractDisplayTypeLabel(string? rawTypeLink);

    /// <summary>
    /// Extracts normalized base-class, implemented-interface, and derived-type node identifiers for a single type diagram.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <returns>A tuple containing distinct base type, implemented interface, and derived type identifiers.</returns>
    (IReadOnlyList<string> BaseNames, IReadOnlyList<string> InterfaceNames, IReadOnlyList<string> DerivedNames) ExtractTypeDiagramRelationships(
        TypeDocumentationModel typeModel);

    /// <summary>
    /// Collects sorted inheritance (<c>&lt;|--</c>) and interface realization (<c>&lt;|..</c>) relation edges across a collection of types.
    /// </summary>
    /// <param name="materializedTypes">The collection of type documentation models.</param>
    /// <returns>Sorted relationship edge lines.</returns>
    IReadOnlyList<string> CollectArchitectureRelationships(IReadOnlyList<TypeDocumentationModel> materializedTypes);

    /// <summary>
    /// Formats a human-readable edge label describing the version linking policy and constraint for a dependency edge.
    /// </summary>
    /// <param name="dependencyEdge">The dependency edge metadata model.</param>
    /// <returns>Sanitized edge label text safe for diagram quoted edge labels.</returns>
    string FormatDependencyEdgeLabel(AssemblyDependencyEdgeModel dependencyEdge);

    /// <summary>
    /// Builds deterministic, collision-free diagram node identifiers for a root assembly and all nodes referenced by <paramref name="dependencyEdges"/>.
    /// </summary>
    /// <param name="rootAssemblyName">The root assembly name anchored at the start of the node list.</param>
    /// <param name="dependencyEdges">The dependency edges referencing source and target assemblies.</param>
    /// <returns>
    /// Ordered node tuples (<c>(NodeName, NodeIdentifier)</c>) and a case-insensitive lookup mapping node names to identifiers.
    /// </returns>
    (IReadOnlyList<(string NodeName, string NodeIdentifier)> OrderedNodes, IReadOnlyDictionary<string, string> IdentifierByNodeName) BuildUniqueNodeIdentifiers(
        string rootAssemblyName,
        IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges);
}
