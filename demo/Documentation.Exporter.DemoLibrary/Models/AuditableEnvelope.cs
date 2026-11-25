using Documentation.Exporter.DemoLibrary.Attributes;
using Documentation.Exporter.DemoLibrary.Contracts;

namespace Documentation.Exporter.DemoLibrary.Models;

/// <summary>
/// Generic audit wrapper encapsulating a strongly-typed domain payload with correlation metadata.
/// </summary>
/// <typeparam name="TPayload">The reference-type domain payload constrained to provide a parameterless constructor.</typeparam>
/// <param name="correlationId">The unique distributed correlation identifier.</param>
/// <param name="payload">The strongly-typed payload instance.</param>
[StabilityTier("Stable")]
public sealed class AuditableEnvelope<[StabilityTier("Stable")] TPayload>(string correlationId, TPayload payload)
    where TPayload : class, new()
{
    /// <summary>
    /// Gets the distributed correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = correlationId;

    /// <summary>
    /// Gets the encapsulated domain payload instance.
    /// </summary>
    public TPayload Payload { get; } = payload;

    /// <summary>
    /// Projects the encapsulated <typeparamref name="TPayload"/> into a value-type summary metric.
    /// </summary>
    /// <typeparam name="TMetric">The target value-type metric constrained to implement <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="projectionSelector">The projection function mapping <typeparamref name="TPayload"/> to <typeparamref name="TMetric"/>.</param>
    /// <returns>The projected <typeparamref name="TMetric"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionSelector"/> is <see langword="null"/>.</exception>
    public TMetric ProjectPayload<[StabilityTier("Preview")] TMetric>(Func<TPayload, TMetric> projectionSelector)
        where TMetric : struct, IEquatable<TMetric>
    {
        ArgumentNullException.ThrowIfNull(projectionSelector);
        return projectionSelector(Payload);
    }

    /// <summary>
    /// Dispatches the encapsulated <typeparamref name="TPayload"/> to a strongly-typed <typeparamref name="TProcessor"/> implementation.
    /// </summary>
    /// <typeparam name="TProcessor">The processor type constrained to implement <see cref="IOrderProcessor"/>.</typeparam>
    /// <param name="processor">The <typeparamref name="TProcessor"/> instance to inspect.</param>
    /// <returns>The processor's <see cref="IOrderProcessor.GatewayProviderName"/> combined with <see cref="CorrelationId"/>.</returns>
    public string DescribeProcessorRoute<TProcessor>(TProcessor processor)
        where TProcessor : class, IOrderProcessor
    {
        ArgumentNullException.ThrowIfNull(processor);
        return $"{processor.GatewayProviderName}:{CorrelationId}";
    }
}
