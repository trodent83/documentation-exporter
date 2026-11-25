using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders sidecar override documentation injected targeting the <see cref="SidecarAnchor.AfterExamples"/> anchor.
/// </summary>
public sealed class SidecarAfterExamplesSectionRenderer()
    : SidecarAnchorSectionRendererBase(SidecarAnchor.AfterExamples, 95);
