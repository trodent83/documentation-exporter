using Documentation.Exporter.DemoLibrary.Attributes;
using Documentation.Exporter.DemoLibrary.Models;
using Documentation.Exporter.DemoLibrary.Services;

namespace Documentation.Exporter.DemoLibrary.Contracts;

/// <summary>
/// Defines the contract for processing customer orders and querying payment statuses.
/// </summary>
/// <seealso cref="OrderProcessingService"/>
/// <seealso cref="OrderRequest"/>
[StabilityTier("Stable")]
public interface IOrderProcessor
{
    /// <summary>
    /// Occurs when an order's <see cref="PaymentStatus"/> changes during processing.
    /// </summary>
    event EventHandler<OrderStatusChangedEventArgs>? OrderStatusChanged;

    /// <summary>
    /// Gets the display name of the configured payment gateway provider.
    /// </summary>
    /// <value>The gateway provider name.</value>
    string GatewayProviderName { get; }

    /// <summary>
    /// Processes a customer order asynchronously and returns the resulting <see cref="PaymentStatus"/>.
    /// </summary>
    /// <param name="orderRequest">The validated <see cref="OrderRequest"/> to process.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, containing the final <see cref="PaymentStatus"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderRequest"/> is <see langword="null"/>.</exception>
    Task<PaymentStatus> ProcessOrderAsync(OrderRequest orderRequest, CancellationToken cancellationToken = default);
}
