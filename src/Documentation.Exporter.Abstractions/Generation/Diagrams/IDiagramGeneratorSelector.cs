using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Diagrams;

/// <summary>
/// Selects at most one diagram generator strategy matching the configured preferred <see cref="DiagramFormat"/>,
/// falling back to another registered supporting generator only when the preferred generator does not support the diagram type.
/// </summary>
public interface IDiagramGeneratorSelector
{
    /// <summary>
    /// Selects a single generator from <paramref name="generators"/> using <paramref name="preferredFormat"/> and <paramref name="canGenerate"/>.
    /// </summary>
    /// <typeparam name="TGenerator">The diagram generator interface type.</typeparam>
    /// <param name="generators">The registered candidate generators.</param>
    /// <param name="preferredFormat">The preferred diagram format configured by the caller.</param>
    /// <param name="canGenerate">Predicate testing whether a candidate generator supports the target diagram input.</param>
    /// <returns>The selected generator, or <see langword="null"/> when diagrams are disabled or no candidate supports the diagram.</returns>
    TGenerator? SelectGenerator<TGenerator>(
        IEnumerable<TGenerator> generators,
        DiagramFormat preferredFormat,
        Func<TGenerator, bool> canGenerate)
        where TGenerator : class, IDiagramGenerator;
}
