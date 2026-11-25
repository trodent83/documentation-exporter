using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Resolution;

/// <summary>
/// Provides canonical relative URL and OS-native path calculations for exported <see cref="TypeDocumentationModel"/> pages
/// so core generators, link resolvers, and external navigation/rendering plugins produce consistent file layouts.
/// </summary>
public static class TypeDocumentationPaths
{
    /// <summary>
    /// Computes the forward-slash subregion directory path for a namespace relative to the root assembly namespace.
    /// </summary>
    /// <param name="namespaceName">The full namespace of the type.</param>
    /// <param name="assemblyName">The assembly name representing the root namespace.</param>
    /// <returns>A relative forward-slash directory path (for example <c>"Services"</c> or <c>"Parsing/Tags"</c>), or <see cref="string.Empty"/> if at root.</returns>
    public static string GetNamespaceSubregionPath(string? namespaceName, string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return string.Empty;
        }

        var trimmedNamespace = namespaceName.Trim();
        var trimmedAssembly = assemblyName?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(trimmedAssembly))
        {
            if (string.Equals(trimmedNamespace, trimmedAssembly, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            var assemblyPrefix = trimmedAssembly + ".";
            return trimmedNamespace.StartsWith(assemblyPrefix, StringComparison.Ordinal)
                ? trimmedNamespace[assemblyPrefix.Length..].Replace('.', '/')
                : string.Empty;
        }

        var firstDotIndex = trimmedNamespace.IndexOf('.');
        return firstDotIndex < 0
            ? string.Empty
            : trimmedNamespace[(firstDotIndex + 1)..].Replace('.', '/');
    }

    /// <summary>
    /// Computes the forward-slash directory path for a <see cref="TypeDocumentationModel"/> (including its dedicated type subdirectory and full nested chain).
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative forward-slash directory path (for example <c>"Services/OrderProcessingService"</c> or <c>"MyAssembly/Services/OrderProcessingService"</c>).</returns>
    public static string GetTypeDirectoryUrlPath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        var leafDirectorySegment = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
            ? typeModel.DirectorySegmentName
            : typeModel.TypeName;

        var subregionPath = GetNamespaceSubregionPath(typeModel.NamespaceName, typeModel.AssemblyName);
        var typeRelativePath = typeModel.DeclaringTypeChain.Count > 0
            ? $"{string.Join("/", typeModel.DeclaringTypeChain)}/{leafDirectorySegment}"
            : !string.IsNullOrWhiteSpace(typeModel.DeclaringTypeName)
                ? $"{typeModel.DeclaringTypeName}/{leafDirectorySegment}"
                : leafDirectorySegment;

        var pathWithinAssembly = string.IsNullOrEmpty(subregionPath)
            ? typeRelativePath
            : $"{subregionPath}/{typeRelativePath}";

        return includeAssemblyDirectory && !string.IsNullOrWhiteSpace(typeModel.AssemblyName)
            ? $"{typeModel.AssemblyName.Trim()}/{pathWithinAssembly}"
            : pathWithinAssembly;
    }

    /// <summary>
    /// Computes the relative URL path (using forward slashes) for a <see cref="TypeDocumentationModel"/> overview <c>index.md</c> file.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative Markdown URL path (for example <c>"Services/OrderProcessingService/index.md"</c>).</returns>
    public static string GetTypeRelativeUrlPath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        var typeDirectoryUrl = GetTypeDirectoryUrlPath(typeModel, includeAssemblyDirectory);
        return $"{typeDirectoryUrl}/index.md";
    }

    /// <summary>
    /// Computes the OS-native relative directory path for a <see cref="TypeDocumentationModel"/>.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative OS directory path for the type.</returns>
    public static string GetTypeDirectoryFilePath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        var typeDirectoryUrl = GetTypeDirectoryUrlPath(typeModel, includeAssemblyDirectory);
        if (string.IsNullOrEmpty(typeDirectoryUrl))
        {
            return !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
                ? typeModel.DirectorySegmentName
                : typeModel.TypeName;
        }

        var pathSegments = typeDirectoryUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(pathSegments);
    }

    /// <summary>
    /// Computes the OS-native relative file path for a <see cref="TypeDocumentationModel"/> overview <c>index.md</c> file.
    /// </summary>
    /// <param name="typeModel">The type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether to prefix the path with <see cref="TypeDocumentationModel.AssemblyName"/>.</param>
    /// <returns>Relative OS file path to the type's <c>index.md</c>.</returns>
    public static string GetTypeRelativeFilePath(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        var typeDirectoryPath = GetTypeDirectoryFilePath(typeModel, includeAssemblyDirectory);
        return Path.Combine(typeDirectoryPath, "index.md");
    }
}
