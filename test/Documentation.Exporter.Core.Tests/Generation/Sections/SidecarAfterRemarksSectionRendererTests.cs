using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class SidecarAfterRemarksSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnEighty()
    {
        // Arrange
        var renderer = new SidecarAfterRemarksSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(80);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new SidecarAfterRemarksSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenMatchingAnchorPresentShouldInjectMarkdown()
    {
        // Arrange
        var renderer = new SidecarAfterRemarksSectionRenderer();
        var model = new TypeDocumentationModel
        {
            InjectedDocumentationSections =
            [
                new DocumentationInjectionModel
                {
                    TargetAnchor = SidecarAnchor.AfterRemarks,
                    CustomMarkdownContent = "### After Remarks Note"
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("### After Remarks Note");
    }
}
