namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Loads compiled .NET assemblies inside an isolated, collectible load context and extracts their exported types.
/// </summary>
public interface IAssemblyLoader
{
    /// <summary>
    /// Tries to load an assembly from disk and extract all exported public and protected types.
    /// </summary>
    /// <param name="assemblyFilePath">File path to the target assembly (<c>.dll</c>).</param>
    /// <param name="exportedTypesMap">Output map of type full names to reflected <see cref="Type"/> instances.</param>
    /// <returns><see langword="true"/> if the assembly was loaded successfully; otherwise <see langword="false"/>.</returns>
    bool TryLoadExportedTypes(string? assemblyFilePath, out IReadOnlyDictionary<string, Type>? exportedTypesMap);

    /// <summary>
    /// Tries to load multiple assemblies from disk and merge all exported public and protected types into a unified map.
    /// </summary>
    /// <param name="assemblyFilePaths">File paths to the target assemblies (<c>.dll</c>).</param>
    /// <param name="exportedTypesMap">Output map of type full names to reflected <see cref="Type"/> instances across all loaded assemblies.</param>
    /// <returns><see langword="true"/> if all specified assemblies were loaded successfully; otherwise <see langword="false"/>.</returns>
    bool TryLoadExportedTypes(IEnumerable<string> assemblyFilePaths, out IReadOnlyDictionary<string, Type>? exportedTypesMap);
}
