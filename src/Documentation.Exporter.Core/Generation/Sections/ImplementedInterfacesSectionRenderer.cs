using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the list of interfaces implemented by the type.
/// </summary>
public sealed class ImplementedInterfacesSectionRenderer : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order => 40;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum ||
            typeModel.ImplementedInterfaces.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"## {MarkdownGenerationResources.ImplementedInterfacesHeading}");
        _ = sectionBuilder.AppendLine();
        foreach (var interfaceName in typeModel.ImplementedInterfaces)
        {
            var formattedInterface = interfaceName.Contains("](", StringComparison.Ordinal)
                ? interfaceName
                : $"`{interfaceName}`";
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"* {formattedInterface}");
        }

        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }
}
