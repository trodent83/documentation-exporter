namespace Documentation.Exporter.DemoLibrary.Diagnostics;

/// <inheritdoc cref="IShowcaseCommandHandler" />
public interface IExtendedCommandHandler : IShowcaseCommandHandler
{
    /// <summary>
    /// Checks whether the specified command can be handled.
    /// </summary>
    /// <param name="command">The command text to evaluate.</param>
    /// <returns><see langword="true"/> if executable; otherwise, <see langword="false"/>.</returns>
    bool CanHandle(string command);
}
