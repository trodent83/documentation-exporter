using Documentation.Exporter.Abstractions.DependencyInjection;
using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Parsing.Tags;
using Documentation.Exporter.Abstractions.Plugins;
using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Configuration;
using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Core.IO;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Parsing.Tags;
using Documentation.Exporter.Core.Plugins;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resolution.Providers;
using Documentation.Exporter.Core.Workflow;
using Documentation.Exporter.Core.Workflow.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Core.DependencyInjection;

/// <summary>
/// Extension methods for setting up Documentation Exporter services and plugins in an <see cref="IServiceCollection"/>.
/// </summary>
public static class DocumentationExporterServiceCollectionExtensions
{
    /// <summary>
    /// Default relative subdirectory name beneath <see cref="AppContext.BaseDirectory"/> scanned for external plugin assemblies.
    /// </summary>
    public const string DefaultPluginsSubdirectoryName = "plugins";

    /// <summary>
    /// Adds Documentation Exporter core services, pipelines, section renderers, workflow steps, and all plugins discovered
    /// in the default <c>plugins/</c> subdirectory beneath <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddDocumentationExporter(this IServiceCollection serviceCollection) =>
        serviceCollection.AddDocumentationExporter(pluginsDirectoryPath: null);

    /// <summary>
    /// Adds Documentation Exporter core services, pipelines, section renderers, workflow steps, and plugins loaded from
    /// <paramref name="pluginsDirectoryPath"/> (or <c>&lt;AppContext.BaseDirectory&gt;/plugins</c> when <see langword="null"/> or whitespace).
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="pluginsDirectoryPath">Optional plugins directory path overriding the default <c>plugins/</c> subdirectory.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddDocumentationExporter(
        this IServiceCollection serviceCollection,
        string? pluginsDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddCoreInfrastructure()
            .AddResolutionHandlers()
            .AddXmlTagHandlers()
            .AddDiagramGenerators()
            .AddMarkdownSectionRenderers()
            .AddMarkdownGenerators()
            .AddTypeDocumentationEnrichers()
            .AddQualityReporting()
            .AddExportWorkflowSteps()
            .AddTransient<IDocumentationExporterEngine, DocumentationExporterEngine>()
            .AddDocumentationExporterPlugins(pluginsDirectoryPath);
    }

    /// <summary>
    /// Scans <paramref name="pluginsDirectoryPath"/> (or <c>&lt;AppContext.BaseDirectory&gt;/plugins</c> when omitted)
    /// for <see cref="IDocumentationExporterPlugin"/> implementations and executes their service registrations.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="pluginsDirectoryPath">Optional custom plugins directory path.</param>
    /// <param name="logger">Optional logger passed to <see cref="DirectoryPluginLoader"/> during discovery.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDocumentationExporterPlugins(
        this IServiceCollection serviceCollection,
        string? pluginsDirectoryPath = null,
        ILogger<DirectoryPluginLoader>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        var targetPluginsDirectory = string.IsNullOrWhiteSpace(pluginsDirectoryPath)
            ? Path.Combine(AppContext.BaseDirectory, DefaultPluginsSubdirectoryName)
            : pluginsDirectoryPath;

        DirectoryPluginLoader pluginLoader = new(new PhysicalFileSystem(), logger);
        var discoveredPlugins = pluginLoader.LoadPluginsFromDirectory(serviceCollection, targetPluginsDirectory);
        foreach (var descriptor in discoveredPlugins)
        {
            _ = serviceCollection.AddSingleton(descriptor);
        }

