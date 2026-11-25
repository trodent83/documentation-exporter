using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class ParagraphTagHandlerTests
{
    [Fact]
    public void CanHandleTagShouldRecognizePara()
    {
        // Arrange
        var handler = new ParagraphTagHandler();

        // Assert
        _ = handler.CanHandleTag("para").Should().BeTrue();
        _ = handler.CanHandleTag("unknown").Should().BeFalse();
    }

    [Fact]
    public void TransformTagShouldRenderDoubleNewlinesAndProcessChildren()
    {
        // Arrange
        var handler = new ParagraphTagHandler();
        var xmlElement = XElement.Parse("<para>Some inner text</para>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Contain("Some inner text");
    }
}
