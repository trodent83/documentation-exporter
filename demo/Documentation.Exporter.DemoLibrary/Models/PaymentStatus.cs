using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.DemoLibrary.Attributes;
using Documentation.Exporter.DemoLibrary.Services;

namespace Documentation.Exporter.DemoLibrary.Models;

/// <summary>
/// Represents the lifecycle states of a financial payment transaction.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="PaymentStatus"/> in combination with <see cref="OrderProcessingService"/> to track
/// settlement progress from initial authorization to completion.
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Pending"/> indicates awaiting gateway confirmation.</description></item>
/// <item><description><see cref="Authorized"/> indicates funds are reserved.</description></item>
/// <item><description><see cref="Settled"/> indicates final clearing succeeded.</description></item>
/// <item><description><see cref="Declined"/> indicates the issuer rejected the request.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="OrderProcessingService"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum">C# Enumeration Types</seealso>
[StabilityTier("Stable")]
public enum PaymentStatus
{
    /// <summary>
    /// The payment has been submitted and is awaiting gateway verification.
    /// </summary>
    [Display(Name = "Pending Verification")]
    Pending = 0,

    /// <summary>
    /// The payment amount has been authorized and reserved by the issuing institution.
    /// </summary>
    Authorized = 1,

    /// <summary>
    /// The payment has been captured and settled permanently.
    /// </summary>
    Settled = 2,

    /// <summary>
    /// The payment was rejected by the gateway or card issuer.
    /// </summary>
    Declined = 3
}
