using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Workflow;
using System.Collections.Concurrent;
using System.Text.Json;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Generates and writes type overview (<c>index.md</c>), member detail Markdown files, per-assembly <c>index.md</c> files,
/// standalone architecture diagrams, root <c>index.md</c>, and pluggable static-site navigation manifests to the output directory.
/// </summary>
/// <param name="typePageMarkdownGenerator">The single-type overview Markdown generator.</param>
/// <param name="memberDetailMarkdownGenerator">The member detail Markdown generator.</param>
/// <param name="indexPageMarkdownGenerator">The index page Markdown generator.</param>
/// <param name="architectureDiagramGenerators">The registered architecture diagram generators.</param>
/// <param name="diagramGeneratorSelector">The preferred-with-fallback diagram generator selector.</param>
/// <param name="navigationGenerators">The registered static-site navigation generator strategies.</param>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class EmitMarkdownDocumentationStep(
    ITypePageMarkdownGenerator typePageMarkdownGenerator,
    IMemberDetailMarkdownGenerator memberDetailMarkdownGenerator,
    IIndexPageMarkdownGenerator indexPageMarkdownGenerator,
    IEnumerable<IArchitectureDiagramGenerator> architectureDiagramGenerators,
    IDiagramGeneratorSelector diagramGeneratorSelector,
    IEnumerable<INavigationGenerator> navigationGenerators,
    IFileSystem fileSystem,
    ILogger<EmitMarkdownDocumentationStep>? logger = null) : IExportWorkflowStep
{
    private static readonly JsonSerializerOptions ManifestSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ITypePageMarkdownGenerator typeGenerator = ArgumentGuard.EnsureNotNull(typePageMarkdownGenerator);
    private readonly IMemberDetailMarkdownGenerator memberGenerator = ArgumentGuard.EnsureNotNull(memberDetailMarkdownGenerator);
    private readonly IIndexPageMarkdownGenerator indexGenerator = ArgumentGuard.EnsureNotNull(indexPageMarkdownGenerator);
    private readonly IReadOnlyList<IArchitectureDiagramGenerator> diagramGenerators = [.. ArgumentGuard.EnsureNotNull(architectureDiagramGenerators)];
    private readonly IDiagramGeneratorSelector diagramSelector = ArgumentGuard.EnsureNotNull(diagramGeneratorSelector);
    private readonly IReadOnlyList<INavigationGenerator> registeredNavigationGenerators = [.. ArgumentGuard.EnsureNotNull(navigationGenerators)];
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<EmitMarkdownDocumentationStep> logger = logger ?? NullLogger<EmitMarkdownDocumentationStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameEmitMarkdownDocumentation;

    [LoggerMessage(EventId = 817, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogFilesGenerated(string statusMessage);

    [LoggerMessage(EventId = 819, Level = LogLevel.Warning, Message = "{StatusMessage}")]
    private partial void LogNavigationGeneratorWarning(string statusMessage);

    /// <inheritdoc />
    public async Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var outputDirectoryPath = context.Options.OutputDirectoryPath;
        var compiledModels = context.CompiledTypeModels
            .OrderBy(typeModel => typeModel.AssemblyName, StringComparer.Ordinal)
            .ThenBy(typeModel => typeModel.NamespaceName, StringComparer.Ordinal)
            .ThenBy(typeModel => typeModel.TypeName, StringComparer.Ordinal)
            .ThenBy(typeModel => typeModel.GenericTypeParameters.Count)
            .ToList();
        var generatedMemberFilesCount = 0;
        ConcurrentBag<string> emittedFilePaths = [];

        var distinctAssemblies = compiledModels
            .Select(typeModel => typeModel.AssemblyName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var useAssemblySubdirectories = context.ShouldUseAssemblySubdirectories(distinctAssemblies.Count);

        await Parallel.ForEachAsync(compiledModels, cancellationToken, async (typeModel, token) =>
        {
            var typeDirectoryPath = Path.Combine(
                outputDirectoryPath,
                TypeDocumentationPaths.GetTypeDirectoryFilePath(typeModel, useAssemblySubdirectories));
            this.fileSystem.CreateDirectory(typeDirectoryPath);

            var typeOverviewMarkdown = typeGenerator.GenerateTypeMarkdown(typeModel, useAssemblySubdirectories);
            var typeOverviewFilePath = Path.Combine(typeDirectoryPath, "index.md");
            await this.fileSystem.WriteAllTextAsync(typeOverviewFilePath, typeOverviewMarkdown, token).ConfigureAwait(false);
            emittedFilePaths.Add(typeOverviewFilePath);

            var memberMarkdownFiles = memberGenerator.GenerateMemberMarkdownFiles(typeModel, useAssemblySubdirectories);
            if (memberMarkdownFiles == null)
            {
                return;
            }

            foreach (var memberFilePair in memberMarkdownFiles)
            {
                var memberFilePath = Path.Combine(typeDirectoryPath, memberFilePair.Key);
                await this.fileSystem.WriteAllTextAsync(memberFilePath, memberFilePair.Value, token).ConfigureAwait(false);
                emittedFilePaths.Add(memberFilePath);
                _ = Interlocked.Increment(ref generatedMemberFilesCount);
            }
        }).ConfigureAwait(false);

        var generatedAssemblyIndexCount = 0;
        var diagramFormat = context.Options.DiagramFormat;

        var assemblyGroups = compiledModels
            .Where(typeModel => !string.IsNullOrWhiteSpace(typeModel.AssemblyName))
            .GroupBy(typeModel => typeModel.AssemblyName, StringComparer.Ordinal)
            .ToList();

        HashSet<string> emittedAssemblyNames =
        [
            with(StringComparer.OrdinalIgnoreCase),
            .. assemblyGroups.Select(assemblyGroup => assemblyGroup.Key)
        ];

        if (useAssemblySubdirectories)
        {
            foreach (var assemblyGroup in assemblyGroups)
            {
                var assemblyDirectoryPath = Path.Combine(outputDirectoryPath, assemblyGroup.Key);
                this.fileSystem.CreateDirectory(assemblyDirectoryPath);

                IReadOnlyList<TypeDocumentationModel> assemblyTypes = [.. assemblyGroup];
                _ = context.AssemblyReferencesByAssemblyName.TryGetValue(assemblyGroup.Key, out var assemblyReferences);
                _ = context.AssemblyDependencyEdgesByAssemblyName.TryGetValue(assemblyGroup.Key, out var assemblyDependencyEdges);
                _ = context.AssemblyAttributesByAssemblyName.TryGetValue(assemblyGroup.Key, out var assemblyAttributes);
                var assemblyIndexMarkdown = indexGenerator.GenerateIndexMarkdown(
                    assemblyTypes,
                    includeAssemblyDirectory: false,
                    diagramFormat,
                    isAssemblySubdirectoryIndex: true,
                    assemblyReferences: assemblyReferences,
                    analyzedAssemblyNames: emittedAssemblyNames,
                    assemblyDependencyEdges: assemblyDependencyEdges,
                    assemblyAttributes: assemblyAttributes);
                var assemblyIndexPath = Path.Combine(assemblyDirectoryPath, "index.md");
                await this.fileSystem.WriteAllTextAsync(assemblyIndexPath, assemblyIndexMarkdown, cancellationToken).ConfigureAwait(false);
                emittedFilePaths.Add(assemblyIndexPath);
                generatedAssemblyIndexCount++;

                await EmitStandaloneArchitectureDiagramsAsync(
                    assemblyDirectoryPath,
                    assemblyTypes,
                    includeAssemblyDirectory: false,
                    diagramFormat,
                    emittedFilePaths,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        IReadOnlyList<AssemblyReferenceModel>? rootAssemblyReferences = null;
        IReadOnlyList<AssemblyDependencyEdgeModel>? rootAssemblyDependencyEdges = null;
        IReadOnlyList<string>? singleAssemblyAttributes = null;
        if (!useAssemblySubdirectories && distinctAssemblies.Count == 1)
        {
            if (context.AssemblyReferencesByAssemblyName.TryGetValue(distinctAssemblies[0], out var singleAssemblyReferences))
            {
                rootAssemblyReferences = singleAssemblyReferences;
            }

            if (context.AssemblyDependencyEdgesByAssemblyName.TryGetValue(distinctAssemblies[0], out var singleAssemblyDependencyEdges))
            {
                rootAssemblyDependencyEdges = singleAssemblyDependencyEdges;
            }

            _ = context.AssemblyAttributesByAssemblyName.TryGetValue(distinctAssemblies[0], out singleAssemblyAttributes);
        }

        var indexMarkdownContent = indexGenerator.GenerateIndexMarkdown(
            compiledModels,
            useAssemblySubdirectories,
            diagramFormat,
            isAssemblySubdirectoryIndex: false,
            assemblyReferences: rootAssemblyReferences,
            analyzedAssemblyNames: null,
            assemblyDependencyEdges: rootAssemblyDependencyEdges,
            assemblyAttributes: singleAssemblyAttributes);
        var rootIndexPath = Path.Combine(outputDirectoryPath, "index.md");
        await this.fileSystem.WriteAllTextAsync(rootIndexPath, indexMarkdownContent, cancellationToken).ConfigureAwait(false);
        emittedFilePaths.Add(rootIndexPath);

        await EmitStandaloneArchitectureDiagramsAsync(
            outputDirectoryPath,
            compiledModels,
            useAssemblySubdirectories,
            diagramFormat,
            emittedFilePaths,
            cancellationToken).ConfigureAwait(false);

        if (!context.Options.NavigationFormat.IsNone)
        {
            var selectedNavigationGenerator = registeredNavigationGenerators.LastOrDefault(generator =>
                generator.Format == context.Options.NavigationFormat);

            if (selectedNavigationGenerator == null)
            {
                if (this.logger.IsEnabled(LogLevel.Warning))
                {
                    var missingNavigationMessage = ExecutionMessagesResources.FormatMessage(
                        ExecutionMessagesResources.NavigationGeneratorNotFoundWarning,
                        context.Options.NavigationFormat.Value);
                    LogNavigationGeneratorWarning(missingNavigationMessage);
                }
            }
            else
            {
                var navigationFilePath = !string.IsNullOrWhiteSpace(context.Options.NavigationFilePath)
                    ? this.fileSystem.GetFullPath(context.Options.NavigationFilePath)
                    : Path.Combine(outputDirectoryPath, selectedNavigationGenerator.DefaultFileName);

                var existingNavigationContent = this.fileSystem.FileExists(navigationFilePath)
                    ? await this.fileSystem.ReadAllTextAsync(navigationFilePath, cancellationToken).ConfigureAwait(false)
                    : null;

                var navigationContent = selectedNavigationGenerator.GenerateNavigation(
                    compiledModels,
                    useAssemblySubdirectories,
                    existingNavigationContent,
                    context.Options.NavigationEntryPoint,
                    context.Options.NavigationUrlPrefix);

                if (!string.IsNullOrWhiteSpace(navigationContent))
                {
                    var navigationDirectory = Path.GetDirectoryName(navigationFilePath);
                    if (!string.IsNullOrWhiteSpace(navigationDirectory))
                    {
                        this.fileSystem.CreateDirectory(navigationDirectory);
                    }

                    await this.fileSystem.WriteAllTextAsync(navigationFilePath, navigationContent, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        var orderedManifestEntries = emittedFilePaths
            .Select(emittedPath => Path.GetRelativePath(outputDirectoryPath, emittedPath).Replace('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(relativePath => relativePath, StringComparer.Ordinal)
            .ToList();
        var manifestFilePath = Path.Combine(outputDirectoryPath, EnsureOutputDirectoryStep.GeneratedFilesManifestFileName);
        var manifestJson = JsonSerializer.Serialize(orderedManifestEntries, ManifestSerializerOptions) + Environment.NewLine;
        await this.fileSystem.WriteAllTextAsync(
            manifestFilePath,
            manifestJson,
            cancellationToken).ConfigureAwait(false);

        var totalFiles = compiledModels.Count + generatedMemberFilesCount + generatedAssemblyIndexCount + 1;
        context.GeneratedFilesCount = totalFiles;
        var filesGeneratedMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepFilesGenerated, totalFiles);
        LogFilesGenerated(filesGeneratedMessage);

        return true;
    }

    private async Task EmitStandaloneArchitectureDiagramsAsync(
        string directoryPath,
        IReadOnlyList<TypeDocumentationModel> types,
        bool includeAssemblyDirectory,
        DiagramFormat diagramFormat,
        ConcurrentBag<string> emittedFilePaths,
        CancellationToken cancellationToken)
    {
        if (diagramFormat == DiagramFormat.None)
        {
            return;
        }

        var selectedGenerator = diagramSelector.SelectGenerator(
            diagramGenerators,
            diagramFormat,
            generator => generator.CanGenerate(types, includeAssemblyDirectory));
        if (selectedGenerator == null || string.IsNullOrWhiteSpace(selectedGenerator.StandaloneFileName))
        {
            return;
        }

        var standaloneContent = selectedGenerator.GenerateStandaloneContent(types, includeAssemblyDirectory);
        if (string.IsNullOrWhiteSpace(standaloneContent))
        {
            return;
        }

        var outputPath = Path.Combine(directoryPath, selectedGenerator.StandaloneFileName);
        await this.fileSystem.WriteAllTextAsync(outputPath, standaloneContent, cancellationToken).ConfigureAwait(false);
        emittedFilePaths.Add(outputPath);
    }
}
