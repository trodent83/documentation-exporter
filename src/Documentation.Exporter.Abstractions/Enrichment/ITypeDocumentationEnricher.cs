using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Enrichment;

/// <summary>
/// Defines an enricher that augments a <see cref="TypeDocumentationModel"/> with metadata, documentation, examples, or overrides.
/// </summary>
public interface ITypeDocumentationEnricher
{
    /// <summary>
    /// Execution order of this enricher in the enrichment pipeline (lower numbers execute first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Enriches the type documentation model asynchronously without modifying the supplied model.
    /// </summary>
    /// <param name="targetType">The reflected .NET runtime type.</param>
    /// <param name="typeModel">The type documentation model to enrich. It is never modified.</param>
    /// <param name="options">The exporter options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched copy of the type documentation model.</returns>
    Task<TypeDocumentationModel> EnrichAsync(
        Type targetType,
        TypeDocumentationModel typeModel,
        ExporterOptions options,
        CancellationToken cancellationToken = default);
}
