using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class RemarksSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnSeventy()
    {
        // Arrange
        var renderer = new RemarksSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(70);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new RemarksSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenRemarksPresentShouldRenderRemarksHeaderAndText()
    {
        // Arrange
        var renderer = new RemarksSectionRenderer();
        var model = new TypeDocumentationModel { RemarksDescription = "Detailed remarks text." };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## Remarks")
            .And.Contain("Detailed remarks text.");
    }

    [Fact]
    public void RenderSectionWhenNoRemarksShouldRenderNothing()
    {
        // Arrange
        var renderer = new RemarksSectionRenderer();
        var model = new TypeDocumentationModel();

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().BeEmpty();
    }
}
