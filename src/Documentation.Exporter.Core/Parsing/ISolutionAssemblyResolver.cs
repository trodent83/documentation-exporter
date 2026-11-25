namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Discovers compiled assembly (<c>.dll</c>) and XML documentation (<c>.xml</c>) paths from a C# solution (<c>.sln</c> or <c>.slnx</c>).
/// </summary>
public interface ISolutionAssemblyResolver
{
    /// <summary>
    /// Discovers all compiled project assemblies and XML documentation files for the specified solution file.
    /// </summary>
    /// <param name="solutionFilePath">Path to the <c>.sln</c> or <c>.slnx</c> solution file.</param>
    /// <param name="buildConfiguration">The build configuration (for example <c>"Debug"</c> or <c>"Release"</c>).</param>
    /// <param name="includeTestProjects">Whether test projects should be included in the results.</param>
    /// <param name="excludedProjects">Optional project/assembly names or wildcard patterns (<c>*</c>, <c>?</c>) to exclude.</param>
    /// <param name="failOnMissing">Whether to throw <see cref="FileNotFoundException"/> if a solution or non-excluded project artifact is missing.</param>
    /// <returns>List of discovered assembly artifacts.</returns>
    IReadOnlyList<DiscoveredAssemblyArtifact> ResolveSolutionAssemblies(
        string solutionFilePath,
        string buildConfiguration = "Debug",
        bool includeTestProjects = false,
        IEnumerable<string>? excludedProjects = null,
        bool failOnMissing = false);
}

/// <summary>
/// Represents a discovered compiled assembly, its companion XML documentation file, and optional originating project file.
/// </summary>
/// <param name="AssemblyFilePath">Full file path to the compiled assembly (<c>.dll</c>).</param>
/// <param name="XmlDocumentationFilePath">Full file path to the companion XML documentation file (<c>.xml</c>).</param>
/// <param name="ProjectFilePath">Optional full file path to the originating <c>.csproj</c> project file.</param>
public sealed record DiscoveredAssemblyArtifact(
    string AssemblyFilePath,
    string XmlDocumentationFilePath,
    string? ProjectFilePath = null);
