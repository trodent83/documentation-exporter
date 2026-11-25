namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Identifies the target section anchor for sidecar Markdown documentation injections.
/// </summary>
public enum SidecarAnchor
{
    /// <summary>
    /// Injects content immediately before the Remarks section.
    /// </summary>
    BeforeRemarks = 0,

    /// <summary>
    /// Injects content immediately after the Remarks section.
    /// </summary>
    AfterRemarks = 1,

    /// <summary>
    /// Injects content immediately after the Examples section.
    /// </summary>
    AfterExamples = 2,

    /// <summary>
    /// Appends content at the bottom of the generated page.
    /// </summary>
    PageAppend = 3,

    /// <summary>
    /// Replaces the XML documentation summary block when <see cref="InjectionMode.Replace"/> is active.
    /// </summary>
    ReplaceSummary = 4
}