        _ = serviceCollection.AddSingleton<IReadOnlyList<PluginDescriptor>>(discoveredPlugins);
        return serviceCollection;
    }

    /// <summary>
    /// Programmatically instantiates and configures a single <see cref="IDocumentationExporterPlugin"/> implementation.
    /// </summary>
    /// <typeparam name="TPlugin">The plugin entry-point type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDocumentationExporterPlugin<TPlugin>(this IServiceCollection serviceCollection)
        where TPlugin : class, IDocumentationExporterPlugin, new()
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        TPlugin plugin = new();
        plugin.ConfigureServices(serviceCollection);
        var pluginAssembly = typeof(TPlugin).Assembly;

        return serviceCollection.AddSingleton(new PluginDescriptor
        {
            PluginName = string.IsNullOrWhiteSpace(plugin.Name) ? typeof(TPlugin).Name : plugin.Name,
            AssemblyName = pluginAssembly.GetName().Name ?? typeof(TPlugin).Assembly.ToString(),
            AssemblyPath = pluginAssembly.Location,
            PluginTypeFullName = typeof(TPlugin).FullName ?? typeof(TPlugin).Name
        });
    }

    /// <summary>
    /// Registers core configuration, resolution, discovery, assembly loading/reflection, XML parsing, plugin loading, and injection services.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddSingleton<IFileSystem, PhysicalFileSystem>()
            .AddSingleton<IPluginLoader, DirectoryPluginLoader>()
            .AddSingleton<IExporterConfigurationLoader, ExporterConfigurationLoader>()
            .AddSingleton<IMicrosoftLearnReferenceRegistry, MicrosoftLearnReferenceRegistry>()
            .AddSingleton<ICSharpSignatureFormatter, CSharpSignatureFormatter>()
            .AddSingleton<IMemberCrefRouteIndexBuilder, MemberCrefRouteIndexBuilder>()
            .AddScoped<ITypeLinkResolver, TypeLinkResolver>()
            .AddScoped<IAssemblyLoader, IsolatedAssemblyLoader>()
            .AddScoped<ICustomAttributeExtractor, CustomAttributeExtractor>()
            .AddScoped<IAssemblyTypeReflector, AssemblyTypeReflector>()
            .AddScoped<IXmlDocumentationParser, XmlDocumentationParser>()
            .AddTransient<ISolutionAssemblyResolver, SolutionAssemblyResolver>()
            .AddTransient<IExportArtifactDiscoveryService, ExportArtifactDiscoveryService>()
            .AddTransient<IAssemblyReferenceAnalyzer, AssemblyReferenceAnalyzer>()
            .AddTransient<IExampleProvider, FileExampleProvider>()
            .AddTransient<IDocumentationInjector, FileDocumentationInjector>();
    }

    /// <summary>
    /// Registers the built-in <see cref="INamespaceHandlerProvider"/> strategies for local projects,
    /// caller-supplied custom handlers, external URL pattern rules, and Microsoft Learn fallbacks.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddResolutionHandlers(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddNamespaceHandlerProvider<LocalProjectNamespaceHandlerProvider>()
            .AddNamespaceHandlerProvider<CustomOptionsNamespaceHandlerProvider>()
            .AddNamespaceHandlerProvider<PatternNamespaceHandlerProvider>()
            .AddNamespaceHandlerProvider<MicrosoftDocsNamespaceHandlerProvider>();
    }

    /// <summary>
    /// Registers a standalone <see cref="INamespaceHandler"/> strategy.
    /// </summary>
    /// <typeparam name="THandler">The namespace handler type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNamespaceHandler<THandler>(this IServiceCollection serviceCollection)
        where THandler : class, INamespaceHandler =>
        DocumentationExporterPluginServiceCollectionExtensions.AddNamespaceHandler<THandler>(serviceCollection);

    /// <summary>
    /// Registers a custom <see cref="INamespaceHandlerProvider"/> strategy.
    /// </summary>
    /// <typeparam name="TProvider">The namespace handler provider type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNamespaceHandlerProvider<TProvider>(this IServiceCollection serviceCollection)
        where TProvider : class, INamespaceHandlerProvider =>
        DocumentationExporterPluginServiceCollectionExtensions.AddNamespaceHandlerProvider<TProvider>(serviceCollection);

    /// <summary>
    /// Registers the built-in <see cref="IXmlTagHandler"/> strategies and <see cref="IXmlTagTransformer"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddXmlTagHandlers(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddXmlTagHandler<CodeTagHandler>()
            .AddXmlTagHandler<SeeTagHandler>()
            .AddXmlTagHandler<ParamRefTagHandler>()
            .AddXmlTagHandler<ParagraphTagHandler>()
            .AddXmlTagHandler<ListTagHandler>()
            .AddXmlTagHandler<NoteTagHandler>()
            .AddXmlTagHandler<PassthroughTagHandler>()
            .AddScoped<IXmlTagTransformer, XmlTagTransformer>();
    }

    /// <summary>
    /// Registers a custom <see cref="IXmlTagHandler"/> implementation.
    /// </summary>
    /// <typeparam name="THandler">The tag handler type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddXmlTagHandler<THandler>(this IServiceCollection serviceCollection)
        where THandler : class, IXmlTagHandler =>
        DocumentationExporterPluginServiceCollectionExtensions.AddXmlTagHandler<THandler>(serviceCollection);

    /// <summary>
    /// Registers shared diagram identifier formatting and generator selection infrastructure.
    /// Concrete diagram generators are supplied dynamically by loaded plugins (or explicit <c>Add*DiagramGenerator</c> calls).
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDiagramGenerators(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddSingleton<IDiagramIdentifierFormatter, DiagramIdentifierFormatter>()
            .AddSingleton<IDiagramGeneratorSelector, DiagramGeneratorSelector>();
    }

    /// <summary>
    /// Registers a custom <see cref="ITypeDiagramGenerator"/> strategy.
    /// </summary>
    /// <typeparam name="TGenerator">The type diagram generator type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTypeDiagramGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, ITypeDiagramGenerator =>
        DiagramGeneratorServiceCollectionExtensions.AddTypeDiagramGenerator<TGenerator>(serviceCollection);

    /// <summary>
    /// Registers a custom <see cref="IArchitectureDiagramGenerator"/> strategy.
    /// </summary>
    /// <typeparam name="TGenerator">The architecture diagram generator type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddArchitectureDiagramGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, IArchitectureDiagramGenerator =>
        DiagramGeneratorServiceCollectionExtensions.AddArchitectureDiagramGenerator<TGenerator>(serviceCollection);

    /// <summary>
    /// Registers a custom <see cref="IAssemblyDependencyDiagramGenerator"/> strategy.
    /// </summary>
    /// <typeparam name="TGenerator">The assembly dependency diagram generator type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddAssemblyDependencyDiagramGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, IAssemblyDependencyDiagramGenerator =>
        DiagramGeneratorServiceCollectionExtensions.AddAssemblyDependencyDiagramGenerator<TGenerator>(serviceCollection);

    /// <summary>
    /// Registers all built-in <see cref="IMarkdownSectionRenderer"/> strategies.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMarkdownSectionRenderers(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddMarkdownSectionRenderer<HeaderTitleSectionRenderer>()
            .AddMarkdownSectionRenderer<SyntaxDeclarationSectionRenderer>()
            .AddMarkdownSectionRenderer<InheritanceHierarchySectionRenderer>()
            .AddMarkdownSectionRenderer<ImplementedInterfacesSectionRenderer>()
            .AddMarkdownSectionRenderer<NestedTypesSectionRenderer>()
            .AddMarkdownSectionRenderer<DerivedTypesSectionRenderer>()
            .AddMarkdownSectionRenderer<TypeDiagramSectionRenderer>()
            .AddMarkdownSectionRenderer<SidecarBeforeRemarksSectionRenderer>()
            .AddMarkdownSectionRenderer<MembersSectionRenderer>()
            .AddMarkdownSectionRenderer<ExceptionsSectionRenderer>()
            .AddMarkdownSectionRenderer<RemarksSectionRenderer>()
            .AddMarkdownSectionRenderer<SidecarAfterRemarksSectionRenderer>()
            .AddMarkdownSectionRenderer<ExamplesSectionRenderer>()
            .AddMarkdownSectionRenderer<SidecarAfterExamplesSectionRenderer>()
            .AddMarkdownSectionRenderer<SeeAlsoSectionRenderer>()
            .AddMarkdownSectionRenderer<SidecarPageAppendSectionRenderer>();
    }

    /// <summary>
    /// Registers segregated Markdown page, member detail, index, and shared formatting services.
    /// Static-site navigation generators (<see cref="INavigationGenerator"/>) are supplied dynamically by plugins
    /// or explicit <see cref="AddNavigationGenerator{TGenerator}"/> calls.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMarkdownGenerators(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddSingleton<IRelativeLinkRewriter, RelativeLinkRewriter>()
            .AddSingleton<IMemberFileNameFormatter, MemberFileNameFormatter>()
            .AddSingleton<IExceptionTableFormatter, ExceptionTableFormatter>()
            .AddTransient<ITypePageMarkdownGenerator, TypePageMarkdownGenerator>()
            .AddTransient<IMemberDetailMarkdownGenerator, MemberDetailMarkdownGenerator>()
            .AddTransient<IIndexPageMarkdownGenerator, IndexPageMarkdownGenerator>();
    }

    /// <summary>
    /// Registers a custom <see cref="INavigationGenerator"/> strategy.
    /// </summary>
    /// <typeparam name="TGenerator">The navigation generator type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNavigationGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, INavigationGenerator =>
        DocumentationExporterPluginServiceCollectionExtensions.AddNavigationGenerator<TGenerator>(serviceCollection);

    /// <summary>
    /// Registers a custom <see cref="IMarkdownSectionRenderer"/> implementation.
    /// </summary>
    /// <typeparam name="TRenderer">The section renderer type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMarkdownSectionRenderer<TRenderer>(this IServiceCollection serviceCollection)
        where TRenderer : class, IMarkdownSectionRenderer =>
        DocumentationExporterPluginServiceCollectionExtensions.AddMarkdownSectionRenderer<TRenderer>(serviceCollection);

    /// <summary>
    /// Registers all built-in <see cref="ITypeDocumentationEnricher"/> stages and <see cref="ITypeDocumentationPipeline"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTypeDocumentationEnrichers(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddTypeDocumentationEnricher<XmlDocumentationEnricher>()
            .AddTypeDocumentationEnricher<ExampleSnippetEnricher>()
            .AddTypeDocumentationEnricher<SidecarOverrideEnricher>()
            .AddTransient<ITypeDocumentationPipeline, TypeDocumentationPipeline>();
    }

    /// <summary>
    /// Registers a custom <see cref="ITypeDocumentationEnricher"/> implementation.
    /// </summary>
    /// <typeparam name="TEnricher">The enricher type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTypeDocumentationEnricher<TEnricher>(this IServiceCollection serviceCollection)
        where TEnricher : class, ITypeDocumentationEnricher =>
        DocumentationExporterPluginServiceCollectionExtensions.AddTypeDocumentationEnricher<TEnricher>(serviceCollection);

    /// <summary>
    /// Registers the default <see cref="IDocumentationQualityAnalyzer"/>, built-in <see cref="IQualityReportFormatter"/>
    /// strategies (JSON, Markdown, SARIF), and <see cref="IDocumentationQualityReportWriter"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQualityReporting(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddSingleton<IDocumentationQualityAnalyzer, DocumentationQualityAnalyzer>()
            .AddQualityReportFormatter<JsonQualityReportFormatter>()
            .AddQualityReportFormatter<MarkdownQualityReportFormatter>()
            .AddQualityReportFormatter<SarifQualityReportFormatter>()
            .AddTransient<IDocumentationQualityReportWriter, DocumentationQualityReportWriter>();
    }

    /// <summary>
    /// Registers a custom <see cref="IQualityReportFormatter"/> strategy.
    /// </summary>
    /// <typeparam name="TFormatter">The report formatter type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQualityReportFormatter<TFormatter>(this IServiceCollection serviceCollection)
        where TFormatter : class, IQualityReportFormatter =>
        DocumentationExporterPluginServiceCollectionExtensions.AddQualityReportFormatter<TFormatter>(serviceCollection);

    /// <summary>
    /// Registers all built-in <see cref="IExportWorkflowStep"/> stages and <see cref="IExportWorkflow"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddExportWorkflowSteps(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddExportWorkflowStep<ValidateOptionsStep>()
            .AddExportWorkflowStep<LoadAssemblyTypesStep>()
            .AddExportWorkflowStep<ConfigureResolutionHandlersStep>()
            .AddExportWorkflowStep<LoadXmlDocumentationStep>()
            .AddExportWorkflowStep<CompileTypeDocumentationModelsStep>()
            .AddExportWorkflowStep<CalculateDocumentationCoverageStep>()
            .AddExportWorkflowStep<EnsureOutputDirectoryStep>()
            .AddExportWorkflowStep<EmitMarkdownDocumentationStep>()
            .AddExportWorkflowStep<ValidateMarkdownLinksStep>()
            .AddExportWorkflowStep<EmitQualityReportStep>()
            .AddTransient<IExportWorkflow, ExportWorkflow>();
    }

    /// <summary>
    /// Registers a custom <see cref="IExportWorkflowStep"/> implementation.
    /// </summary>
    /// <typeparam name="TStep">The workflow step type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddExportWorkflowStep<TStep>(this IServiceCollection serviceCollection)
        where TStep : class, IExportWorkflowStep =>
        DocumentationExporterPluginServiceCollectionExtensions.AddExportWorkflowStep<TStep>(serviceCollection);
}
