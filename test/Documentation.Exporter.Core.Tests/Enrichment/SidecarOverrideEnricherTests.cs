using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Enrichment;

[UnitTest]
public sealed class SidecarOverrideEnricherTests
{
    private sealed class StubDocumentationInjector(IReadOnlyList<DocumentationInjectionModel> injectionsToReturn) : IDocumentationInjector
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<DocumentationInjectionModel>> GetInjectionsAsync(
            string? overridesDirectoryPath,
            string? typeName,
            string? memberName = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(injectionsToReturn);
        }
    }

    [Fact]
    public void OrderPropertyShouldReturnThirty()
    {
        // Arrange
        var injector = new StubDocumentationInjector([]);
        var enricher = new SidecarOverrideEnricher(injector);

        // Assert
        _ = enricher.Order.Should().Be(30);
    }

    [Fact]
    public async Task EnrichAsyncWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var injector = new StubDocumentationInjector([]);
        var enricher = new SidecarOverrideEnricher(injector);
        var model = new TypeDocumentationModel { TypeName = "OrderService" };
        var options = new ExporterOptions();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new SidecarOverrideEnricher(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(null!, model, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(typeof(string), null!, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(typeof(string), model, null!));
    }

    [Fact]
    public async Task EnrichAsyncWhenOverridesDirectoryMissingShouldReturnOriginalModelEarly()
    {
        // Arrange
        var injector = new StubDocumentationInjector([]);
        var enricher = new SidecarOverrideEnricher(injector);
        var model = new TypeDocumentationModel { TypeName = "OrderService" };
        var options = new ExporterOptions { OverridesDirectoryPath = null };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(string), model, options);

        // Assert
        _ = enrichedModel.Should().BeSameAs(model, "because missing OverridesDirectoryPath triggers fast-path early return.");
        _ = injector.CallCount.Should().Be(0, "because injector must not be invoked when directory is absent.");
    }

    [Fact]
    public async Task EnrichAsyncWithValidTypeShouldReturnEnrichedCopyWithInjectedSections()
    {
        // Arrange
        var injection = new DocumentationInjectionModel
        {
            TargetName = "OrderService",
            InjectionMode = InjectionMode.Extend,
            TargetAnchor = SidecarAnchor.AfterRemarks,
            CustomMarkdownContent = "## Extra Remarks"
        };
        var injector = new StubDocumentationInjector([injection]);
        var enricher = new SidecarOverrideEnricher(injector);
        var model = new TypeDocumentationModel { TypeName = "OrderService" };
        var options = new ExporterOptions { OverridesDirectoryPath = Path.Combine(Path.GetTempPath(), "Overrides") };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(string), model, options);

        // Assert
        _ = model.InjectedDocumentationSections.Should().BeEmpty("because the original input model must remain unmodified.");
        _ = enrichedModel.InjectedDocumentationSections.Should().HaveCount(1);
        _ = enrichedModel.InjectedDocumentationSections[0].CustomMarkdownContent.Should().Be("## Extra Remarks");
    }

    [Fact]
    public async Task EnrichAsyncWithMembersShouldInjectMemberLevelSidecarOverridesAndReplaceSummary()
    {
        // Arrange
        var replaceSummaryInjection = new DocumentationInjectionModel
        {
            TargetName = "OrderService.Process",
            InjectionMode = InjectionMode.Replace,
            TargetAnchor = SidecarAnchor.ReplaceSummary,
            CustomMarkdownContent = "Replaced member summary."
        };
        var injector = new StubDocumentationInjector([replaceSummaryInjection]);
        var enricher = new SidecarOverrideEnricher(injector);
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            DirectorySegmentName = "OrderService_T1",
            Methods = [new MemberDocumentationModel { MemberName = "Process", SummaryDescription = "Original summary." }]
        };
        var options = new ExporterOptions { OverridesDirectoryPath = Path.Combine(Path.GetTempPath(), "Overrides") };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(string), model, options);

        // Assert
        _ = enrichedModel.SummaryDescription.Should().Be("Replaced member summary.",
            "because type-level sidecar overrides with replace_summary must also update the type SummaryDescription for index tables and coverage gates.");
        _ = enrichedModel.Methods.Should().ContainSingle();
        _ = enrichedModel.Methods[0].SummaryDescription.Should().Be("Replaced member summary.",
            "because member-level sidecar overrides with replace_summary must update the member SummaryDescription.");
        _ = enrichedModel.Methods[0].InjectedDocumentationSections.Should().ContainSingle();
    }
}
