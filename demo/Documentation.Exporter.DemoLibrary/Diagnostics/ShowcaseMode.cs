namespace Documentation.Exporter.DemoLibrary.Diagnostics;

/// <summary>
/// Defines how <see cref="TagShowcaseProcessor"/> schedules the commands it receives.
/// </summary>
/// <remarks>
/// Every value of this enumeration carries its own <c>&lt;remarks&gt;</c> and <c>&lt;example&gt;</c> block to
/// verify that enumeration member documentation is exported.
/// </remarks>
public enum ShowcaseMode
{
    /// <summary>
    /// Commands run immediately on the calling thread.
    /// </summary>
    /// <remarks>
    /// Immediate mode skips queueing entirely; use it only for short-lived commands.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = processor.Execute("ping", ShowcaseMode.Immediate);
    /// </code>
    /// </example>
    Immediate = 0,

    /// <summary>
    /// Commands are queued and executed in submission order.
    /// </summary>
    /// <remarks>
    /// <para>Queued mode guarantees ordering.</para>
    /// <list type="bullet">
    /// <item><description>Commands never run concurrently.</description></item>
    /// <item><description>Failures do not drop the remaining queue.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = processor.Execute("ping", ShowcaseMode.Queued);
    /// </code>
    /// </example>
    Queued = 1
}
