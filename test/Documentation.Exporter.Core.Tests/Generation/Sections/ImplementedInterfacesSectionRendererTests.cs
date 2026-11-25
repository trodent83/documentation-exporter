using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class ImplementedInterfacesSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnForty()
    {
        // Arrange
        var renderer = new ImplementedInterfacesSectionRenderer();

        // Assert
        _ = renderer.Order.Should().Be(40);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new ImplementedInterfacesSectionRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenInterfacesPresentShouldRenderInterfaceList()
    {
        // Arrange
        var renderer = new ImplementedInterfacesSectionRenderer();
        var model = new TypeDocumentationModel
        {
            ImplementedInterfaces = ["[IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)"]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## Implemented Interfaces")
            .And.Contain("* [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)");
    }

    [Fact]
    public void RenderSectionWhenInterfaceIsPlainTypeNameShouldWrapInBackticks()
    {
        // Arrange
        var renderer = new ImplementedInterfacesSectionRenderer();
        var model = new TypeDocumentationModel
        {
            ImplementedInterfaces = ["System.IDisposable"]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("* `System.IDisposable`");
    }

    [Fact]
    public void RenderSectionWhenNoInterfacesShouldRenderNothing()
    {
        // Arrange
        var renderer = new ImplementedInterfacesSectionRenderer();
        var model = new TypeDocumentationModel();

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().BeEmpty();
    }
}
