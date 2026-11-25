using System.ComponentModel.DataAnnotations;

namespace Documentation.Exporter.DemoLibrary.Models;

/// <summary>
/// Represents a data transfer payload for submitting a customer order.
/// </summary>
/// <remarks>
/// All properties are validated via <see cref="System.ComponentModel.DataAnnotations"/> before execution.
/// </remarks>
public sealed class OrderRequest
{
    /// <summary>
    /// Gets or sets the unique customer order identifier.
    /// </summary>
    /// <value>A non-empty alphanumeric string.</value>
    [Required]
    [MinLength(1)]
    public string OrderIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer contact email address.
    /// </summary>
    /// <value>A valid email address string.</value>
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total monetary amount for the order.
    /// </summary>
    public MonetaryValue TotalAmount { get; set; } = new(0m, MonetaryValue.DefaultCurrencyCode);

    /// <summary>
    /// Represents an individual item line nested within an <see cref="OrderRequest"/>.
    /// </summary>
    /// <param name="ItemSku">The item stock keeping unit identifier.</param>
    /// <param name="Quantity">The quantity of items requested.</param>
    public sealed record ItemLine(
        [property: Required] string ItemSku,
        [property: Range(1, 10000)] int Quantity)
    {
        /// <summary>
        /// Represents tax breakdown details nested under an <see cref="ItemLine"/> (demonstrating multi-level nested types).
        /// </summary>
        /// <param name="TaxCode">The statutory tax jurisdiction code (for example <c>"VAT-DE"</c>).</param>
        /// <param name="TaxRatePercentage">The applied tax rate expressed as a percentage between 0 and 100.</param>
        public sealed record TaxBreakdown(
            [property: Required] string TaxCode,
            [property: Range(0.0, 100.0)] decimal TaxRatePercentage);
    }
}
