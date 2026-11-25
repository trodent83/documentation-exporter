using Documentation.Exporter.Abstractions.Parsing.Tags;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Documentation.Exporter.Core.Parsing.Tags;

/// <summary>
/// Handles XML documentation <c>&lt;note type="..."&gt;</c> elements and renders them as GitHub Alert callout blockquotes.
/// </summary>
public sealed class NoteTagHandler : IXmlTagHandler
{
    /// <inheritdoc />
    public int Order => 55;

    /// <inheritdoc />
    public bool CanHandleTag(string tagName) =>
        string.Equals(tagName, "note", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void TransformTag(
        XElement xmlElement,
        StringBuilder outputBuilder,
        Action<XElement, StringBuilder> processChildrenAction)
    {
        ArgumentNullException.ThrowIfNull(xmlElement);
        ArgumentNullException.ThrowIfNull(outputBuilder);
        ArgumentNullException.ThrowIfNull(processChildrenAction);

        StringBuilder contentBuilder = new();
        processChildrenAction(xmlElement, contentBuilder);
        var noteBody = contentBuilder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(noteBody))
        {
            return;
        }

        var alertType = MapNoteTypeToGitHubAlert(xmlElement.Attribute("type")?.Value);

        _ = outputBuilder.AppendLine();
        _ = outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"> [!{alertType}]");

        var normalizedLines = noteBody
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        foreach (var line in normalizedLines)
        {
            _ = string.IsNullOrWhiteSpace(line)
                ? outputBuilder.AppendLine(">")
                : outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"> {line.TrimEnd()}");
        }

        _ = outputBuilder.AppendLine();
    }

    private static string MapNoteTypeToGitHubAlert(string? noteType) =>
        noteType?.Trim().ToLowerInvariant() switch
        {
            "tip" => "TIP",
            "important" or "implement" or "inherit" or "caller" => "IMPORTANT",
            "warning" => "WARNING",
            "caution" or "security" => "CAUTION",
            _ => "NOTE"
        };
}
