using Documentation.Exporter.Abstractions.Resolution;
using System.Globalization;
using System.Text.RegularExpressions;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;

namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Handles link resolution for types and members that belong to the local target assemblies/projects.
/// </summary>
public sealed partial class LocalProjectNamespaceHandler : INamespaceHandler
{
    private readonly HashSet<string> registeredTypeFullNames;
    private readonly HashSet<string> registeredEnumTypeFullNames;
    private readonly Dictionary<string, string> typeDirectoryUrlByFullName;
    private readonly Dictionary<string, string> typeDirectoryUrlByRuntimeKey;
    private readonly Dictionary<string, (string DisplayName, string RelativeUrl)> memberXmlKeyRoutes;
    private readonly string? rootNamespacePrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalProjectNamespaceHandler"/> class for a set of type names.
    /// </summary>
    /// <param name="targetAssemblyTypes">The set of fully qualified type names present in the target assembly.</param>
    /// <param name="rootNamespace">Optional root namespace (typically the target assembly name) used to calculate namespace subregion directory paths.</param>
    /// <param name="enumTypeFullNames">Optional set of fully qualified enum type names whose values are rendered inline in <c>index.md#values</c> rather than separate member files.</param>
    public LocalProjectNamespaceHandler(
        IEnumerable<string> targetAssemblyTypes,
        string? rootNamespace = null,
        IEnumerable<string>? enumTypeFullNames = null)
    {
        ArgumentNullException.ThrowIfNull(targetAssemblyTypes);

        registeredTypeFullNames = [with(StringComparer.Ordinal)];
        registeredEnumTypeFullNames = enumTypeFullNames != null
            ? [with(StringComparer.Ordinal), .. enumTypeFullNames.Select(CleanCrefAndGenerics)]
            : [with(StringComparer.Ordinal)];
        typeDirectoryUrlByFullName = [with(StringComparer.Ordinal)];
        typeDirectoryUrlByRuntimeKey = [with(StringComparer.Ordinal)];
        memberXmlKeyRoutes = [with(StringComparer.Ordinal)];
        rootNamespacePrefix = string.IsNullOrWhiteSpace(rootNamespace) ? null : rootNamespace.Trim();

        foreach (var rawTypeName in targetAssemblyTypes)
        {
            var cleanedTypeName = CleanCrefAndGenerics(rawTypeName);
            _ = registeredTypeFullNames.Add(cleanedTypeName);

            if (!string.IsNullOrWhiteSpace(rawTypeName) && rawTypeName.Contains('+', StringComparison.Ordinal))
            {
                TryRegisterNestedTypeChainUrl(rawTypeName, cleanedTypeName);
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="LocalProjectNamespaceHandler"/> instance from a map of reflected <see cref="Type"/> instances across one or more assemblies.
    /// </summary>
    /// <param name="exportedTypesMap">Map of full type names to reflected <see cref="Type"/> instances.</param>
    /// <param name="useAssemblySubdirectories">Whether each assembly's documentation is emitted inside its own <c>&lt;AssemblyName&gt;/</c> subdirectory.</param>
    /// <param name="memberXmlKeyRoutes">Optional prebuilt lookup mapping prefixless XML member keys to exact Markdown display labels and URLs.</param>
    /// <returns>A configured <see cref="LocalProjectNamespaceHandler"/> instance.</returns>
    public static LocalProjectNamespaceHandler FromExportedTypesMap(
        IReadOnlyDictionary<string, Type> exportedTypesMap,
        bool useAssemblySubdirectories = false,
        IReadOnlyDictionary<string, (string DisplayName, string RelativeUrl)>? memberXmlKeyRoutes = null) =>
        new(exportedTypesMap, useAssemblySubdirectories, memberXmlKeyRoutes);

    private LocalProjectNamespaceHandler(
        IReadOnlyDictionary<string, Type> exportedTypesMap,
        bool useAssemblySubdirectories,
        IReadOnlyDictionary<string, (string DisplayName, string RelativeUrl)>? memberXmlKeyRoutes)
    {
        ArgumentNullException.ThrowIfNull(exportedTypesMap);

        registeredTypeFullNames = [with(StringComparer.Ordinal)];
        registeredEnumTypeFullNames = [with(StringComparer.Ordinal)];
        typeDirectoryUrlByFullName = [with(StringComparer.Ordinal)];
        typeDirectoryUrlByRuntimeKey = [with(StringComparer.Ordinal)];
        this.memberXmlKeyRoutes = memberXmlKeyRoutes != null
            ? new Dictionary<string, (string DisplayName, string RelativeUrl)>(memberXmlKeyRoutes, StringComparer.Ordinal)
            : [with(StringComparer.Ordinal)];

        var distinctAssemblyNames = exportedTypesMap.Values
            .Select(runtimeType => runtimeType.Assembly.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        rootNamespacePrefix = distinctAssemblyNames.Count == 1 ? distinctAssemblyNames[0] : null;
        var collidingCleanNames = ComputeCollidingCleanTypeNames(exportedTypesMap.Values);

        foreach (var typeEntry in exportedTypesMap)
        {
            var cleanedFullName = CleanCrefAndGenerics(typeEntry.Key);
            _ = registeredTypeFullNames.Add(cleanedFullName);

            var runtimeLookupKey = NormalizeTypeCrefPreserveArity(typeEntry.Key);
            _ = registeredTypeFullNames.Add(runtimeLookupKey);

            if (typeEntry.Value.IsEnum)
            {
                _ = registeredEnumTypeFullNames.Add(cleanedFullName);
                _ = registeredEnumTypeFullNames.Add(runtimeLookupKey);
            }

            var assemblyName = typeEntry.Value.Assembly.GetName().Name ?? string.Empty;
            var hasArityCollision = collidingCleanNames.Contains(GetTypeCollisionGroupKey(typeEntry.Value));
            if ((useAssemblySubdirectories && !string.IsNullOrEmpty(assemblyName)) ||
                typeEntry.Value.IsNested ||
                hasArityCollision)
            {
                var computedDirectoryUrl = ComputeRuntimeTypeDirectoryUrlPath(typeEntry.Value, useAssemblySubdirectories, collidingCleanNames);
                typeDirectoryUrlByRuntimeKey[runtimeLookupKey] = computedDirectoryUrl;

                if (!hasArityCollision || !typeDirectoryUrlByFullName.ContainsKey(cleanedFullName))
                {
                    typeDirectoryUrlByFullName[cleanedFullName] = computedDirectoryUrl;
                }
            }
        }
    }

    /// <inheritdoc />
    public bool CanHandleNamespace(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return false;
        }

        var normalizedMemberKey = NormalizeMemberLookupKey(fullTypeName);
        if (memberXmlKeyRoutes.ContainsKey(normalizedMemberKey))
        {
            return true;
        }

        var arityPreservedKey = NormalizeTypeCrefPreserveArity(fullTypeName);
        if (registeredTypeFullNames.Contains(arityPreservedKey))
        {
            return true;
        }

        var sanitizedName = CleanCrefAndGenerics(fullTypeName);
        return registeredTypeFullNames.Contains(sanitizedName) ||
               TryGetRegisteredDeclaringType(arityPreservedKey, out _) ||
               TryGetRegisteredDeclaringType(sanitizedName, out _);
    }

    /// <inheritdoc />
    public string ResolveTypeLink(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return string.Empty;
        }

        var normalizedMemberKey = NormalizeMemberLookupKey(fullTypeName);
        if (memberXmlKeyRoutes.TryGetValue(normalizedMemberKey, out var exactMemberRoute))
        {
            return $"[{exactMemberRoute.DisplayName}]({exactMemberRoute.RelativeUrl})";
        }

        var arityPreservedKey = NormalizeTypeCrefPreserveArity(fullTypeName);
        var sanitizedName = CleanCrefAndGenerics(fullTypeName);
        var displayName = GetSimpleName(sanitizedName);

        if (typeDirectoryUrlByRuntimeKey.TryGetValue(arityPreservedKey, out var arityDirectoryUrl))
        {
            return $"[{displayName}]({arityDirectoryUrl}/index.md)";
        }

        if (registeredTypeFullNames.Contains(sanitizedName))
        {
            var relativeTypeUrl = BuildMarkdownUrlForFullTypeName(sanitizedName, targetFileName: "index.md");
            return $"[{displayName}]({relativeTypeUrl})";
        }

        if (TryGetRegisteredDeclaringType(arityPreservedKey, out var arityDeclaringType) &&
            typeDirectoryUrlByRuntimeKey.TryGetValue(arityDeclaringType, out var declaringArityUrl))
        {
            var targetFileName = registeredEnumTypeFullNames.Contains(arityDeclaringType)
                ? "index.md#values"
                : $"{displayName}.md";
            return $"[{displayName}]({declaringArityUrl}/{targetFileName})";
        }

        if (TryGetRegisteredDeclaringType(sanitizedName, out var declaringType))
        {
            var targetFileName = registeredEnumTypeFullNames.Contains(declaringType)
                ? "index.md#values"
                : $"{displayName}.md";
            var relativeMemberUrl = BuildMarkdownUrlForFullTypeName(declaringType, targetFileName);
            return $"[{displayName}]({relativeMemberUrl})";
        }

        var fallbackUrl = BuildMarkdownUrlForFullTypeName(sanitizedName, targetFileName: "index.md");
        return $"[{displayName}]({fallbackUrl})";
    }

    /// <summary>
    /// Identifies the set of clean type collision group keys where two or more exported types share the same assembly, namespace, declaring chain, and clean simple name across different generic arities.
    /// </summary>
    /// <param name="exportedTypes">The collection of exported runtime types.</param>
    /// <returns>Set of collision keys requiring generic arity directory suffixing.</returns>
    internal static HashSet<string> ComputeCollidingCleanTypeNames(IEnumerable<Type> exportedTypes)
    {
        ArgumentNullException.ThrowIfNull(exportedTypes);

        return
        [
            with(StringComparer.Ordinal),
            .. exportedTypes
                .Where(runtimeType => runtimeType != null)
                .GroupBy(GetTypeCollisionGroupKey, StringComparer.Ordinal)
                .Where(group => group.Select(candidate => candidate.FullName ?? candidate.Name).Distinct(StringComparer.Ordinal).Count() > 1)
                .Select(group => group.Key)
        ];
    }

    /// <summary>
    /// Computes the directory segment name for <paramref name="runtimeType"/>, appending <c>_T{n}</c> only when the type's clean name collides with another exported type of different generic arity in the same scope.
    /// </summary>
    /// <param name="runtimeType">The reflected target type.</param>
    /// <param name="collidingCleanNames">Optional set of collision group keys produced by <see cref="ComputeCollidingCleanTypeNames"/>.</param>
    /// <returns>The directory segment name (for example <c>"Result"</c> or <c>"Result_T1"</c>).</returns>
    internal static string ComputeTypeDirectorySegmentName(Type runtimeType, IReadOnlySet<string>? collidingCleanNames = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);

        var cleanName = CSharpSignatureFormatter.ExtractCleanTypeName(runtimeType);
        if (collidingCleanNames == null || !collidingCleanNames.Contains(GetTypeCollisionGroupKey(runtimeType)))
        {
            return cleanName;
        }

        var ownGenericCount = CSharpSignatureFormatter.ExtractOwnGenericArguments(runtimeType).Length;
        return ownGenericCount > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{cleanName}_T{ownGenericCount}")
            : cleanName;
    }

    /// <summary>
    /// Computes the ordered declaring-type directory segment chain from outermost to innermost declaring type, applying generic arity disambiguation when outer types collide.
    /// </summary>
    /// <param name="runtimeType">The reflected nested runtime type.</param>
    /// <param name="collidingCleanNames">Optional set of collision group keys produced by <see cref="ComputeCollidingCleanTypeNames"/>.</param>
    /// <returns>Ordered list of declaring directory segments from outermost to immediate parent.</returns>
    internal static IReadOnlyList<string> ComputeDeclaringTypeDirectoryChain(
        Type runtimeType,
        IReadOnlySet<string>? collidingCleanNames = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);

        if (!runtimeType.IsNested || runtimeType.DeclaringType == null)
        {
            return [];
        }

        List<string> declaringChain = [];
        var currentDeclaring = runtimeType.DeclaringType;
        while (currentDeclaring != null)
        {
            declaringChain.Add(ComputeTypeDirectorySegmentName(currentDeclaring, collidingCleanNames));
            currentDeclaring = currentDeclaring.DeclaringType;
        }

        declaringChain.Reverse();
        return declaringChain;
    }

