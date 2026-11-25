using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Documentation.Exporter.Cli;

/// <summary>
/// CLI entry point for Documentation.Exporter.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application main entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Process exit code (0 = success, 1 = export failure, 2 = argument/configuration error).</returns>
    public static async Task<int> Main(string[] args)
    {
        var pluginsDirectoryPath = CliPluginDirectoryResolver.ResolvePluginsDirectory(args);
        ServiceCollection serviceCollection = new();
        _ = serviceCollection.AddDocumentationExporterCli(pluginsDirectoryPath);

        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        var rootCommand = serviceProvider.GetRequiredService<ICliCommandBuilder>().BuildRootCommand();
        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }
}
