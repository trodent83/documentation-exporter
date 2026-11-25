using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class SidecarBeforeRemarksSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnFifty()
    {
        // Arrange
        var renderer = new SidecarBeforeRemarksSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(50);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new SidecarBeforeRemarksSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenMatchingAnchorPresentShouldInjectMarkdown()
    {
        // Arrange
        var renderer = new SidecarBeforeRemarksSectionRenderer();
        var model = new TypeDocumentationModel
        {
            InjectedDocumentationSections =
            [
                new DocumentationInjectionModel
                {
                    TargetAnchor = SidecarAnchor.BeforeRemarks,
                    CustomMarkdownContent = "### Before Remarks Note"
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("### Before Remarks Note");
    }
}
