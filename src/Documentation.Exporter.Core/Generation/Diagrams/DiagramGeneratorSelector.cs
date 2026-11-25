using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Diagrams;

/// <summary>
/// Default implementation of <see cref="IDiagramGeneratorSelector"/> that prioritizes the preferred
/// <see cref="DiagramFormat"/> and falls back to the first supporting alternative generator when needed.
/// </summary>
public sealed class DiagramGeneratorSelector : IDiagramGeneratorSelector
{
    /// <inheritdoc />
    public TGenerator? SelectGenerator<TGenerator>(
        IEnumerable<TGenerator> generators,
        DiagramFormat preferredFormat,
        Func<TGenerator, bool> canGenerate)
        where TGenerator : class, IDiagramGenerator
    {
        ArgumentNullException.ThrowIfNull(generators);
        ArgumentNullException.ThrowIfNull(canGenerate);

        if (preferredFormat == DiagramFormat.None)
        {
            return null;
        }

        List<TGenerator> candidateGenerators = [.. generators];

        return candidateGenerators.FirstOrDefault(generator =>
                   generator.Format == preferredFormat && canGenerate(generator))
               ?? candidateGenerators.FirstOrDefault(generator =>
                   generator.Format != preferredFormat &&
                   generator.Format != DiagramFormat.None &&
                   canGenerate(generator));
    }
}
