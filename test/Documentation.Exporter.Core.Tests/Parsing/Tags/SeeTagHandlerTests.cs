using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class SeeTagHandlerTests
{
    [Fact]
    public void CanHandleTagShouldRecognizeSeeAndSeeAlso()
    {
        // Arrange
        var handler = new SeeTagHandler(new TypeLinkResolver());

        // Assert
        _ = handler.CanHandleTag("see").Should().BeTrue();
        _ = handler.CanHandleTag("seealso").Should().BeTrue();
        _ = handler.CanHandleTag("summary").Should().BeFalse();
    }

    [Fact]
    public void TransformTagWithLangwordShouldRenderInlineCode()
    {
        // Arrange
        var handler = new SeeTagHandler(new TypeLinkResolver());
        var xmlElement = XElement.Parse("<see langword=\"null\"/>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("`null`");
    }

    [Fact]
    public void TransformTagWithHrefAndTextShouldRenderCustomLinkText()
    {
        // Arrange
        var handler = new SeeTagHandler(new TypeLinkResolver());
        var xmlElement = XElement.Parse("<see href=\"https://example.com\">Custom Title</see>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("[Custom Title](https://example.com)");
    }

    [Fact]
    public void TransformTagWithHrefWithoutTextShouldUseHrefAsLinkText()
    {
        // Arrange
        var handler = new SeeTagHandler(new TypeLinkResolver());
        var xmlElement = XElement.Parse("<see href=\"https://example.com\"/>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("[https://example.com](https://example.com)");
    }

    [Fact]
    public void ConstructorWhenTypeLinkResolverNullShouldThrowArgumentNullException() =>
        _ = FluentActions.Invoking(() => new SeeTagHandler(null!))
            .Should().Throw<ArgumentNullException>("because linkResolver cannot be null.");

    [Fact]
    public void TransformTagWithCrefWithoutPrefixShouldResolveLinkDirectly()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        resolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        var handler = new SeeTagHandler(resolver);
        var xmlElement = XElement.Parse("<see cref=\"System.String\"/>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Contain("learn.microsoft.com");
    }

    [Fact]
    public void TransformTagWithCrefAndCustomLabelShouldPreserveCustomLinkLabel()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        resolver.RegisterHandler(new LocalProjectNamespaceHandler(["Demo.Services.IOrderProcessor"], rootNamespace: "Demo"));
        var handler = new SeeTagHandler(resolver);
        var xmlElement = XElement.Parse("<see cref=\"T:Demo.Services.IOrderProcessor\">order processor contract</see>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("[order processor contract](Services/IOrderProcessor/index.md)",
            "because inner text on <see cref=\"...\">label</see> must override the default link text.");
    }

    [Fact]
    public void TransformTagWithUnresolvedCrefAndCustomLabelShouldEmitCustomLabelText()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var handler = new SeeTagHandler(resolver);
        var xmlElement = XElement.Parse("<see cref=\"T:ThirdParty.CustomWidget\">widget API</see>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("widget API",
            "because unresolved crefs with custom inner text must emit the inner label.");

        var plainTextSeeElement = XElement.Parse("<see>Fallback Label</see>");
        StringBuilder plainTextBuilder = new();
        handler.TransformTag(plainTextSeeElement, plainTextBuilder, (element, builder) => builder.Append(element.Value));
        _ = plainTextBuilder.ToString().Should().Be("Fallback Label",
            "because a <see> tag with inner text and no attributes should preserve its inner text.");
    }
}
