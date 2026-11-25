using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Discovers compiled assembly (<c>.dll</c>), compiler XML documentation (<c>.xml</c>), and originating
/// project (<c>.csproj</c>) paths from <see cref="ExporterOptions"/>.
/// </summary>
public interface IExportArtifactDiscoveryService
{
    /// <summary>
    /// Resolves all target assembly, XML documentation, and project mapping metadata in a single discovery pass.
    /// </summary>
    /// <param name="options">The exporter configuration options.</param>
    /// <returns>An <see cref="ExportArtifactDiscoveryResult"/> containing resolved paths and project associations.</returns>
    ExportArtifactDiscoveryResult DiscoverArtifactDetails(ExporterOptions options);

    /// <summary>
    /// Resolves all target assembly (<c>.dll</c>) and XML documentation (<c>.xml</c>) paths together in a single discovery pass.
    /// </summary>
    /// <param name="options">The exporter configuration options.</param>
    /// <returns>A tuple containing distinct resolved assembly file paths and XML documentation file paths.</returns>
    (IReadOnlyList<string> AssemblyFilePaths, IReadOnlyList<string> XmlDocumentationFilePaths) DiscoverArtifactPaths(ExporterOptions options);

    /// <summary>
    /// Resolves all target assembly (<c>.dll</c>) paths from the configured exporter options.
    /// </summary>
    /// <param name="options">The exporter configuration options.</param>
    /// <returns>The ordered list of distinct assembly file paths.</returns>
    IReadOnlyList<string> DiscoverAssemblyFilePaths(ExporterOptions options);

    /// <summary>
    /// Resolves all target XML documentation (<c>.xml</c>) paths from the configured exporter options
    /// or sibling <c>.xml</c> files alongside discovered assemblies.
    /// </summary>
    /// <param name="options">The exporter configuration options.</param>
    /// <returns>The ordered list of distinct XML documentation file paths.</returns>
    IReadOnlyList<string> DiscoverXmlDocumentationFilePaths(ExporterOptions options);
}

/// <summary>
/// Represents resolved assembly and XML documentation paths paired with optional originating <c>.csproj</c> file paths.
/// </summary>
/// <param name="AssemblyFilePaths">Ordered distinct compiled assembly (<c>.dll</c>) paths.</param>
/// <param name="XmlDocumentationFilePaths">Ordered distinct compiler XML documentation (<c>.xml</c>) paths.</param>
/// <param name="ProjectFilePathsByAssemblyPath">Map of assembly <c>.dll</c> file paths to originating <c>.csproj</c> file paths.</param>
public sealed record ExportArtifactDiscoveryResult(
    IReadOnlyList<string> AssemblyFilePaths,
    IReadOnlyList<string> XmlDocumentationFilePaths,
    IReadOnlyDictionary<string, string> ProjectFilePathsByAssemblyPath);
