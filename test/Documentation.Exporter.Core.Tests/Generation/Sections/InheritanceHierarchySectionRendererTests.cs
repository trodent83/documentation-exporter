using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class InheritanceHierarchySectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnFifteen()
    {
        // Arrange
        var renderer = new InheritanceHierarchySectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(15);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new InheritanceHierarchySectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenInheritanceHierarchyPresentShouldRenderInlineLinkChainExcludingObject()
    {
        // Arrange
        var renderer = new InheritanceHierarchySectionRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            InheritanceHierarchy =
            [
                "[Object](https://learn.microsoft.com/en-us/dotnet/api/system.object)",
                "[BaseService](BaseService.md)",
                "[AuditableService](AuditableService.md)"
            ]
        };

        // Act
        var output = renderer.RenderSection(model).Trim();

        // Assert
        _ = output.Should().NotContain("## Inheritance Hierarchy", "because inheritance should be rendered inline rather than as a section.")
            .And.NotContain("Object", "because System.Object should be excluded from the hierarchy.")
            .And.Be("[BaseService](BaseService.md) > [AuditableService](AuditableService.md) > [OrderService](OrderService/index.md)");
    }

    [Fact]
    public void RenderSectionWhenInheritanceHierarchyContainsPlainTypeNamesShouldConvertToLinks()
    {
        // Arrange
        var renderer = new InheritanceHierarchySectionRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "ChildService",
            InheritanceHierarchy = ["System.Object", "", "SimpleBase", "App.Services.BaseService"]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Trim().Should().Be("[SimpleBase](SimpleBase.md) > [BaseService](BaseService.md) > [ChildService](ChildService/index.md)");
    }

    [Fact]
    public void RenderSectionWhenNoHierarchyOrOnlyObjectShouldRenderNothing()
    {
        // Arrange
        var renderer = new InheritanceHierarchySectionRenderer();
        var emptyModel = new TypeDocumentationModel { TypeName = "StandaloneClass" };
        var objectOnlyModel = new TypeDocumentationModel
        {
            TypeName = "DirectObjectClass",
            InheritanceHierarchy = ["System.Object", "[Object](https://learn.microsoft.com/en-us/dotnet/api/system.object)"]
        };

        // Act
        var emptyOutput = renderer.RenderSection(emptyModel);
        var objectOnlyOutput = renderer.RenderSection(objectOnlyModel);

        // Assert
        _ = emptyOutput.Should().BeEmpty();
        _ = objectOnlyOutput.Should().BeEmpty();
    }
}
