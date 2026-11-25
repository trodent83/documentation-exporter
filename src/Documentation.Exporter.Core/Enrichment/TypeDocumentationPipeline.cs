using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Enrichment;

/// <summary>
/// Executes a sequence of ordered <see cref="ITypeDocumentationEnricher"/> instances.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeDocumentationPipeline"/> class.
/// </remarks>
/// <param name="enrichers">The collection of enrichers.</param>
public sealed class TypeDocumentationPipeline(IEnumerable<ITypeDocumentationEnricher> enrichers) : ITypeDocumentationPipeline
{
    private readonly IReadOnlyList<ITypeDocumentationEnricher> orderedEnrichers =
        ArgumentGuard.EnsureNotNull(enrichers)
        .OrderBy(enricher => enricher.Order)
        .ToList()
        .AsReadOnly();

    /// <inheritdoc />
    public async Task<TypeDocumentationModel> EnrichTypeModelAsync(
        Type targetType,
        TypeDocumentationModel typeModel,
        ExporterOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(typeModel);
        ArgumentNullException.ThrowIfNull(options);

        var enrichedModel = typeModel;
        foreach (var enricher in orderedEnrichers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            enrichedModel = await enricher.EnrichAsync(targetType, enrichedModel, options, cancellationToken).ConfigureAwait(false);
        }

        return enrichedModel;
    }
}
