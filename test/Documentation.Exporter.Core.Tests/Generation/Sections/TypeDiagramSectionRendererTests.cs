using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class TypeDiagramSectionRendererTests
{
    private sealed class StubTypeDiagramGenerator(
        DiagramFormat format,
        bool isSupported,
        string renderedContent) : ITypeDiagramGenerator
    {
        public DiagramFormat Format => format;

        public bool CanGenerate(TypeDocumentationModel typeModel) => isSupported;

        public string GenerateTypeDiagram(TypeDocumentationModel typeModel) => renderedContent;
    }

    [Fact]
    public void ConstructorAndRenderSectionWhenArgumentsNullShouldThrowArgumentNullException()
    {
        // Arrange
        var identifierFormatter = new DiagramIdentifierFormatter();
        ITypeDiagramGenerator[] generators =
        [
            new MermaidTypeDiagramGenerator(identifierFormatter),
            new PlantUmlTypeDiagramGenerator(identifierFormatter)
        ];
        DiagramGeneratorSelector selector = new();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new TypeDiagramSectionRenderer(null!, selector));
        _ = Assert.Throws<ArgumentNullException>(() => new TypeDiagramSectionRenderer(generators, null!));

        TypeDiagramSectionRenderer renderer = new(generators, selector);
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionShouldRenderPreferredSingleFormatAndReturnEmptyWhenDisabledOrBlank()
    {
        // Arrange
        var identifierFormatter = new DiagramIdentifierFormatter();
        TypeDiagramSectionRenderer renderer = new(
            [new MermaidTypeDiagramGenerator(identifierFormatter), new PlantUmlTypeDiagramGenerator(identifierFormatter)],
            new DiagramGeneratorSelector());

        TypeDocumentationModel model = new()
        {
            TypeName = "OrderService",
            TypeCategory = TypeCategory.Class,
            DiagramFormat = DiagramFormat.PlantUml,
            ImplementedInterfaces = ["IOrderService"]
        };

        // Act
        var mermaidOnly = renderer.RenderSection(model with { DiagramFormat = DiagramFormat.Mermaid });
        var plantUmlOnly = renderer.RenderSection(model);
        var noneOutput = renderer.RenderSection(model with { DiagramFormat = DiagramFormat.None });

        // Assert
        _ = renderer.Order.Should().Be(45);
        _ = mermaidOnly.Should().Contain("## Type Diagram")
            .And.Contain("```mermaid", "because Mermaid is the preferred diagram format.")
            .And.NotContain("```plantuml", "because only one diagram should be emitted for a type.");
        _ = plantUmlOnly.Should().Contain("## Type Diagram")
            .And.Contain("```plantuml", "because PlantUml was selected as the preferred diagram format.")
            .And.NotContain("```mermaid", "because only one diagram should be emitted for a type.");
        _ = noneOutput.Should().BeEmpty("because DiagramFormat.None disables diagram rendering.");
    }

    [Fact]
    public void RenderSectionWhenPreferredUnsupportedOrEmptyOutputShouldFallbackOrReturnEmpty()
    {
        // Arrange
        var identifierFormatter = new DiagramIdentifierFormatter();
        TypeDiagramSectionRenderer enumFallbackRenderer = new(
            [
                new MermaidTypeDiagramGenerator(identifierFormatter),
                new StubTypeDiagramGenerator(DiagramFormat.PlantUml, isSupported: true, "```plantuml\n@startuml\nenum PaymentStatus\n@enduml\n```\n")
            ],
            new DiagramGeneratorSelector());

        TypeDiagramSectionRenderer emptyOutputRenderer = new(
            [new StubTypeDiagramGenerator(DiagramFormat.Mermaid, isSupported: true, string.Empty)],
            new DiagramGeneratorSelector());

        TypeDocumentationModel enumModel = new()
        {
            TypeName = "PaymentStatus",
            TypeCategory = TypeCategory.Enum,
            DiagramFormat = DiagramFormat.Mermaid
        };

        // Act
        var fallbackOutput = enumFallbackRenderer.RenderSection(enumModel);
        var emptyOutput = emptyOutputRenderer.RenderSection(new TypeDocumentationModel
        {
            TypeName = "OrderService",
            DiagramFormat = DiagramFormat.Mermaid
        });

        // Assert
        _ = fallbackOutput.Should().Contain("## Type Diagram")
            .And.Contain("```plantuml", "because Mermaid does not support enums so rendering falls back to the supporting PlantUML generator.")
            .And.NotContain("```mermaid", "because unsupported preferred generator must not emit a block.");
        _ = emptyOutput.Should().BeEmpty("because whitespace diagram output must omit the Type Diagram heading.");
    }
}
