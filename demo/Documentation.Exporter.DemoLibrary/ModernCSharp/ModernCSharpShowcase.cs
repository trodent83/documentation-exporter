using System.Diagnostics.CodeAnalysis;
using Documentation.Exporter.DemoLibrary.Models;
using Documentation.Exporter.DemoLibrary.Services;

namespace Documentation.Exporter.DemoLibrary.ModernCSharp;

/// <summary>
/// Transforms a contravariant domain input into a covariant formatted result using an optional nullable correlation tag.
/// </summary>
/// <typeparam name="TInput">The contravariant input payload type.</typeparam>
/// <typeparam name="TOutput">The covariant output result type.</typeparam>
/// <param name="inputPayload">The input domain payload.</param>
/// <param name="correlationTag">An optional nullable correlation tag.</param>
/// <returns>The transformed <typeparamref name="TOutput"/> instance.</returns>
public delegate TOutput SettlementTransformHandler<in TInput, out TOutput>(TInput inputPayload, string? correlationTag = null);

/// <summary>
/// Defines a settlement metric contract demonstrating C# 11 <c>static abstract</c> and <c>static virtual</c> interface members.
/// </summary>
/// <typeparam name="TSelf">The implementing metric type.</typeparam>
public interface ISettlementMetric<TSelf> where TSelf : ISettlementMetric<TSelf>
{
    /// <summary>
    /// Gets the canonical metric category name.
    /// </summary>
    static abstract string MetricCategory { get; }

    /// <summary>
    /// Computes a weighted score from a raw settlement amount.
    /// </summary>
    /// <param name="rawAmount">The raw settlement monetary amount.</param>
    /// <returns>The weighted score.</returns>
    static virtual decimal ComputeWeightedScore(decimal rawAmount) => rawAmount;
}

/// <summary>
/// Converts a non-null source model into a covariant result representation.
/// </summary>
/// <typeparam name="TSource">The contravariant non-nullable source type (<c>notnull</c>).</typeparam>
/// <typeparam name="TResult">The covariant output type (<c>notnull</c>).</typeparam>
public interface IMetricConverter<in TSource, out TResult>
    where TSource : notnull
    where TResult : notnull
{
    /// <summary>
    /// Converts the supplied <paramref name="sourceValue"/> into <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="sourceValue">The non-null input instance.</param>
    /// <returns>The converted <typeparamref name="TResult"/> value.</returns>
    TResult Convert(TSource sourceValue);
}

/// <summary>
/// Represents a stack-only span metric contract implemented by C# 13 <c>ref struct</c> types.
/// </summary>
public interface IOrderSpanMetric
{
    /// <summary>
    /// Gets the length of the inspected identifier span.
    /// </summary>
    int CharacterCount { get; }
}

/// <summary>
/// Stack-only <c>readonly ref struct</c> reader implementing <see cref="IOrderSpanMetric"/> with a primary constructor.
/// </summary>
/// <param name="rawIdentifier">The read-only character span identifier.</param>
public readonly ref struct OrderSpanReader(ReadOnlySpan<char> rawIdentifier) : IOrderSpanMetric
{
    private readonly ReadOnlySpan<char> identifierSpan = rawIdentifier;

    /// <inheritdoc />
    public int CharacterCount => identifierSpan.Length;
}

/// <summary>
/// Formats span-based metrics using C# 13 <c>allows ref struct</c> constraints and <c>params ReadOnlySpan&lt;T&gt;</c>.
/// </summary>
public static class SpanFormatter
{
    /// <summary>
    /// Formats a metric summary from an arbitrary <typeparamref name="TBuffer"/> (including <c>ref struct</c> types) and span segments.
    /// </summary>
    /// <typeparam name="TBuffer">A buffer type implementing <see cref="IOrderSpanMetric"/> with <c>allows ref struct</c>.</typeparam>
    /// <param name="reader">The scoped read-only buffer metric reference.</param>
    /// <param name="segments">Variable-length tag segments supplied as a <see cref="ReadOnlySpan{T}"/>.</param>
    /// <returns>A formatted metric string containing the character count and joined segments.</returns>
    public static string FormatWithBuffer<TBuffer>(scoped in TBuffer reader, params ReadOnlySpan<string> segments)
        where TBuffer : IOrderSpanMetric, allows ref struct =>
        segments.Length == 0
            ? $"len={reader.CharacterCount}"
            : $"len={reader.CharacterCount}:{string.Join("-", segments.ToArray())}";
}

/// <summary>
/// Immutable <c>readonly record struct</c> representing an ordered FX currency pair.
/// </summary>
/// <param name="BaseCurrency">The three-letter ISO base currency code.</param>
/// <param name="QuoteCurrency">The three-letter ISO quote currency code.</param>
public readonly record struct CurrencyPair(string BaseCurrency, string QuoteCurrency);

