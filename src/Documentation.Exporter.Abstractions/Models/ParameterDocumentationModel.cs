using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Resources;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Represents documentation metadata for a parameter or generic type parameter.
/// </summary>
public sealed record ParameterDocumentationModel
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ParameterNameRequired))]
    [MinLength(1, ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ParameterNameMinLength))]
    public string ParameterName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the C# type name of the parameter.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessagesResources), ErrorMessageResourceName = nameof(ValidationMessagesResources.ParameterTypeNameRequired))]
    public string ParameterTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the XML documentation description for the parameter.
    /// </summary>
    public string ParameterDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved Markdown link to the parameter type documentation.
    /// </summary>
    public string ResolvedTypeDocumentationLink { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets formatted generic constraint descriptions when this instance represents a generic type parameter (<c>typeparam</c>).
    /// </summary>
    public string ParameterConstraints { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is a <see cref="CancellationToken"/> supporting cooperative async cancellation.
    /// </summary>
    public bool IsCancellationToken { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is decorated with <see cref="System.Runtime.CompilerServices.EnumeratorCancellationAttribute"/> for <see cref="IAsyncEnumerable{T}"/> streams.
    /// </summary>
    public bool IsEnumeratorCancellation { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is optional and can be omitted by callers.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// Gets or sets the formatted C# literal representation of the parameter's default value when <see cref="IsOptional"/> is <see langword="true"/>.
    /// </summary>
    public string DefaultValue { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this parameter accepts a variable-length argument list (<c>params</c>).
    /// </summary>
    public bool IsParams { get; init; }

    /// <summary>
    /// Gets or sets the C# parameter modifier keyword (<c>ref</c>, <c>out</c>, <c>in</c>, <c>ref readonly</c>, <c>params</c>, or <c>this</c>).
    /// </summary>
    public string ParameterModifier { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is the target (<c>this</c>) receiver of an extension method.
    /// </summary>
    public bool IsExtensionTarget { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter accepts <see langword="null"/> values (<see cref="Nullable{T}"/> or nullable reference type).
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets or sets the compiler caller-information attribute applied to this parameter (such as <c>CallerMemberName</c> or <c>CallerArgumentExpression</c>).
    /// </summary>
    public string CallerInformationAttribute { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets custom attributes applied to this parameter or generic type parameter with resolved documentation links.
    /// </summary>
    public IReadOnlyList<string> AppliedAttributes { get; init; } = [];
}
