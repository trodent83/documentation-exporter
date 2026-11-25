using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class NoteTagHandlerTests
{
    [Fact]
    public void CanHandleTagAndOrderShouldMatchNoteTagOnly()
    {
        // Arrange
        var handler = new NoteTagHandler();

        // Assert
        _ = handler.Order.Should().Be(55);
        _ = handler.CanHandleTag("note").Should().BeTrue();
        _ = handler.CanHandleTag("NOTE").Should().BeTrue();
        _ = handler.CanHandleTag("summary").Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "NOTE")]
    [InlineData("", "NOTE")]
    [InlineData("note", "NOTE")]
    [InlineData("tip", "TIP")]
    [InlineData("important", "IMPORTANT")]
    [InlineData("implement", "IMPORTANT")]
    [InlineData("inherit", "IMPORTANT")]
    [InlineData("caller", "IMPORTANT")]
    [InlineData("warning", "WARNING")]
    [InlineData("caution", "CAUTION")]
    [InlineData("security", "CAUTION")]
    public void TransformTagShouldMapNoteTypesToExpectedGitHubAlertCallout(string? noteType, string expectedAlertToken)
    {
        // Arrange
        var handler = new NoteTagHandler();
        var typeAttribute = noteType != null ? $" type=\"{noteType}\"" : string.Empty;
        var xmlElement = XElement.Parse($"<note{typeAttribute}>Callout line one.\nCallout line two.</note>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        var markdown = outputBuilder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        _ = markdown.Should().Contain($"> [!{expectedAlertToken}]")
            .And.Contain("> Callout line one.")
            .And.Contain("> Callout line two.");
    }

    [Fact]
    public void TransformTagWhenEmptyOrNullArgumentsShouldHandleSafely()
    {
        // Arrange
        var handler = new NoteTagHandler();
        var emptyElement = XElement.Parse("<note type=\"tip\">   </note>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(emptyElement, outputBuilder, (_, _) => { });

        // Assert
        _ = outputBuilder.ToString().Should().BeEmpty("because empty <note> blocks should not emit blank callouts.");
        _ = FluentActions.Invoking(() => handler.TransformTag(null!, outputBuilder, (_, _) => { }))
            .Should().Throw<ArgumentNullException>("because xmlElement cannot be null.");
        _ = FluentActions.Invoking(() => handler.TransformTag(emptyElement, null!, (_, _) => { }))
            .Should().Throw<ArgumentNullException>("because outputBuilder cannot be null.");
        _ = FluentActions.Invoking(() => handler.TransformTag(emptyElement, outputBuilder, null!))
            .Should().Throw<ArgumentNullException>("because processChildrenAction cannot be null.");
    }

    [Fact]
    public void TransformTagWithBlankInnerLinesShouldEmitBlankBlockquoteSeparator()
    {
        // Arrange
        var handler = new NoteTagHandler();
        var elementWithBlankLine = XElement.Parse("<note type=\"unknown\">First paragraph.\n\nSecond paragraph.</note>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(elementWithBlankLine, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        var markdown = outputBuilder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        _ = markdown.Should().Contain("> [!NOTE]\n> First paragraph.\n>\n> Second paragraph.");
    }
}
