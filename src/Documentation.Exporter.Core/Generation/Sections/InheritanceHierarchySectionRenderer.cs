using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the type inheritance hierarchy as an inline breadcrumb chain of links (<c>BaseClass &gt; Child1 &gt; Child2</c>), excluding <see cref="object"/>.
/// </summary>
public sealed class InheritanceHierarchySectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 15;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum)
        {
            return string.Empty;
        }

        var filteredBaseTypes = typeModel.InheritanceHierarchy
            .Where(baseType => !IsSystemObjectEntry(baseType))
            .Select(EnsureMarkdownLink)
            .ToList();

        if (filteredBaseTypes.Count == 0)
        {
            return string.Empty;
        }

        var currentTypeRelativeUrl = TypeDocumentationPaths.GetTypeRelativeUrlPath(typeModel);
        var currentTypeLink = $"[{typeModel.DisplayTypeName}]({currentTypeRelativeUrl})";
        filteredBaseTypes.Add(currentTypeLink);

        var hierarchyChain = string.Join(" > ", filteredBaseTypes);
        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"{hierarchyChain}");
        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }

    private static bool IsSystemObjectEntry(string baseTypeEntry)
    {
        if (string.IsNullOrWhiteSpace(baseTypeEntry))
        {
            return true;
        }

        var trimmed = baseTypeEntry.Trim();
        return string.Equals(trimmed, "System.Object", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, "Object", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("[Object](", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("[System.Object](", StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureMarkdownLink(string typeEntry)
    {
        var trimmed = typeEntry.Trim();
        if (trimmed.Contains("](", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lastDotIndex = trimmed.LastIndexOf('.');
        var simpleName = lastDotIndex < 0 ? trimmed : trimmed[(lastDotIndex + 1)..];
        return $"[{simpleName}]({simpleName}.md)";
    }
}
