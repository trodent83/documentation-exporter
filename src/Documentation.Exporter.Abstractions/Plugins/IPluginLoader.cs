using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Abstractions.Plugins;

/// <summary>
/// Discovers and loads <see cref="IDocumentationExporterPlugin"/> implementations from a plugin directory into DI.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Scans <paramref name="pluginsDirectoryPath"/> for plugin assemblies, instantiates their <see cref="IDocumentationExporterPlugin"/>
    /// entry points, and registers their services into <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="pluginsDirectoryPath">The root directory containing plugin assemblies.</param>
    /// <returns>Descriptors for every plugin successfully loaded and configured.</returns>
    IReadOnlyList<PluginDescriptor> LoadPluginsFromDirectory(IServiceCollection services, string pluginsDirectoryPath);
}
