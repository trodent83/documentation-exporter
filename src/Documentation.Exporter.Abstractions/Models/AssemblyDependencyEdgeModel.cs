using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents a directed dependency edge between two non-framework assemblies inside a root assembly's
/// transitive dependency tree, carrying version linking policy and strong-name metadata.
/// </summary>
public sealed record AssemblyDependencyEdgeModel
{
    /// <summary>
    /// Gets the simple name of the source (depending) assembly.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DependencySourceNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DependencySourceNameMinLength))]
    public string SourceAssemblyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the simple name of the target (referenced) assembly or NuGet library.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceNameMinLength))]
    public string TargetAssemblyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the dependency source kind (<see cref="AssemblyReferenceKind.Project"/>, <see cref="AssemblyReferenceKind.Package"/>, or <see cref="AssemblyReferenceKind.Assembly"/>).
    /// </summary>
    [EnumDataType(typeof(AssemblyReferenceKind), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ReferenceKindInvalid))]
    public AssemblyReferenceKind ReferenceKind { get; init; } = AssemblyReferenceKind.Assembly;

    /// <summary>
    /// Gets the version linking policy applied to this dependency edge.
    /// </summary>
    [EnumDataType(typeof(AssemblyVersionLinkingKind), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.VersionLinkingKindInvalid))]
    public AssemblyVersionLinkingKind VersionLinkingKind { get; init; } = AssemblyVersionLinkingKind.Compiled;

    /// <summary>
    /// Gets the raw version constraint expression (for example <c>[1.0.0, 2.0.0)</c>, <c>&gt;= 9.0.2</c>, or <c>[9.0.2]</c>).
    /// </summary>
    public string VersionConstraintExpression { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resolved assembly or NuGet package version for the target node.
    /// </summary>
    public string ResolvedVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the target assembly is signed with a strong-name public key token.
    /// </summary>
    public bool IsTargetStrongNamed { get; init; }

    /// <summary>
    /// Gets the lowercase hexadecimal public key token of the target assembly when <see cref="IsTargetStrongNamed"/> is <see langword="true"/>.
    /// </summary>
    public string TargetPublicKeyToken { get; init; } = string.Empty;
}
