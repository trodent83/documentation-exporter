using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class SidecarPageAppendSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnOneHundredTen()
    {
        // Arrange
        var renderer = new SidecarPageAppendSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(110);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new SidecarPageAppendSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenAppendOrEmptyAnchorPresentShouldInjectMarkdown()
    {
        // Arrange
        var renderer = new SidecarPageAppendSectionRenderer();
        var model = new TypeDocumentationModel
        {
            InjectedDocumentationSections =
            [
                new DocumentationInjectionModel
                {
                    TargetAnchor = SidecarAnchor.PageAppend,
                    CustomMarkdownContent = "### Appended Custom Footer"
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("### Appended Custom Footer");
    }
}
