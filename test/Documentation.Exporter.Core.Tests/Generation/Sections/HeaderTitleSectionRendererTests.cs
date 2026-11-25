using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class HeaderTitleSectionRendererTests
{
    private static HeaderTitleSectionRenderer CreateRenderer() =>
        new(new MicrosoftLearnReferenceRegistry(), new RelativeLinkRewriter());

    [Fact]
    public void OrderPropertyShouldReturnTen()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Assert
        _ = renderer.Order.Should().Be(10);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new HeaderTitleSectionRenderer(null!, new RelativeLinkRewriter()));
        _ = Assert.Throws<ArgumentNullException>(() => new HeaderTitleSectionRenderer(new MicrosoftLearnReferenceRegistry(), null!));
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionShouldRenderTitleNamespaceAndSummary()
    {
        // Arrange
        var renderer = CreateRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            TypeCategory = TypeCategory.Class,
            NamespaceName = "App.Services",
            AssemblyName = "App.Core",
            SummaryDescription = "Processes customer orders."
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("# OrderService [Class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class)")
            .And.Contain("**Namespace:** [App.Services](index.md#namespace-appservices)")
            .And.Contain("**Assembly:** [App.Core](index.md)")
            .And.Contain("Processes customer orders.");
    }

    [Fact]
    public void RenderSectionWhenTypeIsObsoleteShouldRenderWarningBlockWithExtractedMessage()
    {
        // Arrange
        var renderer = CreateRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "LegacyAdapter",
            TypeCategory = TypeCategory.Class,
            NamespaceName = "App.Adapters",
            AssemblyName = "App.Core",
            IsObsolete = true,
            ObsoleteMessage = "Use ModernAdapter instead."
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("> [!WARNING]")
            .And.Contain("> **[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute):** Use ModernAdapter instead.");
    }

    [Fact]
    public void RenderSectionWhenSummaryReplacedBySidecarShouldRenderSidecarSummary()
    {
        // Arrange
        var renderer = CreateRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            TypeCategory = TypeCategory.Class,
            NamespaceName = "App.Services",
            SummaryDescription = "Original summary.",
            InjectedDocumentationSections =
            [
                new DocumentationInjectionModel
                {
                    InjectionMode = InjectionMode.Replace,
                    TargetAnchor = SidecarAnchor.ReplaceSummary,
                    CustomMarkdownContent = "Replaced sidecar summary."
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("Replaced sidecar summary.")
            .And.NotContain("Original summary.");
    }

    [Fact]
    public void RenderSectionWhenExperimentalShouldRenderImportantExperimentalCalloutBanner()
    {
        // Arrange
        var renderer = CreateRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "ExperimentalPreviewService",
            TypeCategory = TypeCategory.Class,
            NamespaceName = "App.Preview",
            SummaryDescription = "Preview service description.",
            IsExperimental = true,
            ExperimentalDiagnosticId = "EXP100",
            ExperimentalUrlFormat = "https://docs.example.com/diagnostics/{0}"
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("> [!IMPORTANT]")
            .And.Contain("[Experimental](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute)")
            .And.Contain("[EXP100](https://docs.example.com/diagnostics/EXP100)")
            .And.Contain("Preview service description.");
    }
}
