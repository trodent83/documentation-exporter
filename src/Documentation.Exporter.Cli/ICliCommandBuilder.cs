using System.CommandLine;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Builds the configured CLI <see cref="RootCommand"/> from DI-registered subcommands and shared options.
/// </summary>
public interface ICliCommandBuilder
{
    /// <summary>
    /// Constructs the configured <see cref="RootCommand"/> with all DI-registered subcommands and shared options.
    /// </summary>
    /// <returns>The configured <see cref="RootCommand"/>.</returns>
    RootCommand BuildRootCommand();
}
