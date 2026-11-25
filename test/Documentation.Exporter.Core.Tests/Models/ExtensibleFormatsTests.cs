using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class ExtensibleFormatsTests
{
    private sealed class NavigationHostModel
    {
        [ValidNavigationFormat]
        public NavigationFormat Format { get; init; }
    }

    private sealed class ReportHostModel
    {
        [ValidQualityReportFormat]
        public QualityReportFormat Format { get; init; }
    }

    [Fact]
    public void NavigationFormatShouldSupportWellKnownCustomParsingEqualityAndJsonSerialization()
    {
        // Arrange & Act
        var parsedMkDocs = NavigationFormat.Parse("mkdocs");
        var parsedNone = NavigationFormat.Parse("none");
        var customFormat = NavigationFormat.Custom("Docusaurus_v3");
        var caseVariantCustom = NavigationFormat.Parse("docusaurus_v3");

        // Assert
        _ = parsedMkDocs.Should().Be(NavigationFormat.MkDocs);
        _ = parsedNone.Should().Be(NavigationFormat.None);
        _ = parsedNone.IsNone.Should().BeTrue();
        _ = parsedMkDocs.IsNone.Should().BeFalse();
        _ = customFormat.IsValid.Should().BeTrue();
        _ = customFormat.Equals(caseVariantCustom).Should().BeTrue("because NavigationFormat comparison is case-insensitive.");
        _ = customFormat.GetHashCode().Should().Be(caseVariantCustom.GetHashCode());
        _ = customFormat.ToString().Should().Be("Docusaurus_v3");

        _ = NavigationFormat.TryParse("DocFx-2", out var docFxFormat).Should().BeTrue();
        _ = docFxFormat.Value.Should().Be("DocFx-2");
        _ = NavigationFormat.TryParse("   ", out _).Should().BeFalse();
        _ = NavigationFormat.TryParse("9Invalid", out _).Should().BeFalse();

        _ = FluentActions.Invoking(() => NavigationFormat.Parse("123Bad"))
            .Should().Throw<ArgumentException>("because navigation format identifiers must start with an ASCII letter.");

        var serializedJson = JsonSerializer.Serialize(customFormat);
        var roundTripped = JsonSerializer.Deserialize<NavigationFormat>(serializedJson);
        _ = roundTripped.Should().Be(customFormat);

        _ = FluentActions.Invoking(() => JsonSerializer.Deserialize<NavigationFormat>("123"))
            .Should().Throw<JsonException>("because JSON numbers are invalid NavigationFormat tokens.");
        _ = FluentActions.Invoking(() => JsonSerializer.Deserialize<NavigationFormat>("\"9Bad\""))
            .Should().Throw<JsonException>("because invalid NavigationFormat strings must throw JsonException.");

        NavigationHostModel validNavigationHost = new() { Format = customFormat };
        NavigationHostModel invalidNavigationHost = new() { Format = default };
        _ = Validator.TryValidateObject(validNavigationHost, new ValidationContext(validNavigationHost), [], validateAllProperties: true)
            .Should().BeTrue();
        _ = Validator.TryValidateObject(invalidNavigationHost, new ValidationContext(invalidNavigationHost), [], validateAllProperties: true)
            .Should().BeFalse("because default(NavigationFormat) has an empty identifier.");
    }

    [Fact]
    public void QualityReportFormatShouldSupportWellKnownCustomParsingEqualityAndJsonSerialization()
    {
        // Arrange & Act
        var parsedJson = QualityReportFormat.Parse("json");
        var parsedMarkdown = QualityReportFormat.Parse("markdown");
        var parsedSarif = QualityReportFormat.Parse("sarif");
        var customFormat = QualityReportFormat.Custom("JUnit-XML");
        var caseVariantCustom = QualityReportFormat.Parse("junit-xml");

        // Assert
        _ = parsedJson.Should().Be(QualityReportFormat.Json);
        _ = parsedMarkdown.Should().Be(QualityReportFormat.Markdown);
        _ = parsedSarif.Should().Be(QualityReportFormat.Sarif);
        _ = customFormat.IsValid.Should().BeTrue();
        _ = customFormat.Equals(caseVariantCustom).Should().BeTrue("because QualityReportFormat comparison is case-insensitive.");
        _ = customFormat.GetHashCode().Should().Be(caseVariantCustom.GetHashCode());
        _ = customFormat.ToString().Should().Be("JUnit-XML");

        _ = QualityReportFormat.TryParse("Checkstyle_v1", out var checkstyleFormat).Should().BeTrue();
        _ = checkstyleFormat.Value.Should().Be("Checkstyle_v1");
        _ = QualityReportFormat.TryParse(null, out _).Should().BeFalse();
        _ = QualityReportFormat.TryParse("Bad!Name", out _).Should().BeFalse();

        _ = FluentActions.Invoking(() => QualityReportFormat.Parse("-Invalid"))
            .Should().Throw<ArgumentException>("because quality report format identifiers must start with an ASCII letter.");

        var serializedJson = JsonSerializer.Serialize(customFormat);
        var roundTripped = JsonSerializer.Deserialize<QualityReportFormat>(serializedJson);
        _ = roundTripped.Should().Be(customFormat);

        _ = FluentActions.Invoking(() => JsonSerializer.Deserialize<QualityReportFormat>("true"))
            .Should().Throw<JsonException>("because booleans are invalid QualityReportFormat tokens.");
        _ = FluentActions.Invoking(() => JsonSerializer.Deserialize<QualityReportFormat>("\"-Bad\""))
            .Should().Throw<JsonException>("because invalid QualityReportFormat strings must throw JsonException.");

        ReportHostModel validReportHost = new() { Format = customFormat };
        ReportHostModel invalidReportHost = new() { Format = default };
        _ = Validator.TryValidateObject(validReportHost, new ValidationContext(validReportHost), [], validateAllProperties: true)
            .Should().BeTrue();
        _ = Validator.TryValidateObject(invalidReportHost, new ValidationContext(invalidReportHost), [], validateAllProperties: true)
            .Should().BeFalse("because default(QualityReportFormat) has an empty identifier.");
    }
}
