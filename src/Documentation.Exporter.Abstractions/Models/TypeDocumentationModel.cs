using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents documentation metadata for a C# Type (Class, Interface, Struct, Enum, Delegate).
/// </summary>
public sealed record TypeDocumentationModel
{
    /// <summary>
    /// Gets or sets the simple type name (e.g. Validator).
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.TypeNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.TypeNameMinLength))]
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional disambiguated directory segment name used when multiple types in the same namespace share <see cref="TypeName"/> across different generic arities (for example <c>"Result_T1"</c>).
    /// When empty, <see cref="TypeName"/> is used directly.
    /// </summary>
    public string DirectorySegmentName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the formatted display title including generic type parameters when <see cref="GenericTypeParameters"/> is populated and this type shares its simple name with another arity overload, or <see cref="TypeName"/> otherwise.
    /// </summary>
    public string DisplayTypeName =>
        !string.IsNullOrWhiteSpace(DirectorySegmentName) &&
        !string.Equals(DirectorySegmentName, TypeName, StringComparison.Ordinal) &&
        GenericTypeParameters.Count > 0
            ? $"{TypeName}<{string.Join(", ", GenericTypeParameters)}>"
            : TypeName;

    /// <summary>
    /// Gets or sets the clean type name of the immediate enclosing/declaring type when this type is a nested type.
    /// </summary>
    public string? DeclaringTypeName { get; init; }

    /// <summary>
    /// Gets or sets the full ordered chain of enclosing/declaring type names from outermost to innermost (for example <c>["Outer", "Middle"]</c>).
    /// </summary>
    public IReadOnlyList<string> DeclaringTypeChain { get; init; } = [];

    /// <summary>
    /// Gets or sets the namespace name (e.g. System.ComponentModel.DataAnnotations).
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.NamespaceNameRequired))]
    public string NamespaceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the assembly name (e.g. System.ComponentModel.Annotations.dll).
    /// </summary>
    public string AssemblyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the type category (<see cref="Models.TypeCategory.Class"/>, <see cref="Models.TypeCategory.Interface"/>,
    /// <see cref="Models.TypeCategory.Struct"/>, <see cref="Models.TypeCategory.Enum"/>, or <see cref="Models.TypeCategory.Delegate"/>).
    /// </summary>
    [EnumDataType(typeof(TypeCategory), ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.TypeCategoryInvalid))]
    public TypeCategory TypeCategory { get; init; } = TypeCategory.Class;

    /// <summary>
    /// Gets or sets the full C# declaration signature syntax.
    /// </summary>
    public string CSharpDeclarationSignature { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved Markdown link (or display name) of the underlying integral type when <see cref="TypeCategory"/> is <see cref="Models.TypeCategory.Enum"/>.
    /// </summary>
    public string EnumUnderlyingTypeLink { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the access modifier of the type (e.g. "public", "protected", "internal").
    /// </summary>
    public string AccessModifier { get; init; } = "public";

    /// <summary>
    /// Gets or sets the constructor classification of the type (e.g. "Default", "Primary (...)", "Overloaded (...)", "None").
    /// </summary>
    public string ConstructorKind { get; init; } = "Default";

    /// <summary>
    /// Gets or sets a value indicating whether the type is abstract.
    /// </summary>
    public bool IsAbstractType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is sealed.
    /// </summary>
    public bool IsSealedType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is static.
    /// </summary>
    public bool IsStaticType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is a C# <c>record</c> (<c>record class</c> or <c>record struct</c>).
    /// </summary>
    public bool IsRecordType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the value type is declared with <c>readonly</c>.
    /// </summary>
    public bool IsReadOnlyType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the value type is a <c>ref struct</c>.
    /// </summary>
    public bool IsRefStructType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the type is marked with <c>[Experimental]</c>.
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
    /// Gets or sets a value indicating whether the type is generic.
    /// </summary>
    public bool IsGenericType { get; init; }

    /// <summary>
    /// Gets or sets the generic type parameter names when <see cref="IsGenericType"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<string> GenericTypeParameters { get; init; } = [];

    /// <summary>
    /// Gets or sets detailed metadata (name, constraints, and XML <c>&lt;typeparam&gt;</c> description) for each generic type parameter.
    /// </summary>
    public IReadOnlyList<ParameterDocumentationModel> GenericTypeParameterDetails { get; init; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the type is marked with <see cref="ObsoleteAttribute"/>.
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
    /// Gets or sets the XML documentation summary description.
    /// </summary>
    public string SummaryDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the XML documentation remarks.
    /// </summary>
    public string RemarksDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the exceptions documented with <c>&lt;exception&gt;</c> on the type itself (key: exception type link, value: condition).
    /// </summary>
    public IReadOnlyDictionary<string, string> DocumentedExceptions { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a value indicating whether this type specifies an <c>&lt;inheritdoc /&gt;</c> tag.
    /// </summary>
    public bool HasInheritDoc { get; init; }

    /// <summary>
    /// Gets or sets the explicit target cref specified in <c>&lt;inheritdoc cref="..." /&gt;</c>, if any.
    /// </summary>
    public string? InheritDocCref { get; init; }

    /// <summary>
    /// Gets or sets the inheritance hierarchy chain (e.g. System.Object -> Validator).
    /// </summary>
    public IReadOnlyList<string> InheritanceHierarchy { get; init; } = [];

    /// <summary>
    /// Gets or sets implemented interface names.
    /// </summary>
    public IReadOnlyList<string> ImplementedInterfaces { get; init; } = [];

    /// <summary>
    /// Gets or sets derived classes or known implementing types discovered across the loaded assemblies.
    /// </summary>
    public IReadOnlyList<string> DerivedTypes { get; init; } = [];

    /// <summary>
    /// Gets or sets applied custom attribute names.
    /// </summary>
    public IReadOnlyList<string> AppliedAttributes { get; init; } = [];

    /// <summary>
    /// Gets or sets constructors.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> Constructors { get; init; } = [];

    /// <summary>
    /// Gets or sets properties.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> Properties { get; init; } = [];

    /// <summary>
    /// Gets or sets methods.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> Methods { get; init; } = [];

    /// <summary>
    /// Gets or sets extension methods targeting this type discovered across the loaded assemblies.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> ExtensionMethods { get; init; } = [];

    /// <summary>
    /// Gets or sets C# 14 extension properties targeting this type discovered across the loaded assemblies.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> ExtensionProperties { get; init; } = [];

    /// <summary>
    /// Gets or sets fields.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> Fields { get; init; } = [];

    /// <summary>
    /// Gets or sets events.
    /// </summary>
    public IReadOnlyList<MemberDocumentationModel> Events { get; init; } = [];

    /// <summary>
    /// Gets or sets code examples.
    /// </summary>
    public IReadOnlyList<string> CodeExamples { get; init; } = [];

    /// <summary>
    /// Gets or sets see-also references.
    /// </summary>
    public IReadOnlyList<string> SeeAlsoReferences { get; init; } = [];

    /// <summary>
    /// Gets or sets injected supplementary documentation sections.
    /// </summary>
    public IReadOnlyList<DocumentationInjectionModel> InjectedDocumentationSections { get; init; } = [];

    /// <summary>
    /// Gets or sets the preferred diagram generator format (such as <see cref="DiagramFormat.Mermaid"/>, <see cref="DiagramFormat.PlantUml"/>,
    /// <see cref="DiagramFormat.None"/>, or a plugin-defined custom format; defaults to <see cref="DiagramFormat.Mermaid"/>)
    /// rendered for this type, falling back to another registered generator only when the preferred generator does not support the type.
    /// </summary>
    [ValidDiagramFormat(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.DiagramFormatInvalid))]
    public DiagramFormat DiagramFormat { get; init; } = DiagramFormat.Mermaid;

    /// <summary>
    /// Gets or sets the resolved Markdown links to public nested types declared inside this type.
    /// </summary>
    public IReadOnlyList<string> NestedTypes { get; init; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this type is marked with an XML <c>&lt;exclude/&gt;</c> documentation tag.
    /// </summary>
    public bool IsExcludedByXmlDoc { get; init; }

    /// <summary>
    /// Enumerates all documented members declared on this type across constructors, properties, methods, fields, and events.
    /// </summary>
    /// <returns>A unified sequence of all member models on this type.</returns>
    public IEnumerable<MemberDocumentationModel> EnumerateAllMembers() =>
        Constructors
            .Concat(Properties)
            .Concat(Methods)
            .Concat(Fields)
            .Concat(Events);
}
