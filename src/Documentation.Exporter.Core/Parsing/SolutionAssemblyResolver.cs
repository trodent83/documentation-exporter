using System.IO.Enumeration;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Documentation.Exporter.Abstractions.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Parses <c>.sln</c> and <c>.slnx</c> solution files to locate compiled project assemblies (<c>.dll</c>) and XML documentation files (<c>.xml</c>).
/// </summary>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class SolutionAssemblyResolver(
    IFileSystem fileSystem,
    ILogger<SolutionAssemblyResolver>? logger = null) : ISolutionAssemblyResolver
{
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<SolutionAssemblyResolver> logger = logger ?? NullLogger<SolutionAssemblyResolver>.Instance;

    [LoggerMessage(EventId = 601, Level = LogLevel.Warning, Message = "Project file '{ProjectFilePath}' referenced in solution '{SolutionFilePath}' does not exist on disk and was skipped.")]
    private partial void LogProjectFileNotFound(string projectFilePath, string solutionFilePath);

    [LoggerMessage(EventId = 602, Level = LogLevel.Warning, Message = "No compiled assembly or XML documentation file found for project '{ProjectFilePath}' (Configuration: '{Configuration}'). Project was skipped.")]
    private partial void LogProjectBuildArtifactNotFound(string projectFilePath, string configuration);

    /// <inheritdoc />
    public IReadOnlyList<DiscoveredAssemblyArtifact> ResolveSolutionAssemblies(
        string solutionFilePath,
        string buildConfiguration = "Debug",
        bool includeTestProjects = false,
        IEnumerable<string>? excludedProjects = null,
        bool failOnMissing = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionFilePath);

        var normalizedConfiguration = string.IsNullOrWhiteSpace(buildConfiguration) ? "Debug" : buildConfiguration.Trim();
        if (!this.fileSystem.FileExists(solutionFilePath))
        {
            return failOnMissing
                ? throw new FileNotFoundException($"Solution file not found: {solutionFilePath}", solutionFilePath)
                : [];
        }

        var exclusionPatterns = excludedProjects?
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(pattern => pattern.Trim())
            .ToList() ?? [];

        var fullSolutionPath = this.fileSystem.GetFullPath(solutionFilePath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath) ?? this.fileSystem.GetCurrentDirectory();
        var projectPaths = ExtractProjectFilePaths(fullSolutionPath, solutionDirectory, failOnMissing);

        List<DiscoveredAssemblyArtifact> discoveredArtifacts = [];
        foreach (var projectFilePath in projectPaths)
        {
            if (!this.fileSystem.FileExists(projectFilePath))
            {
                LogProjectFileNotFound(projectFilePath, fullSolutionPath);
                if (failOnMissing)
                {
                    throw new FileNotFoundException($"Project file '{projectFilePath}' referenced in solution '{fullSolutionPath}' was not found.", projectFilePath);
                }

                continue;
            }

            var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            var projectFileName = Path.GetFileName(projectFilePath);
            if (IsExcludedByPatterns(projectName, exclusionPatterns) ||
                IsExcludedByPatterns(projectFileName, exclusionPatterns))
            {
                continue;
            }

            XmlDocumentLoadResult projectXmlResult = TryLoadXmlDocument(projectFilePath);
            if (projectXmlResult.Document == null)
            {
                LogProjectBuildArtifactNotFound(projectFilePath, normalizedConfiguration);
                if (failOnMissing)
                {
                    throw new FileNotFoundException($"Project file '{projectFilePath}' could not be parsed as valid XML.", projectFilePath, projectXmlResult.Exception);
                }

                continue;
            }

            var projectXml = projectXmlResult.Document;
            if (!includeTestProjects && IsTestProject(projectFilePath, projectXml))
            {
                continue;
            }

            var artifact = TryLocateProjectBuildArtifact(projectFilePath, projectXml, solutionDirectory, normalizedConfiguration);
            if (artifact != null)
            {
                var resolvedAssemblyName = Path.GetFileNameWithoutExtension(artifact.AssemblyFilePath);
                if (!IsExcludedByPatterns(resolvedAssemblyName, exclusionPatterns))
                {
                    discoveredArtifacts.Add(artifact);
                }
            }
            else
            {
                LogProjectBuildArtifactNotFound(projectFilePath, normalizedConfiguration);
                if (failOnMissing)
                {
                    throw new FileNotFoundException($"Build artifacts (assembly .dll or companion .xml) not found for project '{projectFilePath}' in configuration '{normalizedConfiguration}'.", projectFilePath);
                }
            }
        }

        return discoveredArtifacts;
    }

    /// <summary>
    /// Determines whether the specified project or assembly identifier matches any of the configured exclusion patterns (exact or wildcard <c>*</c>/<c>?</c>).
    /// </summary>
    /// <param name="candidateName">The project or assembly name to test.</param>
    /// <param name="exclusionPatterns">The collection of exclusion patterns.</param>
    /// <returns><c>true</c> if <paramref name="candidateName"/> matches any exclusion pattern; otherwise <c>false</c>.</returns>
    public static bool IsExcludedByPatterns(string? candidateName, IEnumerable<string>? exclusionPatterns)
    {
        if (string.IsNullOrWhiteSpace(candidateName) || exclusionPatterns == null)
        {
            return false;
        }

        var trimmedCandidate = candidateName.Trim();
        foreach (var rawPattern in exclusionPatterns)
        {
            if (string.IsNullOrWhiteSpace(rawPattern))
            {
                continue;
            }

            var pattern = rawPattern.Trim();
            if (FileSystemName.MatchesSimpleExpression(pattern, trimmedCandidate, ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }

    private List<string> ExtractProjectFilePaths(string fullSolutionPath, string solutionDirectory, bool failOnMissing)
    {
        if (fullSolutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            var slnxLoadResult = TryLoadXmlDocument(fullSolutionPath);
            return slnxLoadResult.Document == null
                ? failOnMissing
                    ? throw new FileNotFoundException($"Solution file '{fullSolutionPath}' could not be parsed as valid XML.", fullSolutionPath, slnxLoadResult.Exception)
                    : []
                :
                [
                    .. slnxLoadResult.Document
                        .Descendants("Project")
                        .Select(element => element.Attribute("Path")?.Value)
                        .Where(pathValue => !string.IsNullOrWhiteSpace(pathValue) &&
                                            pathValue.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                        .Select(pathValue => this.fileSystem.GetFullPath(Path.Combine(
                            solutionDirectory,
                            pathValue!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))))
                ];
        }

        string solutionText;
        try
        {
            solutionText = this.fileSystem.ReadAllText(fullSolutionPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return failOnMissing
                ? throw new FileNotFoundException($"Solution file '{fullSolutionPath}' could not be read.", fullSolutionPath, exception)
                : [];
        }

        return
        [
            .. ClassicSlnProjectRegex()
                .Matches(solutionText)
                .Select(match => match.Groups[1].Value)
                .Where(relativePath => relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(relativePath => this.fileSystem.GetFullPath(Path.Combine(
                    solutionDirectory,
                    relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))))
        ];
    }

    private static bool IsTestProject(string projectFilePath, XDocument projectXml)
    {
        var projectFileName = Path.GetFileNameWithoutExtension(projectFilePath);
        if (projectFileName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
            projectFileName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
            projectFileName.EndsWith(".IntegrationTests", StringComparison.OrdinalIgnoreCase) ||
            projectFileName.EndsWith(".UnitTests", StringComparison.OrdinalIgnoreCase) ||
            projectFileName.EndsWith(".FunctionalTests", StringComparison.OrdinalIgnoreCase) ||
            projectFileName.EndsWith(".Specs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var isTestProjectElement = projectXml.Descendants("IsTestProject").FirstOrDefault()?.Value;
        return string.Equals(isTestProjectElement, "true", StringComparison.OrdinalIgnoreCase) ||
               projectXml
                   .Descendants("PackageReference")
                   .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                   .Any(packageName => string.Equals(packageName, "Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                                       packageName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                                       packageName.StartsWith("NUnit", StringComparison.OrdinalIgnoreCase) ||
                                       packageName.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase) ||
                                       packageName.StartsWith("TUnit", StringComparison.OrdinalIgnoreCase) ||
                                       packageName.StartsWith("Microsoft.Testing.", StringComparison.OrdinalIgnoreCase));
    }

    private readonly record struct XmlDocumentLoadResult(XDocument? Document, Exception? Exception);

    private XmlDocumentLoadResult TryLoadXmlDocument(string xmlFilePath)
    {
        try
        {
            using var stream = this.fileSystem.OpenRead(xmlFilePath);
            return new XmlDocumentLoadResult(XDocument.Load(stream), null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return new XmlDocumentLoadResult(null, exception);
        }
    }

    private DiscoveredAssemblyArtifact? TryLocateProjectBuildArtifact(
        string projectFilePath,
        XDocument projectXml,
        string solutionDirectory,
        string buildConfiguration)
    {
        var projectDirectory = Path.GetDirectoryName(projectFilePath)!;
        var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
        var assemblyName = projectXml.Descendants("AssemblyName").FirstOrDefault()?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = projectName;
        }

        var assemblyFileName = $"{assemblyName}.dll";
        var xmlFileName = $"{assemblyName}.xml";
        var configLower = buildConfiguration.ToLowerInvariant();

        List<string> candidateSearchRoots = [];

        var currentDirectory = solutionDirectory;
        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var centralizedProjectBin = Path.Combine(currentDirectory, "build", "bin", projectName);
            if (this.fileSystem.DirectoryExists(centralizedProjectBin))
            {
                candidateSearchRoots.Add(centralizedProjectBin);
            }

            var artifactsProjectBin = Path.Combine(currentDirectory, "artifacts", "bin", projectName);
            if (this.fileSystem.DirectoryExists(artifactsProjectBin))
            {
                candidateSearchRoots.Add(artifactsProjectBin);
            }

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        var customOutputPath = projectXml.Descendants("OutputPath").FirstOrDefault()?.Value?.Trim()
                               ?? projectXml.Descendants("BaseOutputPath").FirstOrDefault()?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(customOutputPath) && !customOutputPath.Contains("$(", StringComparison.Ordinal))
        {
            var resolvedCustomOutput = Path.IsPathRooted(customOutputPath)
                ? customOutputPath
                : Path.Combine(projectDirectory, customOutputPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            if (this.fileSystem.DirectoryExists(resolvedCustomOutput))
            {
                candidateSearchRoots.Add(resolvedCustomOutput);
            }
        }

        var standardProjectBin = Path.Combine(projectDirectory, "bin");
        if (this.fileSystem.DirectoryExists(standardProjectBin))
        {
            candidateSearchRoots.Add(standardProjectBin);
        }

        foreach (var searchRoot in candidateSearchRoots)
        {
            var matchingDllFiles = this.fileSystem
                .EnumerateFiles(searchRoot, assemblyFileName, SearchOption.AllDirectories)
                .OrderByDescending(filePath => PathContainsConfigurationSegment(filePath, buildConfiguration, configLower))
                .ToList();

            foreach (var dllPath in matchingDllFiles)
            {
                var siblingXmlPath = Path.Combine(Path.GetDirectoryName(dllPath)!, xmlFileName);
                if (this.fileSystem.FileExists(siblingXmlPath))
                {
                    return new DiscoveredAssemblyArtifact(
                        this.fileSystem.GetFullPath(dllPath),
                        this.fileSystem.GetFullPath(siblingXmlPath),
                        this.fileSystem.GetFullPath(projectFilePath));
                }
            }
        }

        return null;
    }

    private static bool PathContainsConfigurationSegment(string filePath, string configuration, string configurationLower)
    {
        var segments = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, configuration, StringComparison.OrdinalIgnoreCase) ||
            segment.StartsWith(configurationLower + "_", StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"Project\(""[^""]+""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""")]
    private static partial Regex ClassicSlnProjectRegex();
}
