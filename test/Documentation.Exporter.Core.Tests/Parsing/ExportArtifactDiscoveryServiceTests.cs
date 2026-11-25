using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Parsing;

[UnitTest]
public sealed class ExportArtifactDiscoveryServiceTests
{
    [Fact]
    public void ConstructorWhenSolutionResolverNullShouldThrowArgumentNullException() =>
        _ = Assert.Throws<ArgumentNullException>(() => new ExportArtifactDiscoveryService(null!));

    [Fact]
    public void DiscoverArtifactPathsWhenOptionsNullShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new ExportArtifactDiscoveryService(new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem()));

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => service.DiscoverArtifactPaths(null!));
        _ = Assert.Throws<ArgumentNullException>(() => service.DiscoverAssemblyFilePaths(null!));
        _ = Assert.Throws<ArgumentNullException>(() => service.DiscoverXmlDocumentationFilePaths(null!));
    }

    [Fact]
    public void DiscoverArtifactPathsWithMultipleAssembliesAndNoExplicitXmlShouldInferAllSiblingXmlFiles()
    {
        // Arrange
        var service = new ExportArtifactDiscoveryService(new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem()));
        var firstDll = Path.Combine(Path.GetTempPath(), "Alpha.Lib.dll");
        var secondDll = Path.Combine(Path.GetTempPath(), "Beta.Contracts.dll");
        var options = new ExporterOptions
        {
            AssemblyFilePaths = [firstDll, secondDll],
            OutputDirectoryPath = Path.GetTempPath()
        };

        // Act
        var (assemblies, xmlFiles) = service.DiscoverArtifactPaths(options);

        // Assert
        _ = assemblies.Should().BeEquivalentTo([firstDll, secondDll], "because both configured assemblies should be discovered.");
        _ = xmlFiles.Should().BeEquivalentTo(
            [Path.ChangeExtension(firstDll, ".xml"), Path.ChangeExtension(secondDll, ".xml")],
            "because sibling .xml files should be inferred for every assembly when none are explicitly configured.");

        var partialExplicitOptions = new ExporterOptions
        {
            AssemblyFilePaths = [firstDll, secondDll],
            XmlDocumentationFilePaths = [Path.ChangeExtension(firstDll, ".xml")],
            OutputDirectoryPath = Path.GetTempPath()
        };
        var (_, partialXmlFiles) = service.DiscoverArtifactPaths(partialExplicitOptions);
        _ = partialXmlFiles.Should().BeEquivalentTo(
            [Path.ChangeExtension(firstDll, ".xml"), Path.ChangeExtension(secondDll, ".xml")],
            "because missing sibling .xml files must still be inferred when only a subset of --xml paths is explicitly provided.");
    }

    [Fact]
    public void DiscoverArtifactPathsWithExclusionPatternShouldExcludeMatchingArtifacts()
    {
        // Arrange
        var service = new ExportArtifactDiscoveryService(new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem()));
        var options = new ExporterOptions
        {
            AssemblyFilePaths = ["MyApp.Core.dll", "MyApp.Tests.dll"],
            ExcludedProjects = ["*.Tests"],
            OutputDirectoryPath = Path.GetTempPath()
        };

        // Act
        var resolvedAssemblies = service.DiscoverAssemblyFilePaths(options);

        // Assert
        _ = resolvedAssemblies.Should().ContainSingle("because *.Tests must be excluded.")
            .Which.Should().Be("MyApp.Core.dll");
    }
}
