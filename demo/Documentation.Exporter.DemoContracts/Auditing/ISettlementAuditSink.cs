namespace Documentation.Exporter.DemoContracts.Auditing;

/// <summary>
/// Defines a cross-assembly contract for recording settlement audit events across distributed payment gateways.
/// </summary>
/// <remarks>
/// Implementations in downstream assemblies inherit documentation from this contract via <c>&lt;inheritdoc /&gt;</c>.
/// </remarks>
public interface ISettlementAuditSink
{
    /// <summary>
    /// Gets the logical identifier of the target audit sink channel.
    /// </summary>
    /// <value>A non-empty channel identifier string.</value>
    string SinkChannelName { get; }

    /// <summary>
    /// Records a settlement audit correlation event into the underlying sink.
    /// </summary>
    /// <param name="correlationIdentifier">The unique transaction correlation identifier.</param>
    /// <param name="settlementSummary">Human-readable description of the settlement outcome.</param>
    /// <returns><see langword="true"/> if the audit entry was persisted; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="correlationIdentifier"/> is <see langword="null"/>.</exception>
    bool WriteAuditEvent(string correlationIdentifier, string settlementSummary);
}
