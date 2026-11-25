using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Plugins.Navigation.MkDocs;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class MkDocsNavigationGeneratorTests
{
    [Fact]
    public void GeneratorMetadataShouldExposeMkDocsFormatAndDefaultFileName()
    {
        // Arrange
        var generator = new MkDocsNavigationGenerator();

        // Assert
        _ = generator.Format.Should().Be(NavigationFormat.MkDocs);
        _ = generator.DefaultFileName.Should().Be("mkdocs.nav.yml");
    }

    [Fact]
    public void GenerateNavigationWhenTypesNullShouldThrowArgumentNullException()
    {
        // Arrange
        var generator = new MkDocsNavigationGenerator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateNavigation(null!));
    }

    [Fact]
    public void GenerateNavigationWithMultiAndSingleAssemblyModesShouldProduceValidYamlHierarchy()
    {
        // Arrange
        var generator = new MkDocsNavigationGenerator();
        TypeDocumentationModel[] models =
        [
            new() { AssemblyName = "Contoso.Core", NamespaceName = "Contoso.Core.Services", TypeName = "OrderEngine" },
            new() { AssemblyName = "Contoso.Contracts", NamespaceName = "Contoso.Contracts", TypeName = "IOrderSink" }
        ];

        // Act
        var singleAssemblyYaml = generator.GenerateNavigation(models, useAssemblySubdirectories: false);
        var multiAssemblyYaml = generator.GenerateNavigation(models, useAssemblySubdirectories: true);

        // Assert
        _ = singleAssemblyYaml.Should().StartWith("nav:")
            .And.Contain("- Overview: index.md")
            .And.Contain("- Contoso.Core.Services:")
            .And.Contain("- OrderEngine: Services/OrderEngine/index.md");

        _ = multiAssemblyYaml.Should().Contain("- Contoso.Core:")
            .And.Contain("- Contoso.Contracts:")
            .And.Contain("- Overview: Contoso.Core/index.md")
            .And.Contain("- OrderEngine: Contoso.Core/Services/OrderEngine/index.md");
    }

    [Fact]
    public void GenerateNavigationWhenCalledRepeatedlyWithCommentMarkerAndExistingUserOverviewShouldBeIdempotentAndPreserveUserOverview()
    {
        // Arrange
        var generator = new MkDocsNavigationGenerator();
        TypeDocumentationModel[] models =
        [
            new()
            {
                AssemblyName = "Contoso.Core",
                NamespaceName = "Contoso.Core",
                TypeName = "Result",
                DirectorySegmentName = "Result"
            },
            new()
            {
                AssemblyName = "Contoso.Core",
                NamespaceName = "Contoso.Core",
                TypeName = "Result",
                DirectorySegmentName = "Result_T1",
                IsGenericType = true,
                GenericTypeParameters = ["TValue"]
            }
        ];

        var markerTemplate = """
            site_name: Idempotent Marker Portal
            nav:
              - Home: home.md
              - API Reference:
                  # API-NAV-MARKER
              - Changelog: changelog.md
            """;

        var topLevelWithCustomOverview = """
            site_name: Top Level Portal
            nav:
              - Overview: custom-portal-intro.md
              - Home: home.md
            """;

        // Act
        var firstMarkerRun = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: markerTemplate,
            navigationEntryPoint: "# API-NAV-MARKER",
            navigationUrlPrefix: "api");
        var secondMarkerRun = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: firstMarkerRun,
            navigationEntryPoint: "# API-NAV-MARKER",
            navigationUrlPrefix: "api");
        var mergedWithCustomOverview = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: topLevelWithCustomOverview,
            navigationEntryPoint: null,
            navigationUrlPrefix: null);

        // Assert
        _ = secondMarkerRun.Should().Be(firstMarkerRun,
            "because re-running navigation generation with a # comment marker must be byte-for-byte idempotent.");
        _ = secondMarkerRun.Should().Contain("- Result: api/Result/index.md")
            .And.Contain("- Result<TValue>: api/Result_T1/index.md",
                "because generic arity overloads must render distinct DisplayTypeName keys and disambiguated directory URLs.");
        _ = mergedWithCustomOverview.Should().Contain("- Overview: custom-portal-intro.md",
            "because a user's custom Overview scalar pointing to a different Markdown file must not be overwritten during top-level merging.");
    }

    [Fact]
    public void GenerateNavigationShouldHandleNestedSectionPathsAppendedNavAndSpecialCharactersInKeys()
    {
        // Arrange
        var generator = new MkDocsNavigationGenerator();
        TypeDocumentationModel[] models =
        [
            new() { AssemblyName = "Acme.Core", NamespaceName = "Acme.Core.Services", TypeName = "Widget:Alpha" },
            new() { AssemblyName = "", NamespaceName = "", TypeName = "GlobalHelper" }
        ];

        var yamlWithoutNav = """
            site_name: Minimal Site
            theme: material
            """;

        var yamlWithExistingSection = """
            site_name: Existing Section Site
            nav:
              - Home: home.md
              - Developer Portal:
                  - Guide: guide.md
                  - API Reference:
                      - StaleType: stale.md
            """;

        var yamlWithExistingTopLevel = """
            nav:
              - Overview: index.md
              - Home: home.md
              - Acme.Core.Services:
                  - Legacy: legacy.md
            # trailing comment
            extra: true
            """;

        // Act
        var appendedNav = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithoutNav,
            navigationEntryPoint: "Docs > API Reference",
            navigationUrlPrefix: "/api/");

        var updatedSectionNav = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithExistingSection,
            navigationEntryPoint: "Developer Portal/API Reference",
            navigationUrlPrefix: null);

        var createdMissingSubSection = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithExistingSection,
            navigationEntryPoint: "Developer Portal/New Sub/API",
            navigationUrlPrefix: null);

        var topLevelMerged = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithExistingTopLevel,
            navigationEntryPoint: null,
            navigationUrlPrefix: null);

        // Assert
        _ = appendedNav.Should().Contain("nav:")
            .And.Contain("- Docs:")
            .And.Contain("- API Reference:")
            .And.Contain("\"Widget:Alpha\": api/Services/Widget:Alpha/index.md")
            .And.Contain("- Global:");

        _ = updatedSectionNav.Should().Contain("- Guide: guide.md")
            .And.NotContain("StaleType")
            .And.Contain("- GlobalHelper: GlobalHelper/index.md");

        _ = createdMissingSubSection.Should().Contain("- New Sub:")
            .And.Contain("- API:");

        _ = topLevelMerged.Should().Contain("- Overview: index.md")
            .And.NotContain("Legacy")
            .And.Contain("extra: true");
    }
}
