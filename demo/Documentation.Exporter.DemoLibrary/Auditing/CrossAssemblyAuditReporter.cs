using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.DemoContracts.Auditing;
using Documentation.Exporter.DemoLibrary.Attributes;

namespace Documentation.Exporter.DemoLibrary.Auditing;

/// <summary>
/// Reports settlement audit events to an external sink defined in <see cref="ISettlementAuditSink"/>.
/// </summary>
/// <remarks>
/// Demonstrates cross-assembly interface implementation, relative type linking, custom attributes, and <c>&lt;inheritdoc /&gt;</c> migration across solution projects.
/// </remarks>
/// <param name="channelName">The sink channel name.</param>
[StabilityTier("Enterprise-Audit")]
public sealed class CrossAssemblyAuditReporter(string channelName) : ISettlementAuditSink
{
    /// <inheritdoc />
    [Required]
    public string SinkChannelName { get; } = string.IsNullOrWhiteSpace(channelName)
        ? throw new ArgumentNullException(nameof(channelName))
        : channelName;

    /// <inheritdoc />
    public bool WriteAuditEvent(string correlationIdentifier, string settlementSummary)
    {
        ArgumentNullException.ThrowIfNull(correlationIdentifier);
        return !string.IsNullOrWhiteSpace(settlementSummary);
    }
}