/// <summary>
/// Unsealed <c>record</c> base type combining a primary constructor, secondary constructor, and C# 11 <c>required</c> / C# 9 <c>init</c> properties.
/// </summary>
/// <param name="RecordIdentifier">The canonical record identifier passed to the primary constructor.</param>
public record SettlementRecordBase(string RecordIdentifier)
{
    /// <summary>
    /// Initializes a new instance of <see cref="SettlementRecordBase"/> using the fallback <c>"DEFAULT"</c> identifier.
    /// </summary>
    protected SettlementRecordBase()
        : this("DEFAULT")
    {
    }

    /// <summary>
    /// Gets or sets the required immutable audit channel identifier.
    /// </summary>
    public required string AuditChannel { get; init; }
}

/// <summary>
/// Preview advisor class marked with <see cref="ExperimentalAttribute"/> (<c>DEMO001</c>) to showcase experimental callout banners.
/// </summary>
[Experimental("DEMO001", UrlFormat = "https://docs.example.com/diagnostics/{0}")]
public sealed class ExperimentalSettlementPreview
{
    /// <summary>
    /// Evaluates an experimental risk projection score.
    /// </summary>
    /// <param name="amount">The monetary amount to evaluate.</param>
    /// <returns>A projected score value.</returns>
    public static decimal EvaluateProjectedRisk(decimal amount) => amount * 0.015m;
}

/// <summary>
/// Mutable settlement ledger demonstrating primary constructors with secondary overloads, C# 14 <c>field</c> accessors,
/// C# 14 compound assignment operators, indexers, multi-dimensional arrays, NRT returns, and <c>protected internal</c> members.
/// </summary>
/// <param name="tenantPrefix">The tenant identifier prefix captured by the primary constructor.</param>
/// <param name="initialBalance">The opening balance applied to the ledger.</param>
public class SettlementLedger(string tenantPrefix, decimal initialBalance) : ISettlementMetric<SettlementLedger>
{
    /// <summary>
    /// Initializes a new <see cref="SettlementLedger"/> instance for the default tenant with a zero balance.
    /// </summary>
    public SettlementLedger()
        : this("DEFAULT", 0m)
    {
    }

    /// <inheritdoc />
    public static string MetricCategory => "Ledger";

    /// <summary>
    /// Gets the current settlement balance.
    /// </summary>
    public decimal Balance { get; private set; } = initialBalance;

