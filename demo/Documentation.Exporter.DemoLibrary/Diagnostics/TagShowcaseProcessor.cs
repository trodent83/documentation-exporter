namespace Documentation.Exporter.DemoLibrary.Diagnostics;

/// <summary>
/// Showcase processor demonstrating <c>&lt;remarks&gt;</c>, <c>&lt;exception&gt;</c> and <c>&lt;example&gt;</c>
/// documentation on every kind of member (constructor, property, field, event, instance and static methods).
/// </summary>
/// <remarks>
/// <para>
/// Type-level remarks may span several paragraphs and contain lists:
/// </para>
/// <list type="number">
/// <item><description>Construct a <see cref="TagShowcaseProcessor"/>.</description></item>
/// <item><description>Adjust <see cref="RetryLimit"/> (never above <see cref="MaximumRetryLimit"/>).</description></item>
/// <item><description>Call <see cref="Execute"/> with a <see cref="ShowcaseMode"/>.</description></item>
/// </list>
/// <para>
/// Supported execution modes and their operational characteristics:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Mode</term>
/// <description>Behavior</description>
/// </listheader>
/// <item>
/// <term><see cref="ShowcaseMode.Immediate"/></term>
/// <description>Executes commands synchronously without intermediate queueing.</description>
/// </item>
/// <item>
/// <term><see cref="ShowcaseMode.Queued"/></term>
/// <description>Serializes command dispatch through the FIFO command queue.</description>
/// </item>
/// </list>
/// <list type="bullet">
/// <item><term>Custom Label Link</term><description>Implements the <see cref="IShowcaseCommandHandler">command handler contract</see> across all entry points.</description></item>
/// </list>
/// <note type="tip">
/// Prefer <see cref="FormatResult(string, bool)"/> when exporting diagnostics logs so timestamps remain structured.
/// </note>
/// <para>
/// See <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/">XML documentation comments</see>
/// for the full tag reference.
/// </para>
/// </remarks>
/// <example>
/// Create a processor and run a command:
/// <code>
/// var processor = new TagShowcaseProcessor("Primary");
/// var result = processor.Execute("ping");
/// </code>
/// </example>
/// <example>
/// Sample structured telemetry output payload:
/// <code language="json">
/// {
///   "processor": "Primary",
///   "mode": "Immediate"
/// }
/// </code>
/// </example>
/// <example>
/// <code>
/// IShowcaseCommandHandler handler = new TagShowcaseProcessor();
/// handler.Handle("status");
/// </code>
/// </example>
/// <seealso cref="FormatResult(string)"/>
/// <seealso cref="FormatResult(string, bool)"/>
/// <seealso cref="IShowcaseCommandHandler"/>
/// <seealso cref="ShowcaseMode"/>
/// <exception cref="ArgumentException">Thrown by constructors and commands when a name or command text is empty or too long.</exception>
/// <exception cref="InvalidOperationException">Thrown by <see cref="Execute"/> when the configured retry limit is not supported.</exception>
public sealed class TagShowcaseProcessor : IShowcaseCommandHandler
{
    /// <summary>
    /// The highest value accepted by <see cref="RetryLimit"/>.
    /// </summary>
    /// <remarks>
    /// The limit protects downstream systems from retry storms.
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.RetryLimit = TagShowcaseProcessor.MaximumRetryLimit;
    /// </code>
    /// </example>
    public const int MaximumRetryLimit = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagShowcaseProcessor"/> class using the default processor name.
    /// </summary>
    public TagShowcaseProcessor()
        : this("ShowcaseProcessor")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TagShowcaseProcessor"/> class with a custom name.
    /// </summary>
    /// <param name="processorName">The display name of the processor.</param>
    /// <remarks>
    /// The name appears in every result produced by <see cref="Execute"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="processorName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="processorName"/> is longer than 64 characters.</exception>
    /// <example>
    /// <code>
    /// var processor = new TagShowcaseProcessor("Primary");
    /// </code>
    /// </example>
    public TagShowcaseProcessor(string processorName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorName);
        if (processorName.Length > 64)
        {
            throw new ArgumentException("The processor name must not exceed 64 characters.", nameof(processorName));
        }

