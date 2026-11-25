namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Specifies how external documentation content should be injected into the generated page.
/// </summary>
public enum InjectionMode
{
    /// <summary>
    /// Extends the generated documentation by appending or prepending content to specified target sections.
    /// </summary>
    Extend = 0,

    /// <summary>
    /// Replaces the target documentation section or page body with the custom content.
    /// </summary>
    Replace = 1
}
