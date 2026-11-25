using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class SeeAlsoSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnOneHundred()
    {
        // Arrange
        var renderer = new SeeAlsoSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(100);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new SeeAlsoSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenSeeAlsoPresentShouldRenderSeeAlsoList()
    {
        // Arrange
        var renderer = new SeeAlsoSectionRenderer();
        var model = new TypeDocumentationModel
        {
            SeeAlsoReferences = ["[OtherService](OtherService.md)"]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## See Also")
            .And.Contain("* [OtherService](OtherService.md)");
    }

    [Fact]
    public void RenderSectionWhenNoSeeAlsoShouldRenderNothing()
    {
        // Arrange
        var renderer = new SeeAlsoSectionRenderer();
        var model = new TypeDocumentationModel();

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().BeEmpty();
    }
}
