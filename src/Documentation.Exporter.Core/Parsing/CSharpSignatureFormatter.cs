using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Default implementation of <see cref="ICSharpSignatureFormatter"/> generating C# 9–14 signatures and display names.
/// </summary>
public sealed class CSharpSignatureFormatter : ICSharpSignatureFormatter
{
    private static readonly Dictionary<string, string> StandardOperatorTokens = new(StringComparer.Ordinal)
    {
        ["op_UnaryPlus"] = "+",
        ["op_UnaryNegation"] = "-",
        ["op_CheckedUnaryNegation"] = "checked -",
        ["op_LogicalNot"] = "!",
        ["op_OnesComplement"] = "~",
        ["op_Increment"] = "++",
        ["op_CheckedIncrement"] = "checked ++",
        ["op_Decrement"] = "--",
        ["op_CheckedDecrement"] = "checked --",
        ["op_True"] = "true",
        ["op_False"] = "false",
        ["op_Addition"] = "+",
        ["op_CheckedAddition"] = "checked +",
        ["op_Subtraction"] = "-",
        ["op_CheckedSubtraction"] = "checked -",
        ["op_Multiply"] = "*",
        ["op_CheckedMultiply"] = "checked *",
        ["op_Division"] = "/",
        ["op_CheckedDivision"] = "checked /",
        ["op_Modulus"] = "%",
        ["op_BitwiseAnd"] = "&",
        ["op_BitwiseOr"] = "|",
        ["op_ExclusiveOr"] = "^",
        ["op_LeftShift"] = "<<",
        ["op_RightShift"] = ">>",
        ["op_UnsignedRightShift"] = ">>>",
        ["op_Equality"] = "==",
        ["op_Inequality"] = "!=",
        ["op_LessThan"] = "<",
        ["op_GreaterThan"] = ">",
        ["op_LessThanOrEqual"] = "<=",
        ["op_GreaterThanOrEqual"] = ">=",
        ["op_AdditionAssignment"] = "+=",
        ["op_CheckedAdditionAssignment"] = "checked +=",
        ["op_SubtractionAssignment"] = "-=",
        ["op_CheckedSubtractionAssignment"] = "checked -=",
        ["op_MultiplicationAssignment"] = "*=",
        ["op_CheckedMultiplicationAssignment"] = "checked *=",
        ["op_DivisionAssignment"] = "/=",
        ["op_CheckedDivisionAssignment"] = "checked /=",
        ["op_ModulusAssignment"] = "%=",
        ["op_BitwiseAndAssignment"] = "&=",
        ["op_BitwiseOrAssignment"] = "|=",
        ["op_ExclusiveOrAssignment"] = "^=",
        ["op_LeftShiftAssignment"] = "<<=",
        ["op_RightShiftAssignment"] = ">>=",
        ["op_UnsignedRightShiftAssignment"] = ">>>=",
        ["op_IncrementAssignment"] = "++",
        ["op_CheckedIncrementAssignment"] = "checked ++",
        ["op_DecrementAssignment"] = "--",
        ["op_CheckedDecrementAssignment"] = "checked --"
    };

    /// <summary>
    /// Extracts the simple type name without generic arity backtick suffixes (for example <c>"List"</c>).
    /// </summary>
    /// <param name="targetType">The target runtime type.</param>
    /// <returns>Clean simple type name without generic arity suffixes.</returns>
    public static string ExtractCleanTypeName(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        var rawName = targetType.Name;
        var backtickIndex = rawName.IndexOf('`');
        return backtickIndex > 0 ? rawName[..backtickIndex] : rawName;
    }

    /// <inheritdoc />
    public string GetCleanTypeName(Type targetType) => ExtractCleanTypeName(targetType);

    /// <inheritdoc />
    public string FormatDisplayTypeName(Type targetType) => FormatDisplayNameCore(targetType);

    /// <inheritdoc />
    public string FormatDisplayTypeName(NullabilityInfo nullabilityInfo)
    {
        ArgumentNullException.ThrowIfNull(nullabilityInfo);
        return FormatDisplayNameCore(nullabilityInfo);
    }

    /// <inheritdoc />
    public string FormatFullDisplayTypeName(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        var ownArguments = ExtractOwnGenericArguments(targetType);
        if (ownArguments.Length == 0)
        {
            return (targetType.FullName ?? targetType.Name).Replace('+', '.');
        }

        var genericDefinition = targetType.GetGenericTypeDefinition();
        var rawFullName = genericDefinition.FullName ?? genericDefinition.Name;
        var backtickIndex = rawFullName.IndexOf('`');
        var baseFullName = (backtickIndex > 0 ? rawFullName[..backtickIndex] : rawFullName).Replace('+', '.');
        var genericArgumentNames = ownArguments.Select(FormatDisplayNameCore);
        return $"{baseFullName}<{string.Join(", ", genericArgumentNames)}>";
    }

    /// <inheritdoc />
    public string BuildTypeDeclarationSignature(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (typeof(Delegate).IsAssignableFrom(targetType))
        {
            return BuildDelegateDeclarationSignature(targetType);
        }

        StringBuilder signatureBuilder = new();
        _ = signatureBuilder.Append(FormatTypeVisibility(targetType));

        var isStatic = targetType.IsAbstract && targetType.IsSealed && !targetType.IsInterface;
        var isRecord = IsRecordType(targetType);
        var isReadOnly = IsReadOnlyStruct(targetType);
        var isRefStruct = IsRefStruct(targetType);

        if (isStatic)
        {
            _ = signatureBuilder.Append("static ");
        }
        else if (targetType.IsAbstract && !targetType.IsInterface)
        {
            _ = signatureBuilder.Append("abstract ");
        }
        else if (targetType.IsSealed && !targetType.IsValueType && !targetType.IsEnum)
        {
            _ = signatureBuilder.Append("sealed ");
        }

        if (isReadOnly)
        {
            _ = signatureBuilder.Append("readonly ");
        }

        if (isRefStruct)
        {
            _ = signatureBuilder.Append("ref ");
        }

        if (isRecord)
        {
            _ = signatureBuilder.Append(targetType.IsValueType ? "record struct " : "record ");
        }
        else
        {
            var typeKeyword = ResolveTypeCategory(targetType).ToString().ToLower(CultureInfo.InvariantCulture);
            _ = signatureBuilder.Append(typeKeyword);
            _ = signatureBuilder.Append(' ');
        }

        _ = signatureBuilder.Append(ExtractCleanTypeName(targetType));
        _ = signatureBuilder.Append(FormatTypeGenericParameterList(targetType));

        if (targetType.IsEnum)
        {
            var underlyingEnumType = Enum.GetUnderlyingType(targetType);
            _ = signatureBuilder.Append(" : ");
            _ = signatureBuilder.Append(FormatDisplayNameCore(underlyingEnumType));
            return signatureBuilder.ToString();
        }

        if (TryGetPrimaryConstructor(targetType, out var primaryConstructor))
        {
            NullabilityInfoContext nullabilityContext = new();
            _ = signatureBuilder.Append('(');
            var parameters = primaryConstructor.GetParameters();
            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                if (parameterIndex > 0)
                {
                    _ = signatureBuilder.Append(", ");
                }

                _ = signatureBuilder.Append(FormatParameterSignatureCore(
                    parameters[parameterIndex],
                    isExtensionMethodFirstParameter: false,
                    nullabilityContext));
            }

            _ = signatureBuilder.Append(')');
        }

        _ = signatureBuilder.Append(FormatTypeInheritanceClause(targetType, isStatic, isRecord));
        _ = signatureBuilder.Append(FormatGenericWhereClauses(ExtractOwnGenericArguments(targetType)));
        return signatureBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildConstructorSignature(ConstructorInfo constructorInfo, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(constructorInfo);
        ArgumentNullException.ThrowIfNull(targetType);

        NullabilityInfoContext nullabilityContext = new();
        StringBuilder stringBuilder = new();
        AppendInlineMemberAttributes(stringBuilder, constructorInfo);
        _ = stringBuilder.Append(FormatMethodBaseVisibility(constructorInfo));
        _ = stringBuilder.Append(ExtractCleanTypeName(targetType));
        _ = stringBuilder.Append('(');

        var parameters = constructorInfo.GetParameters();
        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            if (parameterIndex > 0)
            {
                _ = stringBuilder.Append(", ");
            }

            _ = stringBuilder.Append(FormatParameterSignatureCore(
                parameters[parameterIndex],
                isExtensionMethodFirstParameter: false,
                nullabilityContext));
        }

