using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class PlantUmlAssemblyDependencyDiagramGeneratorTests
{
    [Fact]
    public void ConstructorAndMethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        PlantUmlAssemblyDependencyDiagramGenerator generator = new(new DiagramIdentifierFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new PlantUmlAssemblyDependencyDiagramGenerator(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.CanGenerate("App.Main", null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateDependencyTreeMarkdown("App.Main", null!));
        _ = generator.Format.Should().Be(DiagramFormat.PlantUml);
    }

    [Fact]
    public void GenerateDependencyTreeMarkdownWhenEmptyOrBlankRootShouldReturnEmpty()
    {
        // Arrange
        PlantUmlAssemblyDependencyDiagramGenerator generator = new(new DiagramIdentifierFormatter());
        AssemblyDependencyEdgeModel[] sampleEdges =
        [
            new()
            {
                SourceAssemblyName = "App.Main",
                TargetAssemblyName = "App.Contracts"
            }
        ];

        // Act & Assert
        _ = generator.CanGenerate("App.Main", []).Should().BeFalse();
        _ = generator.CanGenerate(string.Empty, sampleEdges).Should().BeFalse();
        _ = generator.GenerateDependencyTreeMarkdown("App.Main", []).Should().BeEmpty();
        _ = generator.GenerateDependencyTreeMarkdown(" ", sampleEdges).Should().BeEmpty();
    }

    [Fact]
    public void GenerateDependencyTreeMarkdownShouldRenderComponentGraphColoredArrowsAndLegend()
    {
        // Arrange
        PlantUmlAssemblyDependencyDiagramGenerator generator = new(new DiagramIdentifierFormatter());
        AssemblyDependencyEdgeModel[] edges =
        [
            new()
            {
                SourceAssemblyName = "App.Cli",
                TargetAssemblyName = "App.Core",
                ReferenceKind = AssemblyReferenceKind.Project,
                VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked,
                IsTargetStrongNamed = false
            },
            new()
            {
                SourceAssemblyName = "App.Core",
                TargetAssemblyName = "FluentValidation",
                ReferenceKind = AssemblyReferenceKind.Package,
                VersionLinkingKind = AssemblyVersionLinkingKind.Fixed,
                VersionConstraintExpression = "[11.9.0]",
                IsTargetStrongNamed = true,
                TargetPublicKeyToken = "7de548da2fbae0f0"
            }
        ];

        // Act
        var markdown = generator.GenerateDependencyTreeMarkdown("App.Cli", edges);

        // Assert
        _ = generator.CanGenerate("App.Cli", edges).Should().BeTrue();
        _ = markdown.Should().Contain("```plantuml")
            .And.Contain("@startuml")
            .And.Contain("left to right direction")
            .And.Contain("component \"App.Cli\" as AppCli")
            .And.Contain("component \"App.Core\" as AppCore")
            .And.Contain("component \"FluentValidation\" as FluentValidation")
            .And.Contain("AppCli -[#757575,dashed]-> AppCore : \"Linked (Project)\"",
                "because unsigned target dependencies use dashed gray arrows.")
            .And.Contain("AppCore -[#2E7D32,bold]-> FluentValidation : \"Fixed ([11.9.0])\"",
                "because strong-named target dependencies use bold green arrows.")
            .And.Contain("@enduml")
            .And.Contain($"*{MarkdownGenerationResources.AssemblyDependencyTreeLegend}*");
    }
}
