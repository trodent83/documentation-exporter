using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Configuration;

/// <summary>
/// Service interface for loading and path-resolving <see cref="ExporterOptions"/> from declarative JSON configuration files.
/// </summary>
public interface IExporterConfigurationLoader
{
    /// <summary>
    /// Asynchronously loads <see cref="ExporterOptions"/> from the specified JSON configuration file and resolves relative file and directory paths against the configuration file's directory.
    /// </summary>
    /// <param name="configurationFilePath">The path to the JSON configuration file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The loaded and path-resolved <see cref="ExporterOptions"/> instance.</returns>
    Task<ExporterOptions> LoadAsync(string configurationFilePath, CancellationToken cancellationToken = default);
}