        _ = stringBuilder.Append(')');
        return stringBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildPropertySignature(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        NullabilityInfoContext nullabilityContext = new();
        var getMethod = propertyInfo.GetGetMethod(true);
        var setMethod = propertyInfo.GetSetMethod(true);
        var representativeMethod = SelectMostAccessibleAccessor(getMethod, setMethod);
        var isExplicitInterfaceProperty = representativeMethod != null && IsExplicitInterfaceImplementation(representativeMethod);
        var propertyVisibility = representativeMethod != null
            ? FormatMethodBaseVisibility(representativeMethod)
            : "protected ";

        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(propertyVisibility);

        if (propertyInfo.IsDefined(typeof(RequiredMemberAttribute), inherit: false))
        {
            _ = stringBuilder.Append("required ");
        }

        if (representativeMethod != null && !isExplicitInterfaceProperty)
        {
            AppendMethodModifiers(stringBuilder, representativeMethod, isAsyncMethod: false);
        }

        var isReadOnlyMemberGetter = getMethod != null &&
                                     !getMethod.IsStatic &&
                                     propertyInfo.DeclaringType is { IsValueType: true } declaringValueType &&
                                     !IsReadOnlyStruct(declaringValueType) &&
                                     IsMethodMarkedReadOnly(getMethod);
        if (isReadOnlyMemberGetter && setMethod == null)
        {
            _ = stringBuilder.Append("readonly ");
        }

        if (propertyInfo.PropertyType.IsByRef)
        {
            var isReadOnlyReturn = getMethod != null && IsParameterMarkedReadOnly(getMethod.ReturnParameter);
            _ = stringBuilder.Append(isReadOnlyReturn ? "ref readonly " : "ref ");
        }

        var propertyTypeDisplay = FormatNullabilityWithCompilerMetadata(nullabilityContext.Create(propertyInfo), propertyInfo);
        _ = stringBuilder.Append(propertyTypeDisplay);
        _ = stringBuilder.Append(' ');

        var indexParameters = propertyInfo.GetIndexParameters();
        if (indexParameters.Length > 0)
        {
            var formattedIndexers = indexParameters.Select(parameter =>
                FormatParameterSignatureCore(parameter, isExtensionMethodFirstParameter: false, nullabilityContext));
            var indexerPrefix = isExplicitInterfaceProperty
                ? FormatExplicitInterfaceIndexerPrefix(propertyInfo.Name)
                : "this";
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{indexerPrefix}[{string.Join(", ", formattedIndexers)}]");
        }
        else
        {
            var propertyDisplayName = isExplicitInterfaceProperty
                ? FormatExplicitInterfaceMemberName(propertyInfo.Name)
                : propertyInfo.Name;
            _ = stringBuilder.Append(propertyDisplayName);
        }

        _ = stringBuilder.Append(" { ");

        if (getMethod != null)
        {
            var getPrefix = isExplicitInterfaceProperty
                ? string.Empty
                : FormatAccessorRestrictedVisibility(getMethod, representativeMethod);
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{getPrefix}get; ");
        }

        if (setMethod != null)
        {
            var setPrefix = isExplicitInterfaceProperty
                ? string.Empty
                : FormatAccessorRestrictedVisibility(setMethod, representativeMethod);
            var setKeyword = IsInitOnlySetter(setMethod) ? "init" : "set";
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{setPrefix}{setKeyword}; ");
        }

        _ = stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildMethodSignature(MethodInfo methodInfo, bool isAsyncMethod)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        if (IsFinalizerMethod(methodInfo))
        {
            return $"~{ExtractCleanTypeName(methodInfo.DeclaringType!)}()";
        }

        NullabilityInfoContext nullabilityContext = new();
        StringBuilder stringBuilder = new();
        AppendInlineMemberAttributes(stringBuilder, methodInfo);

        var isExplicitInterfaceMethod = IsExplicitInterfaceImplementation(methodInfo);
        _ = stringBuilder.Append(FormatMethodBaseVisibility(methodInfo));
        if (!isExplicitInterfaceMethod)
        {
            AppendMethodModifiers(stringBuilder, methodInfo, isAsyncMethod);
        }
        else if (isAsyncMethod)
        {
            _ = stringBuilder.Append("async ");
        }

        if (!methodInfo.IsStatic &&
            methodInfo.DeclaringType is { IsValueType: true } declaringValueType &&
            !IsReadOnlyStruct(declaringValueType) &&
            IsMethodMarkedReadOnly(methodInfo))
        {
            _ = stringBuilder.Append("readonly ");
        }

        var returnRefPrefix = methodInfo.ReturnType.IsByRef
            ? IsParameterMarkedReadOnly(methodInfo.ReturnParameter) ? "ref readonly " : "ref "
            : string.Empty;
        var returnTypeDisplay = FormatNullabilityWithCompilerMetadata(
            nullabilityContext.Create(methodInfo.ReturnParameter),
            methodInfo.ReturnParameter);
        var parameterListDisplay = FormatMethodParameterList(methodInfo, nullabilityContext);

        if (TryGetOperatorDisplayToken(methodInfo.Name, out var operatorToken, out var isConversionOperator))
        {
            if (isConversionOperator)
            {
                var conversionKind = string.Equals(methodInfo.Name, "op_Implicit", StringComparison.Ordinal)
                    ? "implicit"
                    : "explicit";
                var checkedPrefix = string.Equals(methodInfo.Name, "op_CheckedExplicit", StringComparison.Ordinal)
                    ? "checked "
                    : string.Empty;
                _ = stringBuilder.Append(
                    CultureInfo.InvariantCulture,
                    $"{conversionKind} operator {checkedPrefix}{returnTypeDisplay}({parameterListDisplay})");
            }
            else
            {
                _ = stringBuilder.Append(
                    CultureInfo.InvariantCulture,
                    $"{returnRefPrefix}{returnTypeDisplay} operator {operatorToken}({parameterListDisplay})");
            }

            return stringBuilder.ToString();
        }

        _ = stringBuilder.Append(returnRefPrefix);
        _ = stringBuilder.Append(returnTypeDisplay);
        _ = stringBuilder.Append(' ');
        _ = stringBuilder.Append(isExplicitInterfaceMethod
            ? FormatExplicitInterfaceMemberName(methodInfo.Name)
            : methodInfo.Name);

        if (methodInfo.IsGenericMethod)
        {
            var genericArgs = string.Join(", ", methodInfo.GetGenericArguments().Select(argument => argument.Name));
            _ = stringBuilder.Append('<');
            _ = stringBuilder.Append(genericArgs);
            _ = stringBuilder.Append('>');
        }

        _ = stringBuilder.Append('(');
        _ = stringBuilder.Append(parameterListDisplay);
        _ = stringBuilder.Append(')');

        if (methodInfo.IsGenericMethod)
        {
            _ = stringBuilder.Append(FormatGenericWhereClauses(methodInfo.GetGenericArguments()));
        }

        return stringBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildFieldSignature(FieldInfo fieldInfo, string fieldTypeName)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldTypeName);

        if (fieldInfo.GetCustomAttribute<FixedBufferAttribute>() is { } fixedBufferAttribute)
        {
            var fixedElementDisplay = FormatDisplayNameCore(fixedBufferAttribute.ElementType);
            return $"{FormatFieldVisibility(fieldInfo)}fixed {fixedElementDisplay} {fieldInfo.Name}[{fixedBufferAttribute.Length}]";
        }

        StringBuilder signatureBuilder = new();
        _ = signatureBuilder.Append(FormatFieldVisibility(fieldInfo));

        if (fieldInfo.IsDefined(typeof(RequiredMemberAttribute), inherit: false))
        {
            _ = signatureBuilder.Append("required ");
        }

        if (fieldInfo.IsLiteral)
        {
            _ = signatureBuilder.Append("const ");
        }
        else
        {
            if (fieldInfo.IsStatic)
            {
                _ = signatureBuilder.Append("static ");
            }

            if (fieldInfo.IsInitOnly)
            {
                _ = signatureBuilder.Append("readonly ");
            }
        }

        if (fieldInfo.FieldType.IsByRef)
        {
            var isReadOnlyRef = fieldInfo.IsDefined(typeof(IsReadOnlyAttribute), inherit: false) ||
                                fieldInfo.GetRequiredCustomModifiers()
                                    .Any(modifier => string.Equals(modifier.Name, "InAttribute", StringComparison.Ordinal));
            _ = signatureBuilder.Append(isReadOnlyRef ? "ref readonly " : "ref ");
        }

        _ = signatureBuilder.Append(fieldTypeName);
        _ = signatureBuilder.Append(' ');
        _ = signatureBuilder.Append(fieldInfo.Name);

