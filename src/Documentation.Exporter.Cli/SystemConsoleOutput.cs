namespace Documentation.Exporter.Cli;

/// <summary>
/// Default <see cref="IConsoleOutput"/> implementation writing highlighted error lines to <see cref="Console.Error"/>.
/// </summary>
public sealed class SystemConsoleOutput : IConsoleOutput
{
    /// <inheritdoc />
    public void WriteErrorLine(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }
}
