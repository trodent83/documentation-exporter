namespace Documentation.Exporter.DemoLibrary.Models;

/// <summary>
/// Encapsulates notification details raised when an order transitions to a new <see cref="PaymentStatus"/>.
/// </summary>
/// <param name="orderIdentifier">The unique order reference identifier.</param>
/// <param name="updatedStatus">The newly assigned <see cref="PaymentStatus"/>.</param>
public sealed class OrderStatusChangedEventArgs(string orderIdentifier, PaymentStatus updatedStatus) : EventArgs
{
    /// <summary>
    /// Gets the unique order reference identifier.
    /// </summary>
    public string OrderIdentifier { get; } = orderIdentifier;

    /// <summary>
    /// Gets the updated payment status for the order.
    /// </summary>
    public PaymentStatus UpdatedStatus { get; } = updatedStatus;
}
