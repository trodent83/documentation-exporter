using Documentation.Exporter.Abstractions.DependencyInjection;
using Documentation.Exporter.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Built-in documentation exporter plugin providing Mermaid and PlantUML diagram generators for
/// type hierarchy diagrams, solution/assembly architecture diagrams, and assembly dependency trees.
/// </summary>
public sealed class StandardDiagramsPlugin : IDocumentationExporterPlugin
{
    /// <inheritdoc />
    public string Name => "Standard Diagrams (Mermaid & PlantUML)";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddTypeDiagramGenerator<MermaidTypeDiagramGenerator>()
            .AddTypeDiagramGenerator<PlantUmlTypeDiagramGenerator>()
            .AddArchitectureDiagramGenerator<MermaidArchitectureDiagramGenerator>()
            .AddArchitectureDiagramGenerator<PlantUmlArchitectureDiagramGenerator>()
            .AddAssemblyDependencyDiagramGenerator<MermaidAssemblyDependencyDiagramGenerator>()
            .AddAssemblyDependencyDiagramGenerator<PlantUmlAssemblyDependencyDiagramGenerator>();
    }
}
