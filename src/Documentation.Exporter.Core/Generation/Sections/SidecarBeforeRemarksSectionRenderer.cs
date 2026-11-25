using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders sidecar override documentation injected targeting the <see cref="SidecarAnchor.BeforeRemarks"/> anchor.
/// </summary>
public sealed class SidecarBeforeRemarksSectionRenderer()
    : SidecarAnchorSectionRendererBase(SidecarAnchor.BeforeRemarks, 50);
