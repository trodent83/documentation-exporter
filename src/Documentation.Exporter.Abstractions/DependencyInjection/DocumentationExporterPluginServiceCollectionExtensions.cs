using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Abstractions.Parsing.Tags;
using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Abstractions.DependencyInjection;

/// <summary>
/// Fluent dependency-injection registration helpers allowing plugins and host composition roots to register
/// all Documentation Exporter extension strategies against <see cref="IServiceCollection"/> with the expected
/// service lifetimes while referencing only <c>Documentation.Exporter.Abstractions</c>.
/// </summary>
public static class DocumentationExporterPluginServiceCollectionExtensions
{
    /// <summary>
    /// Registers a custom <see cref="IMarkdownSectionRenderer"/> strategy as a transient service.
    /// </summary>
    /// <typeparam name="TRenderer">The Markdown section renderer implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMarkdownSectionRenderer<TRenderer>(this IServiceCollection serviceCollection)
        where TRenderer : class, IMarkdownSectionRenderer
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddTransient<IMarkdownSectionRenderer, TRenderer>();
    }

    /// <summary>
    /// Registers a custom <see cref="IXmlTagHandler"/> strategy as a transient service.
    /// </summary>
    /// <typeparam name="THandler">The XML documentation tag handler implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddXmlTagHandler<THandler>(this IServiceCollection serviceCollection)
        where THandler : class, IXmlTagHandler
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddTransient<IXmlTagHandler, THandler>();
    }

    /// <summary>
    /// Registers a custom <see cref="ITypeDocumentationEnricher"/> strategy as a transient service.
    /// </summary>
    /// <typeparam name="TEnricher">The type documentation enricher implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTypeDocumentationEnricher<TEnricher>(this IServiceCollection serviceCollection)
        where TEnricher : class, ITypeDocumentationEnricher
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddTransient<ITypeDocumentationEnricher, TEnricher>();
    }

    /// <summary>
    /// Registers a custom <see cref="IQualityReportFormatter"/> strategy as a singleton service.
    /// </summary>
    /// <typeparam name="TFormatter">The quality report formatter implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQualityReportFormatter<TFormatter>(this IServiceCollection serviceCollection)
        where TFormatter : class, IQualityReportFormatter
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<IQualityReportFormatter, TFormatter>();
    }

    /// <summary>
    /// Registers a standalone <see cref="INamespaceHandler"/> strategy as a singleton service.
    /// </summary>
    /// <typeparam name="THandler">The namespace link handler implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNamespaceHandler<THandler>(this IServiceCollection serviceCollection)
        where THandler : class, INamespaceHandler
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<INamespaceHandler, THandler>();
    }

    /// <summary>
    /// Registers an <see cref="INamespaceHandlerProvider"/> factory strategy as a singleton service.
    /// </summary>
    /// <typeparam name="TProvider">The namespace handler provider implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNamespaceHandlerProvider<TProvider>(this IServiceCollection serviceCollection)
        where TProvider : class, INamespaceHandlerProvider
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<INamespaceHandlerProvider, TProvider>();
    }

    /// <summary>
    /// Registers a custom <see cref="INavigationGenerator"/> strategy as a singleton service.
    /// </summary>
    /// <typeparam name="TGenerator">The static-site navigation generator implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNavigationGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, INavigationGenerator
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<INavigationGenerator, TGenerator>();
    }

    /// <summary>
    /// Registers a custom <see cref="IExportWorkflowStep"/> stage as a transient service.
    /// </summary>
    /// <typeparam name="TStep">The export workflow step implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddExportWorkflowStep<TStep>(this IServiceCollection serviceCollection)
        where TStep : class, IExportWorkflowStep
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddTransient<IExportWorkflowStep, TStep>();
    }
}
