namespace Documentation.Exporter.Cli;

/// <summary>
/// Abstracts CLI console error output for testability.
/// </summary>
public interface IConsoleOutput
{
    /// <summary>
    /// Writes an error message line to standard error.
    /// </summary>
    /// <param name="message">The formatted error message to write.</param>
    void WriteErrorLine(string message);
}