    /// <summary>
    /// Gets or sets the normalized uppercase region code backed by the C# 14 compiler-synthesized <c>field</c>.
    /// </summary>
    public string NormalizedRegionCode
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? "GLOBAL" : value.Trim().ToUpperInvariant();
    } = "GLOBAL";

    /// <summary>
    /// Gets the projected balance offset at the specified <paramref name="bucketIndex"/>.
    /// </summary>
    /// <param name="bucketIndex">Zero-based settlement bucket offset.</param>
    /// <returns>The indexed projected balance.</returns>
    public decimal this[int bucketIndex] => Balance + bucketIndex;

    /// <summary>
    /// Formats a tenant-scoped ledger entry identifier using the captured primary constructor parameter.
    /// </summary>
    /// <param name="entrySuffix">The entry identifier suffix.</param>
    /// <returns>A formatted entry identifier combining tenant prefix and suffix.</returns>
    public string FormatEntryIdentifier(string entrySuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entrySuffix);
        return $"{tenantPrefix}:{entrySuffix}";
    }

    /// <summary>
    /// Computes an experimental adjustment tag using a <c>scoped ref readonly</c> decimal reference.
    /// </summary>
    /// <param name="referenceAmount">The scoped read-only monetary amount reference.</param>
    /// <returns>A formatted tag string containing the tenant prefix and amount.</returns>
    [Experimental("DEMO001", UrlFormat = "https://docs.example.com/diagnostics/{0}")]
    public string CombineScopedTag(scoped ref readonly decimal referenceAmount) =>
        $"{tenantPrefix}:{referenceAmount:F2}";

    /// <summary>
    /// Asynchronously locates a matching pending <see cref="OrderRequest"/> from a list supporting nullable entries and a nullable result.
    /// </summary>
    /// <param name="candidateIdentifiers">Candidate order identifiers (elements may be <see langword="null"/>).</param>
    /// <returns>A matching <see cref="OrderRequest"/> if found; otherwise <see langword="null"/>.</returns>
    public Task<OrderRequest?> FindPendingOrderAsync(IReadOnlyList<string?> candidateIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(candidateIdentifiers);
        _ = candidateIdentifiers.Contains(tenantPrefix);
        return Task.FromResult<OrderRequest?>(null);
    }

    /// <summary>
    /// Builds a 2D quarterly settlement projection matrix demonstrating multi-dimensional array syntax and <c>protected internal</c> visibility.
    /// </summary>
    /// <returns>A 2-by-2 matrix populated with scaled balance projections.</returns>
    protected internal decimal[,] BuildQuarterlyMatrix() =>
        new[,] { { Balance, Balance * 1.1m }, { Balance * 1.2m, Balance * 1.3m } };

    /// <summary>
    /// Adds <paramref name="deltaAmount"/> to the ledger balance in-place using a C# 14 compound assignment operator (<c>+=</c>).
    /// </summary>
    /// <param name="deltaAmount">The monetary increment to apply.</param>
    public void operator +=(decimal deltaAmount) => Balance += deltaAmount;

    /// <summary>
    /// Returns a new <see cref="SettlementLedger"/> whose balance is increased by <paramref name="rightAmount"/>.
    /// </summary>
    /// <param name="leftLedger">The source ledger instance.</param>
    /// <param name="rightAmount">The monetary amount to add.</param>
    /// <returns>A new <see cref="SettlementLedger"/> with the updated balance.</returns>
    public static SettlementLedger operator +(SettlementLedger leftLedger, decimal rightAmount)
    {
        ArgumentNullException.ThrowIfNull(leftLedger);
        return new SettlementLedger(leftLedger.FormatEntryIdentifier("ADD"), leftLedger.Balance + rightAmount);
    }

    /// <summary>
    /// Returns a new <see cref="SettlementLedger"/> whose balance is checked-added by <paramref name="rightAmount"/> (<c>checked +</c>).
    /// </summary>
    /// <param name="leftLedger">The source ledger instance.</param>
    /// <param name="rightAmount">The monetary amount to add under overflow checking.</param>
    /// <returns>A new <see cref="SettlementLedger"/> with the updated balance.</returns>
    public static SettlementLedger operator checked +(SettlementLedger leftLedger, decimal rightAmount)
    {
        ArgumentNullException.ThrowIfNull(leftLedger);
        return new SettlementLedger(leftLedger.FormatEntryIdentifier("CHK"), checked(leftLedger.Balance + rightAmount));
    }

    /// <summary>
    /// Implicitly converts a <see cref="SettlementLedger"/> to its current <see cref="Balance"/> value.
    /// </summary>
    /// <param name="ledger">The ledger instance to convert.</param>
    /// <returns>The ledger's current <see cref="Balance"/>.</returns>
    public static implicit operator decimal(SettlementLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        return ledger.Balance;
    }
}

/// <summary>
/// Provides C# 14 <c>extension</c> blocks defining instance and static extension properties and methods on <see cref="OrderRequest"/>.
/// </summary>
public static class OrderRequestModernExtensions
{
    extension(OrderRequest orderRequest)
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="OrderRequest"/> exceeds the high-value threshold.
        /// </summary>
        public bool IsHighValueOrder => orderRequest.TotalAmount.DecimalAmount >= OrderProcessingService.MaxInstantSettlementThreshold;

        /// <summary>
        /// Formats an audit tag combining <paramref name="prefix"/> and <see cref="OrderRequest.OrderIdentifier"/>.
        /// </summary>
        /// <param name="prefix">The audit tag prefix.</param>
        /// <returns>The formatted audit tag string.</returns>
        public string FormatAuditTag(string prefix)
        {
            ArgumentNullException.ThrowIfNull(orderRequest);
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
            return $"{prefix}:{orderRequest.OrderIdentifier}";
        }
    }

    extension(OrderRequest)
    {
        /// <summary>
        /// Gets the minimum monetary threshold classifying a sample order as high value.
        /// </summary>
        public static decimal MinimumHighValueAmount => OrderProcessingService.MaxInstantSettlementThreshold;

        /// <summary>
        /// Creates a sample high-value <see cref="OrderRequest"/> pre-populated with threshold defaults.
        /// </summary>
        /// <param name="orderIdentifier">The unique order identifier to assign.</param>
        /// <returns>A populated high-value <see cref="OrderRequest"/> instance.</returns>
        public static OrderRequest CreateHighValueSample(string orderIdentifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(orderIdentifier);
            return new OrderRequest
            {
                OrderIdentifier = orderIdentifier,
                CustomerEmail = "highvalue@example.com",
                TotalAmount = new MonetaryValue(OrderProcessingService.MaxInstantSettlementThreshold, MonetaryValue.DefaultCurrencyCode)
            };
        }
    }
}
