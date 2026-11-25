using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class DiagramIdentifierFormatterTests
{
    [Fact]
    public void MethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var formatter = new DiagramIdentifierFormatter();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => formatter.SanitizeIdentifier(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.ExtractTypeDiagramRelationships(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.CollectArchitectureRelationships(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.FormatDependencyEdgeLabel(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.BuildUniqueNodeIdentifiers(null!, []));
        _ = Assert.Throws<ArgumentException>(() => formatter.BuildUniqueNodeIdentifiers("  ", []));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.BuildUniqueNodeIdentifiers("App.Root", null!));
    }

    [Fact]
    public void IdentifierExtractionAndRelationshipCollectionShouldNormalizeLinksAndHierarchies()
    {
        // Arrange
        var formatter = new DiagramIdentifierFormatter();
        TypeDocumentationModel[] models =
        [
            new()
            {
                TypeName = "OrderProcessor",
                InheritanceHierarchy = ["System.Object", "[ProcessorBase<T>](ProcessorBase/index.md)"],
                ImplementedInterfaces = ["[IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)"],
                DerivedTypes = ["SpecialOrderProcessor"]
            }
        ];

        // Act
        var (baseNames, interfaceNames, derivedNames) = formatter.ExtractTypeDiagramRelationships(models[0]);
        var relations = formatter.CollectArchitectureRelationships(models);

        // Assert
        _ = formatter.ExtractDisplayTypeLabel("[String](https://example.com)").Should().Be("String");
        _ = formatter.ExtractSimpleTypeIdentifier("App.Services.OrderProcessor<T>").Should().Be("OrderProcessor");
        _ = baseNames.Should().ContainSingle().Which.Should().Be("ProcessorBase");
        _ = interfaceNames.Should().ContainSingle().Which.Should().Be("IDisposable");
        _ = derivedNames.Should().ContainSingle().Which.Should().Be("SpecialOrderProcessor");
        _ = relations.Should().BeEquivalentTo(["IDisposable <|.. OrderProcessor", "ProcessorBase <|-- OrderProcessor"]);
    }

    [Fact]
    public void DependencyFormatterHelpersShouldFormatLinkingLabelsAndDeduplicateCollidingIdentifiers()
    {
        // Arrange
        DiagramIdentifierFormatter formatter = new();

        AssemblyDependencyEdgeModel[] edges =
        [
            new() { SourceAssemblyName = "App.Main", TargetAssemblyName = "My.Lib", VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked },
            new() { SourceAssemblyName = "App.Main", TargetAssemblyName = "My-Lib", VersionLinkingKind = AssemblyVersionLinkingKind.Fixed, VersionConstraintExpression = "[1.2.3]" },
            new() { SourceAssemblyName = "My.Lib", TargetAssemblyName = "MinPkg", VersionLinkingKind = AssemblyVersionLinkingKind.Minimum, VersionConstraintExpression = ">= 2.0.0" },
            new() { SourceAssemblyName = "My.Lib", TargetAssemblyName = "RangePkg", VersionLinkingKind = AssemblyVersionLinkingKind.Range, VersionConstraintExpression = "[1.0, 2.0)" },
            new() { SourceAssemblyName = "My.Lib", TargetAssemblyName = "FloatPkg", VersionLinkingKind = AssemblyVersionLinkingKind.Floating, VersionConstraintExpression = "8.*" },
            new() { SourceAssemblyName = "My.Lib", TargetAssemblyName = "BclPkg", VersionLinkingKind = AssemblyVersionLinkingKind.Framework, ResolvedVersion = "net11.0" },
            new() { SourceAssemblyName = "My.Lib", TargetAssemblyName = "Quote\"Pkg", VersionLinkingKind = AssemblyVersionLinkingKind.Compiled, VersionConstraintExpression = "\"exact\"" },
            new() { SourceAssemblyName = "My.Lib", TargetAssemblyName = "!!!", VersionLinkingKind = (AssemblyVersionLinkingKind)999 }
        ];

        // Act
        var (orderedNodes, identifierByNodeName) = formatter.BuildUniqueNodeIdentifiers("App.Main", edges);

        // Assert
        _ = formatter.FormatDependencyEdgeLabel(edges[0]).Should().Be("Linked (Project)");
        _ = formatter.FormatDependencyEdgeLabel(edges[1]).Should().Be("Fixed ([1.2.3])");
        _ = formatter.FormatDependencyEdgeLabel(edges[2]).Should().Be("Minimum (>= 2.0.0)");
        _ = formatter.FormatDependencyEdgeLabel(edges[3]).Should().Be("Range ([1.0, 2.0))");
        _ = formatter.FormatDependencyEdgeLabel(edges[4]).Should().Be("Floating (8.*)");
        _ = formatter.FormatDependencyEdgeLabel(edges[5]).Should().Be("Framework (net11.0)");
        _ = formatter.FormatDependencyEdgeLabel(edges[6]).Should().Be("Compiled ('exact')");
        _ = formatter.FormatDependencyEdgeLabel(edges[7]).Should().Be("Compiled");

        _ = orderedNodes[0].NodeName.Should().Be("App.Main", "because the root assembly must appear first.");
        _ = identifierByNodeName["My.Lib"].Should().NotBe(identifierByNodeName["My-Lib"],
            "because distinct assembly names that sanitize identically must receive collision-free identifiers.");
        _ = identifierByNodeName["My-Lib"].Should().Be("MyLib");
        _ = identifierByNodeName["My.Lib"].Should().Be("MyLib_2");
        _ = identifierByNodeName["!!!"].Should().Be("Assembly", "because names without alphanumeric characters fall back to a valid identifier.");
    }
}
