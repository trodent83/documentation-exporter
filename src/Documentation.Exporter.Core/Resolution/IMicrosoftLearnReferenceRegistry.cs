using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Central registry providing canonical Microsoft Learn documentation URLs and Markdown link formatters
/// for C# keywords, modifiers, constraints, attributes, and type categories.
/// </summary>
public interface IMicrosoftLearnReferenceRegistry
{
    /// <summary>Gets the Microsoft Learn URL for C# access modifiers.</summary>
    string AccessModifiersDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# constructors.</summary>
    string ConstructorsDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# default instance constructors.</summary>
    string DefaultConstructorsDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# primary instance constructors.</summary>
    string PrimaryConstructorsDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>abstract</c> keyword.</summary>
    string AbstractKeywordDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>sealed</c> keyword.</summary>
    string SealedKeywordDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>static</c> keyword.</summary>
    string StaticKeywordDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>async</c> keyword.</summary>
    string AsyncKeywordDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# asynchronous programming.</summary>
    string AsyncProgrammingDocUrl { get; }

    /// <summary>Gets the Microsoft Learn API URL for <c>IAsyncEnumerable&lt;T&gt;</c>.</summary>
    string AsyncEnumerableDocUrl { get; }

    /// <summary>Gets the Microsoft Learn API URL for <c>EnumeratorCancellationAttribute</c>.</summary>
    string EnumeratorCancellationDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for cancellation in managed threads.</summary>
    string CancellationDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# optional parameters.</summary>
    string OptionalParametersDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>params</c> parameter modifier.</summary>
    string ParamsModifierDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>ref</c> parameter modifier.</summary>
    string RefModifierDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>out</c> parameter modifier.</summary>
    string OutModifierDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>in</c> parameter modifier.</summary>
    string InModifierDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>ref readonly</c> parameter modifier.</summary>
    string RefReadonlyModifierDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# extension methods.</summary>
    string ExtensionMethodsDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# nullable reference types.</summary>
    string NullableReferencesDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# caller information attributes.</summary>
    string CallerInformationDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>class</c> generic constraint.</summary>
    string ClassConstraintDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>struct</c> generic constraint.</summary>
    string StructConstraintDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>unmanaged</c> generic constraint.</summary>
    string UnmanagedConstraintDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>new()</c> generic constraint.</summary>
    string NewConstraintDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>notnull</c> generic constraint.</summary>
    string NotNullConstraintDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# <c>enum</c> underlying types.</summary>
    string EnumBaseTypeDocUrl { get; }

    /// <summary>Gets the Microsoft Learn API URL for <see cref="int"/> (<c>System.Int32</c>).</summary>
    string DefaultInt32DocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for C# reflection and attributes.</summary>
    string AttributesDocUrl { get; }

    /// <summary>Gets the Microsoft Learn API URL for <see cref="ObsoleteAttribute"/>.</summary>
    string ObsoleteAttributeDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>allows ref struct</c> generic anti-constraint.</summary>
    string AllowsRefStructConstraintDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>scoped</c> ref modifier.</summary>
    string ScopedModifierDocUrl { get; }

    /// <summary>Gets the Microsoft Learn API URL for <c>ExperimentalAttribute</c>.</summary>
    string ExperimentalAttributeDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>record</c> keyword.</summary>
    string RecordKeywordDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>readonly</c> keyword.</summary>
    string ReadOnlyKeywordDocUrl { get; }

    /// <summary>Gets the Microsoft Learn URL for the C# <c>ref struct</c> declaration.</summary>
    string RefStructKeywordDocUrl { get; }

    /// <summary>
    /// Formats a C# type category (<see cref="TypeCategory"/>) as a Markdown link to its Microsoft Learn language reference documentation.
    /// </summary>
    /// <param name="typeCategory">The strongly typed C# category.</param>
    /// <returns>A formatted Markdown link if recognized; otherwise inline code text.</returns>
    string FormatTypeCategoryLink(TypeCategory typeCategory);

    /// <summary>
    /// Formats an access modifier keyword (such as <c>public</c>, <c>protected</c>, <c>internal</c>, or <c>private</c>)
    /// as a Markdown link to Microsoft Learn documentation.
    /// </summary>
    /// <param name="accessModifier">The access modifier token.</param>
    /// <returns>A formatted Markdown link or inline code token.</returns>
    string FormatAccessModifierLink(string accessModifier);

