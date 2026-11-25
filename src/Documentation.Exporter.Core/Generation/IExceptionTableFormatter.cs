namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Formats documented XML exception dictionaries into Markdown tables.
/// </summary>
public interface IExceptionTableFormatter
{
    /// <summary>
    /// Formats documented exceptions into a Markdown table preceded by the specified heading level.
    /// </summary>
    /// <param name="exceptions">Mapping of exception type links/names to failure condition descriptions.</param>
    /// <param name="headingPrefix">The Markdown heading prefix (for example <c>"##"</c> or <c>"####"</c>).</param>
    /// <returns>Formatted Markdown table section, or an empty string when <paramref name="exceptions"/> is empty.</returns>
    string FormatExceptionsTable(IReadOnlyDictionary<string, string> exceptions, string headingPrefix = "####");
}
