using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class PlantUmlArchitectureDiagramGeneratorTests
{
    [Fact]
    public void ConstructorAndMethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var generator = new PlantUmlArchitectureDiagramGenerator(new DiagramIdentifierFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new PlantUmlArchitectureDiagramGenerator(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.CanGenerate(null!, false));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateEmbeddedMarkdown(null!, false));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateStandaloneContent(null!, false));
        _ = generator.Format.Should().Be(DiagramFormat.PlantUml);
        _ = generator.StandaloneFileName.Should().Be("architecture.puml");
    }

    [Fact]
    public void GenerateStandaloneAndEmbeddedContentShouldRenderAssemblyAndNamespacePackages()
    {
        // Arrange
        var generator = new PlantUmlArchitectureDiagramGenerator(new DiagramIdentifierFormatter());
        TypeDocumentationModel[] types =
        [
            new() { AssemblyName = "App.Contracts", NamespaceName = "App.Contracts", TypeName = "ISink", TypeCategory = TypeCategory.Interface },
            new() { AssemblyName = "App.Core", NamespaceName = "App.Core", TypeName = "AuditBase", IsAbstractType = true },
            new() { AssemblyName = "App.Core", NamespaceName = "App.Core", TypeName = "AuditMode", TypeCategory = TypeCategory.Enum }
        ];

        // Act
        var standaloneMulti = generator.GenerateStandaloneContent(types, includeAssemblyDirectory: true);
        var embeddedSingle = generator.GenerateEmbeddedMarkdown(types, includeAssemblyDirectory: false);

        // Assert
        _ = generator.CanGenerate([new TypeDocumentationModel { TypeName = " " }], includeAssemblyDirectory: false).Should().BeFalse("because blank type names do not form architecture nodes.");
        _ = generator.CanGenerate(types, includeAssemblyDirectory: true).Should().BeTrue("because types contains named models.");
        _ = standaloneMulti.Should().Contain("@startuml")
            .And.Contain("package \"App.Contracts\"")
            .And.Contain("interface ISink")
            .And.Contain("abstract class AuditBase")
            .And.Contain("enum AuditMode")
            .And.Contain("@enduml");
        _ = embeddedSingle.Should().Contain("```plantuml").And.Contain("```");
    }
}
