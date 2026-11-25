using Documentation.Exporter.DemoLibrary.Attributes;

namespace Documentation.Exporter.DemoLibrary.Adapters;

/// <summary>
/// Legacy invoice adapter retained for backward compatibility with historical ERP invoice codes.
/// </summary>
/// <remarks>
/// Retained to verify sidecar overrides and <see cref="ObsoleteAttribute"/> warning generation.
/// </remarks>
[StabilityTier("Legacy")]
[Obsolete(
    "LegacyInvoiceAdapter is obsolete and will be removed in v3.0. Use OrderProcessingService instead.",
    DiagnosticId = "DEMO002",
    UrlFormat = "https://docs.example.com/diagnostics/{0}")]
public sealed class LegacyInvoiceAdapter
{
    /// <summary>
    /// Converts a legacy invoice code into a modern order identifier.
    /// </summary>
    /// <param name="legacyInvoiceCode">The legacy invoice string.</param>
    /// <returns>A normalized modern order identifier prefixed with <c>"ORD-"</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="legacyInvoiceCode"/> is <see langword="null"/> or whitespace.</exception>
    [Obsolete("NormalizeInvoiceCode is obsolete; pass normalized identifiers directly to OrderRequest.OrderIdentifier.", DiagnosticId = "DEMO003")]
    public static string NormalizeInvoiceCode(string legacyInvoiceCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyInvoiceCode);
        return $"ORD-{legacyInvoiceCode.Trim().ToUpperInvariant()}";
    }
}
