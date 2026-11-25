using System.Text;
using System.Xml.Linq;
using Documentation.Exporter.Abstractions.DependencyInjection;
using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Parsing.Tags;
using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.DependencyInjection;

[UnitTest]
public sealed class DocumentationExporterPluginServiceCollectionExtensionsTests
{
    private sealed class StubSectionRenderer : IMarkdownSectionRenderer
    {
        public int Order => 999;
        public string RenderSection(TypeDocumentationModel typeModel) => string.Empty;
    }

    private sealed class StubTagHandler : IXmlTagHandler
    {
        public int Order => 999;
        public bool CanHandleTag(string tagName) => false;
        public void TransformTag(XElement xmlElement, StringBuilder outputBuilder, Action<XElement, StringBuilder> processChildrenAction) { }
    }

    private sealed class StubEnricher : ITypeDocumentationEnricher
    {
        public int Order => 999;
        public Task<TypeDocumentationModel> EnrichAsync(
            Type targetType,
            TypeDocumentationModel typeModel,
            ExporterOptions options,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(typeModel);
    }

    private sealed class StubReportFormatter : IQualityReportFormatter
    {
        public QualityReportFormat Format => QualityReportFormat.Custom("Stub");
        public string FormatReport(DocumentationQualityReport report) => "{}";
    }

    private sealed class StubNamespaceHandler : INamespaceHandler
    {
        public bool CanHandleNamespace(string fullTypeName) => false;
        public string ResolveTypeLink(string fullTypeName) => string.Empty;
    }

    private sealed class StubNamespaceHandlerProvider : INamespaceHandlerProvider
    {
        public IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context) => [];
    }

    private sealed class StubNavigationGenerator : INavigationGenerator
    {
        public NavigationFormat Format => NavigationFormat.Custom("StubNav");
        public string DefaultFileName => "stub.nav";
        public string GenerateNavigation(IReadOnlyList<TypeDocumentationModel> types, bool useAssemblySubdirectories = false, string? existingNavigationContent = null, string? navigationEntryPoint = null, string? navigationUrlPrefix = null) => string.Empty;
    }

    private sealed class StubWorkflowStep : IExportWorkflowStep
    {
        public string StepName => "Stub";
        public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    [Fact]
    public void PluginRegistrationHelpersWhenServiceCollectionNullShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? nullServices = null;

        // Act & Assert
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddMarkdownSectionRenderer<StubSectionRenderer>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddXmlTagHandler<StubTagHandler>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddTypeDocumentationEnricher<StubEnricher>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddQualityReportFormatter<StubReportFormatter>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddNamespaceHandler<StubNamespaceHandler>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddNamespaceHandlerProvider<StubNamespaceHandlerProvider>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddNavigationGenerator<StubNavigationGenerator>(nullServices!)).Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => DocumentationExporterPluginServiceCollectionExtensions.AddExportWorkflowStep<StubWorkflowStep>(nullServices!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PluginRegistrationHelpersShouldRegisterAllStrategiesWithExpectedLifetimes()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddMarkdownSectionRenderer<StubSectionRenderer>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddXmlTagHandler<StubTagHandler>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddTypeDocumentationEnricher<StubEnricher>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddQualityReportFormatter<StubReportFormatter>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddNamespaceHandler<StubNamespaceHandler>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddNamespaceHandlerProvider<StubNamespaceHandlerProvider>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddNavigationGenerator<StubNavigationGenerator>(services);
        _ = DocumentationExporterPluginServiceCollectionExtensions.AddExportWorkflowStep<StubWorkflowStep>(services);

        // Also exercise Core forwarding helpers
        _ = DocumentationExporterServiceCollectionExtensions.AddNamespaceHandler<StubNamespaceHandler>(services);
        _ = DocumentationExporterServiceCollectionExtensions.AddNamespaceHandlerProvider<StubNamespaceHandlerProvider>(services);
        _ = DocumentationExporterServiceCollectionExtensions.AddNavigationGenerator<StubNavigationGenerator>(services);

        await using var provider = services.BuildServiceProvider();

        // Assert
        _ = provider.GetServices<IMarkdownSectionRenderer>().Should().ContainSingle(renderer => renderer is StubSectionRenderer);
        _ = provider.GetServices<IXmlTagHandler>().Should().ContainSingle(handler => handler is StubTagHandler);
        _ = provider.GetServices<ITypeDocumentationEnricher>().Should().ContainSingle(enricher => enricher is StubEnricher);
        _ = provider.GetServices<IQualityReportFormatter>().Should().ContainSingle(formatter => formatter is StubReportFormatter);
        _ = provider.GetServices<INamespaceHandler>().Should().HaveCount(2);
        _ = provider.GetServices<INamespaceHandlerProvider>().Should().HaveCount(2);
        _ = provider.GetServices<INavigationGenerator>().Should().HaveCount(2);
        _ = provider.GetServices<IExportWorkflowStep>().Should().ContainSingle(step => step is StubWorkflowStep);

        _ = services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(INavigationGenerator) && descriptor.Lifetime == ServiceLifetime.Singleton);
        _ = services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IMarkdownSectionRenderer) && descriptor.Lifetime == ServiceLifetime.Transient);
    }
}
