using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class MermaidArchitectureDiagramGeneratorTests
{
    [Fact]
    public void ConstructorAndMethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var generator = new MermaidArchitectureDiagramGenerator(new DiagramIdentifierFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MermaidArchitectureDiagramGenerator(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.CanGenerate(null!, false));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateEmbeddedMarkdown(null!, false));
        _ = generator.Format.Should().Be(DiagramFormat.Mermaid);
        _ = generator.StandaloneFileName.Should().BeNull();
    }

    [Fact]
    public void GenerateEmbeddedMarkdownShouldRenderNamespacesAndHierarchy()
    {
        // Arrange
        var generator = new MermaidArchitectureDiagramGenerator(new DiagramIdentifierFormatter());
        TypeDocumentationModel[] types =
        [
            new() { TypeName = "IOrderService", TypeCategory = TypeCategory.Interface, NamespaceName = "App.Contracts" },
            new() { TypeName = "OrderService", TypeCategory = TypeCategory.Class, NamespaceName = "App.Services", ImplementedInterfaces = ["IOrderService"] }
        ];

        // Act
        var embedded = generator.GenerateEmbeddedMarkdown(types, includeAssemblyDirectory: false);
        var emptyStandalone = generator.GenerateStandaloneContent(types, includeAssemblyDirectory: false);

        // Assert
        _ = generator.CanGenerate([], includeAssemblyDirectory: false).Should().BeFalse("because an empty type list cannot form an architecture diagram.");
        _ = generator.CanGenerate(types, includeAssemblyDirectory: false).Should().BeTrue("because types contains named models.");
        _ = embedded.Should().Contain("```mermaid")
            .And.Contain("namespace AppContracts {")
            .And.Contain("IOrderService <|.. OrderService");
        _ = emptyStandalone.Should().BeEmpty();
    }
}
