using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Generates the overview Markdown document (<c>index.md</c>) for a single reflected type.
/// </summary>
public interface ITypePageMarkdownGenerator
{
    /// <summary>
    /// Generates the overview Markdown document (<c>index.md</c>) for a single reflected type,
    /// optionally scoped within an assembly subdirectory.
    /// </summary>
    /// <param name="typeModel">The type documentation model to render.</param>
    /// <param name="includeAssemblyDirectory">Whether types are organized under per-assembly subdirectories.</param>
    /// <returns>Normalized Markdown content for the type overview page.</returns>
    string GenerateTypeMarkdown(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false);
}
