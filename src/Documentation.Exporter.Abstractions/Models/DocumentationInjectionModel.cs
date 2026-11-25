using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents external documentation content to be injected into a generated page.
/// </summary>
public sealed record DocumentationInjectionModel
{
    /// <summary>
    /// Gets or sets the target type name or member name for this injection.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.TargetNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.TargetNameMinLength))]
    public string TargetName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the injection mode (Extend or Replace).
    /// </summary>
    [EnumDataType(typeof(InjectionMode), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.InjectionModeInvalid))]
    public InjectionMode InjectionMode { get; init; } = InjectionMode.Extend;

    /// <summary>
    /// Gets or sets the target section anchor for the injected content.
    /// </summary>
    [EnumDataType(typeof(SidecarAnchor), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.TargetAnchorInvalid))]
    public SidecarAnchor TargetAnchor { get; init; } = SidecarAnchor.AfterRemarks;

    /// <summary>
    /// Gets or sets the custom markdown content to inject.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.CustomMarkdownContentRequired))]
    public string CustomMarkdownContent { get; init; } = string.Empty;
}
