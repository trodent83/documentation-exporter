using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents documentation metadata for a type member (Method, Property, Field, Event, Constructor).
/// </summary>
public sealed record MemberDocumentationModel
{
    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.MemberNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.MemberNameMinLength))]
    public string MemberName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the member signature for display (e.g. C# declaration syntax).
    /// </summary>
    public string MemberSignature { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the XML documentation summary description.
    /// </summary>
    public string SummaryDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the XML documentation remarks.
    /// </summary>
    public string RemarksDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this member specifies an <c>&lt;inheritdoc /&gt;</c> tag.
    /// </summary>
    public bool HasInheritDoc { get; init; }

    /// <summary>
    /// Gets or sets the explicit target cref specified in <c>&lt;inheritdoc cref="..." /&gt;</c>, if any.
    /// </summary>
    public string? InheritDocCref { get; init; }

    /// <summary>
    /// Gets or sets the property value or return value description.
    /// </summary>
    public string ReturnValueDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the C# return type name.
    /// </summary>
    public string ReturnTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved Markdown link to the return type documentation.
    /// </summary>
    public string ResolvedReturnTypeDocumentationLink { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of parameters for methods or constructors.
    /// </summary>
    public IReadOnlyList<ParameterDocumentationModel> MemberParameters { get; init; } = [];

    /// <summary>
    /// Gets or sets the list of generic type parameters.
    /// </summary>
    public IReadOnlyList<ParameterDocumentationModel> GenericTypeParameters { get; init; } = [];

    /// <summary>
    /// Gets or sets the exceptions documented for this member (ExceptionTypeName -> ConditionDescription).
    /// </summary>
    public IReadOnlyDictionary<string, string> DocumentedExceptions { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets code examples associated with this member.
    /// </summary>
    public IReadOnlyList<string> CodeExamples { get; init; } = [];

    /// <summary>
    /// Gets or sets see-also links documented for this member.
    /// </summary>
    public IReadOnlyList<string> SeeAlsoReferences { get; init; } = [];

    /// <summary>
    /// Gets or sets injected supplementary documentation sections from member-level sidecar files.
    /// </summary>
    public IReadOnlyList<DocumentationInjectionModel> InjectedDocumentationSections { get; init; } = [];

    /// <summary>
    /// Gets or sets visibility modifier (e.g., "public", "protected").
    /// </summary>
    public string VisibilityModifier { get; init; } = "public";

    /// <summary>
    /// Gets or sets a value indicating whether this member is static.
    /// </summary>
    public bool IsStaticMember { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this constructor member is the type's C# primary constructor.
    /// </summary>
    public bool IsPrimaryConstructor { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this member is marked with <see cref="ObsoleteAttribute"/>.
    /// </summary>
    public bool IsObsolete { get; init; }

    /// <summary>
    /// Gets or sets the custom message extracted from <see cref="ObsoleteAttribute.Message"/> when <see cref="IsObsolete"/> is <see langword="true"/>.
    /// </summary>
    public string ObsoleteMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ObsoleteAttribute.IsError"/> is <see langword="true"/>.
    /// </summary>
    public bool IsObsoleteError { get; init; }

    /// <summary>
    /// Gets or sets the optional diagnostic identifier from <c>[Obsolete(DiagnosticId = ...)]</c>.
    /// </summary>
    public string ObsoleteDiagnosticId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional documentation URL format template from <c>[Obsolete(UrlFormat = ...)]</c>.
    /// </summary>
    public string ObsoleteUrlFormat { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this member is marked with <c>[Experimental]</c>.
    /// </summary>
    public bool IsExperimental { get; init; }

    /// <summary>
    /// Gets or sets the diagnostic identifier from <c>[Experimental]</c> when <see cref="IsExperimental"/> is <see langword="true"/>.
    /// </summary>
    public string ExperimentalDiagnosticId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional documentation URL format template from <c>[Experimental(UrlFormat = ...)]</c>.
    /// </summary>
    public string ExperimentalUrlFormat { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this member represents a C# 14 extension property.
    /// </summary>
    public bool IsExtensionProperty { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this method is asynchronous (returns <see cref="Task"/>, <see cref="ValueTask"/>, <see cref="IAsyncEnumerable{T}"/>, or has an async state machine).
    /// </summary>
    public bool IsAsyncMethod { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this method is an asynchronous stream returning <see cref="IAsyncEnumerable{T}"/> or <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    public bool IsAsyncEnumerable { get; init; }

    /// <summary>
    /// Gets or sets applied custom attributes on this member.
    /// </summary>
    public IReadOnlyList<string> AppliedAttributes { get; init; } = [];

    /// <summary>
    /// Gets or sets custom attributes applied to this member's return value (for example <c>[return: NotNullIfNotNull]</c>).
    /// </summary>
    public IReadOnlyList<string> ReturnAttributes { get; init; } = [];

    /// <summary>
    /// Gets or sets the fully qualified target type name when this method is an extension method (<c>this</c> first parameter).
    /// </summary>
    public string ExtensionTargetTypeFullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved relative Markdown URL to this member's detail page when referenced from another type (such as an extension method table).
    /// </summary>
    public string ResolvedMemberDetailUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this member is marked with <c>[EditorBrowsable(EditorBrowsableState.Never)]</c>.
    /// </summary>
    public bool IsEditorBrowsableNever { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this member is marked with an XML <c>&lt;exclude/&gt;</c> documentation tag.
    /// </summary>
    public bool IsExcludedByXmlDoc { get; init; }

    /// <summary>
    /// Gets or sets the canonical compiler XML documentation key for this member (for example <c>"M:Namespace.Type.Method(System.String)"</c>).
    /// </summary>
    public string XmlMemberKey { get; init; } = string.Empty;
}
