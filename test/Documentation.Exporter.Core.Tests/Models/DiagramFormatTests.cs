using System.Text.Json;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class DiagramFormatTests
{
    private sealed class CustomFormatContainer
    {
        public DiagramFormat PreferredDiagramFormat { get; set; }
    }

    [Fact]
    public void WellKnownPropertiesAndCustomParseShouldNormalizeCaseInsensitively()
    {
        // Act
        var parsedMermaid = DiagramFormat.Parse("  mermaid  ");
        var parsedPlantUml = DiagramFormat.Parse("plantuml");
        var parsedNone = DiagramFormat.Parse("NONE");
        var customD2 = DiagramFormat.Custom("D2");
        var customLowerD2 = DiagramFormat.Parse("d2");

        // Assert
        _ = parsedMermaid.Should().Be(DiagramFormat.Mermaid);
        _ = parsedMermaid.Value.Should().Be(DiagramFormatNames.Mermaid);
        _ = parsedMermaid.IsValid.Should().BeTrue();
        _ = parsedMermaid.IsNone.Should().BeFalse();

        _ = parsedPlantUml.Should().Be(DiagramFormat.PlantUml);
        _ = parsedPlantUml.Value.Should().Be(DiagramFormatNames.PlantUml);

        _ = parsedNone.Should().Be(DiagramFormat.None);
        _ = parsedNone.IsNone.Should().BeTrue();
        _ = parsedNone.IsValid.Should().BeTrue();

        _ = customD2.Should().Be(customLowerD2, "because DiagramFormat equality is case-insensitive.");
        _ = customD2.GetHashCode().Should().Be(customLowerD2.GetHashCode());
        _ = customD2.ToString().Should().Be("D2");
        _ = customD2.IsValid.Should().BeTrue();
        _ = customD2.IsNone.Should().BeFalse();

        _ = default(DiagramFormat).Value.Should().BeEmpty();
        _ = default(DiagramFormat).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123Diagram")]
    [InlineData("D2 Format")]
    [InlineData("D2$Custom")]
    public void TryParseAndParseWhenInvalidShouldFailValidation(string? invalidFormatName)
    {
        // Act
        var tryResult = DiagramFormat.TryParse(invalidFormatName, out var parsedFormat);

        // Assert
        _ = tryResult.Should().BeFalse($"because '{invalidFormatName}' is not a valid diagram format identifier.");
        _ = parsedFormat.Should().Be(default(DiagramFormat));

        if (string.IsNullOrWhiteSpace(invalidFormatName))
        {
            var act = () => DiagramFormat.Parse(invalidFormatName!);
            _ = act.Should().Throw<ArgumentException>();
        }
        else
        {
            var act = () => DiagramFormat.Parse(invalidFormatName);
            _ = act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void ValidDiagramFormatAttributeAndJsonConverterShouldRoundTripCustomAndRejectInvalid()
    {
        // Arrange
        ValidDiagramFormatAttribute attribute = new();
        CustomFormatContainer container = new()
        {
            PreferredDiagramFormat = DiagramFormat.Custom("Graphviz_Dot-1")
        };

        // Act
        var json = JsonSerializer.Serialize(container);
        var roundTripped = JsonSerializer.Deserialize<CustomFormatContainer>(json);

        // Assert
        _ = attribute.IsValid(DiagramFormat.Mermaid).Should().BeTrue();
        _ = attribute.IsValid(DiagramFormat.Custom("D2")).Should().BeTrue();
        _ = attribute.IsValid(default(DiagramFormat)).Should().BeFalse();
        _ = attribute.IsValid("Mermaid").Should().BeFalse("because the attribute validates strongly-typed DiagramFormat values.");

        _ = json.Should().Contain("\"Graphviz_Dot-1\"");
        _ = roundTripped.Should().NotBeNull();
        _ = roundTripped!.PreferredDiagramFormat.Should().Be(DiagramFormat.Custom("Graphviz_Dot-1"));

        var deserializeNumberAction = () => JsonSerializer.Deserialize<CustomFormatContainer>("""{"PreferredDiagramFormat":42}""");
        var deserializeInvalidStringAction = () => JsonSerializer.Deserialize<CustomFormatContainer>("""{"PreferredDiagramFormat":"9Invalid"}""");
        _ = deserializeNumberAction.Should().Throw<JsonException>();
        _ = deserializeInvalidStringAction.Should().Throw<JsonException>();
    }
}
