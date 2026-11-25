using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Enrichment;

[UnitTest]
public sealed class ExampleSnippetEnricherTests
{
    private sealed class StubExampleProvider(IReadOnlyList<string> snippetsToReturn) : IExampleProvider
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<string>> GetExamplesAsync(
            string? examplesDirectoryPath,
            string? typeName,
            string? memberName = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(snippetsToReturn);
        }
    }

    [Fact]
    public void OrderPropertyShouldReturnTwenty()
    {
        // Arrange
        var provider = new StubExampleProvider([]);
        var enricher = new ExampleSnippetEnricher(provider);

        // Assert
        _ = enricher.Order.Should().Be(20);
    }

    [Fact]
    public async Task EnrichAsyncWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new StubExampleProvider([]);
        var enricher = new ExampleSnippetEnricher(provider);
        var model = new TypeDocumentationModel { TypeName = "OrderService" };
        var options = new ExporterOptions();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new ExampleSnippetEnricher(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(null!, model, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(typeof(string), null!, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(typeof(string), model, null!));
    }

    [Fact]
    public async Task EnrichAsyncWhenExamplesDirectoryMissingShouldReturnOriginalModelEarly()
    {
        // Arrange
        var provider = new StubExampleProvider(["// Should not be loaded"]);
        var enricher = new ExampleSnippetEnricher(provider);
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            Properties = [new MemberDocumentationModel { MemberName = "Id" }]
        };
        var options = new ExporterOptions { ExamplesDirectoryPath = null };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(string), model, options);

        // Assert
        _ = enrichedModel.Should().BeSameAs(model, "because missing ExamplesDirectoryPath triggers fast-path early return.");
        _ = provider.CallCount.Should().Be(0, "because provider must not be called when directory is absent.");
    }

    [Fact]
    public async Task EnrichAsyncWithValidTypeAndMembersShouldReturnEnrichedCopyWithSnippets()
    {
        // Arrange
        var provider = new StubExampleProvider(["// Sample snippet for OrderService"]);
        var enricher = new ExampleSnippetEnricher(provider);
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            Properties = [new MemberDocumentationModel { MemberName = "Id" }],
            Methods = [new MemberDocumentationModel { MemberName = "Process" }]
        };
        var options = new ExporterOptions { ExamplesDirectoryPath = Path.Combine(Path.GetTempPath(), "Examples") };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(string), model, options);

        // Assert
        _ = model.CodeExamples.Should().BeEmpty("because the original input model must remain unmodified.");
        _ = enrichedModel.CodeExamples.Should().Contain("// Sample snippet for OrderService");
        _ = enrichedModel.Properties[0].CodeExamples.Should().Contain("// Sample snippet for OrderService");
        _ = enrichedModel.Methods[0].CodeExamples.Should().Contain("// Sample snippet for OrderService");
    }
}
