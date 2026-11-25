using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Service interface for loading and parsing XML documentation comments.
/// </summary>
public interface IXmlDocumentationParser
{
    /// <summary>
    /// Asynchronously tries to load and parse the specified compiler XML documentation file.
    /// </summary>
    /// <param name="xmlDocumentationFilePath">File path to the XML documentation file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see langword="true"/> if the file was loaded successfully; otherwise <see langword="false"/>.</returns>
    Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously tries to load and index multiple compiler XML documentation files into a unified documentation map.
    /// </summary>
    /// <param name="xmlDocumentationFilePaths">File paths to the XML documentation files.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see langword="true"/> if all specified XML files were loaded successfully; otherwise <see langword="false"/>.</returns>
    Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all currently indexed XML documentation elements.
    /// </summary>
    void Clear();

    /// <summary>
    /// Applies the XML documentation comments of a specific type to a type documentation model.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified name of the target type.</param>
    /// <param name="typeDocumentationModel">The type documentation model to document. It is never modified.</param>
    /// <returns>A new model containing the XML documentation; the unchanged input model when no documentation exists.</returns>
    TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel);

    /// <summary>
    /// Applies the XML documentation comments of a specific member key to a member documentation model.
    /// </summary>
    /// <param name="xmlMemberKey">The XML member key.</param>
    /// <param name="memberDocumentationModel">The member documentation model to document. It is never modified.</param>
    /// <returns>A new model containing the XML documentation; the unchanged input model when no documentation exists.</returns>
    MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel);

    /// <summary>
    /// Resolves a documentation comment reference (cref) to an indexed member key.
    /// </summary>
    /// <param name="cref">The raw cref attribute string.</param>
    /// <param name="declaringTypeFullName">Optional declaring type name to qualify relative references.</param>
    /// <param name="defaultPrefix">The default XML member prefix (e.g. "M", "T", "P").</param>
    /// <returns>The matching XML member key if found; otherwise, null.</returns>
    string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M");
}
