using System.Reflection;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Extracts referenced assembly metadata (kind, version linking policy, and strong-name tokens)
/// by combining reflection <see cref="Assembly.GetReferencedAssemblies"/> with sibling <c>.deps.json</c>
/// and optional source <c>.csproj</c> project metadata.
/// </summary>
public interface IAssemblyReferenceAnalyzer
{
    /// <summary>
    /// Analyzes references for each loaded assembly and returns them keyed by assembly simple name.
    /// </summary>
    /// <param name="loadedAssemblies">The loaded target assemblies to inspect.</param>
    /// <param name="projectFilePathsByAssemblyPath">
    /// Optional lookup mapping compiled assembly <c>.dll</c> file paths to their originating <c>.csproj</c> file paths.
    /// </param>
    /// <returns>
    /// A dictionary mapping each assembly's simple name to its ordered list of <see cref="AssemblyReferenceModel"/> entries.
    /// </returns>
    IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> AnalyzeAssemblyReferences(
        IEnumerable<Assembly> loadedAssemblies,
        IReadOnlyDictionary<string, string>? projectFilePathsByAssemblyPath = null);

    /// <summary>
    /// Builds transitive non-framework dependency trees keyed by root assembly simple name by combining
    /// analyzed direct references across solution assemblies with <c>.deps.json</c> dependency graphs.
    /// </summary>
    /// <param name="loadedAssemblies">The loaded target assemblies to inspect.</param>
    /// <param name="directReferencesByAssemblyName">Previously analyzed direct references keyed by assembly simple name.</param>
    /// <param name="projectFilePathsByAssemblyPath">
    /// Optional lookup mapping compiled assembly <c>.dll</c> file paths to their originating <c>.csproj</c> file paths.
    /// </param>
    /// <returns>
    /// A dictionary mapping each loaded assembly's simple name to its cycle-free, ordered list of non-framework
    /// <see cref="AssemblyDependencyEdgeModel"/> edges.
    /// </returns>
    IReadOnlyDictionary<string, IReadOnlyList<AssemblyDependencyEdgeModel>> AnalyzeAssemblyDependencyTrees(
        IEnumerable<Assembly> loadedAssemblies,
        IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> directReferencesByAssemblyName,
        IReadOnlyDictionary<string, string>? projectFilePathsByAssemblyPath = null);
}
