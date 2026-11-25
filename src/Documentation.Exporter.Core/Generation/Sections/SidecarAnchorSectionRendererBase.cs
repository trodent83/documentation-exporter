using Documentation.Exporter.Abstractions.Generation.Sections;
using System.Text;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Base class for section renderers that emit sidecar override Markdown blocks matching a specific <see cref="SidecarAnchor"/>.
/// </summary>
/// <param name="targetAnchor">The sidecar anchor matched against injected sections.</param>
/// <param name="sectionOrder">The relative rendering order within the Markdown page.</param>
public abstract class SidecarAnchorSectionRendererBase(SidecarAnchor targetAnchor, int sectionOrder) : IMarkdownSectionRenderer
{
    /// <inheritdoc />
    public int Order { get; } = sectionOrder;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        StringBuilder sectionBuilder = new();
        foreach (var injection in typeModel.InjectedDocumentationSections)
        {
            if (injection.TargetAnchor != targetAnchor)
            {
                continue;
            }

            _ = sectionBuilder.AppendLine(injection.CustomMarkdownContent);
            _ = sectionBuilder.AppendLine();
        }

        return sectionBuilder.ToString();
    }
}
