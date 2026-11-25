namespace Documentation.Exporter.DemoLibrary.Services;

/// <summary>
/// Root abstract base class for all transaction processors in the domain service layer.
/// </summary>
public abstract class TransactionProcessorBase
{
    /// <summary>
    /// Gets the UTC timestamp when the processor instance was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Provides foundational auditing and correlation tracking for domain transaction processors.
/// </summary>
/// <remarks>
/// Derived classes such as <see cref="OrderProcessingService"/> inherit automatic correlation ID generation
/// and protected diagnostic hooks.
/// </remarks>
/// <param name="serviceName">The logical name of the derived processor service.</param>
public abstract class AuditableProcessorBase(string serviceName) : TransactionProcessorBase
{
    /// <summary>
    /// Gets the logical service name used in audit trail entries.
    /// </summary>
    public string ServiceName { get; } = string.IsNullOrWhiteSpace(serviceName) ? "DefaultProcessor" : serviceName;

    /// <summary>
    /// Gets the count of audit entries recorded by this processor instance.
    /// </summary>
    protected int RecordedAuditCount { get; private set; }

    /// <summary>
    /// Records an internal audit message and increments <see cref="RecordedAuditCount"/>.
    /// </summary>
    /// <param name="operationName">The name of the audited operation.</param>
    /// <returns>A formatted audit trail token containing <paramref name="operationName"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is <see langword="null"/> or whitespace.</exception>
    protected string RecordAuditEntry(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        RecordedAuditCount++;
        return $"[{ServiceName}#{RecordedAuditCount}] {operationName}";
    }
}
