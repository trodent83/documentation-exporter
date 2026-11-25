using Documentation.Exporter.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Executes the documentation export pipeline from CLI-bound <see cref="ExporterOptions"/>.
/// </summary>
public interface ICliPipelineRunner
{
    /// <summary>
    /// Runs the documentation export pipeline with the specified options and minimum log level.
    /// </summary>
    /// <param name="exporterOptions">The configured exporter options.</param>
    /// <param name="logLevel">The minimum console log level.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Exit code: <c>0</c> for success, <c>1</c> for export failure.</returns>
    Task<int> RunAsync(ExporterOptions exporterOptions, LogLevel logLevel, CancellationToken cancellationToken = default);
}
