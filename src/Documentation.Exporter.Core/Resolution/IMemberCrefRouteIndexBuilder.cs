namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Builds a lookup table mapping canonical XML documentation member keys (without the <c>X:</c> prefix)
/// to their Markdown display titles and relative detail page URLs across exported types.
/// </summary>
public interface IMemberCrefRouteIndexBuilder
{
    /// <summary>
    /// Indexes constructors, methods, operators, indexers, properties, fields, and events across <paramref name="exportedTypesMap"/>
    /// so cross-references to overloaded or multi-level nested members resolve to the exact generated Markdown route.
    /// </summary>
    /// <param name="exportedTypesMap">Map of runtime type full names to exported <see cref="Type"/> metadata.</param>
    /// <param name="useAssemblySubdirectories">Whether per-assembly subdirectories are enabled in output URLs.</param>
    /// <param name="excludeEditorBrowsableNever">Whether members decorated with <c>[EditorBrowsable(EditorBrowsableState.Never)]</c> are excluded from output and overload counts.</param>
    /// <returns>Dictionary keyed by prefixless canonical XML member key (<c>"Namespace.Type.Member(Params)"</c>).</returns>
    IReadOnlyDictionary<string, (string DisplayName, string RelativeUrl)> BuildMemberCrefRoutes(
        IReadOnlyDictionary<string, Type> exportedTypesMap,
        bool useAssemblySubdirectories,
        bool excludeEditorBrowsableNever = true);
}
