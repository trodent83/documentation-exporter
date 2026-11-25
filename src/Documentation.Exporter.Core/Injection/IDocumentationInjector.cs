using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Injection;

/// <summary>
/// Service interface for reading external sidecar override files and injecting supplementary documentation into target pages.
/// </summary>
public interface IDocumentationInjector
{
    /// <summary>
    /// Asynchronously retrieves documentation injection models for the specified target type or member from a given directory path.
    /// </summary>
    /// <param name="overridesDirectoryPath">Path to directory containing markdown sidecar override files.</param>
    /// <param name="typeName">The simple or full name of the target type.</param>
    /// <param name="memberName">The optional member name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of documentation injection models found for the specified target.</returns>
    Task<IReadOnlyList<DocumentationInjectionModel>> GetInjectionsAsync(
        string? overridesDirectoryPath,
        string? typeName,
        string? memberName = null,
        CancellationToken cancellationToken = default);
}
