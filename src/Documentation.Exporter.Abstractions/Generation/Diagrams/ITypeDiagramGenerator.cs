using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Diagrams;

/// <summary>
/// Generates an embedded class/hierarchy diagram block for a single type page in a specific diagram syntax.
/// </summary>
public interface ITypeDiagramGenerator : IDiagramGenerator
{
    /// <summary>
    /// Determines whether this generator supports generating a diagram for <paramref name="typeModel"/>.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <returns><see langword="true"/> if this generator can produce a diagram for <paramref name="typeModel"/>; otherwise <see langword="false"/>.</returns>
    bool CanGenerate(TypeDocumentationModel typeModel);

    /// <summary>
    /// Renders a fenced diagram block (or empty string if nothing should be drawn) for <paramref name="typeModel"/>.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <returns>The rendered Markdown diagram block, or an empty string when not applicable.</returns>
    string GenerateTypeDiagram(TypeDocumentationModel typeModel);
}
