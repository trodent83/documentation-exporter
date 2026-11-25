using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Enrichment;

[UnitTest]
public sealed class TypeDocumentationPipelineTests
{
    private sealed class TrackingEnricher(int order, string tag) : ITypeDocumentationEnricher
    {
        public int Order => order;

        public Task<TypeDocumentationModel> EnrichAsync(Type targetType, TypeDocumentationModel typeModel, ExporterOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult(typeModel with { CodeExamples = [.. typeModel.CodeExamples, tag] });
    }

    [Fact]
    public async Task EnrichTypeModelAsyncWhenTargetTypeOrModelNullShouldThrowArgumentNullException()
    {
        // Arrange
        var pipeline = new TypeDocumentationPipeline([]);
        var model = new TypeDocumentationModel();
        var options = new ExporterOptions();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new TypeDocumentationPipeline(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => pipeline.EnrichTypeModelAsync(null!, model, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => pipeline.EnrichTypeModelAsync(typeof(string), null!, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => pipeline.EnrichTypeModelAsync(typeof(string), model, null!));
    }

    [Fact]
    public async Task EnrichTypeModelAsyncShouldExecuteEnrichersInAscendingOrderWithoutMutatingInput()
    {
        // Arrange
        var enricherThird = new TrackingEnricher(30, "Third");
        var enricherFirst = new TrackingEnricher(10, "First");
        var enricherSecond = new TrackingEnricher(20, "Second");

        var pipeline = new TypeDocumentationPipeline([enricherThird, enricherFirst, enricherSecond]);
        var model = new TypeDocumentationModel();
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await pipeline.EnrichTypeModelAsync(typeof(string), model, options);

        // Assert
        _ = model.CodeExamples.Should().BeEmpty("because the input model must not be mutated.");
        _ = enrichedModel.CodeExamples.Should().ContainInOrder(["First", "Second", "Third"]);
    }
}
