using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Well-known canonical static-site navigation format identifier names.
/// </summary>
public static class NavigationFormatNames
{
    /// <summary>
    /// MkDocs YAML navigation (<c>mkdocs.nav.yml</c> / <c>mkdocs.yml</c>) (<c>"MkDocs"</c>).
    /// </summary>
    public const string MkDocs = "MkDocs";

    /// <summary>
    /// Sentinel value disabling automatic navigation file generation (<c>"None"</c>).
    /// </summary>
    public const string None = "None";
}

/// <summary>
/// Represents an extensible static-site navigation generator format identifier (or opt-out sentinel <see cref="None"/>).
/// The built-in plugin provides <see cref="MkDocs"/>, and external plugins can introduce additional target site
/// generators (such as <c>"Docusaurus"</c>, <c>"DocFx"</c>, or <c>"VitePress"</c>) via <see cref="Custom"/> or <see cref="Parse"/>.
/// </summary>
[JsonConverter(typeof(NavigationFormatJsonConverter))]
public readonly record struct NavigationFormat
{
    private readonly string? formatValue;

    private NavigationFormat(string formatValue) => this.formatValue = formatValue;

    /// <summary>
    /// Generate or merge MkDocs YAML navigation entries (<c>mkdocs.nav.yml</c> or <c>mkdocs.yml</c>).
    /// </summary>
    public static NavigationFormat MkDocs { get; } = new(NavigationFormatNames.MkDocs);

    /// <summary>
    /// Disable automatic navigation file generation.
    /// </summary>
    public static NavigationFormat None { get; } = new(NavigationFormatNames.None);

    /// <summary>
    /// Gets the canonical format identifier string, or an empty string if uninitialized.
    /// </summary>
    public string Value => formatValue ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this format represents the opt-out sentinel <see cref="None"/>.
    /// </summary>
    public bool IsNone => string.Equals(Value, NavigationFormatNames.None, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this instance holds a syntactically valid navigation format name.
    /// </summary>
    public bool IsValid => IsValidFormatName(Value);

    /// <summary>
    /// Parses a navigation format name string into a <see cref="NavigationFormat"/>.
    /// </summary>
    /// <param name="formatName">The navigation format identifier (must start with an ASCII letter and contain only ASCII letters, digits, hyphens, or underscores).</param>
    /// <returns>The parsed <see cref="NavigationFormat"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="formatName"/> is null, whitespace, or contains invalid characters.</exception>
    public static NavigationFormat Parse(string formatName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formatName);

        var trimmedName = formatName.Trim();
        return !IsValidFormatName(trimmedName)
            ? throw new ArgumentException(
                $"Invalid navigation format '{formatName}'. Format names must start with an ASCII letter and contain only letters, digits, '-' or '_'.",
                nameof(formatName))
            : NormalizeWellKnown(trimmedName);
    }

    /// <summary>
    /// Creates a <see cref="NavigationFormat"/> for a plugin-defined custom navigation output (such as <c>"Docusaurus"</c> or <c>"DocFx"</c>).
    /// </summary>
    /// <param name="formatName">The custom navigation format name.</param>
    /// <returns>A validated <see cref="NavigationFormat"/> instance.</returns>
    public static NavigationFormat Custom(string formatName) => Parse(formatName);

    /// <summary>
    /// Attempts to parse <paramref name="rawValue"/> into a validated <see cref="NavigationFormat"/>.
    /// </summary>
    /// <param name="rawValue">The raw format identifier string.</param>
    /// <param name="navigationFormat">Receives the parsed format when valid; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="rawValue"/> is valid; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? rawValue, out NavigationFormat navigationFormat)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            navigationFormat = default;
            return false;
        }

        var trimmedName = rawValue.Trim();
        if (!IsValidFormatName(trimmedName))
        {
            navigationFormat = default;
            return false;
        }

        navigationFormat = NormalizeWellKnown(trimmedName);
        return true;
    }

    /// <summary>
    /// Compares two <see cref="NavigationFormat"/> values case-insensitively.
    /// </summary>
    /// <param name="other">The format to compare with.</param>
    /// <returns><see langword="true"/> if both format names match case-insensitively; otherwise <see langword="false"/>.</returns>
    public bool Equals(NavigationFormat other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    private static NavigationFormat NormalizeWellKnown(string trimmedName) =>
        trimmedName switch
        {
            _ when string.Equals(trimmedName, NavigationFormatNames.MkDocs, StringComparison.OrdinalIgnoreCase) => MkDocs,
            _ when string.Equals(trimmedName, NavigationFormatNames.None, StringComparison.OrdinalIgnoreCase) => None,
            _ => new NavigationFormat(trimmedName)
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
/// Validates that a <see cref="NavigationFormat"/> property carries a syntactically valid format identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValidNavigationFormatAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is NavigationFormat navigationFormat && navigationFormat.IsValid;
}

/// <summary>
/// Serializes and deserializes <see cref="NavigationFormat"/> values as JSON strings, supporting both built-in
/// formats (<c>"MkDocs"</c>, <c>"None"</c>) and custom plugin format names.
/// </summary>
public sealed class NavigationFormatJsonConverter : JsonConverter<NavigationFormat>
{
    /// <inheritdoc />
    public override NavigationFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("NavigationFormat must be serialized as a JSON string.");
        }

        var rawValue = reader.GetString();
        return !NavigationFormat.TryParse(rawValue, out var parsedFormat)
            ? throw new JsonException($"Invalid NavigationFormat value '{rawValue}'.")
            : parsedFormat;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, NavigationFormat value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}
