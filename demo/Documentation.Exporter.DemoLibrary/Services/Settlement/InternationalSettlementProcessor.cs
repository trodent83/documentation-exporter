using Documentation.Exporter.DemoLibrary.Attributes;
using Documentation.Exporter.DemoLibrary.Contracts;
using Documentation.Exporter.DemoLibrary.Models;

namespace Documentation.Exporter.DemoLibrary.Services.Settlement;

/// <summary>
/// Specialized settlement processor handling multi-currency cross-border orders in a nested sub-namespace (<c>Services.Settlement</c>).
/// </summary>
/// <remarks>
/// <para>
/// Demonstrates nested namespace directory generation (<c>Services/Settlement/InternationalSettlementProcessor/</c>)
/// and multi-level relative link resolution across parent and sibling namespace folders.
/// </para>
/// </remarks>
/// <param name="gatewayProviderName">The international clearinghouse provider name.</param>
/// <param name="settlementCurrencyCode">The target ISO-4217 settlement currency code.</param>
/// <seealso cref="OrderProcessingService"/>
/// <seealso cref="IOrderProcessor"/>
/// <seealso cref="MonetaryValue"/>
[StabilityTier("Stable")]
public sealed class InternationalSettlementProcessor(
    string gatewayProviderName,
    string settlementCurrencyCode) : AuditableProcessorBase("InternationalSettlementProcessor"), IOrderProcessor
{
    /// <inheritdoc />
    public event EventHandler<OrderStatusChangedEventArgs>? OrderStatusChanged;

    /// <inheritdoc />
    public string GatewayProviderName { get; } = string.IsNullOrWhiteSpace(gatewayProviderName)
        ? "SwiftGlobalGateway"
        : gatewayProviderName;

    /// <summary>
    /// Gets the target ISO-4217 currency code used for cross-border settlement clearing.
    /// </summary>
    /// <value>A three-letter currency code, defaulting to <see cref="MonetaryValue.DefaultCurrencyCode"/>.</value>
    public string SettlementCurrencyCode { get; } = string.IsNullOrWhiteSpace(settlementCurrencyCode)
        ? MonetaryValue.DefaultCurrencyCode
        : settlementCurrencyCode;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderRequest"/> is <see langword="null"/>.</exception>
    public Task<PaymentStatus> ProcessOrderAsync(OrderRequest orderRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderRequest);
        cancellationToken.ThrowIfCancellationRequested();

        _ = RecordAuditEntry($"InternationalSettle:{orderRequest.OrderIdentifier}:{SettlementCurrencyCode}");
        OrderStatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(orderRequest.OrderIdentifier, PaymentStatus.Settled));
        return Task.FromResult(PaymentStatus.Settled);
    }

    /// <summary>
    /// Converts a source <see cref="MonetaryValue"/> into the processor's configured <see cref="SettlementCurrencyCode"/> using the supplied exchange rate.
    /// </summary>
    /// <param name="sourceAmount">The original <see cref="MonetaryValue"/> to convert.</param>
    /// <param name="exchangeRate">The positive conversion multiplier.</param>
    /// <returns>A converted <see cref="MonetaryValue"/> denominated in <see cref="SettlementCurrencyCode"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="exchangeRate"/> is zero or negative.</exception>
    public MonetaryValue ConvertToSettlementCurrency(MonetaryValue sourceAmount, decimal exchangeRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exchangeRate);

        var convertedDecimal = Math.Round(sourceAmount.DecimalAmount * exchangeRate, 2, MidpointRounding.AwayFromZero);
        return new MonetaryValue(convertedDecimal, SettlementCurrencyCode);
    }
}
