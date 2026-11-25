using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class NestedTypesSectionRendererTests
{
    [Fact]
    public void RenderSectionWhenNestedTypesEmptyShouldReturnEmpty()
    {
        // Arrange
        var renderer = new NestedTypesSectionRenderer();
        var model = new TypeDocumentationModel { TypeName = "OrderRequest" };

        // Act
        var rendered = renderer.RenderSection(model);

        // Assert
        _ = renderer.Order.Should().Be(41, "because nested types render before derived types.");
        _ = rendered.Should().BeEmpty("because types without nested types should omit the section.");
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenNestedTypesPresentShouldRenderMarkdownList()
    {
        // Arrange
        var renderer = new NestedTypesSectionRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderRequest",
            NestedTypes = ["[ItemLine](ItemLine/index.md)", "PlainNestedType"]
        };

        // Act
        var rendered = renderer.RenderSection(model);

        // Assert
        _ = rendered.Should().Contain("## Nested Types", "because section heading must be present.");
        _ = rendered.Should().Contain("* [ItemLine](ItemLine/index.md)", "because markdown links should be preserved.");
        _ = rendered.Should().Contain("* `PlainNestedType`", "because unlinked names should be formatted as code.");
    }
}
