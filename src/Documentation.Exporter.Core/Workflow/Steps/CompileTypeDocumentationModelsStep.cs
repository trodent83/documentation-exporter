using Documentation.Exporter.Abstractions.Workflow;
using System.Collections.Concurrent;
using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Compiles metadata, runs the enrichment pipeline across all exported types concurrently, and links cross-type derived/implementing types and extension methods.
/// </summary>
/// <param name="assemblyTypeReflector">The assembly type reflector.</param>
/// <param name="typeDocumentationPipeline">The type documentation enrichment pipeline.</param>
/// <param name="memberFileNameFormatter">The member detail filename formatter.</param>
/// <param name="customAttributeExtractor">The custom attribute extractor used to extract assembly-level attributes.</param>
public sealed class CompileTypeDocumentationModelsStep(
    IAssemblyTypeReflector assemblyTypeReflector,
    ITypeDocumentationPipeline typeDocumentationPipeline,
    IMemberFileNameFormatter memberFileNameFormatter,
    ICustomAttributeExtractor customAttributeExtractor) : IExportWorkflowStep
{
    private readonly IAssemblyTypeReflector reflector = ArgumentGuard.EnsureNotNull(assemblyTypeReflector);
    private readonly ITypeDocumentationPipeline pipeline = ArgumentGuard.EnsureNotNull(typeDocumentationPipeline);
    private readonly IMemberFileNameFormatter fileNameFormatter = ArgumentGuard.EnsureNotNull(memberFileNameFormatter);
    private readonly ICustomAttributeExtractor attributeExtractor = ArgumentGuard.EnsureNotNull(customAttributeExtractor);

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameCompileTypeDocumentationModels;

    /// <inheritdoc />
    public async Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExportedTypesMap == null || context.ExportedTypesMap.Count == 0)
        {
            return false;
        }

        var collidingCleanNames = LocalProjectNamespaceHandler.ComputeCollidingCleanTypeNames(context.ExportedTypesMap.Values);
        ConcurrentBag<(Type RuntimeType, TypeDocumentationModel EnrichedModel)> compiledEntries = [];

        await Parallel.ForEachAsync(context.ExportedTypesMap.Values, cancellationToken, async (targetType, token) =>
        {
            var reflectedTypeModel = reflector.ReflectTypeMetadata(targetType);
            var directorySegmentName = LocalProjectNamespaceHandler.ComputeTypeDirectorySegmentName(targetType, collidingCleanNames);
            var disambiguatedDeclaringChain = LocalProjectNamespaceHandler.ComputeDeclaringTypeDirectoryChain(targetType, collidingCleanNames);
            if (!string.Equals(directorySegmentName, reflectedTypeModel.TypeName, StringComparison.Ordinal) ||
                !disambiguatedDeclaringChain.SequenceEqual(reflectedTypeModel.DeclaringTypeChain, StringComparer.Ordinal))
            {
                reflectedTypeModel = reflectedTypeModel with
                {
                    DirectorySegmentName = directorySegmentName,
                    DeclaringTypeChain = disambiguatedDeclaringChain,
                    DeclaringTypeName = disambiguatedDeclaringChain.Count > 0
                        ? disambiguatedDeclaringChain[^1]
                        : reflectedTypeModel.DeclaringTypeName
                };
            }

            var enrichedTypeModel = await pipeline.EnrichTypeModelAsync(targetType, reflectedTypeModel, context.Options, token).ConfigureAwait(false);
            if (enrichedTypeModel.IsExcludedByXmlDoc)
            {
                return;
            }

            var filteredTypeModel = FilterExcludedMembers(enrichedTypeModel, context.Options.ExcludeEditorBrowsableNever);
            compiledEntries.Add((targetType, filteredTypeModel));
        }).ConfigureAwait(false);

        var entryList = compiledEntries
            .OrderBy(entry => entry.EnrichedModel.AssemblyName, StringComparer.Ordinal)
            .ThenBy(entry => entry.EnrichedModel.NamespaceName, StringComparer.Ordinal)
            .ThenBy(entry => entry.RuntimeType.FullName ?? entry.EnrichedModel.TypeName, StringComparer.Ordinal)
            .ToList();
        var distinctAssemblyCount = entryList
            .Select(entry => entry.EnrichedModel.AssemblyName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Count();
        var useAssemblySubdirectories = context.ShouldUseAssemblySubdirectories(distinctAssemblyCount);

        var discoveredExtensions = CollectExtensionMethods(entryList, useAssemblySubdirectories);
        var discoveredExtensionProperties = CollectExtensionProperties(entryList, useAssemblySubdirectories);

        Dictionary<string, IReadOnlyList<string>> assemblyAttributesMap = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (var (runtimeType, enrichedModel) in entryList)
        {
            if (!string.IsNullOrWhiteSpace(enrichedModel.AssemblyName) &&
                !assemblyAttributesMap.ContainsKey(enrichedModel.AssemblyName))
            {
                assemblyAttributesMap[enrichedModel.AssemblyName] = attributeExtractor.BuildAppliedAttributes(runtimeType.Assembly);
            }

            var nestedTypeLinks = FindNestedTypeLinks(runtimeType, entryList, useAssemblySubdirectories);
            var derivedTypeLinks = FindDerivedOrImplementingTypeLinks(runtimeType, entryList, useAssemblySubdirectories);
            var matchingExtensionMethods = FindApplicableExtensionMembers(runtimeType, discoveredExtensions);
            var matchingExtensionProperties = FindApplicableExtensionMembers(runtimeType, discoveredExtensionProperties);

            var finalModel = enrichedModel with
            {
                NestedTypes = nestedTypeLinks,
                DerivedTypes = derivedTypeLinks,
                ExtensionMethods = matchingExtensionMethods,
                ExtensionProperties = matchingExtensionProperties,
                DiagramFormat = context.Options.DiagramFormat
            };
            context.CompiledTypeModels.Add(finalModel);
        }

        context.AssemblyAttributesByAssemblyName = assemblyAttributesMap;

        return true;
    }

    private static TypeDocumentationModel FilterExcludedMembers(
        TypeDocumentationModel typeModel,
        bool excludeEditorBrowsableNever)
    {
        bool ShouldKeep(MemberDocumentationModel member) =>
            !member.IsExcludedByXmlDoc && (!excludeEditorBrowsableNever || !member.IsEditorBrowsableNever);

        return typeModel with
        {
            Constructors = [.. typeModel.Constructors.Where(ShouldKeep)],
            Methods = [.. typeModel.Methods.Where(ShouldKeep)],
            Properties = [.. typeModel.Properties.Where(ShouldKeep)],
            Fields = [.. typeModel.Fields.Where(ShouldKeep)],
            Events = [.. typeModel.Events.Where(ShouldKeep)]
        };
    }

    private static List<string> FindNestedTypeLinks(
        Type declaringType,
        List<(Type RuntimeType, TypeDocumentationModel EnrichedModel)> allEntries,
        bool useAssemblySubdirectories)
    {
        List<(string TypeName, string FormattedLink)> nestedTypes = [];
        foreach (var (candidateRuntimeType, candidateModel) in allEntries)
        {
            if (!candidateRuntimeType.IsNested || candidateRuntimeType.DeclaringType == null)
            {
                continue;
            }

            var isDirectNestedType = candidateRuntimeType.DeclaringType == declaringType ||
                string.Equals(candidateRuntimeType.DeclaringType.FullName, declaringType.FullName, StringComparison.Ordinal);

            if (!isDirectNestedType)
            {
                continue;
            }

            var relativeUrl = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(candidateModel, useAssemblySubdirectories);
            nestedTypes.Add((candidateModel.DisplayTypeName, $"[{candidateModel.DisplayTypeName}]({relativeUrl})"));
        }

        return
        [
            .. nestedTypes
                .OrderBy(item => item.TypeName, StringComparer.Ordinal)
                .Select(item => item.FormattedLink)
        ];
    }

    private static List<string> FindDerivedOrImplementingTypeLinks(
        Type baseOrInterfaceType,
        List<(Type RuntimeType, TypeDocumentationModel EnrichedModel)> allEntries,
        bool useAssemblySubdirectories)
    {
        if (baseOrInterfaceType.IsEnum || baseOrInterfaceType.IsValueType)
        {
            return [];
        }

        List<(string TypeName, string FormattedLink)> matchingTypes = [];
        foreach (var (candidateRuntimeType, candidateModel) in allEntries)
        {
            if (candidateRuntimeType == baseOrInterfaceType)
            {
                continue;
            }

            var isDerivedOrImplementing = baseOrInterfaceType.IsInterface
                ? ImplementsInterface(candidateRuntimeType, baseOrInterfaceType)
                : InheritsFromClass(candidateRuntimeType, baseOrInterfaceType);

            if (!isDerivedOrImplementing)
            {
                continue;
            }

            var relativeUrl = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(candidateModel, useAssemblySubdirectories);
            matchingTypes.Add((candidateModel.DisplayTypeName, $"[{candidateModel.DisplayTypeName}]({relativeUrl})"));
        }

        return
        [
            .. matchingTypes
                .OrderBy(item => item.TypeName, StringComparer.Ordinal)
                .Select(item => item.FormattedLink)
        ];
    }

    private static bool ImplementsInterface(Type candidateType, Type interfaceType)
    {
        var targetDefinition = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
        return candidateType
            .GetInterfaces()
            .Any(implemented =>
            {
                var implementedDefinition = implemented.IsGenericType ? implemented.GetGenericTypeDefinition() : implemented;
                return implementedDefinition == targetDefinition ||
                       (implementedDefinition.FullName != null && string.Equals(implementedDefinition.FullName, targetDefinition.FullName, StringComparison.Ordinal));
            });
    }

    private static bool InheritsFromClass(Type candidateType, Type baseClassType)
    {
        var targetDefinition = baseClassType.IsGenericType ? baseClassType.GetGenericTypeDefinition() : baseClassType;
        var currentBase = candidateType.BaseType;
        while (currentBase != null && currentBase != typeof(object))
        {
            var currentDefinition = currentBase.IsGenericType ? currentBase.GetGenericTypeDefinition() : currentBase;
            if (currentDefinition == targetDefinition ||
                (currentDefinition.FullName != null && string.Equals(currentDefinition.FullName, targetDefinition.FullName, StringComparison.Ordinal)))
            {
                return true;
            }

            currentBase = currentBase.BaseType;
        }

        return false;
    }

    private List<MemberDocumentationModel> CollectExtensionMethods(
        List<(Type RuntimeType, TypeDocumentationModel EnrichedModel)> allEntries,
        bool useAssemblySubdirectories) =>
        CollectTargetedExtensionMembers(
            allEntries,
            useAssemblySubdirectories,
            declaringModel => declaringModel.Methods);

    private List<MemberDocumentationModel> CollectExtensionProperties(
        List<(Type RuntimeType, TypeDocumentationModel EnrichedModel)> allEntries,
        bool useAssemblySubdirectories) =>
        CollectTargetedExtensionMembers(
            allEntries,
            useAssemblySubdirectories,
            declaringModel => declaringModel.Properties);

    private List<MemberDocumentationModel> CollectTargetedExtensionMembers(
        List<(Type RuntimeType, TypeDocumentationModel EnrichedModel)> allEntries,
        bool useAssemblySubdirectories,
        Func<TypeDocumentationModel, IReadOnlyList<MemberDocumentationModel>> memberSelector)
    {
        List<MemberDocumentationModel> extensionMembers = [];
        foreach (var (_, declaringModel) in allEntries)
        {
            var members = memberSelector(declaringModel);
            var declaringDirUrl = LocalProjectNamespaceHandler.GetTypeDirectoryUrlPath(declaringModel, useAssemblySubdirectories);
            foreach (var memberModel in members)
            {
                if (string.IsNullOrWhiteSpace(memberModel.ExtensionTargetTypeFullName))
                {
                    continue;
                }

                var isOverloaded = members.Count(candidate => candidate.MemberName == memberModel.MemberName) > 1;
                var detailFileName = fileNameFormatter.GetMemberDetailFileName(memberModel, isOverloaded, isConstructor: false);
                var detailUrl = string.IsNullOrEmpty(declaringDirUrl)
                    ? detailFileName
                    : $"{declaringDirUrl}/{detailFileName}";

                extensionMembers.Add(memberModel with { ResolvedMemberDetailUrl = detailUrl });
            }
        }

        return extensionMembers;
    }

    private static List<MemberDocumentationModel> FindApplicableExtensionMembers(
        Type targetType,
        List<MemberDocumentationModel> allExtensionMembers)
    {
        if (allExtensionMembers.Count == 0 || (targetType.IsAbstract && targetType.IsSealed))
        {
            return [];
        }

        HashSet<string> compatibleTypeFullNames = [with(StringComparer.Ordinal), GetCleanFullName(targetType)];

        foreach (var implementedInterface in targetType.GetInterfaces())
        {
            _ = compatibleTypeFullNames.Add(GetCleanFullName(implementedInterface));
        }

        var currentBase = targetType.BaseType;
        while (currentBase != null && currentBase != typeof(object))
        {
            _ = compatibleTypeFullNames.Add(GetCleanFullName(currentBase));
            currentBase = currentBase.BaseType;
        }

        return
        [
            .. allExtensionMembers
                .Where(extensionMember => compatibleTypeFullNames.Contains(extensionMember.ExtensionTargetTypeFullName))
                .OrderBy(extensionMember => extensionMember.MemberName, StringComparer.Ordinal)
        ];
    }

    private static string GetCleanFullName(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var fullName = definition.FullName ?? definition.Name;
        var backtickIndex = fullName.IndexOf('`');
        var rawClean = backtickIndex >= 0 ? fullName[..backtickIndex] : fullName;
        return rawClean.Replace('+', '.');
    }
}
