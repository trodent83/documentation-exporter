using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class PassthroughTagHandlerTests
{
    [Fact]
    public void CanHandleTagShouldAlwaysReturnTrue()
    {
        // Arrange
        var handler = new PassthroughTagHandler();

        // Assert
        _ = handler.CanHandleTag("summary").Should().BeTrue();
        _ = handler.CanHandleTag("remarks").Should().BeTrue();
        _ = handler.CanHandleTag("anyOtherTag").Should().BeTrue();
    }

    [Fact]
    public void TransformTagShouldDelegateProcessingToChildren()
    {
        // Arrange
        var handler = new PassthroughTagHandler();
        var xmlElement = XElement.Parse("<summary>Some nested text</summary>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("Some nested text");
    }
}
