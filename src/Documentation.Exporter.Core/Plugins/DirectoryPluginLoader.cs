using System.Reflection;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Plugins;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Plugins;

/// <summary>
/// Discovers and loads <see cref="IDocumentationExporterPlugin"/> implementations from a configured plugins directory
/// using isolated <see cref="PluginAssemblyLoadContext"/> instances while sharing host contracts from <c>Documentation.Exporter.Abstractions</c>.
/// </summary>
/// <param name="fileSystem">The file system abstraction used to inspect and enumerate plugin files.</param>
/// <param name="logger">Optional structured logger for plugin discovery and fault-isolation diagnostics.</param>
public sealed partial class DirectoryPluginLoader(
    IFileSystem fileSystem,
    ILogger<DirectoryPluginLoader>? logger = null) : IPluginLoader
{
    private static readonly HashSet<string> SharedHostAssemblyNames =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "Documentation.Exporter.Abstractions",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Options"
    ];

    private static readonly HashSet<string> SkippedHostAssemblyFileNames =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "Documentation.Exporter.Abstractions.dll",
        "Documentation.Exporter.Core.dll",
        "Documentation.Exporter.Cli.dll"
    ];

    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<DirectoryPluginLoader> logger = logger ?? NullLogger<DirectoryPluginLoader>.Instance;

    [LoggerMessage(EventId = 1101, Level = LogLevel.Debug, Message = "{StatusMessage}")]
    private partial void LogScanningDirectory(string statusMessage);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug, Message = "{StatusMessage}")]
    private partial void LogDirectoryMissing(string statusMessage);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogPluginLoaded(string statusMessage);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Warning, Message = "{WarningMessage}")]
    private partial void LogPluginAssemblyLoadFailed(string warningMessage, Exception exception);

    [LoggerMessage(EventId = 1105, Level = LogLevel.Warning, Message = "{WarningMessage}")]
    private partial void LogPluginConfigurationFailed(string warningMessage, Exception exception);

    [LoggerMessage(EventId = 1106, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogPluginsSummary(string statusMessage);

    /// <inheritdoc />
    public IReadOnlyList<PluginDescriptor> LoadPluginsFromDirectory(IServiceCollection services, string pluginsDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginsDirectoryPath);

        var resolvedPluginsDirectory = this.fileSystem.GetFullPath(pluginsDirectoryPath);
        if (!this.fileSystem.DirectoryExists(resolvedPluginsDirectory))
        {
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                var missingMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.PluginScannerDirectoryMissing,
                    resolvedPluginsDirectory);
                LogDirectoryMissing(missingMessage);
            }

            return [];
        }

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            var scanningMessage = ExecutionMessagesResources.FormatMessage(
                ExecutionMessagesResources.PluginScannerScanningDirectory,
                resolvedPluginsDirectory);
            LogScanningDirectory(scanningMessage);
        }

        var candidateAssemblies = this.fileSystem
            .EnumerateFiles(resolvedPluginsDirectory, "*.dll", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<PluginDescriptor> loadedPlugins = [];
        foreach (var candidateAssemblyPath in candidateAssemblies)
        {
            if (ShouldSkipAssemblyFile(candidateAssemblyPath))
            {
                continue;
            }

            LoadCandidateAssembly(candidateAssemblyPath, services, loadedPlugins);
        }

        if (this.logger.IsEnabled(LogLevel.Information))
        {
            var summaryMessage = ExecutionMessagesResources.FormatMessage(
                ExecutionMessagesResources.PluginLoaderSummary,
                loadedPlugins.Count,
                resolvedPluginsDirectory);
            LogPluginsSummary(summaryMessage);
        }

        return loadedPlugins;
    }

    private static bool ShouldSkipAssemblyFile(string assemblyPath)
    {
        var fileName = Path.GetFileName(assemblyPath);
        return string.IsNullOrWhiteSpace(fileName) ||
               SkippedHostAssemblyFileNames.Contains(fileName) ||
               fileName.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase);
    }

    private void LoadCandidateAssembly(
        string assemblyPath,
        IServiceCollection services,
        List<PluginDescriptor> loadedPlugins)
    {
        Assembly loadedAssembly;
        try
        {
            var fullAssemblyPath = this.fileSystem.GetFullPath(assemblyPath);
            PluginAssemblyLoadContext loadContext = new(fullAssemblyPath, SharedHostAssemblyNames);
            loadedAssembly = loadContext.LoadFromAssemblyPath(fullAssemblyPath);
        }
        catch (Exception loadException) when (loadException is IOException or UnauthorizedAccessException or BadImageFormatException or FileLoadException)
        {
            if (this.logger.IsEnabled(LogLevel.Warning))
            {
                var failureMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.PluginLoaderAssemblyFailed,
                    assemblyPath,
                    loadException.Message);
                LogPluginAssemblyLoadFailed(failureMessage, loadException);
            }

            return;
        }

        IReadOnlyList<Type> pluginTypes;
        try
        {
            pluginTypes = DiscoverPluginTypes(loadedAssembly);
        }
        catch (Exception inspectionException)
        {
            if (this.logger.IsEnabled(LogLevel.Warning))
            {
                var failureMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.PluginLoaderAssemblyFailed,
                    assemblyPath,
                    inspectionException.Message);
                LogPluginAssemblyLoadFailed(failureMessage, inspectionException);
            }

            return;
        }

        foreach (var pluginType in pluginTypes)
        {
            TryActivateAndConfigurePlugin(pluginType, loadedAssembly, assemblyPath, services, loadedPlugins);
        }
    }

    private static List<Type> DiscoverPluginTypes(Assembly candidateAssembly)
    {
        Type[] exportedOrAllTypes;
        try
        {
            exportedOrAllTypes = candidateAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException typeLoadException)
        {
            exportedOrAllTypes =
            [
                .. typeLoadException.Types.Where(candidateType => candidateType != null).Cast<Type>()
            ];
        }

        var pluginContractType = typeof(IDocumentationExporterPlugin);
        return
        [
            .. exportedOrAllTypes
                .Where(candidateType =>
                    candidateType is { IsClass: true, IsAbstract: false, IsPublic: true } &&
                    pluginContractType.IsAssignableFrom(candidateType))
                .OrderBy(candidateType => candidateType.FullName, StringComparer.Ordinal)
        ];
    }

    private void TryActivateAndConfigurePlugin(
        Type pluginType,
        Assembly loadedAssembly,
        string assemblyPath,
        IServiceCollection services,
        List<PluginDescriptor> loadedPlugins)
    {
        try
        {
            if (Activator.CreateInstance(pluginType) is not IDocumentationExporterPlugin pluginInstance)
            {
                return;
            }

            pluginInstance.ConfigureServices(services);

            var pluginDisplayName = string.IsNullOrWhiteSpace(pluginInstance.Name)
                ? pluginType.Name
                : pluginInstance.Name;
            var assemblySimpleName = loadedAssembly.GetName().Name
                ?? Path.GetFileNameWithoutExtension(assemblyPath);
            var typeFullName = pluginType.FullName ?? pluginType.Name;

            loadedPlugins.Add(new PluginDescriptor
            {
                PluginName = pluginDisplayName,
                AssemblyName = assemblySimpleName,
                AssemblyPath = assemblyPath,
                PluginTypeFullName = typeFullName
            });

            if (this.logger.IsEnabled(LogLevel.Information))
            {
                var loadedMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.PluginLoaderLoadedPlugin,
                    pluginDisplayName,
                    typeFullName,
                    assemblyPath);
                LogPluginLoaded(loadedMessage);
            }
        }
        catch (Exception activationException)
        {
            if (this.logger.IsEnabled(LogLevel.Warning))
            {
                var activationErrorMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.PluginLoaderActivationFailed,
                    pluginType.FullName ?? pluginType.Name,
                    assemblyPath,
                    activationException.Message);
                LogPluginConfigurationFailed(activationErrorMessage, activationException);
            }
        }
    }
}