    /// <summary>
    /// Computes the forward-slash directory path for a reflected runtime <see cref="Type"/>, preserving arbitrary nested declaring chains.
    /// </summary>
    /// <param name="runtimeType">The reflected target type.</param>
    /// <param name="useAssemblySubdirectories">Whether to prefix the path with the assembly name.</param>
    /// <param name="collidingCleanNames">Optional set of clean type collision keys requiring arity suffixing.</param>
    /// <returns>Relative forward-slash directory path for the type.</returns>
    internal static string ComputeRuntimeTypeDirectoryUrlPath(
        Type runtimeType,
        bool useAssemblySubdirectories,
        IReadOnlySet<string>? collidingCleanNames = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);

        var assemblyName = runtimeType.Assembly.GetName().Name ?? string.Empty;
        var subregionPath = GetNamespaceSubregionPath(runtimeType.Namespace, assemblyName);
        List<string> nestedChainSegments = [];
        var currentType = runtimeType;
        while (currentType != null)
        {
            nestedChainSegments.Add(ComputeTypeDirectorySegmentName(currentType, collidingCleanNames));
            currentType = currentType.DeclaringType;
        }

        nestedChainSegments.Reverse();
        var typeRelativePath = string.Join("/", nestedChainSegments);
        var pathWithinAssembly = string.IsNullOrEmpty(subregionPath)
            ? typeRelativePath
            : $"{subregionPath}/{typeRelativePath}";

