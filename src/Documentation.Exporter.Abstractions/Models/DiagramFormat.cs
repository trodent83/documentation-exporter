using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Well-known canonical diagram format identifier names.
/// </summary>
public static class DiagramFormatNames
{
    /// <summary>
    /// Mermaid fenced diagram syntax (<c>"Mermaid"</c>).
    /// </summary>
    public const string Mermaid = "Mermaid";

    /// <summary>
    /// PlantUML fenced diagram and standalone <c>.puml</c> syntax (<c>"PlantUml"</c>).
    /// </summary>
    public const string PlantUml = "PlantUml";

    /// <summary>
    /// Sentinel value disabling automatic diagram generation (<c>"None"</c>).
    /// </summary>
    public const string None = "None";
}

/// <summary>
/// Represents an extensible preferred diagram generator format identifier (or opt-out sentinel <see cref="None"/>)
/// for type overview pages and index pages. Built-in plugins provide <see cref="Mermaid"/> and <see cref="PlantUml"/>,
/// and external plugins can introduce additional custom format identifiers via <see cref="Custom"/> or <see cref="Parse"/>.
/// </summary>
[JsonConverter(typeof(DiagramFormatJsonConverter))]
public readonly record struct DiagramFormat
{
    private readonly string? formatValue;

    private DiagramFormat(string formatValue) => this.formatValue = formatValue;

    /// <summary>
    /// Prefer rendering diagrams using Mermaid (<c>```mermaid</c>), falling back to another registered generator only when Mermaid is unsupported for that diagram type.
    /// </summary>
    public static DiagramFormat Mermaid { get; } = new(DiagramFormatNames.Mermaid);

    /// <summary>
    /// Prefer rendering diagrams using PlantUML (<c>```plantuml</c>) and standalone <c>.puml</c> files, falling back to another registered generator only when PlantUML is unsupported for that diagram type.
    /// </summary>
    public static DiagramFormat PlantUml { get; } = new(DiagramFormatNames.PlantUml);

    /// <summary>
    /// Disable automatic diagram generation with no fallback.
    /// </summary>
    public static DiagramFormat None { get; } = new(DiagramFormatNames.None);

    /// <summary>
    /// Gets the canonical format identifier string, or an empty string if uninitialized.
    /// </summary>
    public string Value => formatValue ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this format represents the opt-out sentinel <see cref="None"/>.
    /// </summary>
    public bool IsNone => string.Equals(Value, DiagramFormatNames.None, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this instance holds a syntactically valid diagram format name.
    /// </summary>
    public bool IsValid => IsValidFormatName(Value);

    /// <summary>
    /// Parses a diagram format name string into a <see cref="DiagramFormat"/>.
    /// </summary>
    /// <param name="formatName">The diagram format identifier (must start with an ASCII letter and contain only ASCII letters, digits, hyphens, or underscores).</param>
    /// <returns>The parsed <see cref="DiagramFormat"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="formatName"/> is null, whitespace, or contains invalid characters.</exception>
    public static DiagramFormat Parse(string formatName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formatName);

        var trimmedName = formatName.Trim();
        return !IsValidFormatName(trimmedName)
            ? throw new ArgumentException(
                $"Invalid diagram format '{formatName}'. Format names must start with an ASCII letter and contain only letters, digits, '-' or '_'.",
                nameof(formatName))
            : NormalizeWellKnown(trimmedName);
    }

    /// <summary>
    /// Creates a <see cref="DiagramFormat"/> for a plugin-defined custom diagram syntax (such as <c>"D2"</c> or <c>"Graphviz"</c>).
    /// </summary>
    /// <param name="formatName">The custom diagram format name.</param>
    /// <returns>A validated <see cref="DiagramFormat"/> instance.</returns>
    public static DiagramFormat Custom(string formatName) => Parse(formatName);

    /// <summary>
    /// Attempts to parse <paramref name="rawValue"/> into a validated <see cref="DiagramFormat"/>.
    /// </summary>
    /// <param name="rawValue">The raw format identifier string.</param>
    /// <param name="diagramFormat">Receives the parsed format when valid; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="rawValue"/> is valid; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? rawValue, out DiagramFormat diagramFormat)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            diagramFormat = default;
            return false;
        }

        var trimmedName = rawValue.Trim();
        if (!IsValidFormatName(trimmedName))
        {
            diagramFormat = default;
            return false;
        }

        diagramFormat = NormalizeWellKnown(trimmedName);
        return true;
    }

    /// <summary>
    /// Compares two <see cref="DiagramFormat"/> values case-insensitively.
    /// </summary>
    /// <param name="other">The format to compare with.</param>
    /// <returns><see langword="true"/> if both format names match case-insensitively; otherwise <see langword="false"/>.</returns>
    public bool Equals(DiagramFormat other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    private static DiagramFormat NormalizeWellKnown(string trimmedName) =>
        trimmedName switch
        {
            _ when string.Equals(trimmedName, DiagramFormatNames.Mermaid, StringComparison.OrdinalIgnoreCase) => Mermaid,
            _ when string.Equals(trimmedName, DiagramFormatNames.PlantUml, StringComparison.OrdinalIgnoreCase) => PlantUml,
            _ when string.Equals(trimmedName, DiagramFormatNames.None, StringComparison.OrdinalIgnoreCase) => None,
            _ => new DiagramFormat(trimmedName)
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
/// Validates that a <see cref="DiagramFormat"/> property carries a syntactically valid format identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValidDiagramFormatAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is DiagramFormat diagramFormat && diagramFormat.IsValid;
}

/// <summary>
/// Serializes and deserializes <see cref="DiagramFormat"/> values as JSON strings, supporting both built-in
/// formats (<c>"Mermaid"</c>, <c>"PlantUml"</c>, <c>"None"</c>) and custom plugin format names.
/// </summary>
public sealed class DiagramFormatJsonConverter : JsonConverter<DiagramFormat>
{
    /// <inheritdoc />
    public override DiagramFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("DiagramFormat must be serialized as a JSON string.");
        }

        var rawValue = reader.GetString();
        return !DiagramFormat.TryParse(rawValue, out var parsedFormat)
            ? throw new JsonException($"Invalid DiagramFormat value '{rawValue}'.")
            : parsedFormat;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DiagramFormat value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}
