using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class CodeTagHandlerTests
{
    [Fact]
    public void CanHandleTagShouldRecognizeCAndCode()
    {
        // Arrange
        var handler = new CodeTagHandler();

        // Assert
        _ = handler.CanHandleTag("c").Should().BeTrue();
        _ = handler.CanHandleTag("code").Should().BeTrue();
        _ = handler.CanHandleTag("para").Should().BeFalse();
    }

    [Fact]
    public void TransformTagWithInlineTagShouldWrapInBackticks()
    {
        // Arrange
        var handler = new CodeTagHandler();
        var xmlElement = XElement.Parse("<c>int</c>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Be("`int`");

        var backtickElement = XElement.Parse("<c>List`1</c>");
        StringBuilder backtickBuilder = new();
        handler.TransformTag(backtickElement, backtickBuilder, (element, builder) => builder.Append(element.Value));
        _ = backtickBuilder.ToString().Should().Be("`` List`1 ``",
            "because inline <c> content containing a backtick must be wrapped in padded double backticks.");

        var nestedFenceElement = XElement.Parse("<code>```json\n{}\n```</code>");
        StringBuilder nestedFenceBuilder = new();
        handler.TransformTag(nestedFenceElement, nestedFenceBuilder, (element, builder) => builder.Append(element.Value));
        _ = nestedFenceBuilder.ToString().Should().Contain("````csharp",
            "because <code> blocks containing triple backticks must expand the outer fence to 4 backticks.");
    }

    [Fact]
    public void TransformTagWithBlockTagShouldWrapInCSharpCodeBlock()
    {
        // Arrange
        var handler = new CodeTagHandler();
        var xmlElement = XElement.Parse("<code>var x = 10;</code>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Contain("```csharp")
            .And.Contain("var x = 10;");
    }

    [Fact]
    public void TransformTagWithLanguageAttributeAndMultilineCodeShouldUseRequestedFenceLanguageAndDedentCommonIndentation()
    {
        // Arrange
        var handler = new CodeTagHandler();
        var xmlElement = XElement.Parse(
            """
            <code language="json">
                {
                  "name": "Demo",
                  "enabled": true
                }
            </code>
            """);
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        var markdown = outputBuilder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        _ = markdown.Should().Contain("```json\n{\n  \"name\": \"Demo\",\n  \"enabled\": true\n}\n```",
            "because <code language=\"json\"> must emit a json fence and trim common leading whitespace while preserving relative inner indentation.");
    }

    [Fact]
    public void TransformTagWithLangAliasAndUnsafeFenceCharactersShouldSanitizeLanguageIdentifier()
    {
        // Arrange
        var handler = new CodeTagHandler();
        var xmlElement = XElement.Parse("<code lang=\"  bash;drop \">echo ok</code>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        var markdown = outputBuilder.ToString();
        _ = markdown.Should().Contain("```bashdrop", "because disallowed characters in lang/language must be stripped safely.")
            .And.Contain("echo ok");
    }

    [Fact]
    public void TransformTagWithWhitespaceCodeInvalidLanguageAndBlankInnerLinesShouldHandleCleanly()
    {
        // Arrange
        var handler = new CodeTagHandler();
        var whitespaceElement = XElement.Parse("<code>   \n   </code>");
        var invalidLangElement = XElement.Parse("<code lang=\"!!!\">int a = 1;</code>");
        var blankInnerLineElement = XElement.Parse("<code language=\"c++_2.0\">\n    lineOne();\n\n    lineTwo();\n</code>");

        StringBuilder whitespaceBuilder = new();
        StringBuilder invalidLangBuilder = new();
        StringBuilder blankInnerLineBuilder = new();

        // Act
        handler.TransformTag(whitespaceElement, whitespaceBuilder, (element, builder) => builder.Append(element.Value));
        handler.TransformTag(invalidLangElement, invalidLangBuilder, (element, builder) => builder.Append(element.Value));
        handler.TransformTag(blankInnerLineElement, blankInnerLineBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = whitespaceBuilder.ToString().Should().Contain("```csharp");
        _ = invalidLangBuilder.ToString().Should().Contain("```csharp", "because invalid language strings must fall back to csharp.");
        var multiLineMarkdown = blankInnerLineBuilder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        _ = multiLineMarkdown.Should().Contain("```c++_2.0\nlineOne();\n\nlineTwo();\n```",
            "because valid fence symbols should be kept and inner blank lines preserved cleanly.");
    }
}
