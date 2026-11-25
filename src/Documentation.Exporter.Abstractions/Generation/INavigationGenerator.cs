using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation;

/// <summary>
/// Strategy interface for generating or extending static-site navigation manifests (such as MkDocs YAML,
/// DocFX <c>toc.yml</c>, or Docusaurus <c>sidebars.js</c>) from exported <see cref="TypeDocumentationModel"/> metadata.
/// </summary>
public interface INavigationGenerator
{
    /// <summary>
    /// Gets the navigation output format handled by this generator.
    /// </summary>
    NavigationFormat Format { get; }

    /// <summary>
    /// Gets the default relative file name (for example <c>"mkdocs.nav.yml"</c> or <c>"toc.yml"</c>)
    /// written inside the output directory when <see cref="ExporterOptions.NavigationFilePath"/> is not specified.
    /// </summary>
    string DefaultFileName { get; }

    /// <summary>
    /// Generates a standalone navigation manifest or merges generated API entries into existing navigation content
    /// at the specified entry point.
    /// </summary>
    /// <param name="types">Ordered collection of exported type documentation models.</param>
    /// <param name="useAssemblySubdirectories">Whether types are organized under per-assembly subdirectories.</param>
    /// <param name="existingNavigationContent">Optional existing manifest content to merge into while preserving unrelated entries.</param>
    /// <param name="navigationEntryPoint">Optional section path or comment placeholder where generated navigation should be inserted.</param>
    /// <param name="navigationUrlPrefix">Optional relative URL prefix prepended to generated Markdown links.</param>
    /// <returns>Formatted or merged navigation file content, or empty string if nothing should be written.</returns>
    string GenerateNavigation(
        IReadOnlyList<TypeDocumentationModel> types,
        bool useAssemblySubdirectories = false,
        string? existingNavigationContent = null,
        string? navigationEntryPoint = null,
        string? navigationUrlPrefix = null);
}
