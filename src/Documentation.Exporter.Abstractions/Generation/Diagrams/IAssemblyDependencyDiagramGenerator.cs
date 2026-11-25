using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Diagrams;

/// <summary>
/// Generates embedded dependency tree diagram blocks for an assembly overview page
/// showing transitive non-framework dependencies, version linking policies, and strong-name distinction.
/// </summary>
public interface IAssemblyDependencyDiagramGenerator : IDiagramGenerator
{
    /// <summary>
    /// Determines whether this generator can produce a dependency tree diagram for <paramref name="rootAssemblyName"/>
    /// and <paramref name="dependencyEdges"/>.
    /// </summary>
    /// <param name="rootAssemblyName">The root assembly simple name.</param>
    /// <param name="dependencyEdges">The directed dependency edges originating from or transitively reachable from <paramref name="rootAssemblyName"/>.</param>
    /// <returns><see langword="true"/> if this generator can render a diagram; otherwise <see langword="false"/>.</returns>
    bool CanGenerate(string rootAssemblyName, IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges);

    /// <summary>
    /// Generates a fenced Markdown diagram block (and legend) visualizing the assembly dependency tree.
    /// </summary>
    /// <param name="rootAssemblyName">The root assembly simple name.</param>
    /// <param name="dependencyEdges">The directed dependency edges for the tree.</param>
    /// <returns>The rendered Markdown diagram section body, or empty string when not applicable.</returns>
    string GenerateDependencyTreeMarkdown(string rootAssemblyName, IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges);
}
