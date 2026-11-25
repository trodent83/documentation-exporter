using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Diagrams;

/// <summary>
/// Common abstraction for pluggable diagram generator strategies identified by <see cref="DiagramFormat"/>.
/// </summary>
public interface IDiagramGenerator
{
    /// <summary>
    /// Gets the diagram format produced by this generator.
    /// </summary>
    DiagramFormat Format { get; }
}
