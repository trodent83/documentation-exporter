using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Handles bulleted, numbered, and table list tags (<c>&lt;list type="bullet | number | table"&gt;</c>),
/// including <c>&lt;listheader&gt;</c>, <c>&lt;term&gt;</c>, and <c>&lt;description&gt;</c> elements.
/// </summary>
public sealed partial class ListTagHandler : IXmlTagHandler
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) =>
        string.Equals(tagName, "list", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        var listType = xmlElement.Attribute("type")?.Value?.Trim().ToLowerInvariant() ?? "bullet";
        if (string.Equals(listType, "table", StringComparison.Ordinal))
        {
            RenderTableList(xmlElement, outputBuilder, processChildrenAction);
            return;
        }

        _ = outputBuilder.AppendLine();

        var isNumbered = string.Equals(listType, "number", StringComparison.Ordinal);
        var listItemNumber = 1;
        foreach (var itemElement in xmlElement.Elements("item"))
        {
            var itemContent = RenderBulletOrNumberItem(itemElement, processChildrenAction);
            if (isNumbered)
            {
                _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"{listItemNumber}. {itemContent}");
                listItemNumber++;
            }
            else
            {
                _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"* {itemContent}");
            }
        }

        _ = outputBuilder.AppendLine();
    }

    private static string RenderBulletOrNumberItem(
        XElement itemElement,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        var termElement = itemElement.Element("term");
        var descriptionElement = itemElement.Element("description");

        if (termElement == null && descriptionElement == null)
        {
            return RenderElementContent(itemElement, processChildrenAction);
        }

        var termText = termElement != null
            ? RenderElementContent(termElement, processChildrenAction)
            : string.Empty;
        var descriptionText = descriptionElement != null
            ? RenderElementContent(descriptionElement, processChildrenAction)
            : string.Empty;

        return !string.IsNullOrEmpty(termText) && !string.IsNullOrEmpty(descriptionText)
            ? $"**{termText}**: {descriptionText}"
            : !string.IsNullOrEmpty(termText)
                ? $"**{termText}**"
                : descriptionText;
    }

    private static void RenderTableList(
        XElement listElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        var listHeaderElement = listElement.Element("listheader");
        List<string> headerCells = listHeaderElement != null
            ? ExtractRowCells(listHeaderElement, processChildrenAction)
            : [];

        List<List<string>> itemRows =
        [
            .. listElement.Elements("item")
                .Select(itemElement => ExtractRowCells(itemElement, processChildrenAction))
        ];

        var columnCount = Math.Max(
            headerCells.Count,
            itemRows.Count > 0 ? itemRows.Max(tableRow => tableRow.Count) : 0);

        if (columnCount == 0)
        {
            return;
        }

        List<string> normalizedHeaders = headerCells.Count > 0
            ? PadCells(headerCells, columnCount)
            : BuildDefaultHeaders(columnCount);

        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {string.Join(" | ", normalizedHeaders)} |");
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {string.Join(" | ", Enumerable.Repeat(":---", columnCount))} |");

        foreach (var itemRow in itemRows)
        {
            var normalizedRow = PadCells(itemRow, columnCount);
            _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {string.Join(" | ", normalizedRow)} |");
        }

        _ = outputBuilder.AppendLine();
    }

    private static List<string> ExtractRowCells(
        XElement rowElement,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        List<XElement> cellElements =
        [
            .. rowElement.Elements()
                .Where(child => string.Equals(child.Name.LocalName, "term", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(child.Name.LocalName, "description", StringComparison.OrdinalIgnoreCase))
        ];

        if (cellElements.Count == 0)
        {
            var fallbackContent = NormalizeTableCell(RenderElementContent(rowElement, processChildrenAction));
            return string.IsNullOrEmpty(fallbackContent) ? [] : [fallbackContent];
        }

        return
        [
            .. cellElements.Select(cellElement =>
                NormalizeTableCell(RenderElementContent(cellElement, processChildrenAction)))
        ];
    }

    private static List<string> BuildDefaultHeaders(int columnCount) =>
        columnCount == 2
            ?
            [
                NormalizeTableCell(MarkdownGenerationResources.TableTermHeader),
                NormalizeTableCell(MarkdownGenerationResources.TableDescriptionHeader)
            ]
            :
            [
                .. Enumerable.Range(1, columnCount)
                    .Select(columnIndex => string.Create(CultureInfo.InvariantCulture, $"Column {columnIndex}"))
            ];

    private static List<string> PadCells(List<string> cells, int targetColumnCount)
    {
        if (cells.Count >= targetColumnCount)
        {
            return cells;
        }

        List<string> padded = [.. cells];
        while (padded.Count < targetColumnCount)
        {
            padded.Add(string.Empty);
        }

        return padded;
    }

    private static string RenderElementContent(
        XElement element,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        StringBuilder contentBuilder = new();
        processChildrenAction(element, contentBuilder);
        return contentBuilder.ToString().Trim();
    }

    private static string NormalizeTableCell(string cellContent) =>
        LineBreakRegex().Replace(cellContent.Trim(), "<br/>")
            .Replace("|", "\\|", StringComparison.Ordinal);

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex LineBreakRegex();
}
