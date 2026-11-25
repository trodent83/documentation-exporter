using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class ExamplesSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnNinety()
    {
        // Arrange
        var renderer = new ExamplesSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(90);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new ExamplesSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenExamplesPresentShouldRenderCodeBlocks()
    {
        // Arrange
        var renderer = new ExamplesSectionRenderer();
        var model = new TypeDocumentationModel
        {
            CodeExamples = ["var example = new Service();"]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## Examples")
            .And.Contain("var example = new Service();");
    }

    [Fact]
    public void RenderSectionWhenNoExamplesShouldRenderNothing()
    {
        // Arrange
        var renderer = new ExamplesSectionRenderer();
        var model = new TypeDocumentationModel();

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().BeEmpty();
    }
}
