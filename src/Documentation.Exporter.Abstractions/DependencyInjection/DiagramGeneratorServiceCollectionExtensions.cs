using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Abstractions.DependencyInjection;

/// <summary>
/// Extension methods allowing plugins and host composition roots to register diagram generator strategies
/// against <see cref="IServiceCollection"/> with only a dependency on <c>Documentation.Exporter.Abstractions</c>.
/// </summary>
public static class DiagramGeneratorServiceCollectionExtensions
{
    /// <summary>
    /// Registers a custom <see cref="ITypeDiagramGenerator"/> strategy as a singleton service.
    /// </summary>
    /// <typeparam name="TGenerator">The type diagram generator implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTypeDiagramGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, ITypeDiagramGenerator
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<ITypeDiagramGenerator, TGenerator>();
    }

    /// <summary>
    /// Registers a custom <see cref="IArchitectureDiagramGenerator"/> strategy as a singleton service.
    /// </summary>
    /// <typeparam name="TGenerator">The architecture diagram generator implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddArchitectureDiagramGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, IArchitectureDiagramGenerator
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<IArchitectureDiagramGenerator, TGenerator>();
    }

    /// <summary>
    /// Registers a custom <see cref="IAssemblyDependencyDiagramGenerator"/> strategy as a singleton service.
    /// </summary>
    /// <typeparam name="TGenerator">The assembly dependency diagram generator implementation type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddAssemblyDependencyDiagramGenerator<TGenerator>(this IServiceCollection serviceCollection)
        where TGenerator : class, IAssemblyDependencyDiagramGenerator
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddSingleton<IAssemblyDependencyDiagramGenerator, TGenerator>();
    }
}