        return useAssemblySubdirectories && !string.IsNullOrEmpty(assemblyName)
            ? $"{assemblyName}/{pathWithinAssembly}"
            : pathWithinAssembly;
    }

    /// <summary>
    /// Computes the forward-slash subregion directory path for a namespace relative to the root assembly namespace.
    /// </summary>
    /// <param name="namespaceName">The full namespace of the type.</param>
    /// <param name="assemblyName">The assembly name representing the root namespace.</param>
    /// <returns>A relative forward-slash directory path (for example <c>"Services"</c> or <c>"Parsing/Tags"</c>), or <see cref="string.Empty"/> if at root.</returns>
    public static string GetNamespaceSubregionPath(string? namespaceName, string? assemblyName) =>
        TypeDocumentationPaths.GetNamespaceSubregionPath(namespaceName, assemblyName);

    /// <summary>
    /// Computes the forward-slash directory path for a <see cref="TypeDocumentationModel"/> (including its dedicated type subdirectory and full nested chain).
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative forward-slash directory path (for example <c>"Services/OrderProcessingService"</c> or <c>"MyAssembly/Services/OrderProcessingService"</c>).</returns>
    public static string GetTypeDirectoryUrlPath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false) =>
        TypeDocumentationPaths.GetTypeDirectoryUrlPath(typeModel, includeAssemblyDirectory);

    /// <summary>
    /// Computes the relative URL path (using forward slashes) for a <see cref="TypeDocumentationModel"/> overview <c>index.md</c> file.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative Markdown URL path (for example <c>"Services/OrderProcessingService/index.md"</c>).</returns>
    public static string GetTypeRelativeUrlPath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false) =>
        TypeDocumentationPaths.GetTypeRelativeUrlPath(typeModel, includeAssemblyDirectory);

    /// <summary>
    /// Computes the OS-native relative directory path for a <see cref="TypeDocumentationModel"/>.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative OS directory path for the type.</returns>
    public static string GetTypeDirectoryFilePath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false) =>
        TypeDocumentationPaths.GetTypeDirectoryFilePath(typeModel, includeAssemblyDirectory);

    /// <summary>
    /// Computes the OS-native relative file path for a <see cref="TypeDocumentationModel"/> overview <c>index.md</c> file.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative OS file path to the type's <c>index.md</c>.</returns>
    public static string GetTypeRelativeFilePath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false) =>
        TypeDocumentationPaths.GetTypeRelativeFilePath(typeModel, includeAssemblyDirectory);

    private void TryRegisterNestedTypeChainUrl(string rawTypeName, string cleanedTypeName)
    {
        var unprefixed = StripCrefPrefix(rawTypeName.Trim());
        var nestedParts = unprefixed.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (nestedParts.Length < 2)
        {
            return;
        }

        var outermostFullName = GenericArityRegex().Replace(nestedParts[0], string.Empty);
        var lastDotIndex = outermostFullName.LastIndexOf('.');
        var namespacePart = lastDotIndex > 0 ? outermostFullName[..lastDotIndex] : string.Empty;
        var outermostSimpleName = lastDotIndex < 0 ? outermostFullName : outermostFullName[(lastDotIndex + 1)..];

        List<string> chainSegments = [outermostSimpleName];
        for (var partIndex = 1; partIndex < nestedParts.Length; partIndex++)
        {
            chainSegments.Add(GenericArityRegex().Replace(nestedParts[partIndex], string.Empty));
        }

        var typeRelativePath = string.Join("/", chainSegments);
        var subregionPath = GetNamespaceSubregionPath(namespacePart, rootNamespacePrefix);
        typeDirectoryUrlByFullName[cleanedTypeName] = string.IsNullOrEmpty(subregionPath)
            ? typeRelativePath
            : $"{subregionPath}/{typeRelativePath}";
    }

    private string BuildMarkdownUrlForFullTypeName(string fullTypeName, string targetFileName)
    {
        if (typeDirectoryUrlByFullName.TryGetValue(fullTypeName, out var registeredTypeDirectoryUrl))
        {
            return $"{registeredTypeDirectoryUrl}/{targetFileName}";
        }

        var simpleTypeName = GetSimpleName(fullTypeName);
        if (string.IsNullOrEmpty(rootNamespacePrefix))
        {
            return $"{simpleTypeName}.md";
        }

        var lastDotIndex = fullTypeName.LastIndexOf('.');
        if (lastDotIndex <= 0)
        {
            return $"{simpleTypeName}/{targetFileName}";
        }

        var namespacePart = fullTypeName[..lastDotIndex];
        if (!string.Equals(namespacePart, rootNamespacePrefix, StringComparison.Ordinal) &&
            !namespacePart.StartsWith(rootNamespacePrefix + ".", StringComparison.Ordinal))
        {
            return $"{simpleTypeName}.md";
        }

        var subregionPath = GetNamespaceSubregionPath(namespacePart, rootNamespacePrefix);
        return string.IsNullOrEmpty(subregionPath)
            ? $"{simpleTypeName}/{targetFileName}"
            : $"{subregionPath}/{simpleTypeName}/{targetFileName}";
    }

    private bool TryGetRegisteredDeclaringType(string candidateMemberName, out string declaringTypeFullName)
    {
        var lastDotIndex = candidateMemberName.LastIndexOf('.');
        if (lastDotIndex > 0)
        {
            var parentTypeCandidate = candidateMemberName[..lastDotIndex];
            if (registeredTypeFullNames.Contains(parentTypeCandidate))
            {
                declaringTypeFullName = parentTypeCandidate;
                return true;
            }
        }

        declaringTypeFullName = string.Empty;
        return false;
    }

    private static string GetTypeCollisionGroupKey(Type runtimeType)
    {
        var assemblyName = runtimeType.Assembly.GetName().Name ?? string.Empty;
        var namespaceName = runtimeType.Namespace ?? string.Empty;
        List<string> chain = [];
        var current = runtimeType.DeclaringType;
        while (current != null)
        {
            chain.Add(CSharpSignatureFormatter.ExtractCleanTypeName(current));
            current = current.DeclaringType;
        }

        chain.Reverse();
        var cleanName = CSharpSignatureFormatter.ExtractCleanTypeName(runtimeType);
        return $"{assemblyName}|{namespaceName}|{string.Join('.', chain)}|{cleanName}";
    }

    private static string NormalizeTypeCrefPreserveArity(string rawName)
    {
        var cleanedName = StripCrefPrefix(rawName.Trim());

        var parenIndex = cleanedName.IndexOf('(');
        if (parenIndex >= 0)
        {
            cleanedName = cleanedName[..parenIndex];
        }

        var doubleBracketIndex = cleanedName.IndexOf("[[", StringComparison.Ordinal);
        if (doubleBracketIndex >= 0)
        {
            cleanedName = cleanedName[..doubleBracketIndex];
        }

        return cleanedName.Replace('+', '.');
    }

    private static string NormalizeMemberLookupKey(string rawKey) =>
        StripCrefPrefix(rawKey.Trim()).Replace('+', '.');

    private static string StripCrefPrefix(string candidate)
    {
        var doubleColonIndex = candidate.IndexOf("::", StringComparison.Ordinal);
        var withoutAssembly = doubleColonIndex >= 0 ? candidate[(doubleColonIndex + 2)..] : candidate;
        return withoutAssembly.Length > 2 && withoutAssembly[1] == ':' ? withoutAssembly[2..] : withoutAssembly;
    }

    private static string CleanCrefAndGenerics(string rawName)
    {
        var cleanedName = StripCrefPrefix(rawName.Trim());

        var parenIndex = cleanedName.IndexOf('(');
        if (parenIndex >= 0)
        {
            cleanedName = cleanedName[..parenIndex];
        }

        var doubleBracketIndex = cleanedName.IndexOf("[[", StringComparison.Ordinal);
        if (doubleBracketIndex >= 0)
        {
            cleanedName = cleanedName[..doubleBracketIndex];
        }

        cleanedName = GenericArityRegex().Replace(cleanedName, string.Empty);

        return cleanedName.Replace('+', '.');
    }

    [GeneratedRegex(@"`\d+")]
    private static partial Regex GenericArityRegex();

    private static string GetSimpleName(string fullTypeName)
    {
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex < 0 ? fullTypeName : fullTypeName[(lastDotIndex + 1)..];
    }
}
