using Documentation.Exporter.DemoContracts.Auditing;
using Documentation.Exporter.DemoLibrary.Models;

namespace Documentation.Exporter.DemoLibrary.Extensions;

/// <summary>
/// Provides extension methods for <see cref="OrderRequest"/> and <see cref="ISettlementAuditSink"/>.
/// </summary>
/// <remarks>
/// Extension methods declared in this static class are automatically discovered and listed in the
/// <c>## Extension Methods</c> section on the extended target types across the solution.
/// </remarks>
public static class OrderRequestExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="OrderRequest"/> exceeds the high-value settlement threshold.
    /// </summary>
    /// <param name="orderRequest">The target <see cref="OrderRequest"/> instance.</param>
    /// <param name="minimumThreshold">The minimum decimal threshold to classify as high-value.</param>
    /// <returns><see langword="true"/> if <see cref="OrderRequest.TotalAmount"/> is at least <paramref name="minimumThreshold"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orderRequest"/> is <see langword="null"/>.</exception>
    public static bool IsHighValueOrder(this OrderRequest orderRequest, decimal minimumThreshold = 10_000m)
    {
        ArgumentNullException.ThrowIfNull(orderRequest);
        return orderRequest.TotalAmount.DecimalAmount >= minimumThreshold;
    }

    /// <summary>
    /// Writes a formatted verification audit entry for an <see cref="OrderRequest"/> into the specified <see cref="ISettlementAuditSink"/>.
    /// </summary>
    /// <param name="auditSink">The target <see cref="ISettlementAuditSink"/> contract.</param>
    /// <param name="orderRequest">The <see cref="OrderRequest"/> being audited.</param>
    /// <returns><see langword="true"/> if the audit event was recorded; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="auditSink"/> or <paramref name="orderRequest"/> is <see langword="null"/>.</exception>
    public static bool TryWriteVerifiedAudit(this ISettlementAuditSink auditSink, OrderRequest orderRequest)
    {
        ArgumentNullException.ThrowIfNull(auditSink);
        ArgumentNullException.ThrowIfNull(orderRequest);

        return auditSink.WriteAuditEvent(
            orderRequest.OrderIdentifier,
            $"Order {orderRequest.OrderIdentifier} for {orderRequest.CustomerEmail}");
    }
}
