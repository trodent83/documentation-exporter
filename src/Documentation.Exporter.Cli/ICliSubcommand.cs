using System.CommandLine;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Represents a modular CLI subcommand registered through dependency injection.
/// </summary>
public interface ICliSubcommand
{
    /// <summary>
    /// Builds and configures the <see cref="Command"/> instance for this subcommand.
    /// </summary>
    /// <returns>The configured <see cref="Command"/>.</returns>
    Command BuildCommand();
}