        ProcessorName = processorName;
    }

    /// <summary>
    /// Occurs after a command has been executed successfully.
    /// </summary>
    /// <remarks>
    /// The event is raised synchronously on the thread that called <see cref="Execute"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.CommandHandled += (sender, args) => Console.WriteLine("done");
    /// </code>
    /// </example>
    public event EventHandler? CommandHandled;

    /// <summary>
    /// Gets the display name of the processor.
    /// </summary>
    /// <value>The processor name supplied at construction time.</value>
    /// <remarks>
    /// The name is immutable after construction.
    /// </remarks>
    public string ProcessorName { get; }

    /// <summary>
    /// Gets or sets the maximum number of retries performed for a failing command.
    /// </summary>
    /// <value>An integer between <c>0</c> and <see cref="MaximumRetryLimit"/>; the default is <c>3</c>.</value>
    /// <remarks>
    /// Setting a value above <see cref="MaximumRetryLimit"/> is rejected.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative or above <see cref="MaximumRetryLimit"/>.</exception>
    /// <example>
    /// <code>
    /// processor.RetryLimit = 5;
    /// </code>
    /// </example>
    public int RetryLimit
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaximumRetryLimit);
            field = value;
        }
    } = 3;

    /// <summary>
    /// Normalizes raw command text by trimming it and converting it to upper case.
    /// </summary>
    /// <param name="rawCommand">The raw command text.</param>
    /// <returns>The normalized command text.</returns>
    /// <remarks>
    /// Static members support the same documentation tags as instance members.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawCommand"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var normalized = TagShowcaseProcessor.Normalize("  PING ");
    /// </code>
    /// </example>
    public static string Normalize(string rawCommand)
    {
        ArgumentNullException.ThrowIfNull(rawCommand);
        return rawCommand.Trim().ToUpperInvariant();
    }

    /// <inheritdoc cref="Normalize(string)" />
    public static string Clean(string rawCommand) => Normalize(rawCommand);

    /// <summary>
    /// Executes a command using the specified <see cref="ShowcaseMode"/>.
    /// </summary>
    /// <param name="command">The command text to execute.</param>
    /// <param name="mode">The scheduling mode; defaults to <see cref="ShowcaseMode.Immediate"/>.</param>
    /// <returns>A result string combining <see cref="ProcessorName"/>, <paramref name="mode"/> and <paramref name="command"/>.</returns>
    /// <remarks>
    /// <para>
    /// Execution proceeds in three steps:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Validate <paramref name="command"/>.</description></item>
    /// <item><description>Raise <see cref="CommandHandled"/>.</description></item>
    /// <item><description>Return the formatted result.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is longer than 256 characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="RetryLimit"/> is above <see cref="MaximumRetryLimit"/>.</exception>
    /// <example>
    /// Run a command immediately:
    /// <code>
    /// var immediate = processor.Execute("ping");
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// var queued = processor.Execute("ping", ShowcaseMode.Queued);
    /// </code>
    /// </example>
    /// <seealso cref="Normalize"/>
    public string Execute(string command, ShowcaseMode mode = ShowcaseMode.Immediate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        if (command.Length > 256)
        {
            throw new ArgumentException("The command must not exceed 256 characters.", nameof(command));
        }

        if (RetryLimit > MaximumRetryLimit)
        {
            throw new InvalidOperationException("The retry limit exceeds the supported maximum.");
        }

        CommandHandled?.Invoke(this, EventArgs.Empty);
        return $"{ProcessorName}:{mode}:{command}";
    }

    /// <inheritdoc />
    public bool Handle(string command) => Execute(command).Length > 0;

    /// <summary>
    /// Formats a normalized command payload without a timestamp prefix.
    /// </summary>
    /// <param name="payload">The command payload text to format.</param>
    /// <returns>The formatted command result string.</returns>
    public string FormatResult(string payload) => FormatResult(payload, includeTimestamp: false);

    /// <summary>
    /// Formats a normalized command payload with an optional UTC timestamp prefix.
    /// </summary>
    /// <param name="payload">The command payload text to format.</param>
    /// <param name="includeTimestamp"><see langword="true"/> to prepend a UTC ISO-8601 timestamp; otherwise <see langword="false"/>.</param>
    /// <returns>The formatted command result string.</returns>
    public string FormatResult(string payload, bool includeTimestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        return includeTimestamp
            ? $"[{DateTimeOffset.UtcNow:O}] {ProcessorName}: {payload}"
            : $"{ProcessorName}: {payload}";
    }
}
