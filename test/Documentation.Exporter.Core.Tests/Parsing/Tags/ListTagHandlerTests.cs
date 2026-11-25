using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Core.Parsing.Tags;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing.Tags;

[UnitTest]
public sealed class ListTagHandlerTests
{
    [Fact]
    public void CanHandleTagShouldRecognizeList()
    {
        // Arrange
        var handler = new ListTagHandler();

        // Assert
        _ = handler.CanHandleTag("list").Should().BeTrue();
        _ = handler.CanHandleTag("item").Should().BeFalse();
    }

    [Fact]
    public void TransformTagWithNumberTypeShouldRenderNumberedList()
    {
        // Arrange
        var handler = new ListTagHandler();
        var xmlElement = XElement.Parse("<list type=\"number\"><item><description>First item</description></item><item><description>Second item</description></item></list>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Contain("1. First item")
            .And.Contain("2. Second item");
    }

    [Fact]
    public void TransformTagWithBulletTypeShouldRenderTermAndDescriptionPairsAndDescriptionOnlyItems()
    {
        // Arrange
        var handler = new ListTagHandler();
        var xmlElement = XElement.Parse(
            "<list type=\"bullet\">" +
            "<item><term>Key</term><description>Desc</description></item>" +
            "<item><term>TermOnly</term></item>" +
            "<item><description>DescriptionOnly</description></item>" +
            "</list>");
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        var markdown = outputBuilder.ToString();
        _ = markdown.Should().Contain("* **Key**: Desc", "because items with both <term> and <description> should render bolded terms paired with descriptions.");
        _ = markdown.Should().Contain("* **TermOnly**", "because items with only <term> should render the bolded term.");
        _ = markdown.Should().Contain("* DescriptionOnly", "because description-only items should render cleanly.");
    }

    [Fact]
    public void TransformTagWithTableTypeAndListHeaderShouldRenderGitHubFlavoredTableAndEscapePipesAndNewlines()
    {
        // Arrange
        var handler = new ListTagHandler();
        var xmlElement = XElement.Parse(
            """
            <list type="table">
              <listheader>
                <term>Mode</term>
                <description>Behavior</description>
                <description>Notes</description>
              </listheader>
              <item>
                <term>Fast | Direct</term>
                <description>Line1
            Line2</description>
              </item>
            </list>
            """);
        StringBuilder outputBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        var markdown = outputBuilder.ToString();
        _ = markdown.Should().Contain("| Mode | Behavior | Notes |", "because <listheader> defines table column headers.");
        _ = markdown.Should().Contain("| :--- | :--- | :--- |", "because GFM tables require header separator rows.");
        _ = markdown.Should().Contain("| Fast \\| Direct | Line1<br/>Line2 |  |", "because pipes and line breaks inside table cells must be escaped cleanly and missing columns padded.");
    }

    [Fact]
    public void TransformTagWithTableTypeWithoutListHeaderShouldUseDefaultHeadersAndIgnoreEmptyTables()
    {
        // Arrange
        var handler = new ListTagHandler();
        var twoColumnTable = XElement.Parse("<list type=\"table\"><item><term>Alpha</term><description>First</description></item></list>");
        var threeColumnTable = XElement.Parse("<list type=\"table\"><item><term>A</term><description>B</description><description>C</description></item></list>");
        var emptyTable = XElement.Parse("<list type=\"table\"></list>");

        StringBuilder twoColumnBuilder = new();
        StringBuilder threeColumnBuilder = new();
        StringBuilder emptyTableBuilder = new();

        // Act
        handler.TransformTag(twoColumnTable, twoColumnBuilder, (element, builder) => builder.Append(element.Value));
        handler.TransformTag(threeColumnTable, threeColumnBuilder, (element, builder) => builder.Append(element.Value));
        handler.TransformTag(emptyTable, emptyTableBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = twoColumnBuilder.ToString().Should().Contain("| Term | Description |")
            .And.Contain("| Alpha | First |");
        _ = threeColumnBuilder.ToString().Should().Contain("| Column 1 | Column 2 | Column 3 |");
        _ = emptyTableBuilder.ToString().Should().BeEmpty("because empty <list type=\"table\"> blocks must not emit broken headers.");
    }

    [Fact]
    public void TransformTagWithoutTypeAttributeOrDescriptionElementShouldDefaultToBulletsAndValidateNulls()
    {
        // Arrange
        var handler = new ListTagHandler();
        var xmlElement = XElement.Parse("<list><item>Direct item text</item><item>   </item></list>");
        var singleColumnTable = XElement.Parse("<list type=\"table\"><listheader><description>SingleHeader</description></listheader><item><description>CellVal</description></item></list>");
        StringBuilder outputBuilder = new();
        StringBuilder singleColumnBuilder = new();

        // Act
        handler.TransformTag(xmlElement, outputBuilder, (element, builder) => builder.Append(element.Value));
        handler.TransformTag(singleColumnTable, singleColumnBuilder, (element, builder) => builder.Append(element.Value));

        // Assert
        _ = outputBuilder.ToString().Should().Contain("* Direct item text");
        _ = singleColumnBuilder.ToString().Should().Contain("| SingleHeader |").And.Contain("| CellVal |");
        _ = FluentActions.Invoking(() => handler.TransformTag(null!, outputBuilder, (_, _) => { }))
            .Should().Throw<ArgumentNullException>("because xmlElement cannot be null.");
        _ = FluentActions.Invoking(() => handler.TransformTag(xmlElement, null!, (_, _) => { }))
            .Should().Throw<ArgumentNullException>("because outputBuilder cannot be null.");
        _ = FluentActions.Invoking(() => handler.TransformTag(xmlElement, outputBuilder, null!))
            .Should().Throw<ArgumentNullException>("because processChildrenAction cannot be null.");
    }
}
