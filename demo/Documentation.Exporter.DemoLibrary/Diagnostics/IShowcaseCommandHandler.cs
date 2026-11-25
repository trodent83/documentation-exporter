namespace Documentation.Exporter.DemoLibrary.Diagnostics;

/// <summary>
/// Contract for components that handle textual commands.
/// </summary>
/// <remarks>
/// Interface members document <c>&lt;remarks&gt;</c>, <c>&lt;exception&gt;</c> and <c>&lt;example&gt;</c> once;
/// implementations reuse that documentation through <c>&lt;inheritdoc /&gt;</c>.
/// </remarks>
public interface IShowcaseCommandHandler
{
    /// <summary>
    /// Handles a single command.
    /// </summary>
    /// <param name="command">The command text to handle.</param>
    /// <returns><see langword="true"/> when the command was handled; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Implementations must be idempotent: handling the same command twice must not change the outcome.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is empty or whitespace.</exception>
    /// <example>
    /// Dispatch a command through any handler:
    /// <code>
    /// var handled = handler.Handle("ping");
    /// </code>
    /// </example>
    bool Handle(string command);
}
