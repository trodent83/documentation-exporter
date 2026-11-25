using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class MermaidAssemblyDependencyDiagramGeneratorTests
{
    [Fact]
    public void ConstructorAndMethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        MermaidAssemblyDependencyDiagramGenerator generator = new(new DiagramIdentifierFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MermaidAssemblyDependencyDiagramGenerator(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.CanGenerate("App.Main", null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateDependencyTreeMarkdown("App.Main", null!));
        _ = generator.Format.Should().Be(DiagramFormat.Mermaid);
    }

    [Fact]
    public void GenerateDependencyTreeMarkdownWhenEmptyOrUnsupportedShouldReturnEmpty()
    {
        // Arrange
        MermaidAssemblyDependencyDiagramGenerator generator = new(new DiagramIdentifierFormatter());
        AssemblyDependencyEdgeModel[] sampleEdges =
        [
            new()
            {
                SourceAssemblyName = "App.Main",
                TargetAssemblyName = "App.Contracts",
                ReferenceKind = AssemblyReferenceKind.Project,
                VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked
            }
        ];

        // Act & Assert
        _ = generator.CanGenerate("App.Main", []).Should().BeFalse("because empty edge lists produce no dependency tree.");
        _ = generator.CanGenerate("   ", sampleEdges).Should().BeFalse("because blank root assembly names cannot root a tree.");
        _ = generator.GenerateDependencyTreeMarkdown("App.Main", []).Should().BeEmpty();
        _ = generator.GenerateDependencyTreeMarkdown("   ", sampleEdges).Should().BeEmpty();
    }

    [Fact]
    public void GenerateDependencyTreeMarkdownShouldRenderFlowchartColoredLinksVersionLabelsAndLegend()
    {
        // Arrange
        MermaidAssemblyDependencyDiagramGenerator generator = new(new DiagramIdentifierFormatter());
        AssemblyDependencyEdgeModel[] mixedEdges =
        [
            new()
            {
                SourceAssemblyName = "App.Services",
                TargetAssemblyName = "App.Contracts",
                ReferenceKind = AssemblyReferenceKind.Project,
                VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked,
                IsTargetStrongNamed = false
            },
            new()
            {
                SourceAssemblyName = "App.Services",
                TargetAssemblyName = "Newtonsoft.Json",
                ReferenceKind = AssemblyReferenceKind.Package,
                VersionLinkingKind = AssemblyVersionLinkingKind.Range,
                VersionConstraintExpression = "[13.0.1, 14.0.0)",
                ResolvedVersion = "13.0.3",
                IsTargetStrongNamed = true,
                TargetPublicKeyToken = "30ad4fe6b2a6aeed"
            },
            new()
            {
                SourceAssemblyName = "Newtonsoft.Json",
                TargetAssemblyName = "Microsoft.CSharp.Helper",
                ReferenceKind = AssemblyReferenceKind.Package,
                VersionLinkingKind = AssemblyVersionLinkingKind.Minimum,
                VersionConstraintExpression = ">= 4.7.0",
                IsTargetStrongNamed = true
            }
        ];

        // Act
        var markdown = generator.GenerateDependencyTreeMarkdown("App.Services", mixedEdges);
        var strongOnlyMarkdown = generator.GenerateDependencyTreeMarkdown("App.Services", [mixedEdges[1]]);
        var unsignedOnlyMarkdown = generator.GenerateDependencyTreeMarkdown("App.Services", [mixedEdges[0]]);

        // Assert
        _ = generator.CanGenerate("App.Services", mixedEdges).Should().BeTrue();
        _ = markdown.Should().Contain("```mermaid")
            .And.Contain("flowchart LR")
            .And.Contain("AppServices[\"App.Services\"]")
            .And.Contain("AppContracts[\"App.Contracts\"]")
            .And.Contain("NewtonsoftJson[\"Newtonsoft.Json\"]")
            .And.Contain("AppServices -->|\"Linked (Project)\"| AppContracts")
            .And.Contain("AppServices -->|\"Range ([13.0.1, 14.0.0))\"| NewtonsoftJson")
            .And.Contain("NewtonsoftJson -->|\"Minimum (>= 4.7.0)\"| MicrosoftCSharpHelper")
            .And.Contain("linkStyle 1,2 stroke:#2E7D32,stroke-width:2px",
                "because strong-named links at indices 1 and 2 must use solid green styling.")
            .And.Contain("linkStyle 0 stroke:#757575,stroke-width:2px,stroke-dasharray:5 5",
                "because unsigned link at index 0 must use dashed gray styling.")
            .And.Contain($"*{MarkdownGenerationResources.AssemblyDependencyTreeLegend}*");

        _ = strongOnlyMarkdown.Should().Contain("linkStyle 0 stroke:#2E7D32")
            .And.NotContain("#757575", "because unsigned linkStyle must be omitted when no unsigned edges exist.");
        _ = unsignedOnlyMarkdown.Should().Contain("linkStyle 0 stroke:#757575")
            .And.NotContain("#2E7D32", "because strong-named linkStyle must be omitted when no strong-named edges exist.");
    }
}
