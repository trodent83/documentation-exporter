using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Enrichment;

/// <summary>
/// Defines a pipeline for executing registered <see cref="ITypeDocumentationEnricher"/> components on type models.
/// </summary>
public interface ITypeDocumentationPipeline
{
    /// <summary>
    /// Executes all registered enrichers in order for the given target type and model.
    /// </summary>
    /// <param name="targetType">The reflected .NET runtime type.</param>
    /// <param name="typeModel">The type documentation model to enrich. It is never modified.</param>
    /// <param name="options">The exporter options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fully enriched copy of the type documentation model.</returns>
    Task<TypeDocumentationModel> EnrichTypeModelAsync(
        Type targetType,
        TypeDocumentationModel typeModel,
        ExporterOptions options,
        CancellationToken cancellationToken = default);
}
