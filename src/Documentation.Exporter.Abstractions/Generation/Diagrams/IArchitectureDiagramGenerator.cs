using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Diagrams;

/// <summary>
/// Generates solution-level or assembly-level package architecture diagrams for index pages and optional standalone diagram files.
/// </summary>
public interface IArchitectureDiagramGenerator : IDiagramGenerator
{
    /// <summary>
    /// Gets the optional standalone file name to emit alongside <c>index.md</c> (for example <c>"architecture.puml"</c>),
    /// or <see langword="null"/> if this generator only produces embedded Markdown blocks.
    /// </summary>
    string? StandaloneFileName { get; }

    /// <summary>
    /// Determines whether this generator supports generating an architecture diagram for <paramref name="types"/>.
    /// </summary>
    /// <param name="types">The collection of type documentation models to visualize.</param>
    /// <param name="includeAssemblyDirectory">Whether types span multiple assembly directories.</param>
    /// <returns><see langword="true"/> if this generator can produce an architecture diagram; otherwise <see langword="false"/>.</returns>
    bool CanGenerate(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory);

    /// <summary>
    /// Generates an embedded Markdown code fence diagram block for <paramref name="types"/>.
    /// </summary>
    /// <param name="types">The collection of type documentation models to visualize.</param>
    /// <param name="includeAssemblyDirectory">Whether types span multiple assembly directories.</param>
    /// <returns>Fenced Markdown diagram block, or empty when no types can be plotted.</returns>
    string GenerateEmbeddedMarkdown(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory);

    /// <summary>
    /// Generates standalone diagram file contents for <paramref name="types"/>, or empty if standalone export is not applicable.
    /// </summary>
    /// <param name="types">The collection of type documentation models to visualize.</param>
    /// <param name="includeAssemblyDirectory">Whether to group namespaces inside outer assembly packages.</param>
    /// <returns>Standalone diagram source content.</returns>
    string GenerateStandaloneContent(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory);
}
