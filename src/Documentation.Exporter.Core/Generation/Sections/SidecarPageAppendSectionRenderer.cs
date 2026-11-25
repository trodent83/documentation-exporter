using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders sidecar override documentation injected targeting the <see cref="SidecarAnchor.PageAppend"/> anchor at the bottom of the page.
/// </summary>
public sealed class SidecarPageAppendSectionRenderer()
    : SidecarAnchorSectionRendererBase(SidecarAnchor.PageAppend, 110);
