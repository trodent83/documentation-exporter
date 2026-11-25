using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Default implementation of <see cref="IMicrosoftLearnReferenceRegistry"/> providing canonical Microsoft Learn URLs
/// and Markdown link formatters.
/// </summary>
public sealed class MicrosoftLearnReferenceRegistry : IMicrosoftLearnReferenceRegistry
{
    private const string AccessModifiersUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers";
    private const string ConstructorsUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constructors";
    private const string DefaultConstructorsUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#default-constructors";
    private const string PrimaryConstructorsUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#primary-constructors";
    private const string AbstractKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/abstract";
    private const string SealedKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/sealed";
    private const string StaticKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/static";
    private const string AsyncKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/async";
    private const string AsyncProgrammingUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/";
    private const string AsyncEnumerableUrl = "https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1";
    private const string EnumeratorCancellationUrl = "https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.enumeratorcancellationattribute";
    private const string CancellationUrl = "https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads";
    private const string OptionalParametersUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/named-and-optional-arguments#optional-arguments";
    private const string ParamsModifierUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#params-modifier";
    private const string RefModifierUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref";
    private const string OutModifierUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#out-parameter-modifier";
    private const string InModifierUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#in-parameter-modifier";
    private const string RefReadonlyModifierUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#ref-readonly-modifier";
    private const string ExtensionMethodsUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods";
    private const string NullableReferencesUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references";
    private const string CallerInformationUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information";
    private const string ClassConstraintUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class";
    private const string StructConstraintUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct";
    private const string UnmanagedConstraintUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/unmanaged-types";
    private const string NewConstraintUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/new-constraint";
    private const string NotNullConstraintUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters";
    private const string EnumBaseTypeUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum";
    private const string DefaultInt32Url = "https://learn.microsoft.com/en-us/dotnet/api/system.int32";
    private const string AttributesUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/";
    private const string ObsoleteAttributeUrl = "https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute";
    private const string AllowsRefStructConstraintUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters#allows-ref-struct";
    private const string ScopedModifierUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/declarations#scoped-ref";
    private const string ExperimentalAttributeUrl = "https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute";
    private const string RecordKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record";
    private const string ReadOnlyKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly";
    private const string RefStructKeywordUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct";

    private static readonly Dictionary<TypeCategory, string> CategoryDocumentationUrls = new()
    {
        [TypeCategory.Class] = ClassConstraintUrl,
        [TypeCategory.Interface] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/interface",
        [TypeCategory.Struct] = StructConstraintUrl,
        [TypeCategory.Enum] = EnumBaseTypeUrl,
        [TypeCategory.Delegate] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#the-delegate-type"
    };

