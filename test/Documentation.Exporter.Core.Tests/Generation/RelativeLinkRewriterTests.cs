using Documentation.Exporter.Core.Generation;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class RelativeLinkRewriterTests
{
    [Fact]
    public void AdjustLocalLinksAndNormalizeMarkdownShouldRewriteRelativePathsCleanly()
    {
        // Arrange
        var rewriter = new RelativeLinkRewriter();
        var rawMarkdown = "See [OrderRequest](Models/OrderRequest/index.md), [Self](Services/OrderService/index.md), [External](https://example.com), and [Local](Execute.md).";

        // Act
        var rewritten = rewriter.AdjustLocalLinksForTypeDirectory(
            rawMarkdown,
            "DemoLibrary/Services/OrderService",
            assemblyPrefix: "DemoLibrary",
            unprefixedSelfUrl: "Services/OrderService/index.md");
        var normalized = rewriter.NormalizeMarkdown("Line 1\r\n\r\n\r\n\r\nLine 2   ");

        // Assert
        _ = rewritten.Should().Contain("[OrderRequest](../../../Models/OrderRequest/index.md)")
            .And.Contain("[Self](index.md)")
            .And.Contain("[External](https://example.com)")
            .And.Contain("[Local](Execute.md)");
        _ = normalized.Should().Be("Line 1\r\n\r\nLine 2" + Environment.NewLine,
            "because excessive consecutive blank lines must collapse to a single blank line.");
        _ = rewriter.BuildNamespaceSectionLinkTarget(null).Should().Be("index.md");
        _ = rewriter.BuildNamespaceSectionLinkTarget("App.Services").Should().Be("index.md#namespace-appservices");
    }
}
