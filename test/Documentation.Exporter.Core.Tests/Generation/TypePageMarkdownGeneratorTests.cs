using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class TypePageMarkdownGeneratorTests
{
    private sealed class TrackingRenderer(int order, string contentToAppend) : IMarkdownSectionRenderer
    {
        public int Order => order;

        public string RenderSection(TypeDocumentationModel typeDocumentationModel) =>
            contentToAppend + Environment.NewLine;
    }

    [Fact]
    public void ConstructorAndMethodWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var linkRewriter = new RelativeLinkRewriter();
        var generator = new TypePageMarkdownGenerator([], linkRewriter);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new TypePageMarkdownGenerator(null!, linkRewriter));
        _ = Assert.Throws<ArgumentNullException>(() => new TypePageMarkdownGenerator([], null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateTypeMarkdown(null!));
    }

    [Fact]
    public void GenerateTypeMarkdownShouldExecuteRenderersInAscendingOrderAndAdjustRelativeLinks()
    {
        // Arrange
        var rendererThird = new TrackingRenderer(30, "### Section Three");
        var rendererFirst = new TrackingRenderer(10, "# Section One");
        var rendererSecond = new TrackingRenderer(20, "See [OrderRequest](Models/OrderRequest/index.md) and [AuditableProcessorBase](Services/AuditableProcessorBase/index.md) and [ProcessOrderAsync](ProcessOrderAsync.md) and [String](https://learn.microsoft.com/en-us/dotnet/api/system.string).");

        var generator = new TypePageMarkdownGenerator([rendererThird, rendererFirst, rendererSecond], new RelativeLinkRewriter());
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderProcessingService",
            NamespaceName = "Demo.Services",
            AssemblyName = "Demo"
        };

        // Act
        var markdown = generator.GenerateTypeMarkdown(model);

        // Assert
        var indexOfFirst = markdown.IndexOf("# Section One", StringComparison.Ordinal);
        var indexOfSecond = markdown.IndexOf("See [OrderRequest]", StringComparison.Ordinal);
        var indexOfThird = markdown.IndexOf("### Section Three", StringComparison.Ordinal);

        _ = indexOfFirst.Should().BeLessThan(indexOfSecond, "because order 10 renders before order 20.");
        _ = indexOfSecond.Should().BeLessThan(indexOfThird, "because order 20 renders before order 30.");
        _ = markdown.Should().Contain("[OrderRequest](../../Models/OrderRequest/index.md)")
            .And.Contain("[AuditableProcessorBase](../AuditableProcessorBase/index.md)")
            .And.Contain("[ProcessOrderAsync](ProcessOrderAsync.md)")
            .And.Contain("[String](https://learn.microsoft.com/en-us/dotnet/api/system.string)");
    }
}