    /// <summary>
    /// Formats a constructor kind string (such as <c>Default</c>, <c>Primary (...)</c>, or <c>Overloaded (...)</c>)
    /// with a Markdown link to Microsoft Learn documentation.
    /// </summary>
    /// <param name="constructorKind">The constructor classification string.</param>
    /// <returns>Formatted Markdown describing the constructor classification.</returns>
    string FormatConstructorKindLink(string constructorKind);

    /// <summary>
    /// Formats generic type parameter constraints, linking C# constraint keywords (<c>class</c>, <c>struct</c>,
    /// <c>unmanaged</c>, <c>new()</c>, <c>notnull</c>) to Microsoft Learn documentation.
    /// </summary>
    /// <param name="rawConstraints">The raw or pre-resolved constraint text.</param>
    /// <returns>Formatted Markdown cell text for the constraints column.</returns>
    string FormatGenericConstraintsCell(string? rawConstraints);

    /// <summary>
    /// Formats a GitHub-styled warning or caution alert block when an API element is marked with <see cref="ObsoleteAttribute"/>.
    /// </summary>
    /// <param name="isObsolete">Whether the element is marked obsolete.</param>
    /// <param name="obsoleteMessage">The custom obsolete message, if any.</param>
    /// <param name="isObsoleteError">Whether using the obsolete element causes a compiler error.</param>
    /// <param name="diagnosticId">Optional diagnostic identifier from <c>[Obsolete(DiagnosticId = ...)]</c>.</param>
    /// <param name="urlFormat">Optional format template URL from <c>[Obsolete(UrlFormat = ...)]</c>.</param>
    /// <returns>The Markdown alert block, or an empty string when not obsolete.</returns>
    string FormatObsoleteWarningBlock(
        bool isObsolete,
        string? obsoleteMessage,
        bool isObsoleteError,
        string? diagnosticId = null,
        string? urlFormat = null);

    /// <summary>
    /// Formats a GitHub-styled important alert block when an API element is marked with <c>[Experimental]</c>.
    /// </summary>
    /// <param name="isExperimental">Whether the element is marked experimental.</param>
    /// <param name="diagnosticId">The experimental diagnostic identifier.</param>
    /// <param name="urlFormat">Optional format template URL for the diagnostic identifier.</param>
    /// <returns>The Markdown alert block, or an empty string when not experimental.</returns>
    string FormatExperimentalWarningBlock(bool isExperimental, string? diagnosticId, string? urlFormat);

    /// <summary>
    /// Formats a Markdown diagnostic identifier badge (linked when <paramref name="urlFormat"/> is valid, or inline code otherwise).
    /// </summary>
    /// <param name="diagnosticId">Optional diagnostic identifier (for example <c>"DEMO002"</c>).</param>
    /// <param name="urlFormat">Optional format template URL accepting <c>{0}</c> as the diagnostic ID.</param>
    /// <returns>The formatted badge prefixed with a space, or an empty string when <paramref name="diagnosticId"/> is missing.</returns>
    string FormatDiagnosticBadge(string? diagnosticId, string? urlFormat = null);

    /// <summary>
    /// Formats an attribute entry as inline code when it does not already contain a Markdown link or backticks.
    /// </summary>
    /// <param name="rawAttribute">The raw or linked attribute representation.</param>
    /// <returns>Formatted attribute Markdown string.</returns>
    string FormatAttributeItem(string rawAttribute);

    /// <summary>
    /// Formats an enum underlying integral type display name as a Markdown link to its Microsoft Learn <c>System.*</c> page.
    /// </summary>
    /// <param name="underlyingTypeName">The underlying CLR simple type name (for example <c>Byte</c> or <c>Int32</c>).</param>
    /// <returns>A formatted Markdown link to Microsoft Learn.</returns>
    string FormatEnumBaseTypeLink(string underlyingTypeName);

    /// <summary>
    /// Formats a member's visibility modifier along with any <c>static</c> and <c>async</c> modifier links.
    /// </summary>
    /// <param name="memberModel">The member documentation model.</param>
    /// <returns>Formatted Markdown modifier sequence.</returns>
    string FormatMemberVisibility(MemberDocumentationModel memberModel);
}
