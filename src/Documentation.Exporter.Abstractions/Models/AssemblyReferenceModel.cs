using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents metadata describing a referenced assembly dependency, its version linking policy,
/// and strong-name signing details.
/// </summary>
public sealed record AssemblyReferenceModel
{
    /// <summary>
    /// Gets the simple assembly name of the referenced assembly.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceNameMinLength))]
    public string ReferenceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the reference source category (<see cref="AssemblyReferenceKind.Project"/>, <see cref="AssemblyReferenceKind.Package"/>,
    /// <see cref="AssemblyReferenceKind.Assembly"/>, or <see cref="AssemblyReferenceKind.Framework"/>).
    /// </summary>
    [EnumDataType(typeof(AssemblyReferenceKind), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceKindInvalid))]
    public AssemblyReferenceKind ReferenceKind { get; init; } = AssemblyReferenceKind.Assembly;

    /// <summary>
    /// Gets the compiled assembly version recorded in metadata.
    /// </summary>
    public string AssemblyVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resolved NuGet package version when the reference comes from a package dependency.
    /// </summary>
    public string PackageVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the version linking policy declared for this reference.
    /// </summary>
    [EnumDataType(typeof(AssemblyVersionLinkingKind), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.VersionLinkingKindInvalid))]
    public AssemblyVersionLinkingKind VersionLinkingKind { get; init; } = AssemblyVersionLinkingKind.Compiled;

    /// <summary>
    /// Gets the raw or formatted version constraint expression (for example <c>&gt;= 9.0.2</c>, <c>[1.0.0, 2.0.0)</c>, or <c>net11.0</c>).
    /// </summary>
    public string VersionConstraintExpression { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the referenced assembly has a strong-name public key token.
    /// </summary>
    public bool IsStrongNamed { get; init; }

    /// <summary>
    /// Gets the lowercase hexadecimal <c>PublicKeyToken</c> when <see cref="IsStrongNamed"/> is <see langword="true"/>.
    /// </summary>
    public string PublicKeyToken { get; init; } = string.Empty;
}
