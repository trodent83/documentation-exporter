using Documentation.Exporter.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Extension methods for registering Documentation Exporter CLI, Core, and plugin services in a unified <see cref="IServiceCollection"/>.
/// </summary>
public static class CliServiceCollectionExtensions
{
    /// <summary>
    /// Registers core documentation exporter services, default subdirectory plugins, dynamic console logging,
    /// options, subcommands, command builder, and pipeline runner in the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDocumentationExporterCli(this IServiceCollection serviceCollection) =>
        serviceCollection.AddDocumentationExporterCli(pluginsDirectoryPath: null);

    /// <summary>
    /// Registers core documentation exporter services, plugins loaded from <paramref name="pluginsDirectoryPath"/>
    /// (or the default <c>plugins/</c> subdirectory when <see langword="null"/>), dynamic console logging,
    /// options, subcommands, command builder, and pipeline runner in the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="pluginsDirectoryPath">Optional custom plugins directory overriding <c>&lt;AppContext.BaseDirectory&gt;/plugins</c>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDocumentationExporterCli(
        this IServiceCollection serviceCollection,
        string? pluginsDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        CliLoggingLevelSwitch loggingLevelSwitch = new();

        return serviceCollection
            .AddDocumentationExporter(pluginsDirectoryPath)
            .AddSingleton(loggingLevelSwitch)
            .AddLogging(loggingBuilder =>
            {
                _ = loggingBuilder.AddConsole();
                _ = loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                _ = loggingBuilder.AddFilter((_, level) => level >= loggingLevelSwitch.MinimumLevel);
            })
            .AddSingleton<IConsoleOutput, SystemConsoleOutput>()
            .AddSingleton<ISharedCliOptions, SharedCliOptions>()
            .AddTransient<ICliPipelineRunner, CliPipelineRunner>()
            .AddCliSubcommands()
            .AddTransient<ICliCommandBuilder, CliCommandBuilder>();
    }

    /// <summary>
    /// Registers all built-in <see cref="ICliSubcommand"/> implementations.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCliSubcommands(this IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return serviceCollection
            .AddCliSubcommand<AssemblyCliSubcommand>()
            .AddCliSubcommand<SolutionCliSubcommand>()
            .AddCliSubcommand<ConfigCliSubcommand>();
    }

    /// <summary>
    /// Registers a custom <see cref="ICliSubcommand"/> implementation.
    /// </summary>
    /// <typeparam name="TSubcommand">The subcommand type.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCliSubcommand<TSubcommand>(this IServiceCollection serviceCollection)
        where TSubcommand : class, ICliSubcommand
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        return serviceCollection.AddTransient<ICliSubcommand, TSubcommand>();
    }
}
