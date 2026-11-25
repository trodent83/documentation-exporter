using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders sidecar override documentation injected targeting the <see cref="SidecarAnchor.AfterRemarks"/> anchor.
/// </summary>
public sealed class SidecarAfterRemarksSectionRenderer()
    : SidecarAnchorSectionRendererBase(SidecarAnchor.AfterRemarks, 80);
