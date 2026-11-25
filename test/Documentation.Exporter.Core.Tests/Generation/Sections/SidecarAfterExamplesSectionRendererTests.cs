using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class SidecarAfterExamplesSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnNinetyFive()
    {
        // Arrange
        var renderer = new SidecarAfterExamplesSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(95);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new SidecarAfterExamplesSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenMatchingAnchorPresentShouldInjectMarkdown()
    {
        // Arrange
        var renderer = new SidecarAfterExamplesSectionRenderer();
        var model = new TypeDocumentationModel
        {
            InjectedDocumentationSections =
            [
                new DocumentationInjectionModel
                {
                    TargetAnchor = SidecarAnchor.AfterExamples,
                    CustomMarkdownContent = "### Extra Examples Notes"
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("### Extra Examples Notes");
    }
}
