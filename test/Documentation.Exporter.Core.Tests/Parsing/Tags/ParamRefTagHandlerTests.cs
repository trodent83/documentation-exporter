using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class ParamRefTagHandlerTests
{
    [Fact]
    public void CanHandleTagShouldRecognizeParamRefAndTypeParamRef()
    {
        // Arrange
        var handler = new ParamRefTagHandler();

        // Assert
        _ = handler.CanHandleTag("paramref").Should().BeTrue();
        _ = handler.CanHandleTag("typeparamref").Should().BeTrue();
        _ = handler.CanHandleTag("summary").Should().BeFalse();
    }

    [Fact]
    public void TransformTagWithParameterNameShouldRenderInBackticks()
    {
        // Arrange
        var handler = new ParamRefTagHandler();
        var xmlElement = XElement.Parse("<paramref name=\"myParameter\"/>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("`myParameter`");
    }

    [Fact]
    public void TransformTagWithoutNameShouldRenderEmptyBackticks()
    {
        // Arrange
        var handler = new ParamRefTagHandler();
        var xmlElement = XElement.Parse("<paramref />");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("``");
    }
}
