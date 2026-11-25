using Documentation.Exporter.DemoLibrary.Attributes;

namespace Documentation.Exporter.DemoLibrary.Models;

/// <summary>
/// Represents an immutable monetary amount paired with an ISO-4217 currency code.
/// </summary>
/// <remarks>
/// Always verify that two <see cref="MonetaryValue"/> instances share the same <see cref="CurrencyCode"/>
/// before performing arithmetic operations.
/// </remarks>
/// <param name="decimalAmount">The numeric monetary value.</param>
/// <param name="currencyCode">The three-letter ISO-4217 currency identifier (such as <c>"USD"</c> or <c>"EUR"</c>).</param>
/// <example>
/// <code>
/// var price = new MonetaryValue(49.95m, "EUR");
/// var formatted = price.FormatDisplayValue();
/// </code>
/// </example>
/// <seealso cref="OrderRequest"/>
[StabilityTier("Stable")]
public readonly struct MonetaryValue(decimal decimalAmount, string currencyCode) : IEquatable<MonetaryValue>
{
    /// <summary>
    /// The default fallback currency code (<c>"USD"</c>) used when none is specified.
    /// </summary>
    public const string DefaultCurrencyCode = "USD";

    /// <summary>
    /// Gets the numeric monetary amount.
    /// </summary>
    /// <value>A <see cref="decimal"/> value representing the currency amount.</value>
    public decimal DecimalAmount { get; } = decimalAmount;

    /// <summary>
    /// Gets the three-letter ISO-4217 currency code.
    /// </summary>
    /// <value>An uppercase ISO currency string, defaulting to <see cref="DefaultCurrencyCode"/> if <see langword="null"/>.</value>
    public string CurrencyCode { get; } = string.IsNullOrWhiteSpace(currencyCode) ? DefaultCurrencyCode : currencyCode;

    /// <summary>
    /// Formats the monetary amount and currency code as an invariant display string.
    /// </summary>
    /// <returns>A formatted string in the form <c>"49.95 EUR"</c>.</returns>
    public string FormatDisplayValue() =>
        FormattableString.Invariant($"{DecimalAmount:F2} {CurrencyCode}");

    /// <summary>
    /// Determines whether the specified <see cref="MonetaryValue"/> is equal to the current value.
    /// </summary>
    /// <param name="other">The other <see cref="MonetaryValue"/> instance to compare with.</param>
    /// <returns><see langword="true"/> if both amount and currency code match; otherwise, <see langword="false"/>.</returns>
    public bool Equals(MonetaryValue other) =>
        DecimalAmount == other.DecimalAmount &&
        string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is MonetaryValue otherValue && Equals(otherValue);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(DecimalAmount, StringComparer.OrdinalIgnoreCase.GetHashCode(CurrencyCode));

    /// <summary>
    /// Determines whether two <see cref="MonetaryValue"/> instances are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(MonetaryValue left, MonetaryValue right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="MonetaryValue"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(MonetaryValue left, MonetaryValue right) => !left.Equals(right);
}
