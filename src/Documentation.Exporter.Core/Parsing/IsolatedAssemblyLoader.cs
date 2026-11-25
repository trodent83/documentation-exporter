using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Loads target assemblies inside an isolated, collectible <see cref="AssemblyLoadContext"/> with per-assembly
/// dependency resolution via <see cref="AssemblyDependencyResolver"/>.
/// </summary>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class IsolatedAssemblyLoader(
    IFileSystem fileSystem,
    ILogger<IsolatedAssemblyLoader>? logger = null) : IAssemblyLoader, IDisposable
{
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<IsolatedAssemblyLoader> logger = logger ?? NullLogger<IsolatedAssemblyLoader>.Instance;
    private readonly List<AssemblyDependencyResolver> dependencyResolvers = [];
    private readonly List<string> assemblyDirectories = [];
    private AssemblyLoadContext? sharedLoadContext;

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogAssemblyFilePathMissing(string statusMessage);

    [LoggerMessage(EventId = 102, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogAssemblyFileNotFound(string statusMessage);

    [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogLoadingAssembly(string statusMessage);

    [LoggerMessage(EventId = 104, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogExtractedTypes(string statusMessage);

    [LoggerMessage(EventId = 105, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogFailedLoadingAssembly(Exception exception, string statusMessage);

    [LoggerMessage(EventId = 106, Level = LogLevel.Warning, Message = "Failed to load some types from assembly '{AssemblyFilePath}'. Partial types were recovered: {StatusMessage}")]
    private partial void LogPartialReflectionTypeLoad(string assemblyFilePath, string statusMessage);

    [LoggerMessage(EventId = 107, Level = LogLevel.Warning, Message = "Duplicate type name '{TypeName}' found in assembly '{DuplicateAssembly}' (already loaded from '{OriginalAssembly}'). The existing type definition will be retained.")]
    private partial void LogDuplicateTypeFound(string typeName, string duplicateAssembly, string originalAssembly);

    /// <inheritdoc />
    public bool TryLoadExportedTypes(string? assemblyFilePath, out IReadOnlyDictionary<string, Type>? exportedTypesMap)
    {
        exportedTypesMap = null;

        if (string.IsNullOrWhiteSpace(assemblyFilePath))
        {
            LogAssemblyFilePathMissing(ExecutionMessagesResources.ReflectorAssemblyFilePathMissing);
            return false;
        }

        if (!this.fileSystem.FileExists(assemblyFilePath))
        {
            LogAssemblyFileNotFound(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.ReflectorAssemblyFileNotFound, assemblyFilePath));
            return false;
        }

        try
        {
            var loadingMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.ReflectorLoadingAssembly, assemblyFilePath);
            LogLoadingAssembly(loadingMessage);

            var fullAssemblyPath = this.fileSystem.GetFullPath(assemblyFilePath);
            var loadContext = GetOrCreateLoadContext();
            dependencyResolvers.Add(new AssemblyDependencyResolver(fullAssemblyPath));

            var directoryPath = Path.GetDirectoryName(fullAssemblyPath);
            if (!string.IsNullOrWhiteSpace(directoryPath) && !assemblyDirectories.Contains(directoryPath, StringComparer.OrdinalIgnoreCase))
            {
                assemblyDirectories.Add(directoryPath);
            }

            var targetAssembly = loadContext.LoadFromAssemblyPath(fullAssemblyPath);
            var exportedTypes = ExtractLoadableExportedTypes(targetAssembly, fullAssemblyPath);

            var typeMap = exportedTypes
                .Where(exportedType => exportedType.FullName != null)
                .ToDictionary(exportedType => exportedType.FullName!, exportedType => exportedType, StringComparer.Ordinal);

            var extractedMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.ReflectorExtractedTypes, typeMap.Count);
            LogExtractedTypes(extractedMessage);
            exportedTypesMap = typeMap;
            return true;
        }
        catch (Exception exception)
        {
            LogFailedLoadingAssembly(exception, ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.ReflectorFailedLoadingAssembly, assemblyFilePath));
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryLoadExportedTypes(IEnumerable<string> assemblyFilePaths, out IReadOnlyDictionary<string, Type>? exportedTypesMap)
    {
        ArgumentNullException.ThrowIfNull(assemblyFilePaths);

        var pathList = assemblyFilePaths.ToList();
        if (pathList.Count == 0)
        {
            return TryLoadExportedTypes((string?)null, out exportedTypesMap);
        }

        Dictionary<string, Type> mergedTypes = [with(StringComparer.Ordinal)];
        foreach (var assemblyPath in pathList)
        {
            if (!TryLoadExportedTypes(assemblyPath, out var singleAssemblyTypes))
            {
                exportedTypesMap = null;
                return false;
            }

            if (singleAssemblyTypes == null)
            {
                continue;
            }

            MergeLoadedTypes(mergedTypes, singleAssemblyTypes, assemblyPath);
        }

        exportedTypesMap = mergedTypes;
        return true;
    }

    /// <summary>
    /// Unloads the isolated collectible <see cref="AssemblyLoadContext"/> and releases cached dependency resolvers.
    /// </summary>
    public void Dispose()
    {
        sharedLoadContext?.Unload();
        sharedLoadContext = null;
        dependencyResolvers.Clear();
        assemblyDirectories.Clear();
    }

    private AssemblyLoadContext GetOrCreateLoadContext()
    {
        if (sharedLoadContext != null)
        {
            return sharedLoadContext;
        }

        var contextName = $"DocumentationExporter_{Guid.NewGuid():N}";
        var loadContext = new AssemblyLoadContext(contextName, isCollectible: true);

        loadContext.Resolving += (context, assemblyName) =>
        {
            foreach (var resolver in dependencyResolvers)
            {
                var resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
                if (resolvedPath != null && this.fileSystem.FileExists(resolvedPath))
                {
                    return context.LoadFromAssemblyPath(resolvedPath);
                }
            }

            foreach (var directoryPath in assemblyDirectories)
            {
                var candidateDllPath = Path.Combine(directoryPath, $"{assemblyName.Name}.dll");
                if (this.fileSystem.FileExists(candidateDllPath))
                {
                    return context.LoadFromAssemblyPath(candidateDllPath);
                }
            }

            return null;
        };

        sharedLoadContext = loadContext;
        return sharedLoadContext;
    }

    private Type[] ExtractLoadableExportedTypes(Assembly targetAssembly, string fullAssemblyPath)
    {
        try
        {
            return [.. targetAssembly.GetExportedTypes().Where(IsExportablePublicType)];
        }
        catch (ReflectionTypeLoadException reflectionException)
        {
            var loaderErrorCount = reflectionException.LoaderExceptions.Count(loaderException => loaderException != null);
            LogPartialReflectionTypeLoad(fullAssemblyPath, $"{loaderErrorCount} loader exception(s) encountered.");
            return
            [
                .. reflectionException.Types
                    .Where(candidateType => candidateType != null && IsExportablePublicType(candidateType))
                    .Select(candidateType => candidateType!)
            ];
        }
    }

    private static bool IsExportablePublicType(Type candidateType) =>
        candidateType.IsVisible &&
        !candidateType.IsSpecialName &&
        !candidateType.Name.StartsWith('<') &&
        !candidateType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    private void MergeLoadedTypes(
        Dictionary<string, Type> mergedTypes,
        IReadOnlyDictionary<string, Type> singleAssemblyTypes,
        string assemblyPath)
    {
        foreach (var typeEntry in singleAssemblyTypes)
        {
            if (mergedTypes.TryGetValue(typeEntry.Key, out var existingType))
            {
                var duplicateAssembly = typeEntry.Value.Assembly.GetName().Name ?? assemblyPath;
                var originalAssembly = existingType.Assembly.GetName().Name ?? string.Empty;
                LogDuplicateTypeFound(typeEntry.Key, duplicateAssembly, originalAssembly);

                if (!string.Equals(duplicateAssembly, originalAssembly, StringComparison.OrdinalIgnoreCase))
                {
                    var qualifiedDuplicateKey = $"{duplicateAssembly}::{typeEntry.Key}";
                    _ = mergedTypes.TryAdd(qualifiedDuplicateKey, typeEntry.Value);
                }

                continue;
            }

            mergedTypes[typeEntry.Key] = typeEntry.Value;
        }
    }
}
