using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Default implementation of <see cref="IAssemblyReferenceAnalyzer"/> combining compiled
/// <see cref="AssemblyName"/> metadata, sibling <c>.deps.json</c> manifests, and source <c>.csproj</c> project definitions.
/// </summary>
/// <param name="fileSystem">The file system abstraction used to inspect <c>.deps.json</c>, <c>.csproj</c>, and <c>Directory.Packages.props</c>.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class AssemblyReferenceAnalyzer(
    IFileSystem fileSystem,
    ILogger<AssemblyReferenceAnalyzer>? logger = null) : IAssemblyReferenceAnalyzer
{
    private static readonly HashSet<string> FrameworkAssemblyExactNames =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "mscorlib",
        "netstandard",
        "System",
        "WindowsBase",
        "PresentationCore",
        "PresentationFramework"
    ];

    private static readonly string[] FrameworkAssemblyPrefixes =
    [
        "System.",
        "Microsoft.CSharp",
        "Microsoft.VisualBasic",
        "Microsoft.Win32"
    ];

    private static readonly HashSet<string> MicrosoftFrameworkPublicKeyTokens =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "b03f5f7f11d50a3a",
        "7cec85d7bea7798e",
        "b77a5c561934e089",
        "cc7b13ffcd2ddd51",
        "31bf3856ad364e35"
    ];

    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<AssemblyReferenceAnalyzer> logger = logger ?? NullLogger<AssemblyReferenceAnalyzer>.Instance;

    [LoggerMessage(EventId = 603, Level = LogLevel.Warning, Message = "Failed to parse metadata file '{FilePath}' while analyzing assembly references: {ErrorMessage}")]
    private partial void LogMetadataParseFailed(string filePath, string errorMessage);

    private sealed record PackageConstraintInfo(
        AssemblyVersionLinkingKind LinkingKind,
        string ConstraintExpression,
        string RawVersion);

    private sealed record DirectAssemblyConstraintInfo(
        AssemblyVersionLinkingKind LinkingKind,
        string ConstraintExpression);

    private sealed record ProjectMetadataSnapshot(
        string TargetFrameworkMoniker,
        HashSet<string> ProjectReferences,
        Dictionary<string, PackageConstraintInfo> PackageReferences,
        Dictionary<string, DirectAssemblyConstraintInfo> DirectAssemblyReferences);

    private sealed record DepsLibraryInfo(
        string PackageId,
        string LibraryType,
        string Version);

    private sealed record DepsDependencyInfo(
        string TargetName,
        string RawVersion);

    private sealed record StrongNameInfo(
        bool IsStrongNamed,
        string PublicKeyToken);

    private sealed record DepsMetadataSnapshot(
        Dictionary<string, DepsLibraryInfo> RuntimeLibrariesByAssemblyName,
        Dictionary<string, DepsLibraryInfo> LibrariesByName,
        Dictionary<string, IReadOnlyList<DepsDependencyInfo>> DependenciesByLibraryName,
        Dictionary<string, IReadOnlyList<string>> RuntimeAssetPathsByLibraryName);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> AnalyzeAssemblyReferences(
        IEnumerable<Assembly> loadedAssemblies,
        IReadOnlyDictionary<string, string>? projectFilePathsByAssemblyPath = null)
    {
        ArgumentNullException.ThrowIfNull(loadedAssemblies);

        var normalizedProjectLookup = NormalizeProjectPathLookup(projectFilePathsByAssemblyPath);
        Dictionary<string, IReadOnlyList<AssemblyReferenceModel>> resultByAssembly = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var assembly in loadedAssemblies.Distinct())
        {
            var assemblySimpleName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(assemblySimpleName) || resultByAssembly.ContainsKey(assemblySimpleName))
            {
                continue;
            }

            resultByAssembly[assemblySimpleName] = AnalyzeSingleAssembly(assembly, normalizedProjectLookup);
        }

        return resultByAssembly;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<AssemblyDependencyEdgeModel>> AnalyzeAssemblyDependencyTrees(
        IEnumerable<Assembly> loadedAssemblies,
        IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> directReferencesByAssemblyName,
        IReadOnlyDictionary<string, string>? projectFilePathsByAssemblyPath = null)
    {
        ArgumentNullException.ThrowIfNull(loadedAssemblies);
        ArgumentNullException.ThrowIfNull(directReferencesByAssemblyName);

        var distinctAssemblies = loadedAssemblies.Distinct().ToList();
        var normalizedProjectLookup = NormalizeProjectPathLookup(projectFilePathsByAssemblyPath);
        var knownStrongNames = BuildKnownStrongNamesLookup(distinctAssemblies, directReferencesByAssemblyName);

        Dictionary<string, DepsMetadataSnapshot?> depsSnapshotsByAssemblyName = [with(StringComparer.OrdinalIgnoreCase)];
        Dictionary<string, string> outputDirectoryByAssemblyName = [with(StringComparer.OrdinalIgnoreCase)];
        Dictionary<string, ProjectMetadataSnapshot?> projectSnapshotsByAssemblyName = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var assembly in distinctAssemblies)
        {
            var simpleName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(simpleName) || depsSnapshotsByAssemblyName.ContainsKey(simpleName))
            {
                continue;
            }

            var assemblyLocation = assembly.Location;
            depsSnapshotsByAssemblyName[simpleName] = LoadDepsJsonSnapshot(assemblyLocation);
            projectSnapshotsByAssemblyName[simpleName] = TryLoadProjectMetadata(assemblyLocation, normalizedProjectLookup);
            var assemblyDirectory = !string.IsNullOrWhiteSpace(assemblyLocation)
                ? Path.GetDirectoryName(assemblyLocation) ?? string.Empty
                : string.Empty;
            outputDirectoryByAssemblyName[simpleName] = assemblyDirectory;
        }

        Dictionary<string, IReadOnlyList<AssemblyDependencyEdgeModel>> dependencyTreesByAssembly = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (var assembly in distinctAssemblies)
        {
            var rootAssemblyName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(rootAssemblyName) || dependencyTreesByAssembly.ContainsKey(rootAssemblyName))
            {
                continue;
            }

            dependencyTreesByAssembly[rootAssemblyName] = BuildDependencyTreeForRoot(
                rootAssemblyName,
                directReferencesByAssemblyName,
                depsSnapshotsByAssemblyName,
                projectSnapshotsByAssemblyName,
                outputDirectoryByAssemblyName,
                knownStrongNames);
        }

        return dependencyTreesByAssembly;
    }

    private static (AssemblyVersionLinkingKind LinkingKind, string ConstraintExpression, string CleanPackageVersion) ParseNuGetVersionConstraint(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return (AssemblyVersionLinkingKind.Minimum, string.Empty, string.Empty);
        }

        var trimmedVersion = rawVersion.Trim();
        if (trimmedVersion.Contains(',', StringComparison.Ordinal))
        {
            return (AssemblyVersionLinkingKind.Range, trimmedVersion, ExtractFirstVersionToken(trimmedVersion));
        }

        if (trimmedVersion.Contains('*', StringComparison.Ordinal))
        {
            return (AssemblyVersionLinkingKind.Floating, trimmedVersion, trimmedVersion);
        }

        if (trimmedVersion.StartsWith('[') && trimmedVersion.EndsWith(']') && trimmedVersion.Length > 2)
        {
            var innerVersion = trimmedVersion[1..^1].Trim();
            return (AssemblyVersionLinkingKind.Fixed, trimmedVersion, innerVersion);
        }

        return (AssemblyVersionLinkingKind.Minimum, $">= {trimmedVersion}", trimmedVersion);
    }

    private static string FormatTargetFrameworkDisplayName(string? frameworkName)
    {
        if (string.IsNullOrWhiteSpace(frameworkName))
        {
            return string.Empty;
        }

        var trimmedName = frameworkName.Trim();
        var versionMarkerIndex = trimmedName.IndexOf(",Version=v", StringComparison.OrdinalIgnoreCase);
        if (versionMarkerIndex < 0)
        {
            return trimmedName;
        }

        var identifier = trimmedName[..versionMarkerIndex].Trim();
        var versionText = trimmedName[(versionMarkerIndex + ",Version=v".Length)..].Trim();

        return identifier.ToUpperInvariant() switch
        {
            ".NETCOREAPP" => $"net{versionText}",
            ".NETSTANDARD" => $"netstandard{versionText}",
            ".NETFRAMEWORK" => $"net{versionText.Replace(".", string.Empty, StringComparison.Ordinal)}",
            _ => trimmedName
        };
    }

    private IReadOnlyList<AssemblyReferenceModel> AnalyzeSingleAssembly(
        Assembly targetAssembly,
        Dictionary<string, string> normalizedProjectLookup)
    {
        var assemblyLocation = targetAssembly.Location;
        var depsSnapshot = LoadDepsJsonSnapshot(assemblyLocation);
        var projectSnapshot = TryLoadProjectMetadata(assemblyLocation, normalizedProjectLookup);

        var targetFrameworkDisplay = !string.IsNullOrWhiteSpace(projectSnapshot?.TargetFrameworkMoniker)
            ? projectSnapshot.TargetFrameworkMoniker
            : FormatTargetFrameworkDisplayName(targetAssembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);

        var referencedAssemblyNames = targetAssembly.GetReferencedAssemblies();
        List<AssemblyReferenceModel> references = [];
        HashSet<string> seenReferenceNames = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var referenceName in referencedAssemblyNames)
        {
            var simpleName = referenceName.Name;
            if (string.IsNullOrWhiteSpace(simpleName) || !seenReferenceNames.Add(simpleName))
            {
                continue;
            }

            references.Add(CreateReferenceModel(referenceName, simpleName, targetFrameworkDisplay, projectSnapshot, depsSnapshot));
        }

        return
        [
            .. references
                .OrderBy(reference => reference.ReferenceKind)
                .ThenBy(reference => reference.ReferenceName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static AssemblyReferenceModel CreateReferenceModel(
        AssemblyName referenceName,
        string simpleName,
        string targetFrameworkDisplay,
        ProjectMetadataSnapshot? projectSnapshot,
        DepsMetadataSnapshot? depsSnapshot)
    {
        var assemblyVersion = referenceName.Version?.ToString() ?? string.Empty;
        var publicKeyTokenBytes = referenceName.GetPublicKeyToken();
        var isStrongNamed = publicKeyTokenBytes is { Length: > 0 };
        var publicKeyToken = isStrongNamed ? Convert.ToHexStringLower(publicKeyTokenBytes!) : string.Empty;

        var depsLibrary = ResolveDepsLibrary(depsSnapshot, simpleName);

        if (IsProjectReference(simpleName, projectSnapshot, depsLibrary))
        {
            return new AssemblyReferenceModel
            {
                ReferenceName = simpleName,
                ReferenceKind = AssemblyReferenceKind.Project,
                AssemblyVersion = assemblyVersion,
                VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked,
                IsStrongNamed = isStrongNamed,
                PublicKeyToken = publicKeyToken
            };
        }

        if (TryResolvePackageReference(simpleName, projectSnapshot, depsLibrary, out var packageVersion, out var packageLinkingKind, out var packageConstraint))
        {
            return new AssemblyReferenceModel
            {
                ReferenceName = simpleName,
                ReferenceKind = AssemblyReferenceKind.Package,
                AssemblyVersion = assemblyVersion,
                PackageVersion = packageVersion,
                VersionLinkingKind = packageLinkingKind,
                VersionConstraintExpression = packageConstraint,
                IsStrongNamed = isStrongNamed,
                PublicKeyToken = publicKeyToken
            };
        }

        if (TryResolveDirectAssemblyReference(simpleName, projectSnapshot, depsLibrary, out var directLinkingKind, out var directConstraint))
        {
            return new AssemblyReferenceModel
            {
                ReferenceName = simpleName,
                ReferenceKind = AssemblyReferenceKind.Assembly,
                AssemblyVersion = assemblyVersion,
                VersionLinkingKind = directLinkingKind,
                VersionConstraintExpression = directConstraint,
                IsStrongNamed = isStrongNamed,
                PublicKeyToken = publicKeyToken
            };
        }

        var isFramework = IsFrameworkReference(simpleName, publicKeyToken);
        return new AssemblyReferenceModel
        {
            ReferenceName = simpleName,
            ReferenceKind = isFramework ? AssemblyReferenceKind.Framework : AssemblyReferenceKind.Assembly,
            AssemblyVersion = assemblyVersion,
            VersionLinkingKind = isFramework ? AssemblyVersionLinkingKind.Framework : AssemblyVersionLinkingKind.Compiled,
            VersionConstraintExpression = isFramework ? targetFrameworkDisplay : string.Empty,
            IsStrongNamed = isStrongNamed,
            PublicKeyToken = publicKeyToken
        };
    }

    private Dictionary<string, string> NormalizeProjectPathLookup(IReadOnlyDictionary<string, string>? projectFilePathsByAssemblyPath)
    {
        Dictionary<string, string> normalizedLookup = [with(StringComparer.OrdinalIgnoreCase)];
        if (projectFilePathsByAssemblyPath == null)
        {
            return normalizedLookup;
        }

        foreach (var mappingEntry in projectFilePathsByAssemblyPath)
        {
            if (string.IsNullOrWhiteSpace(mappingEntry.Key) || string.IsNullOrWhiteSpace(mappingEntry.Value))
            {
                continue;
            }

            normalizedLookup[this.fileSystem.GetFullPath(mappingEntry.Key)] = this.fileSystem.GetFullPath(mappingEntry.Value);
        }

        return normalizedLookup;
    }

    private ProjectMetadataSnapshot? TryLoadProjectMetadata(
        string? assemblyLocation,
        Dictionary<string, string> normalizedProjectLookup)
    {
        if (string.IsNullOrWhiteSpace(assemblyLocation) || normalizedProjectLookup.Count == 0)
        {
            return null;
        }

        var fullAssemblyPath = this.fileSystem.GetFullPath(assemblyLocation);
        if (!normalizedProjectLookup.TryGetValue(fullAssemblyPath, out var projectFilePath) ||
            !this.fileSystem.FileExists(projectFilePath))
        {
            return null;
        }

        try
        {
            using var projectStream = this.fileSystem.OpenRead(projectFilePath);
            var projectXml = XDocument.Load(projectStream);
            var projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
            var centralPackageVersions = LoadCentralPackageVersions(projectDirectory);

            var targetFrameworkMoniker = ExtractTargetFrameworkFromProject(projectXml);
            var projectReferences = ExtractProjectReferences(projectXml, projectDirectory);
            var packageReferences = ExtractPackageReferences(projectXml, centralPackageVersions);
            var directReferences = ExtractDirectAssemblyReferences(projectXml);

            return new ProjectMetadataSnapshot(
                targetFrameworkMoniker,
                projectReferences,
                packageReferences,
                directReferences);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
        {
            LogMetadataParseFailed(projectFilePath, exception.Message);
            return null;
        }
    }

    private Dictionary<string, string> LoadCentralPackageVersions(string startingDirectory)
    {
        Dictionary<string, string> centralVersions = [with(StringComparer.OrdinalIgnoreCase)];
        var currentDirectory = startingDirectory;

        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var candidatePropsPath = Path.Combine(currentDirectory, "Directory.Packages.props");
            if (this.fileSystem.FileExists(candidatePropsPath))
            {
                try
                {
                    using var propsStream = this.fileSystem.OpenRead(candidatePropsPath);
                    var propsDocument = XDocument.Load(propsStream);
                    var cpmEnabledValue = propsDocument
                        .Descendants()
                        .FirstOrDefault(element => element.Name.LocalName == "ManagePackageVersionsCentrally")?
                        .Value?
                        .Trim();
                    if (string.Equals(cpmEnabledValue, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    foreach (var packageVersionElement in propsDocument.Descendants().Where(element => element.Name.LocalName == "PackageVersion"))
                    {
                        var packageId = packageVersionElement.Attribute("Include")?.Value?.Trim();
                        var rawVersion = ReadAttributeOrChildValue(packageVersionElement, "Version");
                        if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(rawVersion))
                        {
                            _ = centralVersions.TryAdd(packageId, rawVersion);
                        }
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
                {
                    LogMetadataParseFailed(candidatePropsPath, exception.Message);
                }
            }

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        return centralVersions;
    }

    private HashSet<string> ExtractProjectReferences(XDocument projectXml, string projectDirectory)
    {
        HashSet<string> referencedProjects = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var referenceElement in projectXml.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
        {
            var rawInclude = referenceElement.Attribute("Include")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(rawInclude))
            {
                continue;
            }

            var normalizedRelativePath = rawInclude
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            var referencedProjectPath = this.fileSystem.GetFullPath(Path.Combine(projectDirectory, normalizedRelativePath));
            var resolvedAssemblyName = ResolveProjectOutputAssemblyName(referencedProjectPath);

            if (!string.IsNullOrWhiteSpace(resolvedAssemblyName))
            {
                _ = referencedProjects.Add(resolvedAssemblyName);
            }
        }

        return referencedProjects;
    }

    private string ResolveProjectOutputAssemblyName(string referencedProjectPath)
    {
        var fallbackName = Path.GetFileNameWithoutExtension(referencedProjectPath);
        if (!this.fileSystem.FileExists(referencedProjectPath))
        {
            return fallbackName;
        }

        try
        {
            using var projectStream = this.fileSystem.OpenRead(referencedProjectPath);
            var referencedXml = XDocument.Load(projectStream);
            var explicitAssemblyName = referencedXml
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "AssemblyName")?
                .Value?
                .Trim();

            return string.IsNullOrWhiteSpace(explicitAssemblyName) ? fallbackName : explicitAssemblyName;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
        {
            LogMetadataParseFailed(referencedProjectPath, exception.Message);
            return fallbackName;
        }
    }

    private static string ExtractTargetFrameworkFromProject(XDocument projectXml)
    {
        var singleFramework = projectXml
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "TargetFramework")?
            .Value?
            .Trim();
        if (!string.IsNullOrWhiteSpace(singleFramework))
        {
            return singleFramework;
        }

        var multiFrameworks = projectXml
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "TargetFrameworks")?
            .Value;
        return string.IsNullOrWhiteSpace(multiFrameworks)
            ? string.Empty
            : multiFrameworks
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(moniker => moniker.Trim())
                .FirstOrDefault(moniker => !string.IsNullOrWhiteSpace(moniker))
                ?? string.Empty;
    }

    private static Dictionary<string, PackageConstraintInfo> ExtractPackageReferences(
        XDocument projectXml,
        Dictionary<string, string> centralPackageVersions)
    {
        Dictionary<string, PackageConstraintInfo> packageReferences = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var packageElement in projectXml.Descendants().Where(element => element.Name.LocalName == "PackageReference"))
        {
            var packageId = packageElement.Attribute("Include")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(packageId))
            {
                continue;
            }

            var rawVersion = ReadAttributeOrChildValue(packageElement, "VersionOverride")
                ?? ReadAttributeOrChildValue(packageElement, "Version")
                ?? (centralPackageVersions.TryGetValue(packageId, out var centralVersion) ? centralVersion : null);

            var (linkingKind, constraintExpression, cleanVersion) = ParseNuGetVersionConstraint(rawVersion);
            packageReferences[packageId] = new PackageConstraintInfo(linkingKind, constraintExpression, cleanVersion);
        }

        return packageReferences;
    }

    private static Dictionary<string, DirectAssemblyConstraintInfo> ExtractDirectAssemblyReferences(XDocument projectXml)
    {
        Dictionary<string, DirectAssemblyConstraintInfo> directReferences = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var referenceElement in projectXml.Descendants().Where(element => element.Name.LocalName == "Reference"))
        {
            var rawInclude = referenceElement.Attribute("Include")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(rawInclude))
            {
                continue;
            }

            var separatorIndex = rawInclude.IndexOf(',', StringComparison.Ordinal);
            var simpleName = separatorIndex > 0 ? rawInclude[..separatorIndex].Trim() : rawInclude;
            if (string.IsNullOrWhiteSpace(simpleName))
            {
                continue;
            }

            var specificVersionRaw = referenceElement
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "SpecificVersion")?
                .Value?
                .Trim();

            directReferences[simpleName] = string.Equals(specificVersionRaw, "true", StringComparison.OrdinalIgnoreCase)
                ? new DirectAssemblyConstraintInfo(AssemblyVersionLinkingKind.Fixed, "SpecificVersion=true")
                : string.Equals(specificVersionRaw, "false", StringComparison.OrdinalIgnoreCase)
                    ? new DirectAssemblyConstraintInfo(AssemblyVersionLinkingKind.Compiled, "SpecificVersion=false")
                    : rawInclude.Contains("Version=", StringComparison.OrdinalIgnoreCase)
                        ? new DirectAssemblyConstraintInfo(AssemblyVersionLinkingKind.Fixed, "SpecificVersion=true")
                        : new DirectAssemblyConstraintInfo(AssemblyVersionLinkingKind.Compiled, string.Empty);
        }

        return directReferences;
    }

    private DepsMetadataSnapshot? LoadDepsJsonSnapshot(string? assemblyLocation)
    {
        if (string.IsNullOrWhiteSpace(assemblyLocation))
        {
            return null;
        }

        var depsFilePath = Path.ChangeExtension(assemblyLocation, ".deps.json");
        if (string.IsNullOrWhiteSpace(depsFilePath) || !this.fileSystem.FileExists(depsFilePath))
        {
            return null;
        }

        try
        {
            using var depsStream = this.fileSystem.OpenRead(depsFilePath);
            using var depsDocument = JsonDocument.Parse(depsStream);

            Dictionary<string, DepsLibraryInfo> librariesByName = [with(StringComparer.OrdinalIgnoreCase)];
            if (depsDocument.RootElement.TryGetProperty("libraries", out var librariesElement) &&
                librariesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var libraryProperty in librariesElement.EnumerateObject())
                {
                    var (libraryName, libraryVersion) = SplitLibraryKey(libraryProperty.Name);
                    var libraryType = libraryProperty.Value.TryGetProperty("type", out var typeElement)
                        ? typeElement.GetString() ?? string.Empty
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(libraryName))
                    {
                        librariesByName[libraryName] = new DepsLibraryInfo(libraryName, libraryType, libraryVersion);
                    }
                }
            }

            Dictionary<string, DepsLibraryInfo> runtimeLibrariesByAssemblyName = [with(StringComparer.OrdinalIgnoreCase)];
            Dictionary<string, IReadOnlyList<DepsDependencyInfo>> dependenciesByLibraryName = [with(StringComparer.OrdinalIgnoreCase)];
            Dictionary<string, IReadOnlyList<string>> runtimeAssetPathsByLibraryName = [with(StringComparer.OrdinalIgnoreCase)];

            if (depsDocument.RootElement.TryGetProperty("targets", out var targetsElement) &&
                targetsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var targetProperty in targetsElement.EnumerateObject())
                {
                    if (targetProperty.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    PopulateTargetMetadata(
                        targetProperty.Value,
                        librariesByName,
                        runtimeLibrariesByAssemblyName,
                        dependenciesByLibraryName,
                        runtimeAssetPathsByLibraryName);
                }
            }

            return new DepsMetadataSnapshot(
                runtimeLibrariesByAssemblyName,
                librariesByName,
                dependenciesByLibraryName,
                runtimeAssetPathsByLibraryName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            LogMetadataParseFailed(depsFilePath, exception.Message);
            return null;
        }
    }

    private static void PopulateTargetMetadata(
        JsonElement targetObject,
        Dictionary<string, DepsLibraryInfo> librariesByName,
        Dictionary<string, DepsLibraryInfo> runtimeLibrariesByAssemblyName,
        Dictionary<string, IReadOnlyList<DepsDependencyInfo>> dependenciesByLibraryName,
        Dictionary<string, IReadOnlyList<string>> runtimeAssetPathsByLibraryName)
    {
        foreach (var libraryEntry in targetObject.EnumerateObject())
        {
            var (libraryName, libraryVersion) = SplitLibraryKey(libraryEntry.Name);
            if (string.IsNullOrWhiteSpace(libraryName))
            {
                continue;
            }

            var resolvedInfo = librariesByName.TryGetValue(libraryName, out var libraryInfo)
                ? libraryInfo
                : new DepsLibraryInfo(libraryName, string.Empty, libraryVersion);

            if (libraryEntry.Value.TryGetProperty("dependencies", out var dependenciesElement) &&
                dependenciesElement.ValueKind == JsonValueKind.Object)
            {
                List<DepsDependencyInfo> targetDependencies = [];
                foreach (var dependencyProperty in dependenciesElement.EnumerateObject())
                {
                    if (string.IsNullOrWhiteSpace(dependencyProperty.Name))
                    {
                        continue;
                    }

                    var rawVersion = dependencyProperty.Value.ValueKind == JsonValueKind.String
                        ? dependencyProperty.Value.GetString() ?? string.Empty
                        : string.Empty;
                    targetDependencies.Add(new DepsDependencyInfo(dependencyProperty.Name, rawVersion));
                }

                if (targetDependencies.Count > 0)
                {
                    dependenciesByLibraryName[libraryName] = targetDependencies;
                }
            }

            if (!libraryEntry.Value.TryGetProperty("runtime", out var runtimeObject) ||
                runtimeObject.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            List<string> assetPaths = [];
            foreach (var assetProperty in runtimeObject.EnumerateObject())
            {
                var assetAssemblyName = Path.GetFileNameWithoutExtension(assetProperty.Name);
                if (!string.IsNullOrWhiteSpace(assetAssemblyName))
                {
                    _ = runtimeLibrariesByAssemblyName.TryAdd(assetAssemblyName, resolvedInfo);
                }

                var localPathValue = assetProperty.Value.ValueKind == JsonValueKind.Object &&
                                     assetProperty.Value.TryGetProperty("localPath", out var localPathElement) &&
                                     localPathElement.ValueKind == JsonValueKind.String
                    ? localPathElement.GetString()
                    : null;

                var candidateAssetPath = !string.IsNullOrWhiteSpace(localPathValue)
                    ? localPathValue
                    : Path.GetFileName(assetProperty.Name);
                if (!string.IsNullOrWhiteSpace(candidateAssetPath))
                {
                    assetPaths.Add(candidateAssetPath);
                }
            }

            if (assetPaths.Count > 0)
            {
                runtimeAssetPathsByLibraryName[libraryName] = assetPaths;
            }
        }
    }

    private static Dictionary<string, StrongNameInfo> BuildKnownStrongNamesLookup(
        IEnumerable<Assembly> loadedAssemblies,
        IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> directReferencesByAssemblyName)
    {
        Dictionary<string, StrongNameInfo> lookup = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var referenceList in directReferencesByAssemblyName.Values)
        {
            foreach (var referenceModel in referenceList)
            {
                if (string.IsNullOrWhiteSpace(referenceModel.ReferenceName))
                {
                    continue;
                }

                if (!lookup.TryGetValue(referenceModel.ReferenceName, out var existing) ||
                    (!existing.IsStrongNamed && referenceModel.IsStrongNamed))
                {
                    lookup[referenceModel.ReferenceName] = new StrongNameInfo(
                        referenceModel.IsStrongNamed,
                        referenceModel.PublicKeyToken);
                }
            }
        }

        foreach (var assembly in loadedAssemblies)
        {
            var assemblyName = assembly.GetName();
            if (string.IsNullOrWhiteSpace(assemblyName.Name))
            {
                continue;
            }

            var publicKeyTokenBytes = assemblyName.GetPublicKeyToken();
            var isStrongNamed = publicKeyTokenBytes is { Length: > 0 };
            var publicKeyToken = isStrongNamed ? Convert.ToHexStringLower(publicKeyTokenBytes!) : string.Empty;

            if (!lookup.TryGetValue(assemblyName.Name, out var existing) ||
                (!existing.IsStrongNamed && isStrongNamed))
            {
                lookup[assemblyName.Name] = new StrongNameInfo(isStrongNamed, publicKeyToken);
            }
        }

        return lookup;
    }

    private IReadOnlyList<AssemblyDependencyEdgeModel> BuildDependencyTreeForRoot(
        string rootAssemblyName,
        IReadOnlyDictionary<string, IReadOnlyList<AssemblyReferenceModel>> directReferencesByAssemblyName,
        Dictionary<string, DepsMetadataSnapshot?> depsSnapshotsByAssemblyName,
        Dictionary<string, ProjectMetadataSnapshot?> projectSnapshotsByAssemblyName,
        Dictionary<string, string> outputDirectoryByAssemblyName,
        Dictionary<string, StrongNameInfo> knownStrongNames)
    {
        List<AssemblyDependencyEdgeModel> collectedEdges = [];
        HashSet<string> visitedNodes = [with(StringComparer.OrdinalIgnoreCase), rootAssemblyName];
        HashSet<string> seenEdgeKeys = [with(StringComparer.OrdinalIgnoreCase)];
        Queue<string> pendingNodes = new();
        pendingNodes.Enqueue(rootAssemblyName);

        _ = depsSnapshotsByAssemblyName.TryGetValue(rootAssemblyName, out var rootDepsSnapshot);
        _ = outputDirectoryByAssemblyName.TryGetValue(rootAssemblyName, out var rootOutputDirectory);

        while (pendingNodes.Count > 0)
        {
            var currentNode = pendingNodes.Dequeue();

            if (directReferencesByAssemblyName.TryGetValue(currentNode, out var directReferences))
            {
                foreach (var directReference in directReferences)
                {
                    if (directReference.ReferenceKind == AssemblyReferenceKind.Framework ||
                        string.IsNullOrWhiteSpace(directReference.ReferenceName) ||
                        string.Equals(directReference.ReferenceName, currentNode, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var edgeKey = $"{currentNode}->{directReference.ReferenceName}";
                    if (!seenEdgeKeys.Add(edgeKey))
                    {
                        continue;
                    }

                    var resolvedVersion = !string.IsNullOrWhiteSpace(directReference.PackageVersion)
                        ? directReference.PackageVersion
                        : directReference.AssemblyVersion;

                    collectedEdges.Add(new AssemblyDependencyEdgeModel
                    {
                        SourceAssemblyName = currentNode,
                        TargetAssemblyName = directReference.ReferenceName,
                        ReferenceKind = directReference.ReferenceKind,
                        VersionLinkingKind = directReference.VersionLinkingKind,
                        VersionConstraintExpression = directReference.VersionConstraintExpression,
                        ResolvedVersion = resolvedVersion,
                        IsTargetStrongNamed = directReference.IsStrongNamed,
                        TargetPublicKeyToken = directReference.PublicKeyToken
                    });

                    if (visitedNodes.Add(directReference.ReferenceName))
                    {
                        pendingNodes.Enqueue(directReference.ReferenceName);
                    }
                }
            }

            _ = depsSnapshotsByAssemblyName.TryGetValue(currentNode, out var nodeOwnDepsSnapshot);
            _ = projectSnapshotsByAssemblyName.TryGetValue(currentNode, out var nodeProjectSnapshot);
            var probeDirectory = outputDirectoryByAssemblyName.TryGetValue(currentNode, out var nodeDirectory) &&
                                 !string.IsNullOrWhiteSpace(nodeDirectory)
                ? nodeDirectory
                : rootOutputDirectory ?? string.Empty;

            ExpandDepsSnapshotEdges(
                currentNode,
                nodeOwnDepsSnapshot,
                rootDepsSnapshot,
                nodeProjectSnapshot,
                probeDirectory,
                knownStrongNames,
                seenEdgeKeys,
                visitedNodes,
                pendingNodes,
                collectedEdges);
        }

        return
        [
            .. collectedEdges
                .OrderBy(edge => edge.SourceAssemblyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(edge => edge.ReferenceKind)
                .ThenBy(edge => edge.TargetAssemblyName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private void ExpandDepsSnapshotEdges(
        string currentNode,
        DepsMetadataSnapshot? nodeOwnDepsSnapshot,
        DepsMetadataSnapshot? rootDepsSnapshot,
        ProjectMetadataSnapshot? nodeProjectSnapshot,
        string probeDirectory,
        Dictionary<string, StrongNameInfo> knownStrongNames,
        HashSet<string> seenEdgeKeys,
        HashSet<string> visitedNodes,
        Queue<string> pendingNodes,
        List<AssemblyDependencyEdgeModel> collectedEdges)
    {
        IEnumerable<DepsMetadataSnapshot?> candidateSnapshots = nodeOwnDepsSnapshot != null &&
                                                               !ReferenceEquals(nodeOwnDepsSnapshot, rootDepsSnapshot)
            ? [nodeOwnDepsSnapshot, rootDepsSnapshot]
            : [rootDepsSnapshot ?? nodeOwnDepsSnapshot];

        foreach (var snapshot in candidateSnapshots)
        {
            if (snapshot == null)
            {
                continue;
            }

            foreach (var dependency in EnumerateSnapshotDependenciesForNode(snapshot, currentNode))
            {
                if (string.IsNullOrWhiteSpace(dependency.TargetName) ||
                    string.Equals(dependency.TargetName, currentNode, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var targetLibraryInfo = snapshot.LibrariesByName.TryGetValue(dependency.TargetName, out var exactLibrary)
                    ? exactLibrary
                    : null;

                var strongNameInfo = ResolveTargetStrongName(
                    dependency.TargetName,
                    snapshot,
                    probeDirectory,
                    knownStrongNames);

                var referenceKind = ClassifyDepsDependencyKind(
                    dependency.TargetName,
                    targetLibraryInfo,
                    nodeProjectSnapshot,
                    strongNameInfo.PublicKeyToken,
                    out var shouldSkipAsFramework);

                if (shouldSkipAsFramework)
                {
                    continue;
                }

                var edgeKey = $"{currentNode}->{dependency.TargetName}";
                if (!seenEdgeKeys.Add(edgeKey))
                {
                    continue;
                }

                var (linkingKind, constraintExpression, resolvedVersion) = ResolveDepsEdgeVersionMetadata(
                    referenceKind,
                    dependency.RawVersion,
                    targetLibraryInfo,
                    nodeProjectSnapshot,
                    dependency.TargetName);

                collectedEdges.Add(new AssemblyDependencyEdgeModel
                {
                    SourceAssemblyName = currentNode,
                    TargetAssemblyName = dependency.TargetName,
                    ReferenceKind = referenceKind,
                    VersionLinkingKind = linkingKind,
                    VersionConstraintExpression = constraintExpression,
                    ResolvedVersion = resolvedVersion,
                    IsTargetStrongNamed = strongNameInfo.IsStrongNamed,
                    TargetPublicKeyToken = strongNameInfo.PublicKeyToken
                });

                if (visitedNodes.Add(dependency.TargetName))
                {
                    pendingNodes.Enqueue(dependency.TargetName);
                }
            }
        }
    }

    private static IEnumerable<DepsDependencyInfo> EnumerateSnapshotDependenciesForNode(
        DepsMetadataSnapshot snapshot,
        string nodeName)
    {
        if (snapshot.DependenciesByLibraryName.TryGetValue(nodeName, out var directDependencies))
        {
            foreach (var dependency in directDependencies)
            {
                yield return dependency;
            }
        }

        if (snapshot.RuntimeLibrariesByAssemblyName.TryGetValue(nodeName, out var runtimeLibrary) &&
            !string.Equals(runtimeLibrary.PackageId, nodeName, StringComparison.OrdinalIgnoreCase) &&
            snapshot.DependenciesByLibraryName.TryGetValue(runtimeLibrary.PackageId, out var packageDependencies))
        {
            foreach (var dependency in packageDependencies)
            {
                yield return dependency;
            }
        }
    }

    private StrongNameInfo ResolveTargetStrongName(
        string targetName,
        DepsMetadataSnapshot snapshot,
        string probeDirectory,
        Dictionary<string, StrongNameInfo> knownStrongNames)
    {
        if (knownStrongNames.TryGetValue(targetName, out var cachedStrongName))
        {
            return cachedStrongName;
        }

        if (string.IsNullOrWhiteSpace(probeDirectory))
        {
            return new StrongNameInfo(false, string.Empty);
        }

        List<string> candidateRelativePaths = [];
        if (snapshot.RuntimeAssetPathsByLibraryName.TryGetValue(targetName, out var runtimeAssets))
        {
            candidateRelativePaths.AddRange(runtimeAssets);
        }

        candidateRelativePaths.Add($"{targetName}.dll");

        foreach (var relativePath in candidateRelativePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var fileNameOnly = Path.GetFileName(relativePath);
            IEnumerable<string> probePaths = string.Equals(relativePath, fileNameOnly, StringComparison.OrdinalIgnoreCase)
                ? [Path.Combine(probeDirectory, relativePath)]
                : [Path.Combine(probeDirectory, relativePath), Path.Combine(probeDirectory, fileNameOnly)];

            foreach (var candidatePath in probePaths)
            {
                if (!this.fileSystem.FileExists(candidatePath))
                {
                    continue;
                }

                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(candidatePath);
                    var tokenBytes = assemblyName.GetPublicKeyToken();
                    var isStrongNamed = tokenBytes is { Length: > 0 };
                    var publicKeyToken = isStrongNamed ? Convert.ToHexStringLower(tokenBytes!) : string.Empty;

                    StrongNameInfo resolved = new(isStrongNamed, publicKeyToken);
                    knownStrongNames[targetName] = resolved;
                    return resolved;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException or FileLoadException)
                {
                    LogMetadataParseFailed(candidatePath, exception.Message);
                }
            }
        }

        StrongNameInfo unsignedFallback = new(false, string.Empty);
        knownStrongNames[targetName] = unsignedFallback;
        return unsignedFallback;
    }

    private static AssemblyReferenceKind ClassifyDepsDependencyKind(
        string targetName,
        DepsLibraryInfo? targetLibraryInfo,
        ProjectMetadataSnapshot? nodeProjectSnapshot,
        string publicKeyToken,
        out bool shouldSkipAsFramework)
    {
        if (nodeProjectSnapshot?.ProjectReferences.Contains(targetName) == true ||
            string.Equals(targetLibraryInfo?.LibraryType, "project", StringComparison.OrdinalIgnoreCase))
        {
            shouldSkipAsFramework = false;
            return AssemblyReferenceKind.Project;
        }

        if (string.Equals(targetLibraryInfo?.LibraryType, "reference", StringComparison.OrdinalIgnoreCase))
        {
            shouldSkipAsFramework = false;
            return AssemblyReferenceKind.Assembly;
        }

        if (string.Equals(targetLibraryInfo?.LibraryType, "package", StringComparison.OrdinalIgnoreCase))
        {
            shouldSkipAsFramework = false;
            return AssemblyReferenceKind.Package;
        }

        shouldSkipAsFramework = IsFrameworkReference(targetName, publicKeyToken);
        return shouldSkipAsFramework ? AssemblyReferenceKind.Framework : AssemblyReferenceKind.Package;
    }

    private static (AssemblyVersionLinkingKind LinkingKind, string ConstraintExpression, string ResolvedVersion) ResolveDepsEdgeVersionMetadata(
        AssemblyReferenceKind referenceKind,
        string rawDependencyVersion,
        DepsLibraryInfo? targetLibraryInfo,
        ProjectMetadataSnapshot? nodeProjectSnapshot,
        string targetName)
    {
        if (referenceKind == AssemblyReferenceKind.Project)
        {
            var projectVersion = !string.IsNullOrWhiteSpace(targetLibraryInfo?.Version)
                ? targetLibraryInfo.Version
                : rawDependencyVersion;
            return (AssemblyVersionLinkingKind.ProjectLinked, string.Empty, projectVersion);
        }

        if (nodeProjectSnapshot != null &&
            nodeProjectSnapshot.PackageReferences.TryGetValue(targetName, out var packageConstraint))
        {
            var resolvedPackageVersion = !string.IsNullOrWhiteSpace(targetLibraryInfo?.Version)
                ? targetLibraryInfo.Version
                : !string.IsNullOrWhiteSpace(packageConstraint.RawVersion)
                    ? packageConstraint.RawVersion
                    : rawDependencyVersion;
            return (packageConstraint.LinkingKind, packageConstraint.ConstraintExpression, resolvedPackageVersion);
        }

        var (linkingKind, constraintExpression, cleanVersion) = ParseNuGetVersionConstraint(rawDependencyVersion);
        var resolvedVersion = !string.IsNullOrWhiteSpace(targetLibraryInfo?.Version)
            ? targetLibraryInfo.Version
            : cleanVersion;
        return (linkingKind, constraintExpression, resolvedVersion);
    }

    private static DepsLibraryInfo? ResolveDepsLibrary(DepsMetadataSnapshot? depsSnapshot, string simpleName) =>
        depsSnapshot == null
            ? null
            : depsSnapshot.RuntimeLibrariesByAssemblyName.TryGetValue(simpleName, out var runtimeLibrary)
                ? runtimeLibrary
                : depsSnapshot.LibrariesByName.TryGetValue(simpleName, out var namedLibrary)
                    ? namedLibrary
                    : null;

    private static bool IsProjectReference(
        string simpleName,
        ProjectMetadataSnapshot? projectSnapshot,
        DepsLibraryInfo? depsLibrary) =>
        projectSnapshot?.ProjectReferences.Contains(simpleName) == true ||
        string.Equals(depsLibrary?.LibraryType, "project", StringComparison.OrdinalIgnoreCase);

    private static bool TryResolvePackageReference(
        string simpleName,
        ProjectMetadataSnapshot? projectSnapshot,
        DepsLibraryInfo? depsLibrary,
        out string packageVersion,
        out AssemblyVersionLinkingKind linkingKind,
        out string constraintExpression)
    {
        PackageConstraintInfo? packageConstraint = null;
        if (projectSnapshot != null)
        {
            if (projectSnapshot.PackageReferences.TryGetValue(simpleName, out var directMatch))
            {
                packageConstraint = directMatch;
            }
            else if (depsLibrary != null &&
                     projectSnapshot.PackageReferences.TryGetValue(depsLibrary.PackageId, out var packageIdMatch))
            {
                packageConstraint = packageIdMatch;
            }
        }

        var isDepsPackage = string.Equals(depsLibrary?.LibraryType, "package", StringComparison.OrdinalIgnoreCase);
        if (packageConstraint == null && !isDepsPackage)
        {
            packageVersion = string.Empty;
            linkingKind = AssemblyVersionLinkingKind.Compiled;
            constraintExpression = string.Empty;
            return false;
        }

        packageVersion = !string.IsNullOrWhiteSpace(depsLibrary?.Version)
            ? depsLibrary.Version
            : packageConstraint?.RawVersion ?? string.Empty;
        linkingKind = packageConstraint?.LinkingKind ?? AssemblyVersionLinkingKind.Compiled;
        constraintExpression = packageConstraint?.ConstraintExpression ?? string.Empty;
        return true;
    }

    private static bool TryResolveDirectAssemblyReference(
        string simpleName,
        ProjectMetadataSnapshot? projectSnapshot,
        DepsLibraryInfo? depsLibrary,
        out AssemblyVersionLinkingKind linkingKind,
        out string constraintExpression)
    {
        if (projectSnapshot != null &&
            projectSnapshot.DirectAssemblyReferences.TryGetValue(simpleName, out var directConstraint))
        {
            linkingKind = directConstraint.LinkingKind;
            constraintExpression = directConstraint.ConstraintExpression;
            return true;
        }

        if (string.Equals(depsLibrary?.LibraryType, "reference", StringComparison.OrdinalIgnoreCase))
        {
            linkingKind = AssemblyVersionLinkingKind.Compiled;
            constraintExpression = string.Empty;
            return true;
        }

        linkingKind = AssemblyVersionLinkingKind.Compiled;
        constraintExpression = string.Empty;
        return false;
    }

    private static bool IsFrameworkReference(string simpleName, string publicKeyToken) =>
        FrameworkAssemblyExactNames.Contains(simpleName) ||
        FrameworkAssemblyPrefixes.Any(prefix => simpleName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(publicKeyToken) &&
         MicrosoftFrameworkPublicKeyTokens.Contains(publicKeyToken));

    private static (string Name, string Version) SplitLibraryKey(string libraryKey)
    {
        var slashIndex = libraryKey.IndexOf('/', StringComparison.Ordinal);
        return slashIndex > 0
            ? (libraryKey[..slashIndex], libraryKey[(slashIndex + 1)..])
            : (libraryKey, string.Empty);
    }

    private static string? ReadAttributeOrChildValue(XElement element, string name)
    {
        var attributeValue = element.Attribute(name)?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(attributeValue))
        {
            return attributeValue;
        }

        var childValue = element
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == name)?
            .Value?
            .Trim();
        return string.IsNullOrWhiteSpace(childValue) ? null : childValue;
    }

    private static string ExtractFirstVersionToken(string rangeExpression)
    {
        var stripped = rangeExpression.Trim('[', ']', '(', ')', ' ');
        var commaIndex = stripped.IndexOf(',', StringComparison.Ordinal);
        return commaIndex >= 0 ? stripped[..commaIndex].Trim() : stripped;
    }
}