        if (fieldInfo.IsLiteral)
        {
            var rawValue = fieldInfo.GetRawConstantValue();
            if (rawValue is string stringConstant)
            {
                _ = signatureBuilder.Append(CultureInfo.InvariantCulture, $" = \"{EscapeStringLiteral(stringConstant)}\"");
            }
            else if (rawValue is char charConstant)
            {
                _ = signatureBuilder.Append(CultureInfo.InvariantCulture, $" = '{EscapeCharLiteral(charConstant)}'");
            }
            else if (rawValue != null)
            {
                _ = signatureBuilder.Append(CultureInfo.InvariantCulture, $" = {rawValue}");
            }
        }

        return signatureBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildEventSignature(EventInfo eventInfo)
    {
        ArgumentNullException.ThrowIfNull(eventInfo);

        NullabilityInfoContext nullabilityContext = new();
        var addMethod = eventInfo.GetAddMethod(true);
        var isExplicitInterfaceEvent = addMethod != null && IsExplicitInterfaceImplementation(addMethod);
        StringBuilder signatureBuilder = new();
        _ = signatureBuilder.Append(addMethod != null ? FormatMethodBaseVisibility(addMethod) : "public ");

        if (addMethod != null && !isExplicitInterfaceEvent)
        {
            AppendMethodModifiers(signatureBuilder, addMethod, isAsyncMethod: false);
        }

        _ = signatureBuilder.Append("event ");
        var eventHandlerDisplay = eventInfo.EventHandlerType != null
            ? FormatNullabilityWithCompilerMetadata(nullabilityContext.Create(eventInfo), eventInfo)
            : "EventHandler";
        _ = signatureBuilder.Append(eventHandlerDisplay);
        _ = signatureBuilder.Append(' ');
        _ = signatureBuilder.Append(isExplicitInterfaceEvent
            ? FormatExplicitInterfaceMemberName(eventInfo.Name)
            : eventInfo.Name);
        return signatureBuilder.ToString();
    }

