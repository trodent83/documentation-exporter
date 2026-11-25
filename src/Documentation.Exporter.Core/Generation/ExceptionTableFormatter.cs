using System.Globalization;
using System.Text;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Default implementation of <see cref="IExceptionTableFormatter"/>.
/// </summary>
public sealed class ExceptionTableFormatter : IExceptionTableFormatter
{
    /// <inheritdoc />
    public string FormatExceptionsTable(IReadOnlyDictionary<string, string> exceptions, string headingPrefix = "####")
    {
        ArgumentNullException.ThrowIfNull(exceptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(headingPrefix);

        if (exceptions.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder tableBuilder = new();
        _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"{headingPrefix} {MarkdownGenerationResources.ExceptionsHeading}");
        _ = tableBuilder.AppendLine();
        _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {MarkdownGenerationResources.TableExceptionHeader} | {MarkdownGenerationResources.TableConditionHeader} |");
        _ = tableBuilder.AppendLine("| :--- | :--- |");

        foreach (var exceptionPair in exceptions)
        {
            var formattedException = exceptionPair.Key.Contains("](", StringComparison.Ordinal)
                ? exceptionPair.Key
                : $"`{exceptionPair.Key}`";
            var sanitizedCondition = FormatTableCell(exceptionPair.Value);
            _ = tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {formattedException} | {sanitizedCondition} |");
        }

        _ = tableBuilder.AppendLine();
        return tableBuilder.ToString();
    }

    private static string FormatTableCell(string? rawText) =>
        string.IsNullOrWhiteSpace(rawText)
            ? string.Empty
            : rawText
                .Replace("\r\n", "<br/>", StringComparison.Ordinal)
                .Replace("\n", "<br/>", StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("|", "\\|", StringComparison.Ordinal)
                .Trim();
}