    private static readonly Dictionary<string, string> AccessKeywordUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["public"] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/public",
        ["protected"] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected",
        ["internal"] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/internal",
        ["private"] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/private",
        ["explicit"] = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation",
        ["interface"] = "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/interface"
    };

    /// <inheritdoc />
    public string AccessModifiersDocUrl => AccessModifiersUrl;

    /// <inheritdoc />
    public string ConstructorsDocUrl => ConstructorsUrl;

    /// <inheritdoc />
    public string DefaultConstructorsDocUrl => DefaultConstructorsUrl;

    /// <inheritdoc />
    public string PrimaryConstructorsDocUrl => PrimaryConstructorsUrl;

    /// <inheritdoc />
    public string AbstractKeywordDocUrl => AbstractKeywordUrl;

    /// <inheritdoc />
    public string SealedKeywordDocUrl => SealedKeywordUrl;

    /// <inheritdoc />
    public string StaticKeywordDocUrl => StaticKeywordUrl;

    /// <inheritdoc />
    public string AsyncKeywordDocUrl => AsyncKeywordUrl;

    /// <inheritdoc />
    public string AsyncProgrammingDocUrl => AsyncProgrammingUrl;

    /// <inheritdoc />
    public string AsyncEnumerableDocUrl => AsyncEnumerableUrl;

    /// <inheritdoc />
    public string EnumeratorCancellationDocUrl => EnumeratorCancellationUrl;

    /// <inheritdoc />
    public string CancellationDocUrl => CancellationUrl;

    /// <inheritdoc />
    public string OptionalParametersDocUrl => OptionalParametersUrl;

    /// <inheritdoc />
    public string ParamsModifierDocUrl => ParamsModifierUrl;

    /// <inheritdoc />
    public string RefModifierDocUrl => RefModifierUrl;

    /// <inheritdoc />
    public string OutModifierDocUrl => OutModifierUrl;

    /// <inheritdoc />
    public string InModifierDocUrl => InModifierUrl;

    /// <inheritdoc />
    public string RefReadonlyModifierDocUrl => RefReadonlyModifierUrl;

    /// <inheritdoc />
    public string ExtensionMethodsDocUrl => ExtensionMethodsUrl;

    /// <inheritdoc />
    public string NullableReferencesDocUrl => NullableReferencesUrl;

    /// <inheritdoc />
    public string CallerInformationDocUrl => CallerInformationUrl;

    /// <inheritdoc />
    public string ClassConstraintDocUrl => ClassConstraintUrl;

    /// <inheritdoc />
    public string StructConstraintDocUrl => StructConstraintUrl;

    /// <inheritdoc />
    public string UnmanagedConstraintDocUrl => UnmanagedConstraintUrl;

    /// <inheritdoc />
    public string NewConstraintDocUrl => NewConstraintUrl;

    /// <inheritdoc />
    public string NotNullConstraintDocUrl => NotNullConstraintUrl;

    /// <inheritdoc />
    public string EnumBaseTypeDocUrl => EnumBaseTypeUrl;

    /// <inheritdoc />
    public string DefaultInt32DocUrl => DefaultInt32Url;

    /// <inheritdoc />
    public string AttributesDocUrl => AttributesUrl;

    /// <inheritdoc />
    public string ObsoleteAttributeDocUrl => ObsoleteAttributeUrl;

    /// <inheritdoc />
    public string AllowsRefStructConstraintDocUrl => AllowsRefStructConstraintUrl;

    /// <inheritdoc />
    public string ScopedModifierDocUrl => ScopedModifierUrl;

    /// <inheritdoc />
    public string ExperimentalAttributeDocUrl => ExperimentalAttributeUrl;

    /// <inheritdoc />
    public string RecordKeywordDocUrl => RecordKeywordUrl;

    /// <inheritdoc />
    public string ReadOnlyKeywordDocUrl => ReadOnlyKeywordUrl;

    /// <inheritdoc />
    public string RefStructKeywordDocUrl => RefStructKeywordUrl;

    /// <inheritdoc />
    public string FormatTypeCategoryLink(TypeCategory typeCategory) =>
        CategoryDocumentationUrls.TryGetValue(typeCategory, out var documentationUrl)
            ? $"[{typeCategory}]({documentationUrl})"
            : $"`{typeCategory}`";

    /// <inheritdoc />
    public string FormatAccessModifierLink(string accessModifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessModifier);

        var keywordTokens = accessModifier.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var formattedTokens = keywordTokens.Select(keywordToken =>
            AccessKeywordUrls.TryGetValue(keywordToken, out var keywordUrl)
                ? $"[{keywordToken}]({keywordUrl})"
                : $"`{keywordToken}`");
        return string.Join(" ", formattedTokens);
    }

    /// <inheritdoc />
    public string FormatConstructorKindLink(string constructorKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(constructorKind);

        if (string.Equals(constructorKind, "Default", StringComparison.OrdinalIgnoreCase))
        {
            return $"[Default]({DefaultConstructorsUrl})";
        }

        if (constructorKind.StartsWith("Primary", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = constructorKind["Primary".Length..].Trim();
            return string.IsNullOrEmpty(suffix)
                ? $"[Primary]({PrimaryConstructorsUrl})"
                : $"[Primary]({PrimaryConstructorsUrl}) `{suffix}`";
        }

        if (constructorKind.StartsWith("Overloaded", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = constructorKind["Overloaded".Length..].Trim();
            return string.IsNullOrEmpty(suffix)
                ? $"[Overloaded]({ConstructorsUrl})"
                : $"[Overloaded]({ConstructorsUrl}) `{suffix}`";
        }

        return $"`{constructorKind}`";
    }

    /// <inheritdoc />
    public string FormatGenericConstraintsCell(string? rawConstraints) =>
        string.IsNullOrWhiteSpace(rawConstraints)
            ? $"`{MarkdownGenerationResources.NoConstraintsLabel}`"
            : rawConstraints
                .Replace("`class`", $"[class]({ClassConstraintUrl})", StringComparison.Ordinal)
                .Replace("`struct`", $"[struct]({StructConstraintUrl})", StringComparison.Ordinal)
                .Replace("`unmanaged`", $"[unmanaged]({UnmanagedConstraintUrl})", StringComparison.Ordinal)
                .Replace("`new()`", $"[new()]({NewConstraintUrl})", StringComparison.Ordinal)
                .Replace("`notnull`", $"[notnull]({NotNullConstraintUrl})", StringComparison.Ordinal)
                .Replace("`allows ref struct`", $"[allows ref struct]({AllowsRefStructConstraintUrl})", StringComparison.Ordinal);

    /// <inheritdoc />
    public string FormatObsoleteWarningBlock(
        bool isObsolete,
        string? obsoleteMessage,
        bool isObsoleteError,
        string? diagnosticId = null,
        string? urlFormat = null)
    {
        if (!isObsolete)
        {
            return string.Empty;
        }

        var alertKind = isObsoleteError ? "CAUTION" : "WARNING";
        var displayMessage = string.IsNullOrWhiteSpace(obsoleteMessage)
            ? MarkdownGenerationResources.ObsoleteDefaultMessage
            : obsoleteMessage.Trim();
        var diagnosticBadge = FormatDiagnosticBadge(diagnosticId, urlFormat);

        StringBuilder alertBuilder = new();
        _ = alertBuilder.AppendLine(CultureInfo.InvariantCulture, $"> [!{alertKind}]");
        _ = alertBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"> **[{MarkdownGenerationResources.ObsoleteWarningTitle}]({ObsoleteAttributeUrl}){diagnosticBadge}:** {displayMessage}");
        _ = alertBuilder.AppendLine();
        return alertBuilder.ToString();
    }

    /// <inheritdoc />
    public string FormatExperimentalWarningBlock(bool isExperimental, string? diagnosticId, string? urlFormat)
    {
        if (!isExperimental)
        {
            return string.Empty;
        }

        var diagnosticBadge = FormatDiagnosticBadge(diagnosticId, urlFormat);

        StringBuilder alertBuilder = new();
        _ = alertBuilder.AppendLine("> [!IMPORTANT]");
        _ = alertBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $"> **[{MarkdownGenerationResources.ExperimentalWarningTitle}]({ExperimentalAttributeUrl}){diagnosticBadge}:** {MarkdownGenerationResources.ExperimentalDefaultMessage}");
        _ = alertBuilder.AppendLine();
        return alertBuilder.ToString();
    }

    /// <inheritdoc />
    public string FormatDiagnosticBadge(string? diagnosticId, string? urlFormat = null)
    {
        if (string.IsNullOrWhiteSpace(diagnosticId))
        {
            return string.Empty;
        }

        var trimmedDiagnosticId = diagnosticId.Trim();
        var resolvedDiagnosticUrl = ResolveDiagnosticUrl(trimmedDiagnosticId, urlFormat);
        return !string.IsNullOrWhiteSpace(resolvedDiagnosticUrl)
            ? $" ([{trimmedDiagnosticId}]({resolvedDiagnosticUrl}))"
            : $" (`{trimmedDiagnosticId}`)";
    }

    private static string? ResolveDiagnosticUrl(string diagnosticId, string? urlFormat)
    {
        if (string.IsNullOrWhiteSpace(urlFormat))
        {
            return null;
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, urlFormat.Trim(), diagnosticId);
        }
        catch (FormatException)
        {
            return urlFormat.Trim();
        }
    }

    /// <inheritdoc />
    public string FormatAttributeItem(string rawAttribute)
    {
        ArgumentNullException.ThrowIfNull(rawAttribute);
        return rawAttribute.Contains("](", StringComparison.Ordinal) || rawAttribute.StartsWith('`')
            ? rawAttribute
            : $"`[{rawAttribute}]`";
    }

    /// <inheritdoc />
    public string FormatEnumBaseTypeLink(string underlyingTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(underlyingTypeName);
        return $"[{underlyingTypeName}](https://learn.microsoft.com/en-us/dotnet/api/system.{underlyingTypeName.ToLowerInvariant()})";
    }

    /// <inheritdoc />
    public string FormatMemberVisibility(MemberDocumentationModel memberModel)
    {
        ArgumentNullException.ThrowIfNull(memberModel);

        var modifier = string.IsNullOrWhiteSpace(memberModel.VisibilityModifier)
            ? "public"
            : memberModel.VisibilityModifier.Trim();
        var formattedModifier = FormatAccessModifierLink(modifier);

        if (memberModel.IsStaticMember)
        {
            formattedModifier = $"{formattedModifier} [static]({StaticKeywordUrl})";
        }

        if (memberModel.IsAsyncMethod)
        {
            formattedModifier = $"{formattedModifier} [async]({AsyncKeywordUrl})";
        }

        return formattedModifier;
    }
}
