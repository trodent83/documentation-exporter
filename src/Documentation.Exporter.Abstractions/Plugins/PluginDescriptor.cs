using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Plugins;

/// <summary>
/// Describes a plugin assembly and entry-point type discovered and registered during startup.
/// </summary>
public sealed record PluginDescriptor
{
    /// <summary>
    /// Gets the display name reported by <see cref="IDocumentationExporterPlugin.Name"/>.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.PluginNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.PluginNameRequired))]
    public string PluginName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the simple name of the loaded plugin assembly.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceNameMinLength))]
    public string AssemblyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full file path of the loaded plugin assembly.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.PluginAssemblyPathRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.PluginAssemblyPathRequired))]
    public string AssemblyPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full CLR type name implementing <see cref="IDocumentationExporterPlugin"/>.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.PluginTypeFullNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.PluginTypeFullNameRequired))]
    public string PluginTypeFullName { get; init; } = string.Empty;
}
