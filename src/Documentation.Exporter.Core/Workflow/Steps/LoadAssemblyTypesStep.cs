using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Loads the target assembly (or solution assemblies) and extracts all filtered exported public types into the workflow context.
/// </summary>
/// <param name="assemblyLoader">The isolated assembly loader.</param>
/// <param name="artifactDiscoveryService">The export artifact discovery service.</param>
/// <param name="assemblyReferenceAnalyzer">The assembly reference analyzer extracting dependency and strong-name metadata.</param>
/// <param name="customAttributeExtractor">The custom attribute extractor used to inspect <c>[EditorBrowsable]</c>.</param>
/// <param name="signatureFormatter">The C# signature formatter used for clean type name matching.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class LoadAssemblyTypesStep(
    IAssemblyLoader assemblyLoader,
    IExportArtifactDiscoveryService artifactDiscoveryService,
    IAssemblyReferenceAnalyzer assemblyReferenceAnalyzer,
    ICustomAttributeExtractor customAttributeExtractor,
    ICSharpSignatureFormatter signatureFormatter,
    ILogger<LoadAssemblyTypesStep>? logger = null) : IExportWorkflowStep
{
    private readonly IAssemblyLoader loader = ArgumentGuard.EnsureNotNull(assemblyLoader);
    private readonly IExportArtifactDiscoveryService discoveryService = ArgumentGuard.EnsureNotNull(artifactDiscoveryService);
    private readonly IAssemblyReferenceAnalyzer referenceAnalyzer = ArgumentGuard.EnsureNotNull(assemblyReferenceAnalyzer);
    private readonly ICustomAttributeExtractor attributeExtractor = ArgumentGuard.EnsureNotNull(customAttributeExtractor);
    private readonly ICSharpSignatureFormatter formatter = ArgumentGuard.EnsureNotNull(signatureFormatter);
    private readonly ILogger<LoadAssemblyTypesStep> logger = logger ?? NullLogger<LoadAssemblyTypesStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameLoadAssemblyTypes;

    [LoggerMessage(EventId = 812, Level = LogLevel.Warning, Message = "{StatusMessage}")]
    private partial void LogNoTypesFound(string statusMessage);

    [LoggerMessage(EventId = 813, Level = LogLevel.Information, Message = "Loaded {RetainedCount} types after filtering out {ExcludedCount} types matching exclusion rules.")]
    private partial void LogTypesFiltered(int retainedCount, int excludedCount);

    [LoggerMessage(EventId = 815, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogMissingProjectArtifact(Exception exception, string statusMessage);

    /// <inheritdoc />
    public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        ExportArtifactDiscoveryResult discoveryResult;
        try
        {
            discoveryResult = discoveryService.DiscoverArtifactDetails(context.Options);
            context.ResolvedXmlDocumentationFilePaths = discoveryResult.XmlDocumentationFilePaths;
        }
        catch (FileNotFoundException missingArtifactException)
        {
            LogMissingProjectArtifact(missingArtifactException, missingArtifactException.Message);
            return Task.FromResult(false);
        }

        var targetAssemblyPaths = discoveryResult.AssemblyFilePaths;
        if (targetAssemblyPaths.Count == 0 && !string.IsNullOrWhiteSpace(context.Options.SolutionFilePath))
        {
            LogNoTypesFound(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepNoTypesFound, context.Options.SolutionFilePath));
            return Task.FromResult(false);
        }

        var singleAssemblyPath = targetAssemblyPaths.Count == 1 ? targetAssemblyPaths[0] : context.Options.AssemblyFilePath;
        var loadedSuccessfully = targetAssemblyPaths.Count <= 1
            ? loader.TryLoadExportedTypes(singleAssemblyPath, out var exportedTypesMap)
            : loader.TryLoadExportedTypes(targetAssemblyPaths, out exportedTypesMap);

        if (!loadedSuccessfully || exportedTypesMap == null || exportedTypesMap.Count == 0)
        {
            if (loadedSuccessfully)
            {
                LogNoTypesFound(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepNoTypesFound, context.Options.AssemblyFilePath));
            }

            return Task.FromResult(false);
        }

        var filteredTypesMap = FilterExportedTypes(exportedTypesMap, context.Options);
        if (filteredTypesMap.Count == 0)
        {
            LogNoTypesFound(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepNoTypesFound, context.Options.AssemblyFilePath));
            return Task.FromResult(false);
        }

        context.ExportedTypesMap = filteredTypesMap;
        var distinctAssemblies = filteredTypesMap.Values
            .Select(runtimeType => runtimeType.Assembly)
            .Distinct()
            .ToList();
        context.AssemblyReferencesByAssemblyName = referenceAnalyzer.AnalyzeAssemblyReferences(
            distinctAssemblies,
            discoveryResult.ProjectFilePathsByAssemblyPath);
        context.AssemblyDependencyEdgesByAssemblyName = referenceAnalyzer.AnalyzeAssemblyDependencyTrees(
            distinctAssemblies,
            context.AssemblyReferencesByAssemblyName,
            discoveryResult.ProjectFilePathsByAssemblyPath);
        return Task.FromResult(true);
    }

    private Dictionary<string, Type> FilterExportedTypes(
        IReadOnlyDictionary<string, Type> typesMap,
        ExporterOptions options)
    {
        Dictionary<string, Type> filteredTypes = [with(StringComparer.Ordinal)];
        var excludedCount = 0;

        foreach (var (typeName, runtimeType) in typesMap)
        {
            if (IsTypeExcluded(runtimeType, options))
            {
                excludedCount++;
                continue;
            }

            filteredTypes[typeName] = runtimeType;
        }

        if (excludedCount > 0)
        {
            LogTypesFiltered(filteredTypes.Count, excludedCount);
        }

        return filteredTypes;
    }

    private bool IsTypeExcluded(Type runtimeType, ExporterOptions options)
    {
        if (options.ExcludeEditorBrowsableNever && attributeExtractor.IsEditorBrowsableNever(runtimeType))
        {
            return true;
        }

        if (options.ExcludedNamespaces.Count > 0)
        {
            var namespaceName = runtimeType.Namespace ?? string.Empty;
            if (SolutionAssemblyResolver.IsExcludedByPatterns(namespaceName, options.ExcludedNamespaces))
            {
                return true;
            }
        }

        if (options.ExcludedTypes.Count == 0)
        {
            return false;
        }

        var simpleName = runtimeType.Name;
        var cleanName = formatter.GetCleanTypeName(runtimeType);
        var fullName = runtimeType.FullName ?? runtimeType.Name;
        return SolutionAssemblyResolver.IsExcludedByPatterns(simpleName, options.ExcludedTypes) ||
               SolutionAssemblyResolver.IsExcludedByPatterns(cleanName, options.ExcludedTypes) ||
               SolutionAssemblyResolver.IsExcludedByPatterns(fullName, options.ExcludedTypes);
    }
}
