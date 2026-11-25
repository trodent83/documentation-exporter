using Documentation.Exporter.Abstractions.DependencyInjection;
using Documentation.Exporter.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Plugins.Navigation.MkDocs;

/// <summary>
/// Built-in plugin exposing the standard MkDocs YAML navigation generator (<c>mkdocs.nav.yml</c> / <c>mkdocs.yml</c>).
/// </summary>
public sealed class MkDocsNavigationPlugin : IDocumentationExporterPlugin
{
    /// <inheritdoc />
    public string Name => "MkDocs Navigation";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddNavigationGenerator<MkDocsNavigationGenerator>();
    }
}
