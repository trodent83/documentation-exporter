using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Parsing.Tags;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing;

[UnitTest]
public sealed class XmlTagTransformerTests
{
    private static XmlTagTransformer CreateDefaultTransformer()
    {
        var handlers = new IXmlTagHandler[]
        {
            new CodeTagHandler(),
            new SeeTagHandler(new TypeLinkResolver()),
            new ParamRefTagHandler(),
            new ParagraphTagHandler(),
            new ListTagHandler(),
            new PassthroughTagHandler()
        };
        return new XmlTagTransformer(handlers);
    }

    [Fact]
    public void ConstructorWhenTagHandlersNullShouldThrowArgumentNullException() =>
        _ = Assert.Throws<ArgumentNullException>(() => new XmlTagTransformer(null!));

    [Fact]
    public void TransformXmlToMarkdownWhenElementNullShouldReturnEmpty()
    {
        // Arrange
        var transformer = CreateDefaultTransformer();

        // Act
        var markdown = transformer.TransformXmlToMarkdown(null);

        // Assert
        _ = markdown.Should().BeEmpty();
    }

    [Fact]
    public void TransformXmlToMarkdownShouldTransformMixedElementsCorrectly()
    {
        // Arrange
        var transformer = CreateDefaultTransformer();
        var xmlElement = XElement.Parse(@"<summary>
    Processes an item. See <see cref=""T:System.String""/> and call <c>Execute()</c>.
    <para>Additional details provided here.</para>
</summary>");

        // Act
        var markdown = transformer.TransformXmlToMarkdown(xmlElement);

        // Assert
        _ = markdown.Should().Contain("Processes an item")
            .And.Contain("`Execute()`")
            .And.Contain("Additional details provided here.");

        var emptyHandlersTransformer = new XmlTagTransformer([]);
        var commentXml = XElement.Parse("<summary><!-- comment -->Plain text</summary>");
        _ = transformer.TransformXmlToMarkdown(commentXml).Should().Be("Plain text");
        _ = emptyHandlersTransformer.TransformXmlToMarkdown(commentXml).Should().BeEmpty();
    }
}
