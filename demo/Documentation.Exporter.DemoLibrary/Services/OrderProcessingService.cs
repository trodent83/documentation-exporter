using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Documentation.Exporter.DemoLibrary.Attributes;
using Documentation.Exporter.DemoLibrary.Contracts;
using Documentation.Exporter.DemoLibrary.Models;

namespace Documentation.Exporter.DemoLibrary.Services;

/// <summary>
/// Primary domain service responsible for validating, auditing, and settling customer orders.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OrderProcessingService"/> demonstrates the full spectrum of XML documentation tags supported by
/// the exporter pipeline:
/// </para>
/// <list type="number">
/// <item><description>Validate incoming <see cref="OrderRequest"/> payloads.</description></item>
/// <item><description>Record audit entries via <see cref="AuditableProcessorBase.RecordAuditEntry"/>.</description></item>
/// <item><description>Dispatch settlement and raise <see cref="OrderStatusChanged"/> notifications.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var processor = new OrderProcessingService("StripeGateway");
/// var request = new OrderRequest
/// {
///     OrderIdentifier = "ORD-2026-001",
///     CustomerEmail = "buyer@example.com",
///     TotalAmount = new MonetaryValue(125.00m, "USD")
/// };
/// var status = await processor.ProcessOrderAsync(request);
/// </code>
/// </example>
/// <seealso cref="IOrderProcessor"/>
/// <seealso cref="AuditableProcessorBase"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1062">CA1062 Validate Arguments of Public Methods</seealso>
[StabilityTier("Stable")]
public sealed class OrderProcessingService : AuditableProcessorBase, IOrderProcessor
{
    /// <summary>
    /// Maximum monetary amount permitted for instant automated settlement without manual review.
    /// </summary>
    [StabilityTier("Stable")]
    public static readonly decimal MaxInstantSettlementThreshold = 10_000m;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderProcessingService"/> class using the default gateway provider.
    /// </summary>
    public OrderProcessingService()
        : this("DefaultGatewayProvider")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderProcessingService"/> class with a specific gateway provider name.
    /// </summary>
    /// <param name="gatewayProviderName">The display name of the payment gateway provider.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="gatewayProviderName"/> is <see langword="null"/> or whitespace.</exception>
    [StabilityTier("Stable")]
    public OrderProcessingService([Required] string gatewayProviderName)
        : base("OrderProcessingService")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayProviderName);
        GatewayProviderName = gatewayProviderName;
    }

    /// <inheritdoc />
    [StabilityTier("Stable")]
    public event EventHandler<OrderStatusChangedEventArgs>? OrderStatusChanged;

    /// <inheritdoc />
    public string GatewayProviderName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether high-value orders above <see cref="MaxInstantSettlementThreshold"/> require manual approval.
    /// </summary>
    /// <value><see langword="true"/> if manual approval is enforced; otherwise, <see langword="false"/>.</value>
    public bool RequireManualApprovalForHighValue { get; set; } = true;

    /// <inheritdoc />
    /// <remarks>
    /// If <see cref="OrderRequest.TotalAmount"/> exceeds <see cref="MaxInstantSettlementThreshold"/> and
    /// <see cref="RequireManualApprovalForHighValue"/> is <see langword="true"/>, the order transitions to
    /// <see cref="PaymentStatus.Authorized"/> rather than <see cref="PaymentStatus.Settled"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderRequest"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="orderRequest"/> has a negative amount.</exception>
    [StabilityTier("Stable")]
    [return: StabilityTier("Stable")]
    public Task<PaymentStatus> ProcessOrderAsync(OrderRequest orderRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderRequest);
        cancellationToken.ThrowIfCancellationRequested();

        if (orderRequest.TotalAmount.DecimalAmount < 0m)
        {
            throw new InvalidOperationException("Order total amount cannot be negative.");
        }

        _ = RecordAuditEntry($"ProcessOrder:{orderRequest.OrderIdentifier}");

        var resultingStatus = RequireManualApprovalForHighValue &&
                              orderRequest.TotalAmount.DecimalAmount > MaxInstantSettlementThreshold
            ? PaymentStatus.Authorized
            : PaymentStatus.Settled;

        OrderStatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(orderRequest.OrderIdentifier, resultingStatus));
        return Task.FromResult(resultingStatus);
    }

    /// <summary>
    /// Processes a quick order using primitive identifier and amount parameters (overloaded method demonstration).
    /// </summary>
    /// <param name="orderIdentifier">The unique order reference identifier.</param>
    /// <param name="decimalAmount">The numeric monetary value in <see cref="MonetaryValue.DefaultCurrencyCode"/>.</param>
    /// <returns>The resulting <see cref="PaymentStatus"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="orderIdentifier"/> is <see langword="null"/> or whitespace.</exception>
    public PaymentStatus ProcessOrder(string orderIdentifier, decimal decimalAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderIdentifier);

        _ = RecordAuditEntry($"QuickProcessOrder:{orderIdentifier}");
        var resultingStatus = decimalAmount > MaxInstantSettlementThreshold
            ? PaymentStatus.Authorized
            : PaymentStatus.Settled;

        OrderStatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(orderIdentifier, resultingStatus));
        return resultingStatus;
    }

    /// <summary>
    /// Processes a pre-validated <see cref="OrderRequest"/> synchronously (overloaded method demonstration).
    /// </summary>
    /// <param name="orderRequest">The <see cref="OrderRequest"/> instance to process.</param>
    /// <returns>The resulting <see cref="PaymentStatus"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderRequest"/> is <see langword="null"/>.</exception>
    public PaymentStatus ProcessOrder(OrderRequest orderRequest)
    {
        ArgumentNullException.ThrowIfNull(orderRequest);
        return ProcessOrder(orderRequest.OrderIdentifier, orderRequest.TotalAmount.DecimalAmount);
    }

    /// <summary>
    /// Evaluates whether the specified <see cref="MonetaryValue"/> qualifies for instant automated settlement.
    /// </summary>
    /// <param name="monetaryValue">The <see cref="MonetaryValue"/> to evaluate.</param>
    /// <returns><see langword="true"/> if the amount is non-negative and at or below <see cref="MaxInstantSettlementThreshold"/>; otherwise, <see langword="false"/>.</returns>
    public static bool CanSettleInstantly(MonetaryValue monetaryValue) =>
        monetaryValue.DecimalAmount >= 0m && monetaryValue.DecimalAmount <= MaxInstantSettlementThreshold;

    /// <summary>
    /// Streams settled orders asynchronously as an <see cref="IAsyncEnumerable{T}"/> sequence with cooperative enumerator cancellation.
    /// </summary>
    /// <param name="batchLimit">The maximum number of settled orders to yield before completing the stream.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> decorated with <see cref="EnumeratorCancellationAttribute"/> to cancel the async iteration.</param>
    /// <returns>An asynchronous stream of <see cref="OrderRequest"/> items.</returns>
    public async IAsyncEnumerable<OrderRequest> StreamSettledOrdersAsync(
        int batchLimit = 25,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < batchLimit; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new OrderRequest
            {
                OrderIdentifier = $"ORD-STREAM-{index + 1}@{GatewayProviderName}",
                CustomerEmail = "stream@example.com",
                TotalAmount = new MonetaryValue(100m, MonetaryValue.DefaultCurrencyCode)
            };
        }
    }

    /// <summary>
    /// Validates an <see cref="OrderRequest"/> against a minimum <see cref="MonetaryValue"/> threshold and required audit tags, demonstrating <c>in</c>, <c>out</c>, optional default, and <c>params</c> parameter modifiers.
    /// </summary>
    /// <param name="orderRequest">The <see cref="OrderRequest"/> instance to validate.</param>
    /// <param name="minimumAmount">The minimum required <see cref="MonetaryValue"/> passed by read-only reference (<c>in</c>).</param>
    /// <param name="validationFailureReason">When this method returns <see langword="false"/>, contains the reason validation failed; otherwise, <see langword="null"/>.</param>
    /// <param name="fallbackStatus">Optional <see cref="PaymentStatus"/> used when validation is deferred.</param>
    /// <param name="requiredAuditTags">A variable-length <c>params</c> list of audit compliance tags to record.</param>
    /// <returns><see langword="true"/> if the order passes validation; otherwise, <see langword="false"/>.</returns>
    public bool TryValidateOrderWithTags(
        OrderRequest orderRequest,
        in MonetaryValue minimumAmount,
        out string? validationFailureReason,
        PaymentStatus fallbackStatus = PaymentStatus.Pending,
        params string[] requiredAuditTags)
    {
        ArgumentNullException.ThrowIfNull(orderRequest);
        ArgumentNullException.ThrowIfNull(requiredAuditTags);

        if (orderRequest.TotalAmount.DecimalAmount < minimumAmount.DecimalAmount)
        {
            validationFailureReason = $"Order amount is below minimum {minimumAmount.DecimalAmount} (fallback: {fallbackStatus}).";
            return false;
        }

        foreach (var auditTag in requiredAuditTags)
        {
            _ = RecordAuditEntry($"Tag:{auditTag}");
        }

        validationFailureReason = null;
        return true;
    }
}
