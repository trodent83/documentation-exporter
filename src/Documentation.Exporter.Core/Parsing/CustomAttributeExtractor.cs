using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Documentation.Exporter.Abstractions.Resolution;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Default implementation of <see cref="ICustomAttributeExtractor"/> using <see cref="ITypeLinkResolver"/>
/// to resolve documentation links for applied custom attribute types.
/// </summary>
/// <param name="typeLinkResolver">The type link resolver used to link attribute types.</param>
public sealed class CustomAttributeExtractor(ITypeLinkResolver typeLinkResolver) : ICustomAttributeExtractor
{
    private static readonly HashSet<string> ExcludedCompilerAttributeNames =
    [
        with(StringComparer.Ordinal),
        "CompilerGeneratedAttribute",
        "NullableAttribute",
        "NullableContextAttribute",
        "ExtensionAttribute",
        "AsyncStateMachineAttribute",
        "IteratorStateMachineAttribute",
        "ObsoleteAttribute",
        "DebuggerBrowsableAttribute",
        "DebuggerStepThroughAttribute",
        "SuppressMessageAttribute",
        "RefSafetyRulesAttribute",
        "IsReadOnlyAttribute",
        "IsByRefLikeAttribute",
        "ParamArrayAttribute",
        "OptionalAttribute",
        "RequiredMemberAttribute",
        "CompilerFeatureRequiredAttribute",
        "IsUnmanagedAttribute",
        "DynamicAttribute",
        "TupleElementNamesAttribute",
        "AsyncIteratorStateMachineAttribute",
        "ParamCollectionAttribute",
        "ExtensionMarkerAttribute",
        "ExtensionMarkerNameAttribute",
        "NativeIntegerAttribute",
        "FixedBufferAttribute",
        "ExperimentalAttribute",
        "DefaultMemberAttribute"
    ];

    private static readonly HashSet<string> ExcludedParameterAttributeNames =
    [
        with(StringComparer.Ordinal),
        .. ExcludedCompilerAttributeNames,
        "InAttribute",
        "OutAttribute",
        "CallerMemberNameAttribute",
        "CallerFilePathAttribute",
        "CallerLineNumberAttribute",
        "CallerArgumentExpressionAttribute",
        "EnumeratorCancellationAttribute",
        "ScopedRefAttribute",
        "RequiresLocationAttribute"
    ];

    private static readonly HashSet<string> ExcludedAssemblyAttributeNames =
    [
        with(StringComparer.Ordinal),
        .. ExcludedCompilerAttributeNames,
        "CompilationRelaxationsAttribute",
        "RuntimeCompatibilityAttribute",
        "DebuggableAttribute",
        "TargetFrameworkAttribute",
        "NullablePublicOnlyAttribute",
        "AssemblyCompanyAttribute",
        "AssemblyConfigurationAttribute",
        "AssemblyFileVersionAttribute",
        "AssemblyInformationalVersionAttribute",
        "AssemblyProductAttribute",
        "AssemblyTitleAttribute",
        "AssemblyVersionAttribute"
    ];

    private readonly ITypeLinkResolver linkResolver = ArgumentGuard.EnsureNotNull(typeLinkResolver);

    /// <inheritdoc />
    public List<string> BuildAppliedAttributes(MemberInfo memberInfo)
    {
        ArgumentNullException.ThrowIfNull(memberInfo);
        return FormatCustomAttributes(memberInfo.GetCustomAttributesData(), ExcludedCompilerAttributeNames);
    }

