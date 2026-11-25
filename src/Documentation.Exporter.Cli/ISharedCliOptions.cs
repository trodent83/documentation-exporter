using System.CommandLine;
using System.CommandLine.Parsing;
using Documentation.Exporter.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Centralizes shared CLI option definitions, parsing, and <see cref="ExporterOptions"/> binding.
/// </summary>
public interface ISharedCliOptions
{
    /// <summary>Gets the console minimum log-level option.</summary>
    Option<LogLevel> LogLevel { get; }

    /// <summary>
    /// Adds the standard documentation export options to <paramref name="targetCommand"/>.
    /// </summary>
    /// <param name="targetCommand">The command receiving the shared options.</param>
    /// <param name="includeFailOnMissing">Whether to include the solution-specific <c>--fail-on-missing</c> flag.</param>
    void AddExportOptionsTo(Command targetCommand, bool includeFailOnMissing);

    /// <summary>
    /// Adds optional CLI override flags (<c>--output</c>, <c>--clean</c>, <c>--strict</c>, <c>--fail-on-undocumented</c>,
    /// <c>--fail-on-missing</c>, <c>--diagram-format</c>, <c>--nav-format</c>, <c>--report-file</c>, <c>--report-format</c>, <c>--plugins-dir</c>) to the <c>config</c> subcommand.
    /// </summary>
    /// <param name="targetCommand">The config command receiving the optional override options.</param>
    void AddConfigOverrideOptionsTo(Command targetCommand);

    /// <summary>
    /// Applies any explicitly specified CLI override options from <paramref name="parseResult"/> onto <paramref name="exporterOptions"/>.
    /// </summary>
    /// <param name="exporterOptions">The loaded configuration options to override.</param>
    /// <param name="parseResult">The parsed CLI command result.</param>
    void ApplyOptionalOverrides(ExporterOptions exporterOptions, ParseResult parseResult);

    /// <summary>
    /// Parses CLI options into a configured <see cref="ExporterOptions"/> instance and requested <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="parseResult">The System.CommandLine parse result.</param>
    /// <param name="exporterOptions">When successful, receives the bound exporter options.</param>
    /// <param name="logLevel">Receives the parsed console log level.</param>
    /// <returns><see langword="true"/> if options bound without error; otherwise <see langword="false"/>.</returns>
    bool TryCreateBaseExporterOptions(
        ParseResult parseResult,
        out ExporterOptions? exporterOptions,
        out LogLevel logLevel);
}