    /// <inheritdoc />
    public string FormatParameterSignature(ParameterInfo parameterInfo, bool isExtensionMethodFirstParameter)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);
        NullabilityInfoContext nullabilityContext = new();
        return FormatParameterSignatureCore(parameterInfo, isExtensionMethodFirstParameter, nullabilityContext);
    }

    /// <inheritdoc />
    public bool TryGetPrimaryConstructor(Type targetType, out ConstructorInfo primaryConstructor)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        primaryConstructor = null!;

        if (targetType.IsInterface || targetType.IsEnum || typeof(Delegate).IsAssignableFrom(targetType))
        {
            return false;
        }

        var visibleConstructors = GetVisibleConstructors(targetType);
        if (visibleConstructors.Length == 0)
        {
            return false;
        }

        var capturedPrimaryFieldNames = targetType
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(fieldInfo => fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) &&
                                fieldInfo.Name.StartsWith('<') &&
                                fieldInfo.Name.EndsWith(">P", StringComparison.Ordinal))
            .Select(fieldInfo => fieldInfo.Name[1..^2])
            .ToList();

        if (capturedPrimaryFieldNames.Count > 0)
        {
            var capturedMatch = visibleConstructors.FirstOrDefault(candidateConstructor =>
            {
                var parameterNames = candidateConstructor.GetParameters()
                    .Select(parameter => parameter.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.Ordinal);
                return capturedPrimaryFieldNames.All(parameterNames.Contains);
            });

            if (capturedMatch != null)
            {
                primaryConstructor = capturedMatch;
                return true;
            }
        }

        if (IsRecordType(targetType))
        {
            var deconstructMethod = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(methodInfo => string.Equals(methodInfo.Name, "Deconstruct", StringComparison.Ordinal) &&
                                     methodInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                .OrderByDescending(methodInfo => methodInfo.GetParameters().Length)
                .FirstOrDefault();

            if (deconstructMethod != null && TryMatchDeconstructConstructor(visibleConstructors, deconstructMethod, out var recordConstructor))
            {
                primaryConstructor = recordConstructor;
                return true;
            }
        }

        if (visibleConstructors.Length == 1 && visibleConstructors[0].GetParameters().Length > 0)
        {
            primaryConstructor = visibleConstructors[0];
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public string DetermineConstructorKind(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (targetType.IsInterface ||
            targetType.IsEnum ||
            typeof(Delegate).IsAssignableFrom(targetType) ||
            (targetType.IsAbstract && targetType.IsSealed))
        {
            return "None";
        }

        if (TryGetPrimaryConstructor(targetType, out var primaryConstructor))
        {
            var parametersSummary = string.Join(
                ", ",
                primaryConstructor.GetParameters().Select(parameter => $"{FormatDisplayNameCore(parameter.ParameterType)} {parameter.Name}"));
            return $"Primary ({parametersSummary})";
        }

        var visibleConstructors = GetVisibleConstructors(targetType);
        return visibleConstructors.Length switch
        {
            0 => "None",
            1 => "Default",
            _ => $"Overloaded ({visibleConstructors.Length})"
        };
    }

    /// <inheritdoc />
    public bool IsAwaitableReturnType(Type returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);

        if (typeof(Task).IsAssignableFrom(returnType) || returnType == typeof(ValueTask))
        {
            return true;
        }

        if (!returnType.IsGenericType)
        {
            return false;
        }

        var genericDefinition = returnType.GetGenericTypeDefinition();
        return genericDefinition == typeof(Task<>) || genericDefinition == typeof(ValueTask<>);
    }

    /// <inheritdoc />
    public bool IsAsyncEnumerableType(Type returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);

        if (!returnType.IsGenericType)
        {
            return false;
        }

        var genericDefinition = returnType.GetGenericTypeDefinition();
        return genericDefinition == typeof(IAsyncEnumerable<>) || genericDefinition == typeof(IAsyncEnumerator<>);
    }

    /// <inheritdoc />
    public bool HasAsyncStateMachineAttribute(MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        return methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null ||
               methodInfo.GetCustomAttribute<AsyncIteratorStateMachineAttribute>() != null;
    }

    internal static TypeCategory ResolveTypeCategory(Type targetType) =>
        targetType.IsInterface
            ? TypeCategory.Interface
            : targetType.IsEnum
            ? TypeCategory.Enum
            : typeof(Delegate).IsAssignableFrom(targetType)
            ? TypeCategory.Delegate
            : targetType.IsValueType && !targetType.IsPrimitive ? TypeCategory.Struct : TypeCategory.Class;

    internal static bool IsRecordType(Type targetType) =>
        !targetType.IsInterface &&
        !targetType.IsEnum &&
        !typeof(Delegate).IsAssignableFrom(targetType) &&
        (!targetType.IsValueType
            ? targetType.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null
            : targetType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(methodInfo =>
                {
                    if (!string.Equals(methodInfo.Name, "PrintMembers", StringComparison.Ordinal) ||
                        !methodInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                    {
                        return false;
                    }

                    var parameters = methodInfo.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(StringBuilder);
                }));

    internal static bool IsReadOnlyStruct(Type targetType) =>
        targetType.IsValueType &&
        !targetType.IsEnum &&
        targetType.IsDefined(typeof(IsReadOnlyAttribute), inherit: false);

    internal static bool IsRefStruct(Type targetType) =>
        targetType.IsByRefLike;

    internal static bool TryGetOperatorDisplayToken(string methodName, out string operatorToken, out bool isConversionOperator)
    {
        if (string.Equals(methodName, "op_Implicit", StringComparison.Ordinal) ||
            string.Equals(methodName, "op_Explicit", StringComparison.Ordinal) ||
            string.Equals(methodName, "op_CheckedExplicit", StringComparison.Ordinal))
        {
            operatorToken = string.Equals(methodName, "op_Implicit", StringComparison.Ordinal)
                ? "implicit"
                : string.Equals(methodName, "op_CheckedExplicit", StringComparison.Ordinal)
                    ? "checked explicit"
                    : "explicit";
            isConversionOperator = true;
            return true;
        }

        isConversionOperator = false;
        return StandardOperatorTokens.TryGetValue(methodName, out operatorToken!);
    }

    internal static string ResolveTypeAccessModifier(Type targetType) =>
        targetType.IsPublic || targetType.IsNestedPublic
            ? "public"
            : targetType.IsNestedFamORAssem
                ? "protected internal"
                : targetType.IsNestedFamily
                    ? "protected"
                    : targetType.IsNestedFamANDAssem
                        ? "private protected"
                        : "internal";

    internal static bool IsMethodAccessible(MethodBase methodBase) =>
        methodBase.IsPublic ||
        methodBase.IsFamily ||
        methodBase.IsFamilyOrAssembly ||
        methodBase.IsFamilyAndAssembly ||
        IsExplicitInterfaceImplementation(methodBase);

    internal static bool IsExplicitInterfaceImplementation(MethodBase methodBase) =>
        methodBase is MethodInfo { IsPrivate: true, IsFinal: true, IsVirtual: true } methodInfo &&
        methodInfo.Name.Contains('.', StringComparison.Ordinal) &&
        !methodInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    internal static bool IsFinalizerMethod(MethodInfo methodInfo) =>
        string.Equals(methodInfo.Name, "Finalize", StringComparison.Ordinal) &&
        methodInfo.IsVirtual &&
        methodInfo.IsFamily &&
        methodInfo.ReturnType == typeof(void) &&
        methodInfo.GetParameters().Length == 0 &&
        methodInfo.DeclaringType is { IsInterface: false, IsValueType: false };

    internal static string FormatMemberAccessModifier(MethodBase methodBase) =>
        IsExplicitInterfaceImplementation(methodBase)
            ? "explicit interface"
            : methodBase.IsPublic
                ? "public"
                : methodBase.IsFamilyOrAssembly
                    ? "protected internal"
                    : methodBase.IsFamilyAndAssembly
                        ? "private protected"
                        : "protected";

    internal static bool IsFieldAccessible(FieldInfo fieldInfo) =>
        fieldInfo.IsPublic || fieldInfo.IsFamily || fieldInfo.IsFamilyOrAssembly || fieldInfo.IsFamilyAndAssembly;

    internal static string FormatFieldAccessModifier(FieldInfo fieldInfo) =>
        fieldInfo.IsPublic
            ? "public"
            : fieldInfo.IsFamilyOrAssembly
                ? "protected internal"
                : fieldInfo.IsFamilyAndAssembly
                    ? "private protected"
                    : "protected";

    internal static bool IsParamsParameter(ParameterInfo parameterInfo) =>
        parameterInfo.IsDefined(typeof(ParamArrayAttribute), inherit: false) ||
        HasAttributeNamed(parameterInfo, "ParamCollectionAttribute");

    internal static string DetermineParameterModifier(
        ParameterInfo parameterInfo,
        bool isParams,
        bool isExtensionMethodFirstParameter)
    {
        var isScoped = HasAttributeNamed(parameterInfo, "ScopedRefAttribute");
        string baseModifier;

        if (parameterInfo.ParameterType.IsByRef)
        {
            var refKind = parameterInfo.IsOut && !parameterInfo.IsIn
                ? "out"
                : HasAttributeNamed(parameterInfo, "RequiresLocationAttribute")
                    ? "ref readonly"
                    : parameterInfo.IsIn
                        ? "in"
                        : "ref";

            baseModifier = isExtensionMethodFirstParameter ? $"this {refKind}" : refKind;
        }
        else
        {
            baseModifier = isExtensionMethodFirstParameter
                ? "this"
                : isParams
                    ? "params"
                    : string.Empty;
        }

        return !isScoped
            ? baseModifier
            : baseModifier.StartsWith("this ", StringComparison.Ordinal)
                ? $"this scoped {baseModifier["this ".Length..]}"
                : string.IsNullOrEmpty(baseModifier)
                    ? "scoped"
                    : $"scoped {baseModifier}";
    }

    internal static string FormatParameterDefaultValue(ParameterInfo parameterInfo, Type effectiveType)
    {
        var nonNullableValueType = effectiveType.IsValueType && Nullable.GetUnderlyingType(effectiveType) == null;
        if (!parameterInfo.HasDefaultValue)
        {
            return nonNullableValueType ? "default" : "null";
        }

        var rawDefault = parameterInfo.RawDefaultValue;
        if (rawDefault is null or DBNull || ReferenceEquals(rawDefault, Type.Missing))
        {
            return nonNullableValueType ? "default" : "null";
        }

        var underlyingType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
        if (underlyingType.IsEnum)
        {
            var enumConstant = Enum.ToObject(underlyingType, rawDefault);
            return $"{underlyingType.Name}.{enumConstant}";
        }

        return rawDefault switch
        {
            string stringLiteral => $"\"{EscapeStringLiteral(stringLiteral)}\"",
            char charLiteral => $"'{EscapeCharLiteral(charLiteral)}'",
            bool boolLiteral => boolLiteral ? "true" : "false",
            _ => Convert.ToString(rawDefault, CultureInfo.InvariantCulture) ?? "default"
        };
    }

    private static string EscapeStringLiteral(string rawLiteral) =>
        rawLiteral
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\0", "\\0", StringComparison.Ordinal);

    private static string EscapeCharLiteral(char rawCharacter) =>
        rawCharacter switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\r' => "\\r",
            '\n' => "\\n",
            '\t' => "\\t",
            '\0' => "\\0",
            _ => rawCharacter.ToString()
        };

    internal static Type[] ExtractOwnGenericArguments(Type targetType)
    {
        var definition = targetType.IsGenericType && !targetType.IsGenericTypeDefinition
            ? targetType.GetGenericTypeDefinition()
            : targetType;
        var ownArity = ExtractOwnGenericArity(definition.Name);
        if (ownArity == 0 || !targetType.IsGenericType)
        {
            return [];
        }

        var allArguments = targetType.GetGenericArguments();
        return ownArity >= allArguments.Length
            ? allArguments
            : allArguments[^ownArity..];
    }

    internal static string BuildExtensionPropertySignature(
        PropertyInfo extensionProperty,
        MethodInfo representativeAccessor,
        MethodInfo? matchingGetter = null,
        MethodInfo? matchingSetter = null)
    {
        NullabilityInfoContext nullabilityContext = new();
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append("public ");

        if (representativeAccessor.GetParameters().Length == 0)
        {
            _ = stringBuilder.Append("static ");
        }

        var skeletonGetter = extensionProperty.GetGetMethod(true) ?? matchingGetter;
        var skeletonSetter = extensionProperty.GetSetMethod(true) ?? matchingSetter;
        if (extensionProperty.PropertyType.IsByRef || skeletonGetter?.ReturnType.IsByRef == true)
        {
            var isReadOnlyReturn = skeletonGetter != null && IsParameterMarkedReadOnly(skeletonGetter.ReturnParameter);
            _ = stringBuilder.Append(isReadOnlyReturn ? "ref readonly " : "ref ");
        }

        _ = stringBuilder.Append(FormatNullabilityWithCompilerMetadata(nullabilityContext.Create(extensionProperty), extensionProperty));
        _ = stringBuilder.Append(' ');
        _ = stringBuilder.Append(extensionProperty.Name);
        _ = stringBuilder.Append(" { ");

        if (extensionProperty.CanRead)
        {
            _ = stringBuilder.Append("get; ");
        }

        if (extensionProperty.CanWrite)
        {
            var setKeyword = skeletonSetter != null && IsInitOnlySetter(skeletonSetter) ? "init; " : "set; ";
            _ = stringBuilder.Append(setKeyword);
        }

        _ = stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    private static string BuildDelegateDeclarationSignature(Type delegateType)
    {
        NullabilityInfoContext nullabilityContext = new();
        StringBuilder builder = new();
        _ = builder.Append(FormatTypeVisibility(delegateType));
        _ = builder.Append("delegate ");

        var invokeMethod = delegateType.GetMethod("Invoke");
        if (invokeMethod == null)
        {
            _ = builder.Append(ExtractCleanTypeName(delegateType));
            _ = builder.Append(FormatTypeGenericParameterList(delegateType));
            _ = builder.Append("()");
            _ = builder.Append(FormatGenericWhereClauses(ExtractOwnGenericArguments(delegateType)));
            return builder.ToString();
        }

        var returnRefPrefix = invokeMethod.ReturnType.IsByRef
            ? IsParameterMarkedReadOnly(invokeMethod.ReturnParameter) ? "ref readonly " : "ref "
            : string.Empty;
        var returnDisplay = FormatNullabilityWithCompilerMetadata(
            nullabilityContext.Create(invokeMethod.ReturnParameter),
            invokeMethod.ReturnParameter);
        var parameterDisplay = string.Join(
            ", ",
            invokeMethod.GetParameters().Select(parameter =>
                FormatParameterSignatureCore(parameter, isExtensionMethodFirstParameter: false, nullabilityContext)));

        _ = builder.Append(returnRefPrefix);
        _ = builder.Append(returnDisplay);
        _ = builder.Append(' ');
        _ = builder.Append(ExtractCleanTypeName(delegateType));
        _ = builder.Append(FormatTypeGenericParameterList(delegateType));
        _ = builder.Append('(');
        _ = builder.Append(parameterDisplay);
        _ = builder.Append(')');
        _ = builder.Append(FormatGenericWhereClauses(ExtractOwnGenericArguments(delegateType)));
        return builder.ToString();
    }

    private static string FormatTypeGenericParameterList(Type targetType)
    {
        var ownGenericArguments = ExtractOwnGenericArguments(targetType);
        if (ownGenericArguments.Length == 0)
        {
            return string.Empty;
        }

        var formattedParameters = ownGenericArguments.Select(argument =>
        {
            if (!argument.IsGenericParameter)
            {
                return FormatDisplayNameCore(argument);
            }

            var variance = argument.GenericParameterAttributes & GenericParameterAttributes.VarianceMask;
            return variance == GenericParameterAttributes.Covariant
                ? $"out {argument.Name}"
                : variance == GenericParameterAttributes.Contravariant
                    ? $"in {argument.Name}"
                    : argument.Name;
        });

        return $"<{string.Join(", ", formattedParameters)}>";
    }

    private static bool TryMatchDeconstructConstructor(
        ConstructorInfo[] visibleConstructors,
        MethodInfo deconstructMethod,
        out ConstructorInfo matchingConstructor)
    {
        var deconstructParameters = deconstructMethod.GetParameters();
        foreach (var candidateConstructor in visibleConstructors)
        {
            var ctorParameters = candidateConstructor.GetParameters();
            if (ctorParameters.Length != deconstructParameters.Length)
            {
                continue;
            }

            var allTypesMatch = true;
            for (var index = 0; index < ctorParameters.Length; index++)
            {
                var outElementType = deconstructParameters[index].ParameterType.IsByRef
                    ? deconstructParameters[index].ParameterType.GetElementType()
                    : deconstructParameters[index].ParameterType;
                if (ctorParameters[index].ParameterType != outElementType)
                {
                    allTypesMatch = false;
                    break;
                }
            }

            if (allTypesMatch)
            {
                matchingConstructor = candidateConstructor;
                return true;
            }
        }

        matchingConstructor = null!;
        return false;
    }

    private static void AppendMethodModifiers(StringBuilder stringBuilder, MethodInfo methodInfo, bool isAsyncMethod)
    {
        var isInterface = methodInfo.DeclaringType?.IsInterface == true;
        if (methodInfo.IsStatic)
        {
            _ = stringBuilder.Append("static ");
            if (isInterface)
            {
                if (methodInfo.IsAbstract)
                {
                    _ = stringBuilder.Append("abstract ");
                }
                else if (methodInfo.IsVirtual)
                {
                    _ = stringBuilder.Append("virtual ");
                }
            }
        }
        else if (isInterface)
        {
            if (!methodInfo.IsAbstract)
            {
                _ = stringBuilder.Append(methodInfo.IsFinal || !methodInfo.IsVirtual ? "sealed " : "virtual ");
            }
        }
        else
        {
            var isOverride = methodInfo.IsVirtual &&
                             (methodInfo.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot &&
                             methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType;

            if (methodInfo.IsAbstract)
            {
                _ = stringBuilder.Append("abstract ");
            }
            else if (isOverride)
            {
                _ = stringBuilder.Append(methodInfo.IsFinal ? "sealed override " : "override ");
            }
            else if (methodInfo.IsVirtual && !methodInfo.IsFinal)
            {
                _ = stringBuilder.Append("virtual ");
            }
        }

        if ((methodInfo.Attributes & MethodAttributes.PinvokeImpl) != 0)
        {
            _ = stringBuilder.Append("extern ");
        }

        if (isAsyncMethod)
        {
            _ = stringBuilder.Append("async ");
        }
    }

    private static string FormatMethodParameterList(MethodInfo methodInfo, NullabilityInfoContext nullabilityContext)
    {
        var isExtensionMethod = methodInfo.IsDefined(typeof(ExtensionAttribute), inherit: false);
        var parameters = methodInfo.GetParameters();
        List<string> formattedParameters = [];
        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            var isExtensionTarget = isExtensionMethod && parameterIndex == 0;
            formattedParameters.Add(FormatParameterSignatureCore(
                parameters[parameterIndex],
                isExtensionTarget,
                nullabilityContext));
        }

        return string.Join(", ", formattedParameters);
    }

    private static string FormatTypeVisibility(Type targetType) =>
        targetType.IsPublic || targetType.IsNestedPublic
            ? "public "
            : targetType.IsNestedFamORAssem
                ? "protected internal "
                : targetType.IsNestedFamily
                    ? "protected "
                    : targetType.IsNestedFamANDAssem
                        ? "private protected "
                        : string.Empty;

    private static string FormatMethodBaseVisibility(MethodBase methodBase) =>
        IsExplicitInterfaceImplementation(methodBase)
            ? string.Empty
            : methodBase.IsPublic
                ? "public "
                : methodBase.IsFamilyOrAssembly
                    ? "protected internal "
                    : methodBase.IsFamily
                        ? "protected "
                        : methodBase.IsFamilyAndAssembly
                            ? "private protected "
                            : string.Empty;

    private static string FormatFieldVisibility(FieldInfo fieldInfo) =>
        fieldInfo.IsPublic
            ? "public "
            : fieldInfo.IsFamilyOrAssembly
                ? "protected internal "
                : fieldInfo.IsFamily
                    ? "protected "
                    : fieldInfo.IsFamilyAndAssembly
                        ? "private protected "
                        : "private ";

    private static MethodInfo? SelectMostAccessibleAccessor(MethodInfo? getMethod, MethodInfo? setMethod) =>
        getMethod == null || (setMethod != null && GetAccessibilityRank(setMethod) > GetAccessibilityRank(getMethod))
            ? setMethod
            : getMethod;

    private static int GetAccessibilityRank(MethodBase methodBase) =>
        methodBase.IsPublic
            ? 5
            : methodBase.IsFamilyOrAssembly
                ? 4
                : methodBase.IsFamily
                    ? 3
                    : methodBase.IsAssembly
                        ? 2
                        : methodBase.IsFamilyAndAssembly
                            ? 1
                            : 0;

    private static string FormatAccessorRestrictedVisibility(MethodInfo accessorMethod, MethodInfo? propertyRepresentative) =>
        propertyRepresentative == null || GetAccessibilityRank(accessorMethod) >= GetAccessibilityRank(propertyRepresentative)
            ? string.Empty
            : accessorMethod.IsPrivate
                ? "private "
                : accessorMethod.IsFamilyAndAssembly
                    ? "private protected "
                    : accessorMethod.IsAssembly
                        ? "internal "
                        : accessorMethod.IsFamilyOrAssembly
                            ? "protected internal "
                            : accessorMethod.IsFamily ? "protected " : string.Empty;

    private static bool IsInitOnlySetter(MethodInfo setMethod) =>
        setMethod.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => string.Equals(modifier.FullName, "System.Runtime.CompilerServices.IsExternalInit", StringComparison.Ordinal));

    private static bool IsMethodMarkedReadOnly(MethodInfo methodInfo) =>
        methodInfo.GetCustomAttributesData()
            .Any(attributeData => string.Equals(attributeData.AttributeType.Name, "IsReadOnlyAttribute", StringComparison.Ordinal));

    private static bool IsParameterMarkedReadOnly(ParameterInfo parameterInfo) =>
        parameterInfo.GetCustomAttributesData()
            .Any(attributeData => string.Equals(attributeData.AttributeType.Name, "IsReadOnlyAttribute", StringComparison.Ordinal));

    private static bool HasAttributeNamed(ParameterInfo parameterInfo, string attributeTypeName) =>
        parameterInfo.GetCustomAttributesData()
            .Any(attributeData => string.Equals(attributeData.AttributeType.Name, attributeTypeName, StringComparison.Ordinal));

    private static string FormatArrayBrackets(Type arrayType)
    {
        var rank = arrayType.GetArrayRank();
        return rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
    }

    private static int ExtractOwnGenericArity(string metadataName)
    {
        var backtickIndex = metadataName.IndexOf('`');
        return backtickIndex < 0 || backtickIndex + 1 >= metadataName.Length
            ? 0
            : int.TryParse(metadataName[(backtickIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var arity)
                ? arity
                : 0;
    }

    private static string FormatDisplayNameCore(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (targetType.IsByRef)
        {
            return FormatDisplayNameCore(targetType.GetElementType()!);
        }

        if (targetType.IsFunctionPointer)
        {
            return FormatFunctionPointerDisplayName(targetType);
        }

        if (targetType.IsPointer)
        {
            return $"{FormatDisplayNameCore(targetType.GetElementType()!)}*";
        }

        if (targetType.IsArray)
        {
            return $"{FormatDisplayNameCore(targetType.GetElementType()!)}{FormatArrayBrackets(targetType)}";
        }

        if (Nullable.GetUnderlyingType(targetType) is { } underlyingValueType)
        {
            return $"{FormatDisplayNameCore(underlyingValueType)}?";
        }

        var ownArguments = ExtractOwnGenericArguments(targetType);
        if (ownArguments.Length == 0)
        {
            return targetType.Name;
        }

        var rawName = targetType.Name;
        var backtickIndex = rawName.IndexOf('`');
        var baseName = backtickIndex > 0 ? rawName[..backtickIndex] : rawName;
        var genericArgumentNames = ownArguments.Select(FormatDisplayNameCore);
        return $"{baseName}<{string.Join(", ", genericArgumentNames)}>";
    }

    private static string FormatDisplayNameCore(NullabilityInfo nullabilityInfo) =>
        FormatDisplayNameWithMetadataCore(
            nullabilityInfo,
            tupleElementNames: null,
            nativeIntegerFlags: null,
            dynamicFlags: null,
            modifiedFunctionPointerType: null,
            tupleNameOffset: 0,
            nativeIntIndex: 0,
            dynamicIndex: 0).Display;

    internal static string FormatNullabilityWithCompilerMetadata(
        NullabilityInfo nullabilityInfo,
        ICustomAttributeProvider? attributeProvider)
    {
        ArgumentNullException.ThrowIfNull(nullabilityInfo);
        if (attributeProvider == null)
        {
            return FormatDisplayNameCore(nullabilityInfo);
        }

        var tupleElementNames = ExtractTupleElementNames(attributeProvider);
        var nativeIntegerFlags = ExtractTransformFlags(attributeProvider, "System.Runtime.CompilerServices.NativeIntegerAttribute");
        var dynamicFlags = ExtractTransformFlags(attributeProvider, "System.Runtime.CompilerServices.DynamicAttribute");
        var modifiedFunctionPointerType = TryExtractModifiedFunctionPointerType(attributeProvider);

        return FormatDisplayNameWithMetadataCore(
            nullabilityInfo,
            tupleElementNames,
            nativeIntegerFlags,
            dynamicFlags,
            modifiedFunctionPointerType,
            tupleNameOffset: 0,
            nativeIntIndex: 0,
            dynamicIndex: 0).Display;
    }

    internal static Type? TryExtractModifiedFunctionPointerType(ICustomAttributeProvider? attributeProvider)
    {
        var modifiedType = attributeProvider switch
        {
            ParameterInfo parameterInfo => TryGetModifiedType(parameterInfo.GetModifiedParameterType),
            PropertyInfo propertyInfo => TryGetModifiedType(propertyInfo.GetModifiedPropertyType),
            FieldInfo fieldInfo => TryGetModifiedType(fieldInfo.GetModifiedFieldType),
            _ => null
        };

        if (modifiedType == null)
        {
            return null;
        }

        var unwrappedType = modifiedType.IsByRef ? modifiedType.GetElementType() : modifiedType;
        return unwrappedType is { IsFunctionPointer: true } ? unwrappedType : null;
    }

    private static Type? TryGetModifiedType(Func<Type> modifiedTypeAccessor)
    {
        try
        {
            return modifiedTypeAccessor();
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static (string Display, int NextTupleOffset, int NextNativeIndex, int NextDynamicIndex) FormatDisplayNameWithMetadataCore(
        NullabilityInfo nullabilityInfo,
        IReadOnlyList<string?>? tupleElementNames,
        IReadOnlyList<bool>? nativeIntegerFlags,
        IReadOnlyList<bool>? dynamicFlags,
        Type? modifiedFunctionPointerType,
        int tupleNameOffset,
        int nativeIntIndex,
        int dynamicIndex)
    {
        var rawTargetType = nullabilityInfo.Type;
        var targetType = rawTargetType.IsByRef
            ? (rawTargetType.GetElementType() ?? rawTargetType)
            : rawTargetType;

        if (targetType.IsFunctionPointer)
        {
            return (FormatFunctionPointerDisplayName(modifiedFunctionPointerType ?? targetType), tupleNameOffset, nativeIntIndex, dynamicIndex);
        }

        if (targetType.IsPointer)
        {
            var isCurrentDynamic = ConsumeDynamicFlag(dynamicFlags, ref dynamicIndex);
            _ = isCurrentDynamic;
            string pointerElement;
            if (nullabilityInfo.ElementType != null)
            {
                (pointerElement, tupleNameOffset, nativeIntIndex, dynamicIndex) = FormatDisplayNameWithMetadataCore(
                    nullabilityInfo.ElementType,
                    tupleElementNames,
                    nativeIntegerFlags,
                    dynamicFlags,
                    modifiedFunctionPointerType: null,
                    tupleNameOffset,
                    nativeIntIndex,
                    dynamicIndex);
            }
            else
            {
                pointerElement = FormatDisplayNameCore(targetType.GetElementType()!);
            }

            return ($"{pointerElement}*", tupleNameOffset, nativeIntIndex, dynamicIndex);
        }

        var isByRefNullable = rawTargetType.IsByRef && nullabilityInfo.WriteState == NullabilityState.Nullable;
        var nullableSuffix = nullabilityInfo.ReadState == NullabilityState.Nullable || isByRefNullable
            ? "?"
            : string.Empty;

        if (targetType.IsArray)
        {
            _ = ConsumeDynamicFlag(dynamicFlags, ref dynamicIndex);
            string elementDisplay;
            if (nullabilityInfo.ElementType != null)
            {
                (elementDisplay, tupleNameOffset, nativeIntIndex, dynamicIndex) = FormatDisplayNameWithMetadataCore(
                    nullabilityInfo.ElementType,
                    tupleElementNames,
                    nativeIntegerFlags,
                    dynamicFlags,
                    modifiedFunctionPointerType: null,
                    tupleNameOffset,
                    nativeIntIndex,
                    dynamicIndex);
            }
            else
            {
                elementDisplay = FormatDisplayNameCore(targetType.GetElementType()!);
            }

            return ($"{elementDisplay}{FormatArrayBrackets(targetType)}{nullableSuffix}", tupleNameOffset, nativeIntIndex, dynamicIndex);
        }

        if (Nullable.GetUnderlyingType(targetType) is { } underlyingValueType)
        {
            _ = ConsumeDynamicFlag(dynamicFlags, ref dynamicIndex);
            string underlyingDisplay;
            if (nullabilityInfo.GenericTypeArguments.Length > 0)
            {
                (underlyingDisplay, tupleNameOffset, nativeIntIndex, dynamicIndex) = FormatDisplayNameWithMetadataCore(
                    nullabilityInfo.GenericTypeArguments[0],
                    tupleElementNames,
                    nativeIntegerFlags,
                    dynamicFlags,
                    modifiedFunctionPointerType: null,
                    tupleNameOffset,
                    nativeIntIndex,
                    dynamicIndex);
            }
            else
            {
                underlyingDisplay = FormatDisplayNameCore(underlyingValueType);
            }

            var formattedNullable = underlyingDisplay.EndsWith('?') ? underlyingDisplay : $"{underlyingDisplay}?";
            return (formattedNullable, tupleNameOffset, nativeIntIndex, dynamicIndex);
        }

        if (IsValueTupleType(targetType) && tupleElementNames is { Count: > 0 })
        {
            return FormatNamedValueTuple(
                nullabilityInfo,
                targetType,
                nullableSuffix,
                tupleElementNames,
                nativeIntegerFlags,
                dynamicFlags,
                tupleNameOffset,
                nativeIntIndex,
                dynamicIndex);
        }

        var ownArguments = ExtractOwnGenericArguments(targetType);
        if (ownArguments.Length == 0)
        {
            var isDynamicObject = ConsumeDynamicFlag(dynamicFlags, ref dynamicIndex) && targetType == typeof(object);
            var isNativeInt = (targetType == typeof(IntPtr) || targetType == typeof(UIntPtr)) &&
                              ConsumeNativeIntFlag(nativeIntegerFlags, ref nativeIntIndex);
            var simpleName = isDynamicObject
                ? "dynamic"
                : isNativeInt
                    ? targetType == typeof(IntPtr) ? "nint" : "nuint"
                    : targetType.Name;
            var formattedSimple = !string.IsNullOrEmpty(nullableSuffix) && !simpleName.EndsWith('?')
                ? $"{simpleName}{nullableSuffix}"
                : simpleName;
            return (formattedSimple, tupleNameOffset, nativeIntIndex, dynamicIndex);
        }

        _ = ConsumeDynamicFlag(dynamicFlags, ref dynamicIndex);
        var rawName = targetType.Name;
        var backtickIndex = rawName.IndexOf('`');
        var baseName = backtickIndex > 0 ? rawName[..backtickIndex] : rawName;
        var allArgumentsCount = targetType.GetGenericArguments().Length;
        List<string> genericArgumentNames = [];

        if (nullabilityInfo.GenericTypeArguments.Length == allArgumentsCount && allArgumentsCount >= ownArguments.Length)
        {
            var ownStartIndex = allArgumentsCount - ownArguments.Length;
            foreach (var genericArgNullability in nullabilityInfo.GenericTypeArguments[ownStartIndex..])
            {
                string formattedArg;
                (formattedArg, tupleNameOffset, nativeIntIndex, dynamicIndex) = FormatDisplayNameWithMetadataCore(
                    genericArgNullability,
                    tupleElementNames,
                    nativeIntegerFlags,
                    dynamicFlags,
                    modifiedFunctionPointerType: null,
                    tupleNameOffset,
                    nativeIntIndex,
                    dynamicIndex);
                genericArgumentNames.Add(formattedArg);
            }
        }
        else
        {
            genericArgumentNames.AddRange(ownArguments.Select(FormatDisplayNameCore));
        }

        return ($"{baseName}<{string.Join(", ", genericArgumentNames)}>{nullableSuffix}", tupleNameOffset, nativeIntIndex, dynamicIndex);
    }

    private static (string Display, int NextTupleOffset, int NextNativeIndex, int NextDynamicIndex) FormatNamedValueTuple(
        NullabilityInfo tupleNullability,
        Type tupleType,
        string nullableSuffix,
        IReadOnlyList<string?> tupleElementNames,
        IReadOnlyList<bool>? nativeIntegerFlags,
        IReadOnlyList<bool>? dynamicFlags,
        int tupleNameOffset,
        int nativeIntIndex,
        int dynamicIndex)
    {
        List<NullabilityInfo> flattenedElements = [];
        FlattenValueTupleNullabilityElements(tupleNullability, flattenedElements, ref dynamicIndex, dynamicFlags);
        var elementCount = flattenedElements.Count;
        var currentLevelNames = new string?[elementCount];
        var hasAnyElementName = false;

        for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
        {
            if (tupleNameOffset + elementIndex < tupleElementNames.Count)
            {
                currentLevelNames[elementIndex] = tupleElementNames[tupleNameOffset + elementIndex];
                if (!string.IsNullOrWhiteSpace(currentLevelNames[elementIndex]))
                {
                    hasAnyElementName = true;
                }
            }
        }

        tupleNameOffset += elementCount;
        List<string> formattedElements = [];
        for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
        {
            string elementTypeDisplay;
            (elementTypeDisplay, tupleNameOffset, nativeIntIndex, dynamicIndex) = FormatDisplayNameWithMetadataCore(
                flattenedElements[elementIndex],
                tupleElementNames,
                nativeIntegerFlags,
                dynamicFlags,
                modifiedFunctionPointerType: null,
                tupleNameOffset,
                nativeIntIndex,
                dynamicIndex);

            var elementName = currentLevelNames[elementIndex];
            formattedElements.Add(!string.IsNullOrWhiteSpace(elementName)
                ? $"{elementTypeDisplay} {elementName}"
                : elementTypeDisplay);
        }

        if (!hasAnyElementName)
        {
            var rawName = tupleType.Name;
            var backtickIndex = rawName.IndexOf('`');
            var baseName = backtickIndex > 0 ? rawName[..backtickIndex] : rawName;
            return ($"{baseName}<{string.Join(", ", formattedElements)}>{nullableSuffix}", tupleNameOffset, nativeIntIndex, dynamicIndex);
        }

        return ($"({string.Join(", ", formattedElements)}){nullableSuffix}", tupleNameOffset, nativeIntIndex, dynamicIndex);
    }

    private static void FlattenValueTupleNullabilityElements(
        NullabilityInfo tupleNullability,
        List<NullabilityInfo> flattenedElements,
        ref int dynamicIndex,
        IReadOnlyList<bool>? dynamicFlags)
    {
        _ = ConsumeDynamicFlag(dynamicFlags, ref dynamicIndex);
        var genericArguments = tupleNullability.GenericTypeArguments;
        for (var index = 0; index < genericArguments.Length; index++)
        {
            if (index == 7 && IsValueTupleType(genericArguments[index].Type))
            {
                FlattenValueTupleNullabilityElements(genericArguments[index], flattenedElements, ref dynamicIndex, dynamicFlags);
            }
            else
            {
                flattenedElements.Add(genericArguments[index]);
            }
        }
    }

    internal static bool IsValueTupleType(Type candidateType) =>
        candidateType.IsValueType &&
        candidateType.IsGenericType &&
        candidateType.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    internal static string FormatFunctionPointerDisplayName(Type functionPointerType)
    {
        var prefix = FormatFunctionPointerPrefix(functionPointerType);
        var parameterDisplays = functionPointerType.GetFunctionPointerParameterTypes().Select(FormatDisplayNameCore);
        var returnDisplay = FormatDisplayNameCore(functionPointerType.GetFunctionPointerReturnType());
        return $"{prefix}<{string.Join(", ", [.. parameterDisplays, returnDisplay])}>";
    }

    internal static string FormatFunctionPointerPrefix(Type functionPointerType)
    {
        if (!functionPointerType.IsUnmanagedFunctionPointer)
        {
            return "delegate*";
        }

        var callingConventions = functionPointerType
            .GetFunctionPointerCallingConventions()
            .Select(conventionType => conventionType.Name.StartsWith("CallConv", StringComparison.Ordinal)
                ? conventionType.Name["CallConv".Length..]
                : conventionType.Name)
            .ToArray();

        return callingConventions.Length > 0
            ? $"delegate* unmanaged[{string.Join(", ", callingConventions)}]"
            : "delegate* unmanaged";
    }

    internal static string FormatExplicitInterfaceMemberName(string rawMetadataName)
    {
        var splitIndex = FindLastDotOutsideGenerics(rawMetadataName);
        if (splitIndex <= 0)
        {
            return rawMetadataName;
        }

        var interfacePart = SimplifyInterfaceTypePrefix(rawMetadataName[..splitIndex]);
        var memberPart = rawMetadataName[(splitIndex + 1)..];
        return $"{interfacePart}.{memberPart}";
    }

    internal static string FormatExplicitInterfaceIndexerPrefix(string rawPropertyMetadataName)
    {
        var splitIndex = FindLastDotOutsideGenerics(rawPropertyMetadataName);
        if (splitIndex <= 0)
        {
            return "this";
        }

        var interfacePart = SimplifyInterfaceTypePrefix(rawPropertyMetadataName[..splitIndex]);
        return $"{interfacePart}.this";
    }

    private static int FindLastDotOutsideGenerics(string rawName)
    {
        var angleDepth = 0;
        for (var index = rawName.Length - 1; index >= 0; index--)
        {
            var character = rawName[index];
            if (character == '>')
            {
                angleDepth++;
            }
            else if (character == '<')
            {
                angleDepth--;
            }
            else if (character == '.' && angleDepth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static string SimplifyInterfaceTypePrefix(string rawInterfacePrefix)
    {
        StringBuilder simplifiedBuilder = new();
        StringBuilder tokenBuilder = new();

        foreach (var character in rawInterfacePrefix)
        {
            if (character is '<' or '>' or ',')
            {
                AppendSimplifiedTypeToken(simplifiedBuilder, tokenBuilder);
                _ = simplifiedBuilder.Append(character);
                if (character == ',')
                {
                    _ = simplifiedBuilder.Append(' ');
                }
            }
            else if (!char.IsWhiteSpace(character))
            {
                _ = tokenBuilder.Append(character);
            }
        }

        AppendSimplifiedTypeToken(simplifiedBuilder, tokenBuilder);
        return simplifiedBuilder.ToString();
    }

    private static void AppendSimplifiedTypeToken(StringBuilder destination, StringBuilder tokenBuilder)
    {
        if (tokenBuilder.Length == 0)
        {
            return;
        }

        var token = tokenBuilder.ToString();
        _ = tokenBuilder.Clear();
        var lastSeparatorIndex = token.LastIndexOfAny(['.', '+']);
        var simpleToken = lastSeparatorIndex >= 0 ? token[(lastSeparatorIndex + 1)..] : token;
        var backtickIndex = simpleToken.IndexOf('`');
        if (backtickIndex > 0)
        {
            simpleToken = simpleToken[..backtickIndex];
        }

        _ = destination.Append(simpleToken);
    }

    private static string FormatTypeInheritanceClause(Type targetType, bool isStatic, bool isRecord)
    {
        if (targetType.IsEnum || isStatic)
        {
            return string.Empty;
        }

        List<string> inheritanceTokens = [];
        if (!targetType.IsValueType &&
            !targetType.IsInterface &&
            targetType.BaseType != null &&
            targetType.BaseType != typeof(object))
        {
            inheritanceTokens.Add(FormatDisplayNameCore(targetType.BaseType));
        }

        HashSet<Type> baseInterfaces = targetType.BaseType != null
            ? [.. targetType.BaseType.GetInterfaces()]
            : [];

        foreach (var interfaceType in targetType.GetInterfaces())
        {
            if (baseInterfaces.Contains(interfaceType))
            {
                continue;
            }

            if (isRecord &&
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(IEquatable<>) &&
                interfaceType.GetGenericArguments()[0] == targetType)
            {
                continue;
            }

            inheritanceTokens.Add(FormatDisplayNameCore(interfaceType));
        }

        return inheritanceTokens.Count > 0
            ? $" : {string.Join(", ", inheritanceTokens)}"
            : string.Empty;
    }

    private static string FormatGenericWhereClauses(Type[] genericParameters)
    {
        if (genericParameters.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder clauseBuilder = new();
        foreach (var genericParameter in genericParameters)
        {
            if (!genericParameter.IsGenericParameter)
            {
                continue;
            }

            var constraints = ExtractPlainGenericConstraints(genericParameter);
            if (constraints.Count > 0)
            {
                _ = clauseBuilder.Append(
                    CultureInfo.InvariantCulture,
                    $" where {genericParameter.Name} : {string.Join(", ", constraints)}");
            }
        }

        return clauseBuilder.ToString();
    }

    private static List<string> ExtractPlainGenericConstraints(Type genericParameter)
    {
        List<string> constraintsList = [];
        var rawAttributes = genericParameter.GenericParameterAttributes;
        var specialConstraints = rawAttributes & GenericParameterAttributes.SpecialConstraintMask;
        var isValueTypeConstraint = (specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
        var isReferenceTypeConstraint = (specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0;

        if (isValueTypeConstraint)
        {
            var isUnmanaged = genericParameter.GetCustomAttributes(inherit: false)
                .Any(attribute => string.Equals(attribute.GetType().Name, "IsUnmanagedAttribute", StringComparison.Ordinal));
            constraintsList.Add(isUnmanaged ? "unmanaged" : "struct");
        }
        else if (isReferenceTypeConstraint)
        {
            constraintsList.Add("class");
        }
        else if (AssemblyTypeReflector.HasNotNullConstraint(genericParameter))
        {
            constraintsList.Add("notnull");
        }

        foreach (var typeConstraint in genericParameter.GetGenericParameterConstraints())
        {
            if (isValueTypeConstraint && typeConstraint == typeof(ValueType))
            {
                continue;
            }

            constraintsList.Add(FormatDisplayNameCore(typeConstraint));
        }

        if (!isValueTypeConstraint && (specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
        {
            constraintsList.Add("new()");
        }

        if ((rawAttributes & GenericParameterAttributes.AllowByRefLike) != 0)
        {
            constraintsList.Add("allows ref struct");
        }

        return constraintsList;
    }

    private static void AppendInlineMemberAttributes(StringBuilder stringBuilder, MethodBase methodBase)
    {
        if (methodBase.IsDefined(typeof(SetsRequiredMembersAttribute), inherit: false))
        {
            _ = stringBuilder.Append("[SetsRequiredMembers] ");
        }

        if (TryGetOverloadResolutionPriority(methodBase, out var priority))
        {
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"[OverloadResolutionPriority({priority})] ");
        }
    }

    private static bool TryGetOverloadResolutionPriority(MethodBase methodBase, out int priority)
    {
        var attributeData = methodBase.GetCustomAttributesData()
            .FirstOrDefault(candidate => string.Equals(
                candidate.AttributeType.FullName,
                "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute",
                StringComparison.Ordinal));
        if (attributeData is { ConstructorArguments.Count: > 0 } &&
            attributeData.ConstructorArguments[0].Value is int priorityValue)
        {
            priority = priorityValue;
            return true;
        }

        priority = 0;
        return false;
    }

    internal static IReadOnlyList<string?>? ExtractTupleElementNames(ICustomAttributeProvider attributeProvider) =>
        attributeProvider.GetCustomAttributes(typeof(TupleElementNamesAttribute), inherit: false)
            .OfType<TupleElementNamesAttribute>()
            .FirstOrDefault()
            ?.TransformNames
            .ToArray();

    internal static IReadOnlyList<bool>? ExtractTransformFlags(ICustomAttributeProvider attributeProvider, string attributeFullName) =>
        attributeProvider switch
        {
            MemberInfo memberInfo => ExtractTransformFlagsFromData(memberInfo.GetCustomAttributesData(), attributeFullName),
            ParameterInfo parameterInfo => ExtractTransformFlagsFromData(parameterInfo.GetCustomAttributesData(), attributeFullName),
            _ => null
        };

    private static IReadOnlyList<bool>? ExtractTransformFlagsFromData(IList<CustomAttributeData> customAttributes, string attributeFullName)
    {
        var matchingAttribute = customAttributes.FirstOrDefault(attributeData =>
            string.Equals(attributeData.AttributeType.FullName, attributeFullName, StringComparison.Ordinal));
        if (matchingAttribute == null)
        {
            return null;
        }

        if (matchingAttribute.ConstructorArguments.Count == 0)
        {
            return [true];
        }

        var firstArg = matchingAttribute.ConstructorArguments[0];
        return firstArg.Value is bool singleFlag
            ? [singleFlag]
            : firstArg.Value is IReadOnlyCollection<CustomAttributeTypedArgument> flagCollection
                ? [.. flagCollection.Select(item => item.Value is true)]
                : [true];
    }

    private static bool ConsumeNativeIntFlag(IReadOnlyList<bool>? flags, ref int currentIndex) =>
        flags != null && currentIndex < flags.Count && flags[currentIndex++];

    private static bool ConsumeDynamicFlag(IReadOnlyList<bool>? flags, ref int currentIndex) =>
        flags != null && currentIndex < flags.Count && flags[currentIndex++];

    private static ConstructorInfo[] GetVisibleConstructors(Type targetType) =>
        [
            .. targetType
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(constructorInfo => IsMethodAccessible(constructorInfo) &&
                                          !constructorInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
        ];

    private static string FormatParameterSignatureCore(
        ParameterInfo parameterInfo,
        bool isExtensionMethodFirstParameter,
        NullabilityInfoContext nullabilityContext)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);

        StringBuilder parameterBuilder = new();
        if (parameterInfo.GetCustomAttribute<EnumeratorCancellationAttribute>() != null)
        {
            _ = parameterBuilder.Append("[EnumeratorCancellation] ");
        }

        var callerInfoAttribute = CustomAttributeExtractor.GetCallerInformationAttribute(parameterInfo);
        if (!string.IsNullOrWhiteSpace(callerInfoAttribute))
        {
            _ = parameterBuilder.Append(CultureInfo.InvariantCulture, $"[{callerInfoAttribute}] ");
        }

        var isParams = IsParamsParameter(parameterInfo);
        var modifier = DetermineParameterModifier(parameterInfo, isParams, isExtensionMethodFirstParameter);
        if (!string.IsNullOrWhiteSpace(modifier))
        {
            _ = parameterBuilder.Append(modifier);
            _ = parameterBuilder.Append(' ');
        }

        var parameterTypeDisplay = FormatNullabilityWithCompilerMetadata(nullabilityContext.Create(parameterInfo), parameterInfo);
        _ = parameterBuilder.Append(parameterTypeDisplay);
        _ = parameterBuilder.Append(' ');
        _ = parameterBuilder.Append(parameterInfo.Name);

        var effectiveType = parameterInfo.ParameterType.IsByRef
            ? (parameterInfo.ParameterType.GetElementType() ?? parameterInfo.ParameterType)
            : parameterInfo.ParameterType;

        if (parameterInfo.IsOptional || parameterInfo.HasDefaultValue)
        {
            _ = parameterBuilder.Append(" = ");
            _ = parameterBuilder.Append(FormatParameterDefaultValue(parameterInfo, effectiveType));
        }

        return parameterBuilder.ToString();
    }
}
