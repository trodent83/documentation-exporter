using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Generates central multi-assembly, per-assembly, and single-assembly <c>index.md</c> overview pages.
/// </summary>
public interface IIndexPageMarkdownGenerator
{
    /// <summary>
    /// Generates an index/overview Markdown document listing exported assemblies or namespaces and types,
    /// optionally embedding architecture diagrams and adjusting links for per-assembly subdirectories.
    /// </summary>
    /// <param name="allTypes">Collection of all type documentation models.</param>
    /// <param name="includeAssemblyDirectory">Whether to emit a central multi-assembly index linking into <c>&lt;AssemblyName&gt;/</c> subdirectories.</param>
    /// <param name="diagramFormat">The optional diagram format to render on the index page (defaults to <see cref="DiagramFormat.None"/> when omitted).</param>
    /// <param name="isAssemblySubdirectoryIndex">Whether this index page resides inside an <c>&lt;AssemblyName&gt;/</c> subdirectory of a multi-assembly export.</param>
    /// <param name="assemblyReferences">Optional analyzed assembly references rendered into an Assembly References table for assembly pages.</param>
    /// <param name="analyzedAssemblyNames">Optional set of exported assembly simple names whose sibling <c>../&lt;AssemblyName&gt;/index.md</c> pages exist for cross-linking.</param>
    /// <param name="assemblyDependencyEdges">Optional transitive non-framework assembly dependency edges rendered as a dependency tree diagram on assembly pages.</param>
    /// <param name="assemblyAttributes">Optional custom attributes applied at the assembly level rendered on assembly pages.</param>
    /// <returns>Normalized Markdown content for <c>index.md</c>.</returns>
    string GenerateIndexMarkdown(
        IEnumerable<TypeDocumentationModel> allTypes,
        bool includeAssemblyDirectory = false,
        DiagramFormat? diagramFormat = null,
        bool isAssemblySubdirectoryIndex = false,
        IReadOnlyList<AssemblyReferenceModel>? assemblyReferences = null,
        IReadOnlySet<string>? analyzedAssemblyNames = null,
        IReadOnlyList<AssemblyDependencyEdgeModel>? assemblyDependencyEdges = null,
        IReadOnlyList<string>? assemblyAttributes = null);
}
