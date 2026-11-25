using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Plugins.Diagrams;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Plugins;

[UnitTest]
public sealed class StandardDiagramsPluginTests
{
    [Fact]
    public void StandardDiagramsPluginWhenServicesNullShouldThrowArgumentNullException()
    {
        // Arrange
        StandardDiagramsPlugin plugin = new();

        // Act
        var act = () => plugin.ConfigureServices(null!);

        // Assert
        _ = plugin.Name.Should().Be("Standard Diagrams (Mermaid & PlantUML)");
        _ = act.Should().Throw<ArgumentNullException>("because ConfigureServices requires a non-null IServiceCollection.");
    }

    [Fact]
    public void ConfigureServicesShouldRegisterAllSixMermaidAndPlantUmlGenerators()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddSingleton<IDiagramIdentifierFormatter, DiagramIdentifierFormatter>();
        StandardDiagramsPlugin plugin = new();

        // Act
        plugin.ConfigureServices(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        var typeGenerators = provider.GetServices<ITypeDiagramGenerator>().ToList();
        var architectureGenerators = provider.GetServices<IArchitectureDiagramGenerator>().ToList();
        var dependencyGenerators = provider.GetServices<IAssemblyDependencyDiagramGenerator>().ToList();

        _ = typeGenerators.Should().HaveCount(2);
        _ = typeGenerators.Select(generator => generator.Format).Should().BeEquivalentTo([DiagramFormat.Mermaid, DiagramFormat.PlantUml]);

        _ = architectureGenerators.Should().HaveCount(2);
        _ = architectureGenerators.Select(generator => generator.Format).Should().BeEquivalentTo([DiagramFormat.Mermaid, DiagramFormat.PlantUml]);

        _ = dependencyGenerators.Should().HaveCount(2);
        _ = dependencyGenerators.Select(generator => generator.Format).Should().BeEquivalentTo([DiagramFormat.Mermaid, DiagramFormat.PlantUml]);
    }
}
