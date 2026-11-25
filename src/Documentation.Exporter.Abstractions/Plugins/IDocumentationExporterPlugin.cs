using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Abstractions.Plugins;

/// <summary>
/// Entry point contract implemented by external or built-in plugin assemblies discovered in the plugins directory.
/// Implementations must provide a public parameterless constructor.
/// </summary>
public interface IDocumentationExporterPlugin
{
    /// <summary>
    /// Gets the human-readable display name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers extension strategies (diagram generators, section renderers, XML tag handlers, enrichers, workflow steps, etc.)
    /// into the host <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    void ConfigureServices(IServiceCollection services);
}
