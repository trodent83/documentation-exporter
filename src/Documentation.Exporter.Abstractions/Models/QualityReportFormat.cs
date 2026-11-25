using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Well-known canonical quality report format identifier names.
/// </summary>
public static class QualityReportFormatNames
{
    /// <summary>
    /// Structured JSON quality report containing summary metrics and diagnostics (<c>"Json"</c>).
    /// </summary>
    public const string Json = "Json";

    /// <summary>
    /// GitHub Flavored Markdown summary report suitable for pull-request comments or job summaries (<c>"Markdown"</c>).
    /// </summary>
    public const string Markdown = "Markdown";

    /// <summary>
    /// Static Analysis Results Interchange Format (SARIF v2.1.0) JSON compatible with code scanning tools (<c>"Sarif"</c>).
    /// </summary>
    public const string Sarif = "Sarif";
}

/// <summary>
/// Represents an extensible serialization format identifier for emitted documentation quality reports.
/// Built-in formatters provide <see cref="Json"/>, <see cref="Markdown"/>, and <see cref="Sarif"/>, and
/// external plugins can introduce additional custom format identifiers (such as <c>"JUnit"</c> or <c>"Html"</c>)
/// via <see cref="Custom"/> or <see cref="Parse"/>.
/// </summary>
[JsonConverter(typeof(QualityReportFormatJsonConverter))]
public readonly record struct QualityReportFormat
{
    private readonly string? formatValue;

    private QualityReportFormat(string formatValue) => this.formatValue = formatValue;

    /// <summary>
    /// Structured JSON quality report containing summary metrics and diagnostics.
    /// </summary>
    public static QualityReportFormat Json { get; } = new(QualityReportFormatNames.Json);

    /// <summary>
    /// GitHub Flavored Markdown summary report suitable for pull-request comments or job summaries.
    /// </summary>
    public static QualityReportFormat Markdown { get; } = new(QualityReportFormatNames.Markdown);

    /// <summary>
    /// Static Analysis Results Interchange Format (SARIF v2.1.0) JSON compatible with code scanning tools.
    /// </summary>
    public static QualityReportFormat Sarif { get; } = new(QualityReportFormatNames.Sarif);

    /// <summary>
    /// Gets the canonical format identifier string, or an empty string if uninitialized.
    /// </summary>
    public string Value => formatValue ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this instance holds a syntactically valid quality report format name.
    /// </summary>
    public bool IsValid => IsValidFormatName(Value);

    /// <summary>
    /// Parses a quality report format name string into a <see cref="QualityReportFormat"/>.
    /// </summary>
    /// <param name="formatName">The format identifier (must start with an ASCII letter and contain only ASCII letters, digits, hyphens, or underscores).</param>
    /// <returns>The parsed <see cref="QualityReportFormat"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="formatName"/> is null, whitespace, or contains invalid characters.</exception>
    public static QualityReportFormat Parse(string formatName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formatName);

        var trimmedName = formatName.Trim();
        return !IsValidFormatName(trimmedName)
            ? throw new ArgumentException(
                $"Invalid quality report format '{formatName}'. Format names must start with an ASCII letter and contain only letters, digits, '-' or '_'.",
                nameof(formatName))
            : NormalizeWellKnown(trimmedName);
    }

    /// <summary>
    /// Creates a <see cref="QualityReportFormat"/> for a plugin-defined custom report serialization (such as <c>"JUnit"</c> or <c>"Html"</c>).
    /// </summary>
    /// <param name="formatName">The custom quality report format name.</param>
    /// <returns>A validated <see cref="QualityReportFormat"/> instance.</returns>
    public static QualityReportFormat Custom(string formatName) => Parse(formatName);

    /// <summary>
    /// Attempts to parse <paramref name="rawValue"/> into a validated <see cref="QualityReportFormat"/>.
    /// </summary>
    /// <param name="rawValue">The raw format identifier string.</param>
    /// <param name="reportFormat">Receives the parsed format when valid; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="rawValue"/> is valid; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? rawValue, out QualityReportFormat reportFormat)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            reportFormat = default;
            return false;
        }

        var trimmedName = rawValue.Trim();
        if (!IsValidFormatName(trimmedName))
        {
            reportFormat = default;
            return false;
        }

        reportFormat = NormalizeWellKnown(trimmedName);
        return true;
    }

    /// <summary>
    /// Compares two <see cref="QualityReportFormat"/> values case-insensitively.
    /// </summary>
    /// <param name="other">The format to compare with.</param>
    /// <returns><see langword="true"/> if both format names match case-insensitively; otherwise <see langword="false"/>.</returns>
    public bool Equals(QualityReportFormat other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    private static QualityReportFormat NormalizeWellKnown(string trimmedName) =>
        trimmedName switch
        {
            _ when string.Equals(trimmedName, QualityReportFormatNames.Json, StringComparison.OrdinalIgnoreCase) => Json,
            _ when string.Equals(trimmedName, QualityReportFormatNames.Markdown, StringComparison.OrdinalIgnoreCase) => Markdown,
            _ when string.Equals(trimmedName, QualityReportFormatNames.Sarif, StringComparison.OrdinalIgnoreCase) => Sarif,
            _ => new QualityReportFormat(trimmedName)
        };

    private static bool IsValidFormatName(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || !char.IsAsciiLetter(candidate[0]))
        {
            return false;
        }

        foreach (var character in candidate)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Validates that a <see cref="QualityReportFormat"/> property carries a syntactically valid format identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValidQualityReportFormatAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is QualityReportFormat reportFormat && reportFormat.IsValid;
}

/// <summary>
/// Serializes and deserializes <see cref="QualityReportFormat"/> values as JSON strings, supporting both built-in
/// formats (<c>"Json"</c>, <c>"Markdown"</c>, <c>"Sarif"</c>) and custom plugin format names.
/// </summary>
public sealed class QualityReportFormatJsonConverter : JsonConverter<QualityReportFormat>
{
    /// <inheritdoc />
    public override QualityReportFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("QualityReportFormat must be serialized as a JSON string.");
        }

        var rawValue = reader.GetString();
        return !QualityReportFormat.TryParse(rawValue, out var parsedFormat)
            ? throw new JsonException($"Invalid QualityReportFormat value '{rawValue}'.")
            : parsedFormat;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, QualityReportFormat value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}
