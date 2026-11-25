using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class ExceptionsSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldPlaceSectionBetweenMembersAndRemarks()
    {
        // Arrange
        var renderer = new ExceptionsSectionRenderer(new ExceptionTableFormatter());

        // Assert
        _ = renderer.Order.Should().Be(70, "because exceptions render after members (60) and before remarks.");
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new ExceptionsSectionRenderer(new ExceptionTableFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new ExceptionsSectionRenderer(null!));
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenTypeHasDocumentedExceptionsShouldRenderTable()
    {
        // Arrange
        var renderer = new ExceptionsSectionRenderer(new ExceptionTableFormatter());
        var model = new TypeDocumentationModel
        {
            TypeName = "TagShowcaseProcessor",
            DocumentedExceptions = new Dictionary<string, string>
            {
                ["[ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception)"] = "Name is empty.<br/>Name is too long.",
                ["System.TimeoutException"] = "Raw exception name."
            }
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## Exceptions")
            .And.Contain("| Exception | Condition |")
            .And.Contain("| [ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception) | Name is empty.<br/>Name is too long. |")
            .And.Contain("| `System.TimeoutException` | Raw exception name. |");
    }

    [Fact]
    public void RenderSectionWhenTypeHasNoExceptionsShouldRenderNothing()
    {
        // Arrange
        var renderer = new ExceptionsSectionRenderer(new ExceptionTableFormatter());

        // Act
        var output = renderer.RenderSection(new TypeDocumentationModel { TypeName = "Plain" });

        // Assert
        _ = output.Should().BeEmpty();
    }
}
