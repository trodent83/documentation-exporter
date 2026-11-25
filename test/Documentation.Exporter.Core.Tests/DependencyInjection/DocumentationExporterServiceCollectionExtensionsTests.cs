using System.Runtime.Loader;
using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Parsing.Tags;
using Documentation.Exporter.Abstractions.Plugins;
using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Core.Configuration;
using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.DependencyInjection;

[IntegrationTest]
public sealed class DocumentationExporterServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDocumentationExporterWhenServicesNullShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddDocumentationExporter();

        // Assert
        _ = act.Should().Throw<ArgumentNullException>("because services parameter cannot be null.");
    }

    [Fact]
    public void AddDocumentationExporterPluginsAndGenericRegistrationWhenServicesNullShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var actDirectoryPlugins = () => services!.AddDocumentationExporterPlugins();
        var actSinglePlugin = () => services!.AddDocumentationExporterPlugin<Documentation.Exporter.Plugins.Diagrams.StandardDiagramsPlugin>();

        // Assert
        _ = actDirectoryPlugins.Should().Throw<ArgumentNullException>("because services parameter cannot be null.");
        _ = actSinglePlugin.Should().Throw<ArgumentNullException>("because services parameter cannot be null.");
    }

    [Fact]
    public async Task AddDocumentationExporterShouldBuildWithValidateScopesAndLoadDiagramsPluginFromSubdirectory()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        var returnedServices = services.AddDocumentationExporter();

        // Assert
        _ = returnedServices.Should().BeSameAs(services);

        ServiceProviderOptions validationOptions = new()
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        };

        await using var provider = services.BuildServiceProvider(validationOptions);

        _ = provider.GetRequiredService<IDocumentationExporterEngine>().Should().NotBeNull("because the engine must resolve from root without captive dependencies.");
        _ = provider.GetRequiredService<IExporterConfigurationLoader>().Should().NotBeNull();
        _ = provider.GetRequiredService<IMicrosoftLearnReferenceRegistry>().Should().NotBeNull();
        _ = provider.GetRequiredService<ICSharpSignatureFormatter>().Should().NotBeNull();
        _ = provider.GetRequiredService<IRelativeLinkRewriter>().Should().NotBeNull();
        _ = provider.GetRequiredService<IMemberFileNameFormatter>().Should().NotBeNull();
        _ = provider.GetRequiredService<IExceptionTableFormatter>().Should().NotBeNull();
        _ = provider.GetRequiredService<IDiagramIdentifierFormatter>().Should().NotBeNull();
        _ = provider.GetRequiredService<IDiagramGeneratorSelector>().Should().BeOfType<DiagramGeneratorSelector>(
            "because preferred-with-fallback single-diagram selection is registered in DI.");
        var navigationGenerators = provider.GetServices<INavigationGenerator>().ToList();
        _ = navigationGenerators.Should().Contain(generator => generator.Format == NavigationFormat.MkDocs,
            "because MkDocs navigation generator must be loaded dynamically from Documentation.Exporter.Plugins.Navigation.MkDocs.");
        _ = navigationGenerators.Should().AllSatisfy(generator =>
            AssemblyLoadContext.GetLoadContext(generator.GetType().Assembly).Should().NotBeSameAs(
                AssemblyLoadContext.Default,
                "because navigation plugin assemblies must execute inside an isolated PluginAssemblyLoadContext."));
        _ = provider.GetRequiredService<IPluginLoader>().Should().NotBeNull("because the directory plugin loader must be registered in DI.");

        var loadedPlugins = provider.GetRequiredService<IReadOnlyList<PluginDescriptor>>();
        _ = loadedPlugins.Should().Contain(descriptor =>
                descriptor.AssemblyName == "Documentation.Exporter.Plugins.Diagrams",
            "because the standard diagrams plugin must be dynamically discovered from the plugins/ subdirectory.");
        _ = loadedPlugins.Should().Contain(descriptor =>
                descriptor.AssemblyName == "Documentation.Exporter.Plugins.Navigation.MkDocs",
            "because the MkDocs navigation plugin must be dynamically discovered from the plugins/ subdirectory.");

        var typeGenerators = provider.GetServices<ITypeDiagramGenerator>().ToList();
        var architectureGenerators = provider.GetServices<IArchitectureDiagramGenerator>().ToList();
        var dependencyGenerators = provider.GetServices<IAssemblyDependencyDiagramGenerator>().ToList();

        _ = typeGenerators.Should().HaveCount(2, "because Mermaid and PlantUML type diagram generators are loaded from the Diagrams plugin.");
        _ = typeGenerators.Select(generator => generator.Format).Should().BeEquivalentTo([DiagramFormat.Mermaid, DiagramFormat.PlantUml]);
        _ = typeGenerators.Should().AllSatisfy(generator =>
            AssemblyLoadContext.GetLoadContext(generator.GetType().Assembly).Should().NotBeSameAs(
                AssemblyLoadContext.Default,
                "because plugin assemblies must execute inside an isolated PluginAssemblyLoadContext."));

        _ = architectureGenerators.Should().HaveCount(2, "because Mermaid and PlantUML architecture diagram generators are loaded from the Diagrams plugin.");
        _ = dependencyGenerators.Should().HaveCount(2, "because Mermaid and PlantUML assembly dependency diagram generators are loaded from the Diagrams plugin.");

        await using var scope = provider.CreateAsyncScope();
        var scopedProvider = scope.ServiceProvider;

        _ = scopedProvider.GetRequiredService<ITypeLinkResolver>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IAssemblyLoader>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<ICustomAttributeExtractor>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IAssemblyTypeReflector>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IXmlDocumentationParser>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<ISolutionAssemblyResolver>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IExportArtifactDiscoveryService>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IAssemblyReferenceAnalyzer>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IExampleProvider>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IDocumentationInjector>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IMemberCrefRouteIndexBuilder>().Should().NotBeNull(
            "because overloaded and nested member cref resolution requires IMemberCrefRouteIndexBuilder in DI.");
        _ = scopedProvider.GetRequiredService<IDocumentationQualityAnalyzer>().Should().NotBeNull(
            "because documentation coverage and diagnostic analysis requires IDocumentationQualityAnalyzer in DI.");
        _ = scopedProvider.GetRequiredService<IDocumentationQualityReportWriter>().Should().NotBeNull(
            "because quality report serialization requires IDocumentationQualityReportWriter in DI.");
        _ = scopedProvider.GetServices<IQualityReportFormatter>().Should().HaveCount(3,
            "because Json, Markdown, and Sarif quality report formatters must be registered.");
        _ = scopedProvider.GetRequiredService<ITypePageMarkdownGenerator>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IMemberDetailMarkdownGenerator>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IIndexPageMarkdownGenerator>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<ITypeDocumentationPipeline>().Should().NotBeNull();
        _ = scopedProvider.GetRequiredService<IExportWorkflow>().Should().NotBeNull();

        _ = scopedProvider.GetServices<IXmlTagHandler>().Should().HaveCount(7);
        _ = scopedProvider.GetServices<IMarkdownSectionRenderer>().Should().HaveCount(16, "because all 16 markdown section renderers are registered.");
        _ = scopedProvider.GetServices<ITypeDocumentationEnricher>().Should().HaveCount(3);
        _ = scopedProvider.GetServices<INamespaceHandlerProvider>().Should().HaveCount(4,
            "because built-in local, custom options, pattern rule, and Microsoft Learn namespace handler providers are registered in DI.");
    }

    [Fact]
    public async Task AddDocumentationExporterWithEmptyPluginsDirectoryShouldRegisterZeroHardcodedDiagramGenerators()
    {
        // Arrange
        var emptyPluginsDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_EmptyPlugins_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(emptyPluginsDirectory);
        ServiceCollection services = new();

        try
        {
            // Act
            _ = services.AddDocumentationExporter(emptyPluginsDirectory);
            await using var provider = services.BuildServiceProvider();

            // Assert
            _ = provider.GetRequiredService<IReadOnlyList<PluginDescriptor>>().Should().BeEmpty(
                "because the specified plugins directory contains no plugin assemblies.");
            _ = provider.GetServices<ITypeDiagramGenerator>().Should().BeEmpty(
                "because Core must not hardcode diagram generators when no diagram plugin is loaded.");
            _ = provider.GetServices<IArchitectureDiagramGenerator>().Should().BeEmpty(
                "because Core must not hardcode architecture diagram generators.");
            _ = provider.GetServices<IAssemblyDependencyDiagramGenerator>().Should().BeEmpty(
                "because Core must not hardcode dependency diagram generators.");
            _ = provider.GetServices<INavigationGenerator>().Should().BeEmpty(
                "because Core must not hardcode navigation generators when no navigation plugin is loaded.");
        }
        finally
        {
            if (Directory.Exists(emptyPluginsDirectory))
            {
                Directory.Delete(emptyPluginsDirectory, recursive: true);
            }
        }
    }
}
