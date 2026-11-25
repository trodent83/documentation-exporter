using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Handles inline code tags (<c>&lt;c&gt;</c>) and multi-line code blocks (<c>&lt;code language="..."&gt;</c>)
/// with common leading-indentation removal.
/// </summary>
public sealed class CodeTagHandler : IXmlTagHandler
{
    private const string DefaultFenceLanguage = "csharp";

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) =>
        string.Equals(tagName, "c", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(tagName, "code", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        if (string.Equals(xmlElement.Name.LocalName, "c", StringComparison.OrdinalIgnoreCase))
        {
            StringBuilder inlineBuilder = new();
            processChildrenAction(xmlElement, inlineBuilder);
            var inlineText = inlineBuilder.ToString();
            var formattedInline = inlineText.Contains('`', StringComparison.Ordinal)
                ? $"`` {inlineText} ``"
                : $"`{inlineText}`";
            _ = outputBuilder.Append(formattedInline);
            return;
        }

        var rawLanguage = xmlElement.Attribute("language")?.Value ?? xmlElement.Attribute("lang")?.Value;
        var fenceLanguage = SanitizeFenceLanguage(rawLanguage);
        var dedentedCode = DedentCodeBlock(xmlElement.Value);
        var fenceDelimiter = dedentedCode.Contains("```", StringComparison.Ordinal) ? "````" : "```";

        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"{fenceDelimiter}{fenceLanguage}");
        _ = outputBuilder.AppendLine(dedentedCode);
        _ = outputBuilder.AppendLine(fenceDelimiter);
    }

    private static string SanitizeFenceLanguage(string? rawLanguage)
    {
        if (string.IsNullOrWhiteSpace(rawLanguage))
        {
            return DefaultFenceLanguage;
        }

        StringBuilder sanitizedBuilder = new();
        foreach (var character in rawLanguage.Trim())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_' or '#' or '+' or '.')
            {
                _ = sanitizedBuilder.Append(character);
            }
        }

        return sanitizedBuilder.Length > 0 ? sanitizedBuilder.ToString() : DefaultFenceLanguage;
    }

    private static string DedentCodeBlock(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return string.Empty;
        }

        var normalized = rawCode
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var rawLines = normalized.Split('\n');

        var startIndex = 0;
        while (startIndex < rawLines.Length && string.IsNullOrWhiteSpace(rawLines[startIndex]))
        {
            startIndex++;
        }

        var endIndex = rawLines.Length - 1;
        while (endIndex >= startIndex && string.IsNullOrWhiteSpace(rawLines[endIndex]))
        {
            endIndex--;
        }

        var minimumIndent = int.MaxValue;
        for (var lineIndex = startIndex; lineIndex <= endIndex; lineIndex++)
        {
            var currentLine = rawLines[lineIndex];
            if (string.IsNullOrWhiteSpace(currentLine))
            {
                continue;
            }

            var leadingSpaces = CountLeadingWhitespace(currentLine);
            if (leadingSpaces < minimumIndent)
            {
                minimumIndent = leadingSpaces;
            }
        }

        if (minimumIndent == int.MaxValue)
        {
            minimumIndent = 0;
        }

        List<string> dedentedLines = [];
        for (var lineIndex = startIndex; lineIndex <= endIndex; lineIndex++)
        {
            var currentLine = rawLines[lineIndex];
            if (string.IsNullOrWhiteSpace(currentLine))
            {
                dedentedLines.Add(string.Empty);
                continue;
            }

            dedentedLines.Add(minimumIndent > 0 && currentLine.Length >= minimumIndent
                ? currentLine[minimumIndent..].TrimEnd()
                : currentLine.TrimEnd());
        }

        return string.Join('\n', dedentedLines);
    }

    private static int CountLeadingWhitespace(string line)
    {
        var whitespaceCount = 0;
        while (whitespaceCount < line.Length && char.IsWhiteSpace(line[whitespaceCount]))
        {
            whitespaceCount++;
        }

        return whitespaceCount;
    }
}
