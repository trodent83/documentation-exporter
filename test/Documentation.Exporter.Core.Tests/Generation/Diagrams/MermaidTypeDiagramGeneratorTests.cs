using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class MermaidTypeDiagramGeneratorTests
{
    [Fact]
    public void ConstructorAndMethodWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var generator = new MermaidTypeDiagramGenerator(new DiagramIdentifierFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MermaidTypeDiagramGenerator(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.CanGenerate(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateTypeDiagram(null!));
        _ = generator.Format.Should().Be(DiagramFormat.Mermaid);
    }

    [Fact]
    public void GenerateTypeDiagramShouldRenderMermaidEdgesAndSkipEnumsOrStandaloneTypes()
    {
        // Arrange
        var generator = new MermaidTypeDiagramGenerator(new DiagramIdentifierFormatter());
        var standaloneModel = new TypeDocumentationModel { TypeName = "Standalone" };
        var enumModel = new TypeDocumentationModel { TypeName = "PaymentStatus", TypeCategory = TypeCategory.Enum };
        var whitespaceModel = new TypeDocumentationModel { TypeName = "   " };
        var interfaceModel = new TypeDocumentationModel
        {
            TypeName = "IOrderProcessor",
            TypeCategory = TypeCategory.Interface,
            DerivedTypes = ["[OrderProcessingService](OrderProcessingService/index.md)"]
        };

        // Act & Assert
        _ = generator.CanGenerate(standaloneModel).Should().BeFalse("because standalone types without relationships produce no Mermaid class diagram.");
        _ = generator.CanGenerate(enumModel).Should().BeFalse("because Mermaid type generator does not render enums.");
        _ = generator.CanGenerate(whitespaceModel).Should().BeFalse("because empty type names cannot produce diagrams.");
        _ = generator.CanGenerate(interfaceModel).Should().BeTrue("because interfaceModel has derived implementations.");

        _ = generator.GenerateTypeDiagram(standaloneModel).Should().BeEmpty();
        _ = generator.GenerateTypeDiagram(enumModel).Should().BeEmpty();

        var diagram = generator.GenerateTypeDiagram(interfaceModel);
        _ = diagram.Should().Contain("```mermaid")
            .And.Contain("<<interface>>")
            .And.Contain("IOrderProcessor <|.. OrderProcessingService");
    }
}
