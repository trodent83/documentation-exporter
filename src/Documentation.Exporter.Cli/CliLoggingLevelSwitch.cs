using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Provides a dynamic, thread-safe minimum <see cref="LogLevel"/> switch shared across the unified CLI logging provider.
/// </summary>
public sealed class CliLoggingLevelSwitch
{
    private int minimumLevel = (int)LogLevel.Information;

    /// <summary>
    /// Gets or sets the active minimum <see cref="LogLevel"/> checked by the console logging filter.
    /// </summary>
    public LogLevel MinimumLevel
    {
        get => (LogLevel)Volatile.Read(ref minimumLevel);
        set => Volatile.Write(ref minimumLevel, (int)value);
    }
}
