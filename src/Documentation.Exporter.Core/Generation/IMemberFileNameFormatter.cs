using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation;

/// <summary>
/// Computes deterministic Markdown file names and display headers for member documentation pages and overview links.
/// </summary>
public interface IMemberFileNameFormatter
{
    /// <summary>
    /// Computes the deterministic Markdown filename for a member's detailed documentation page within its declaring type directory.
    /// </summary>
    /// <param name="memberModel">The member documentation model.</param>
    /// <param name="isOverloaded">Whether multiple members share the same <see cref="MemberDocumentationModel.MemberName"/>.</param>
    /// <param name="isConstructor">Whether the member is a constructor.</param>
    /// <returns>The Markdown file name (for example <c>"ProcessOrderAsync.md"</c> or <c>"ProcessOrder_String_Decimal.md"</c>).</returns>
    string GetMemberDetailFileName(MemberDocumentationModel memberModel, bool isOverloaded, bool isConstructor = false);

    /// <summary>
    /// Formats the human-readable member display title used in tables and member detail page headings.
    /// </summary>
    /// <param name="memberModel">The member documentation model.</param>
    /// <param name="isOverloaded">Whether overload parameter types should be appended to the title.</param>
    /// <returns>Formatted display header (for example <c>"ProcessOrder(OrderRequest)"</c>).</returns>
    string FormatMemberHeaderTitle(MemberDocumentationModel memberModel, bool isOverloaded);
}
