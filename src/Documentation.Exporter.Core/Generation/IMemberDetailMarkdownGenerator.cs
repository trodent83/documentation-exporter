using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Generates dedicated detail Markdown documents for members of a reflected type.
/// </summary>
public interface IMemberDetailMarkdownGenerator
{
    /// <summary>
    /// Generates dedicated detail Markdown documents for every member (constructor, property, method, field, event)
    /// of a reflected type, optionally scoped within an assembly subdirectory.
    /// </summary>
    /// <param name="typeModel">The declaring type documentation model.</param>
    /// <param name="includeAssemblyDirectory">Whether types are organized under per-assembly subdirectories.</param>
    /// <returns>A dictionary mapping member detail file names to their formatted Markdown content.</returns>
    IReadOnlyDictionary<string, string> GenerateMemberMarkdownFiles(TypeDocumentationModel typeModel, bool includeAssemblyDirectory = false);

    /// <summary>
    /// Renders the raw detailed Markdown page for a single member prior to directory link adjustment.
    /// </summary>
    /// <param name="declaringTypeModel">The declaring type documentation model.</param>
    /// <param name="memberModel">The member documentation model.</param>
    /// <param name="isOverloaded">Whether the member is overloaded or a constructor group.</param>
    /// <returns>The rendered Markdown page content.</returns>
    string RenderMemberDetailMarkdown(
        TypeDocumentationModel declaringTypeModel,
        MemberDocumentationModel memberModel,
        bool isOverloaded);
}