    /// <inheritdoc />
    public List<string> BuildAppliedAttributes(ParameterInfo parameterInfo)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);
        return FormatCustomAttributes(parameterInfo.GetCustomAttributesData(), ExcludedParameterAttributeNames);
    }

    /// <inheritdoc />
    public List<string> BuildAppliedAttributes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return FormatCustomAttributes(assembly.GetCustomAttributesData(), ExcludedAssemblyAttributeNames);
    }

    /// <inheritdoc />
    public bool IsEditorBrowsableNever(MemberInfo memberInfo)
    {
        ArgumentNullException.ThrowIfNull(memberInfo);

        return memberInfo.CustomAttributes.Any(candidateAttribute =>
            string.Equals(candidateAttribute.AttributeType.FullName, "System.ComponentModel.EditorBrowsableAttribute", StringComparison.Ordinal) &&
            candidateAttribute.ConstructorArguments.Count > 0 &&
            (Equals(candidateAttribute.ConstructorArguments[0].Value, 1) ||
             candidateAttribute.ConstructorArguments[0].Value?.ToString() == "Never"));
    }

    /// <inheritdoc />
    public (bool IsObsolete, string Message, bool IsError, string DiagnosticId, string UrlFormat) ExtractObsoleteMetadata(MemberInfo memberInfo)
    {
        ArgumentNullException.ThrowIfNull(memberInfo);

        var obsoleteAttributeData = FindAttributeDataByFullName(memberInfo, "System.ObsoleteAttribute");
        if (obsoleteAttributeData == null)
        {
            return (false, string.Empty, false, string.Empty, string.Empty);
        }

        var message = obsoleteAttributeData.ConstructorArguments.Count > 0
            ? obsoleteAttributeData.ConstructorArguments[0].Value as string ?? string.Empty
            : string.Empty;

        if (IsCompilerSynthesizedObsolete(message))
        {
            return (false, string.Empty, false, string.Empty, string.Empty);
        }

        var isError = obsoleteAttributeData.ConstructorArguments.Count > 1 &&
                      obsoleteAttributeData.ConstructorArguments[1].Value is true;
        var diagnosticId = ExtractNamedStringArgument(obsoleteAttributeData, "DiagnosticId");
        var urlFormat = ExtractNamedStringArgument(obsoleteAttributeData, "UrlFormat");

        return (true, message, isError, diagnosticId, urlFormat);
    }

    private static bool IsCompilerSynthesizedObsolete(string? message) =>
        message != null &&
        (message.StartsWith("Constructors of types with required members are not supported", StringComparison.Ordinal) ||
         message.StartsWith("Types with embedded references are not supported", StringComparison.Ordinal));

    /// <inheritdoc />
    public (bool IsExperimental, string DiagnosticId, string UrlFormat) ExtractExperimentalMetadata(MemberInfo memberInfo)
    {
        ArgumentNullException.ThrowIfNull(memberInfo);

        var experimentalAttributeData = FindAttributeDataByFullName(
            memberInfo,
            "System.Diagnostics.CodeAnalysis.ExperimentalAttribute");

        if (experimentalAttributeData == null)
        {
            return (false, string.Empty, string.Empty);
        }

        var diagnosticId = experimentalAttributeData.ConstructorArguments.Count > 0
            ? experimentalAttributeData.ConstructorArguments[0].Value as string ?? string.Empty
            : string.Empty;
        var urlFormat = ExtractNamedStringArgument(experimentalAttributeData, "UrlFormat");

        return (true, diagnosticId, urlFormat);
    }

    private static CustomAttributeData? FindAttributeDataByFullName(MemberInfo memberInfo, string attributeFullName) =>
        memberInfo.GetCustomAttributesData()
            .FirstOrDefault(attributeData => string.Equals(
                attributeData.AttributeType.FullName,
                attributeFullName,
                StringComparison.Ordinal));

    private static string ExtractNamedStringArgument(CustomAttributeData attributeData, string memberName) =>
        attributeData.NamedArguments
            .FirstOrDefault(namedArgument => string.Equals(namedArgument.MemberName, memberName, StringComparison.Ordinal))
            .TypedValue.Value as string ?? string.Empty;

    /// <inheritdoc />
    public string ExtractCallerInformationAttribute(ParameterInfo parameterInfo) =>
        GetCallerInformationAttribute(parameterInfo);

    /// <summary>
    /// Extracts caller-information attribute syntax (such as <c>CallerMemberName</c> or <c>CallerArgumentExpression</c>) from a parameter.
    /// </summary>
    /// <param name="parameterInfo">The reflected parameter metadata.</param>
    /// <returns>Formatted attribute string, or empty when none is applied.</returns>
    public static string GetCallerInformationAttribute(ParameterInfo parameterInfo)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);

        if (parameterInfo.GetCustomAttribute<CallerMemberNameAttribute>() != null)
        {
            return "CallerMemberName";
        }

        if (parameterInfo.GetCustomAttribute<CallerFilePathAttribute>() != null)
        {
            return "CallerFilePath";
        }

        if (parameterInfo.GetCustomAttribute<CallerLineNumberAttribute>() != null)
        {
            return "CallerLineNumber";
        }

        var callerExpressionAttribute = parameterInfo.GetCustomAttribute<CallerArgumentExpressionAttribute>();
        return callerExpressionAttribute != null
            ? $"CallerArgumentExpression(\"{callerExpressionAttribute.ParameterName}\")"
            : string.Empty;
    }

    private List<string> FormatCustomAttributes(
        IEnumerable<CustomAttributeData> customAttributes,
        HashSet<string> excludedAttributeNames)
    {
        List<string> formattedAttributes = [];
        foreach (var attributeData in customAttributes)
        {
            var attributeType = attributeData.AttributeType;
            if (excludedAttributeNames.Contains(attributeType.Name))
            {
                continue;
            }

            var resolvedLink = ResolveAttributeLink(attributeType);
            var argumentTokens = FormatAttributeArguments(attributeData);
            var formattedAttribute = argumentTokens.Count > 0
                ? $"{resolvedLink}`({string.Join(", ", argumentTokens)})`"
                : resolvedLink;
            formattedAttributes.Add(formattedAttribute);
        }

        return formattedAttributes;
    }

    private string ResolveAttributeLink(Type attributeType)
    {
        if (!attributeType.IsGenericType)
        {
            var fullTypeName = attributeType.FullName ?? attributeType.Name;
            return linkResolver.ResolveLink(fullTypeName);
        }

        var genericTypeDefinition = attributeType.GetGenericTypeDefinition();
        var definitionFullName = genericTypeDefinition.FullName ?? genericTypeDefinition.Name;
        var resolvedOuterLink = linkResolver.ResolveLink(definitionFullName);
        var resolvedArguments = attributeType.GetGenericArguments().Select(ResolveAttributeLink);
        return $"{resolvedOuterLink}<{string.Join(", ", resolvedArguments)}>";
    }

    private static List<string> FormatAttributeArguments(CustomAttributeData attributeData)
    {
        List<string> argumentTokens = [];
        foreach (var constructorArgument in attributeData.ConstructorArguments)
        {
            var formattedValue = FormatTypedArgumentValue(constructorArgument, omitEmptyString: true);
            if (!string.IsNullOrEmpty(formattedValue))
            {
                argumentTokens.Add(formattedValue);
            }
        }

        foreach (var namedArgument in attributeData.NamedArguments)
        {
            if (string.Equals(namedArgument.MemberName, "ErrorMessageResourceType", StringComparison.Ordinal) ||
                string.Equals(namedArgument.MemberName, "ErrorMessageResourceName", StringComparison.Ordinal))
            {
                continue;
            }

            var formattedValue = FormatTypedArgumentValue(namedArgument.TypedValue, omitEmptyString: false);
            argumentTokens.Add($"{namedArgument.MemberName} = {formattedValue}");
        }

        return argumentTokens;
    }

    private static string FormatTypedArgumentValue(CustomAttributeTypedArgument typedArgument, bool omitEmptyString)
    {
        var rawValue = typedArgument.Value;
        if (rawValue is string textValue)
        {
            return omitEmptyString && string.IsNullOrEmpty(textValue)
                ? string.Empty
                : $"\"{EscapeAttributeString(textValue)}\"";
        }

        if (rawValue is bool booleanValue)
        {
            return booleanValue ? "true" : "false";
        }

        if (rawValue is Type reflectedType)
        {
            return $"typeof({FormatTypeofArgument(reflectedType)})";
        }

        if (rawValue is null)
        {
            return omitEmptyString ? string.Empty : "null";
        }

        if (typedArgument.ArgumentType.IsEnum)
        {
            return FormatEnumArgumentValue(typedArgument.ArgumentType, rawValue);
        }

        if (rawValue is IReadOnlyCollection<CustomAttributeTypedArgument> arrayElements)
        {
            var formattedElements = arrayElements
                .Select(element => FormatTypedArgumentValue(element, omitEmptyString: false));
            return $"[{string.Join(", ", formattedElements)}]";
        }

        return Convert.ToString(rawValue, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatTypeofArgument(Type reflectedType)
    {
        if (reflectedType.IsArray)
        {
            var elementType = reflectedType.GetElementType();
            var rankCommas = new string(',', reflectedType.GetArrayRank() - 1);
            return elementType != null
                ? $"{FormatTypeofArgument(elementType)}[{rankCommas}]"
                : CSharpSignatureFormatter.ExtractCleanTypeName(reflectedType);
        }

        var cleanName = CSharpSignatureFormatter.ExtractCleanTypeName(reflectedType);
        if (!reflectedType.IsGenericType || reflectedType.IsGenericTypeDefinition)
        {
            return cleanName;
        }

        var formattedTypeArgs = reflectedType
            .GetGenericArguments()
            .Select(FormatTypeofArgument);
        return $"{cleanName}<{string.Join(", ", formattedTypeArgs)}>";
    }

    private static string EscapeAttributeString(string rawText) =>
        rawText
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

    private static string FormatEnumArgumentValue(Type enumType, object rawValue)
    {
        var formattedNames = Enum.Format(enumType, rawValue, "G");
        if (string.IsNullOrWhiteSpace(formattedNames))
        {
            return Convert.ToString(rawValue, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        var tokens = formattedNames
            .Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var formattedTokens = tokens
            .Select(token => char.IsLetter(token[0]) ? $"{enumType.Name}.{token}" : token);
        return string.Join(" | ", formattedTokens);
    }
}
