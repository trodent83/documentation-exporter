using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Generation.Diagrams;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class DiagramGeneratorSelectorTests
{
    private sealed class StubTypeDiagramGenerator(
        DiagramFormat format,
        bool isSupported,
        string renderedDiagram = "") : ITypeDiagramGenerator
    {
        public DiagramFormat Format => format;

        public bool CanGenerate(TypeDocumentationModel typeModel) => isSupported;

        public string GenerateTypeDiagram(TypeDocumentationModel typeModel) => renderedDiagram;
    }

    private sealed class TrackingTypeDiagramGenerator(
        DiagramFormat format,
        Action onCanGenerate) : ITypeDiagramGenerator
    {
        public DiagramFormat Format => format;

        public bool CanGenerate(TypeDocumentationModel typeModel)
        {
            onCanGenerate();
            return true;
        }

        public string GenerateTypeDiagram(TypeDocumentationModel typeModel) => string.Empty;
    }

    [Fact]
    public void SelectGeneratorWhenArgumentsNullShouldThrowArgumentNullException()
    {
        // Arrange
        DiagramGeneratorSelector selector = new();
        ITypeDiagramGenerator[] candidates = [];

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            selector.SelectGenerator<ITypeDiagramGenerator>(null!, DiagramFormat.Mermaid, generator => true));
        _ = Assert.Throws<ArgumentNullException>(() =>
            selector.SelectGenerator(candidates, DiagramFormat.Mermaid, null!));
    }

    [Fact]
    public void SelectGeneratorWhenPreferredFormatIsNoneShouldReturnNullWithoutInvokingPredicate()
    {
        // Arrange
        DiagramGeneratorSelector selector = new();
        var predicateInvoked = false;
        ITypeDiagramGenerator[] candidates =
        [
            new TrackingTypeDiagramGenerator(DiagramFormat.Mermaid, () => predicateInvoked = true),
            new TrackingTypeDiagramGenerator(DiagramFormat.PlantUml, () => predicateInvoked = true)
        ];

        // Act
        var selectedGenerator = selector.SelectGenerator(
            candidates,
            DiagramFormat.None,
            generator => generator.CanGenerate(new TypeDocumentationModel { TypeName = "SampleType" }));

        // Assert
        _ = selectedGenerator.Should().BeNull("because DiagramFormat.None disables diagram generation without fallback.");
        _ = predicateInvoked.Should().BeFalse("because no candidate generator should be queried when diagrams are disabled.");
    }

    [Theory]
    [InlineData(DiagramFormatNames.Mermaid)]
    [InlineData(DiagramFormatNames.PlantUml)]
    public void SelectGeneratorWhenPreferredGeneratorSupportsDiagramShouldSelectPreferred(string preferredFormatName)
    {
        // Arrange
        var preferredFormat = DiagramFormat.Parse(preferredFormatName);
        DiagramGeneratorSelector selector = new();
        StubTypeDiagramGenerator mermaidGenerator = new(DiagramFormat.Mermaid, isSupported: true);
        StubTypeDiagramGenerator plantUmlGenerator = new(DiagramFormat.PlantUml, isSupported: true);
        ITypeDiagramGenerator[] candidates = [mermaidGenerator, plantUmlGenerator];

        // Act
        var selectedGenerator = selector.SelectGenerator(
            candidates,
            preferredFormat,
            generator => generator.CanGenerate(new TypeDocumentationModel { TypeName = "SampleType" }));

        // Assert
        _ = selectedGenerator.Should().NotBeNull("because the preferred generator supports the diagram.");
        _ = selectedGenerator!.Format.Should().Be(preferredFormat, "because the preferred generator takes priority when supported.");
    }

    [Theory]
    [InlineData(DiagramFormatNames.Mermaid, DiagramFormatNames.PlantUml)]
    [InlineData(DiagramFormatNames.PlantUml, DiagramFormatNames.Mermaid)]
    public void SelectGeneratorWhenPreferredUnsupportedShouldFallbackToSupportingAlternative(
        string preferredFormatName,
        string expectedFallbackFormatName)
    {
        // Arrange
        var preferredFormat = DiagramFormat.Parse(preferredFormatName);
        var expectedFallbackFormat = DiagramFormat.Parse(expectedFallbackFormatName);
        DiagramGeneratorSelector selector = new();
        StubTypeDiagramGenerator preferredUnsupported = new(preferredFormat, isSupported: false);
        StubTypeDiagramGenerator disabledCandidate = new(DiagramFormat.None, isSupported: true);
        StubTypeDiagramGenerator fallbackSupported = new(expectedFallbackFormat, isSupported: true);
        ITypeDiagramGenerator[] candidates = [preferredUnsupported, disabledCandidate, fallbackSupported];

        // Act
        var selectedGenerator = selector.SelectGenerator(
            candidates,
            preferredFormat,
            generator => generator.CanGenerate(new TypeDocumentationModel { TypeName = "SampleType" }));

        // Assert
        _ = selectedGenerator.Should().NotBeNull("because an alternative registered generator supports the diagram.");
        _ = selectedGenerator!.Format.Should().Be(expectedFallbackFormat, "because selection must fall back to a supporting alternative only when preferred is unsupported.");
    }

    [Fact]
    public void SelectGeneratorWhenCustomPluginFormatProvidedShouldMatchCaseInsensitivelyOrFallback()
    {
        // Arrange
        DiagramGeneratorSelector selector = new();
        var customFormat = DiagramFormat.Custom("D2");
        StubTypeDiagramGenerator mermaidGenerator = new(DiagramFormat.Mermaid, isSupported: true);
        StubTypeDiagramGenerator customGenerator = new(DiagramFormat.Custom("d2"), isSupported: true);
        ITypeDiagramGenerator[] candidates = [mermaidGenerator, customGenerator];

        // Act
        var selectedCustom = selector.SelectGenerator(
            candidates,
            customFormat,
            generator => generator.CanGenerate(new TypeDocumentationModel { TypeName = "SampleType" }));
        var fallbackWhenUnregistered = selector.SelectGenerator(
            [mermaidGenerator],
            DiagramFormat.Custom("Graphviz"),
            generator => generator.CanGenerate(new TypeDocumentationModel { TypeName = "SampleType" }));

        // Assert
        _ = selectedCustom.Should().BeSameAs(customGenerator, "because custom plugin DiagramFormat values match case-insensitively.");
        _ = fallbackWhenUnregistered.Should().BeSameAs(mermaidGenerator, "because an unregistered custom format falls back to the first supporting registered generator.");
    }

    [Fact]
    public void SelectGeneratorWhenNoCandidateSupportsDiagramShouldReturnNull()
    {
        // Arrange
        DiagramGeneratorSelector selector = new();
        StubTypeDiagramGenerator unsupportedMermaid = new(DiagramFormat.Mermaid, isSupported: false);
        StubTypeDiagramGenerator unsupportedPlantUml = new(DiagramFormat.PlantUml, isSupported: false);

        // Act
        var noneSupported = selector.SelectGenerator(
            [unsupportedMermaid, unsupportedPlantUml],
            DiagramFormat.Mermaid,
            generator => generator.CanGenerate(new TypeDocumentationModel { TypeName = "SampleType" }));
        var emptyCandidates = selector.SelectGenerator<ITypeDiagramGenerator>(
            [],
            DiagramFormat.Mermaid,
            generator => true);

        // Assert
        _ = noneSupported.Should().BeNull("because no generator supports the diagram type.");
        _ = emptyCandidates.Should().BeNull("because no diagram generators are registered.");
    }
}
