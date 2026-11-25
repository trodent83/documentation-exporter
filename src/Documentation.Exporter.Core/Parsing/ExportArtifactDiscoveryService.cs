using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Default implementation of <see cref="IExportArtifactDiscoveryService"/> delegating solution parsing
/// to <see cref="ISolutionAssemblyResolver"/> and applying wildcard project exclusions.
/// </summary>
/// <param name="solutionAssemblyResolver">The solution assembly resolver service.</param>
public sealed class ExportArtifactDiscoveryService(ISolutionAssemblyResolver solutionAssemblyResolver) : IExportArtifactDiscoveryService
{
    private readonly ISolutionAssemblyResolver resolver = ArgumentGuard.EnsureNotNull(solutionAssemblyResolver);

    /// <inheritdoc />
    public ExportArtifactDiscoveryResult DiscoverArtifactDetails(ExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> resolvedAssemblyPaths = [];
        List<string> resolvedXmlPaths = [];
        Dictionary<string, string> projectFilePathsByAssembly = [with(StringComparer.OrdinalIgnoreCase)];

        if (!string.IsNullOrWhiteSpace(options.SolutionFilePath))
        {
            var discoveredArtifacts = resolver.ResolveSolutionAssemblies(
                options.SolutionFilePath,
                options.SolutionConfiguration,
                options.IncludeTestProjects,
                options.ExcludedProjects,
                options.FailOnMissingProjectArtifacts);

            foreach (var artifact in discoveredArtifacts)
            {
                resolvedAssemblyPaths.Add(artifact.AssemblyFilePath);
                resolvedXmlPaths.Add(artifact.XmlDocumentationFilePath);

                if (!string.IsNullOrWhiteSpace(artifact.ProjectFilePath) &&
                    !SolutionAssemblyResolver.IsExcludedByPatterns(Path.GetFileNameWithoutExtension(artifact.AssemblyFilePath), options.ExcludedProjects))
                {
                    _ = projectFilePathsByAssembly.TryAdd(artifact.AssemblyFilePath, artifact.ProjectFilePath);
                }
            }
        }

        if (options.HasExplicitAssemblyFilePath)
        {
            resolvedAssemblyPaths.Add(options.AssemblyFilePath);
        }

        resolvedAssemblyPaths.AddRange(options.AssemblyFilePaths.Where(path => !string.IsNullOrWhiteSpace(path)));

        IReadOnlyList<string> filteredAssemblies =
        [
            .. resolvedAssemblyPaths
                .Where(path => !SolutionAssemblyResolver.IsExcludedByPatterns(Path.GetFileNameWithoutExtension(path), options.ExcludedProjects))
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

        if (options.HasExplicitXmlDocumentationFilePath)
        {
            resolvedXmlPaths.Add(options.XmlDocumentationFilePath);
        }

        resolvedXmlPaths.AddRange(options.XmlDocumentationFilePaths.Where(path => !string.IsNullOrWhiteSpace(path)));

        if (resolvedXmlPaths.Count < filteredAssemblies.Count)
        {
            foreach (var candidateAssemblyPath in filteredAssemblies)
            {
                var assemblySimpleName = Path.GetFileNameWithoutExtension(candidateAssemblyPath);
                var alreadyHasMatchingXml = resolvedXmlPaths.Any(xmlPath =>
                    string.Equals(Path.GetFileNameWithoutExtension(xmlPath), assemblySimpleName, StringComparison.OrdinalIgnoreCase));
                if (alreadyHasMatchingXml)
                {
                    continue;
                }

                var inferredXmlPath = TryInferSiblingXmlFilePath(candidateAssemblyPath);
                if (!string.IsNullOrWhiteSpace(inferredXmlPath))
                {
                    resolvedXmlPaths.Add(inferredXmlPath);
                }
            }
        }

        IReadOnlyList<string> filteredXmlPaths =
        [
            .. resolvedXmlPaths
                .Where(path => !SolutionAssemblyResolver.IsExcludedByPatterns(Path.GetFileNameWithoutExtension(path), options.ExcludedProjects))
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

        return new ExportArtifactDiscoveryResult(filteredAssemblies, filteredXmlPaths, projectFilePathsByAssembly);
    }

    /// <inheritdoc />
    public (IReadOnlyList<string> AssemblyFilePaths, IReadOnlyList<string> XmlDocumentationFilePaths) DiscoverArtifactPaths(ExporterOptions options)
    {
        var details = DiscoverArtifactDetails(options);
        return (details.AssemblyFilePaths, details.XmlDocumentationFilePaths);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> DiscoverAssemblyFilePaths(ExporterOptions options) =>
        DiscoverArtifactDetails(options).AssemblyFilePaths;

    /// <inheritdoc />
    public IReadOnlyList<string> DiscoverXmlDocumentationFilePaths(ExporterOptions options) =>
        DiscoverArtifactDetails(options).XmlDocumentationFilePaths;

    private static string? TryInferSiblingXmlFilePath(string? candidateAssemblyPath) =>
        string.IsNullOrWhiteSpace(candidateAssemblyPath) || !candidateAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? null
            : Path.ChangeExtension(candidateAssemblyPath, ".xml");
}
