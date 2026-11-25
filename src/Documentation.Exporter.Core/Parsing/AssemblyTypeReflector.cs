using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Core.Resolution;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Reflects on runtime <see cref="Type"/> metadata to extract type definitions, constructors, properties, indexers, methods, operators, fields, events, and C# 14 extension members.
/// </summary>
/// <param name="typeLinkResolver">The type link resolver used for parameter, return, inheritance, and interface links.</param>
/// <param name="customAttributeExtractor">The custom attribute, obsolescence, and experimental extractor.</param>
/// <param name="signatureFormatter">The C# declaration signature and display name formatter.</param>
/// <param name="referenceRegistry">The canonical Microsoft Learn reference URL registry.</param>
public sealed class AssemblyTypeReflector(
    ITypeLinkResolver typeLinkResolver,
    ICustomAttributeExtractor customAttributeExtractor,
    ICSharpSignatureFormatter signatureFormatter,
    IMicrosoftLearnReferenceRegistry referenceRegistry) : IAssemblyTypeReflector
{
    private sealed record ExtensionMarkerInfo(
        string MarkerTypeName,
        bool IsStaticExtension,
        Type ReceiverType);

    private readonly record struct ExtensionMethodSignatureKey(
        string MethodName,
        int ParameterCount,
        int GenericArity);

    private sealed record ExtensionPropertyEntry(
        PropertyInfo SkeletonProperty,
        ExtensionMarkerInfo MarkerInfo,
        int NestedGenericArity);

    private sealed record ExtensionBlockDiscovery(
        IReadOnlyDictionary<ExtensionMethodSignatureKey, ExtensionMarkerInfo> ExtensionMethodTargets,
        IReadOnlySet<ExtensionMethodSignatureKey> PropertyAccessorKeys,
        IReadOnlyList<ExtensionPropertyEntry> Properties)
    {
        public static ExtensionBlockDiscovery Empty { get; } = new(
            new Dictionary<ExtensionMethodSignatureKey, ExtensionMarkerInfo>(),
            new HashSet<ExtensionMethodSignatureKey>(),
            []);
    }

    private readonly ITypeLinkResolver linkResolver = ArgumentGuard.EnsureNotNull(typeLinkResolver);
    private readonly ICustomAttributeExtractor attributeExtractor = ArgumentGuard.EnsureNotNull(customAttributeExtractor);
    private readonly ICSharpSignatureFormatter formatter = ArgumentGuard.EnsureNotNull(signatureFormatter);
    private readonly IMicrosoftLearnReferenceRegistry learnRegistry = ArgumentGuard.EnsureNotNull(referenceRegistry);

    /// <inheritdoc />
    public TypeDocumentationModel ReflectTypeMetadata(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        var isStatic = targetType.IsAbstract && targetType.IsSealed && !targetType.IsInterface;
        var isAbstract = targetType.IsAbstract && !targetType.IsSealed && !targetType.IsInterface;
        var isSealed = targetType.IsSealed && !targetType.IsAbstract && !targetType.IsValueType && !targetType.IsEnum;
        var isRecord = CSharpSignatureFormatter.IsRecordType(targetType);
        var isReadOnly = CSharpSignatureFormatter.IsReadOnlyStruct(targetType);
        var isRefStruct = CSharpSignatureFormatter.IsRefStruct(targetType);

        var cleanTypeName = formatter.GetCleanTypeName(targetType);
        List<string> declaringTypeChain = [];
        var currentDeclaringType = targetType.DeclaringType;
        while (currentDeclaringType != null)
        {
            declaringTypeChain.Add(formatter.GetCleanTypeName(currentDeclaringType));
            currentDeclaringType = currentDeclaringType.DeclaringType;
        }

        declaringTypeChain.Reverse();
        var declaringTypeName = declaringTypeChain.Count > 0 ? declaringTypeChain[^1] : null;
        var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(targetType);
        var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(targetType);
        var typeFullName = (targetType.FullName ?? targetType.Name).Replace('+', '.');
        var ownGenericArguments = CSharpSignatureFormatter.ExtractOwnGenericArguments(targetType);
        NullabilityInfoContext nullabilityContext = new();
        var extensionDiscovery = isStatic
            ? DiscoverExtensionBlocks(targetType)
            : ExtensionBlockDiscovery.Empty;

        return new TypeDocumentationModel
        {
            TypeName = cleanTypeName,
            DeclaringTypeName = declaringTypeName,
            DeclaringTypeChain = declaringTypeChain,
            NamespaceName = targetType.Namespace ?? string.Empty,
            AssemblyName = targetType.Assembly.GetName().Name ?? string.Empty,
            TypeCategory = CSharpSignatureFormatter.ResolveTypeCategory(targetType),
            CSharpDeclarationSignature = formatter.BuildTypeDeclarationSignature(targetType),
            EnumUnderlyingTypeLink = targetType.IsEnum
                ? ResolveTypeOrGenericLink(Enum.GetUnderlyingType(targetType))
                : string.Empty,
            AccessModifier = CSharpSignatureFormatter.ResolveTypeAccessModifier(targetType),
            ConstructorKind = formatter.DetermineConstructorKind(targetType),
            IsAbstractType = isAbstract,
            IsSealedType = isSealed,
            IsStaticType = isStatic,
            IsRecordType = isRecord,
            IsReadOnlyType = isReadOnly,
            IsRefStructType = isRefStruct,
            IsGenericType = ownGenericArguments.Length > 0,
            GenericTypeParameters = ownGenericArguments.Length > 0
                ? [.. ownGenericArguments.Select(argument => argument.Name)]
                : [],
            GenericTypeParameterDetails = ownGenericArguments.Length > 0
                ? ExtractGenericTypeParameterDetails(ownGenericArguments)
                : [],
            IsObsolete = isObsolete,
            ObsoleteMessage = obsoleteMessage,
            IsObsoleteError = isObsoleteError,
            ObsoleteDiagnosticId = obsoleteDiagnosticId,
            ObsoleteUrlFormat = obsoleteUrlFormat,
            IsExperimental = isExperimental,
            ExperimentalDiagnosticId = experimentalDiagnosticId,
            ExperimentalUrlFormat = experimentalUrlFormat,
            InheritanceHierarchy = BuildInheritanceHierarchy(targetType),
            ImplementedInterfaces = BuildImplementedInterfaces(targetType),
            AppliedAttributes = attributeExtractor.BuildAppliedAttributes(targetType),
            Constructors = BuildConstructors(targetType, typeFullName, nullabilityContext),
            Properties = BuildProperties(targetType, typeFullName, isRecord, extensionDiscovery, nullabilityContext),
            Methods = BuildMethods(targetType, typeFullName, isRecord, extensionDiscovery, nullabilityContext),
            Fields = BuildFields(targetType, typeFullName, nullabilityContext),
            Events = BuildEvents(targetType, typeFullName, nullabilityContext)
        };
    }

    internal static string BuildConstructorXmlKey(string declaringTypeFullName, ParameterInfo[] parameters) =>
        parameters.Length == 0
            ? $"M:{declaringTypeFullName}.#ctor"
            : $"M:{declaringTypeFullName}.#ctor({string.Join(",", parameters.Select(parameter => FormatXmlParameterType(parameter.ParameterType)))})";

    internal static string BuildMethodXmlKey(string declaringTypeFullName, string methodName, int genericArity, ParameterInfo[] parameters) =>
        BuildMethodXmlKey(declaringTypeFullName, methodName, genericArity, parameters, returnType: null);

    internal static string BuildMethodXmlKey(
        string declaringTypeFullName,
        string methodName,
        int genericArity,
        ParameterInfo[] parameters,
        Type? returnType)
    {
        var xmlMethodName = FormatXmlMemberMetadataName(methodName);
        var aritySuffix = genericArity > 0 ? $"``{genericArity}" : string.Empty;
        var baseSignature = parameters.Length == 0
            ? $"M:{declaringTypeFullName}.{xmlMethodName}{aritySuffix}"
            : $"M:{declaringTypeFullName}.{xmlMethodName}{aritySuffix}({string.Join(",", parameters.Select(parameter => FormatXmlParameterType(parameter.ParameterType)))})";

        return returnType != null &&
               (string.Equals(methodName, "op_Implicit", StringComparison.Ordinal) ||
                string.Equals(methodName, "op_Explicit", StringComparison.Ordinal) ||
                string.Equals(methodName, "op_CheckedExplicit", StringComparison.Ordinal))
            ? $"{baseSignature}~{FormatXmlParameterType(returnType)}"
            : baseSignature;
    }

    internal static string FormatXmlMemberMetadataName(string rawMemberName) =>
        !rawMemberName.Contains('.', StringComparison.Ordinal)
            ? rawMemberName
            : rawMemberName
                .Replace('.', '#')
                .Replace('<', '{')
                .Replace('>', '}');

    internal static string FormatXmlParameterType(Type parameterType)
    {
        if (parameterType.IsByRef)
        {
            return $"{FormatXmlParameterType(parameterType.GetElementType()!)}@";
        }

        if (parameterType.IsFunctionPointer)
        {
            var returnXmlType = FormatXmlParameterType(parameterType.GetFunctionPointerReturnType());
            var parameterXmlTypes = parameterType.GetFunctionPointerParameterTypes().Select(FormatXmlParameterType);
            return $"=FUNC:{returnXmlType}({string.Join(",", parameterXmlTypes)})";
        }

        if (parameterType.IsPointer)
        {
            return $"{FormatXmlParameterType(parameterType.GetElementType()!)}*";
        }

        if (parameterType.IsArray)
        {
            var elementType = parameterType.GetElementType()!;
            var rank = parameterType.GetArrayRank();
            var arrayDimensions = rank == 1
                ? "[]"
                : $"[{string.Join(",", Enumerable.Repeat("0:", rank))}]";
            return $"{FormatXmlParameterType(elementType)}{arrayDimensions}";
        }

        if (parameterType.IsGenericParameter)
        {
            return parameterType.DeclaringMethod != null
                ? $"``{parameterType.GenericParameterPosition}"
                : $"`{parameterType.GenericParameterPosition}";
        }

        if (!parameterType.IsGenericType)
        {
            var fullName = parameterType.FullName ?? parameterType.Name;
            return fullName.Replace('+', '.');
        }

        var genericDefinition = parameterType.GetGenericTypeDefinition();
        var definitionName = genericDefinition.FullName ?? genericDefinition.Name;
        var backtickIndex = definitionName.IndexOf('`');
        var cleanDefinitionName = (backtickIndex > 0 ? definitionName[..backtickIndex] : definitionName).Replace('+', '.');
        var genericArgTypes = parameterType.GetGenericArguments().Select(FormatXmlParameterType);
        return $"{cleanDefinitionName}{{{string.Join(",", genericArgTypes)}}}";
    }

    internal static string BuildPropertyXmlKey(string declaringTypeFullName, PropertyInfo propertyInfo)
    {
        var xmlPropertyName = FormatXmlMemberMetadataName(propertyInfo.Name);
        var indexParameters = propertyInfo.GetIndexParameters();
        return indexParameters.Length == 0
            ? $"P:{declaringTypeFullName}.{xmlPropertyName}"
            : $"P:{declaringTypeFullName}.{xmlPropertyName}({string.Join(",", indexParameters.Select(parameter => FormatXmlParameterType(parameter.ParameterType)))})";
    }

    private List<string> BuildInheritanceHierarchy(Type targetType)
    {
        if (targetType.IsInterface || targetType.IsEnum)
        {
            return [];
        }

        var currentBaseType = targetType.BaseType;
        List<string> baseList = [];

        while (currentBaseType != null && currentBaseType != typeof(object))
        {
            var resolvedBaseLink = ResolveTypeOrGenericLink(currentBaseType);
            var baseDisplay = resolvedBaseLink.Contains("](", StringComparison.Ordinal)
                ? resolvedBaseLink
                : formatter.FormatFullDisplayTypeName(currentBaseType);
            baseList.Add(baseDisplay);
            currentBaseType = currentBaseType.BaseType;
        }

        baseList.Reverse();
        return baseList;
    }

    private List<string> BuildImplementedInterfaces(Type targetType) =>
        targetType.IsEnum
            ? []
            :
            [
                .. targetType.GetInterfaces().Select(interfaceType =>
                {
                    var resolvedInterfaceLink = ResolveTypeOrGenericLink(interfaceType);
                    return resolvedInterfaceLink.Contains("](", StringComparison.Ordinal)
                        ? resolvedInterfaceLink
                        : formatter.FormatFullDisplayTypeName(interfaceType);
                })
            ];

    private List<MemberDocumentationModel> BuildConstructors(
        Type targetType,
        string typeFullName,
        NullabilityInfoContext nullabilityContext)
    {
        var hasPrimaryConstructor = formatter.TryGetPrimaryConstructor(targetType, out var primaryConstructor);
        var cleanTypeName = formatter.GetCleanTypeName(targetType);
        var constructors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        List<MemberDocumentationModel> constructorModels = [];

        foreach (var constructorInfo in constructors)
        {
            if (!CSharpSignatureFormatter.IsMethodAccessible(constructorInfo) ||
                constructorInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            var constructorParameters = constructorInfo.GetParameters();
            var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(constructorInfo);
            var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(constructorInfo);

            constructorModels.Add(new MemberDocumentationModel
            {
                MemberName = cleanTypeName,
                XmlMemberKey = BuildConstructorXmlKey(typeFullName, constructorParameters),
                MemberSignature = formatter.BuildConstructorSignature(constructorInfo, targetType),
                VisibilityModifier = CSharpSignatureFormatter.FormatMemberAccessModifier(constructorInfo),
                IsPrimaryConstructor = hasPrimaryConstructor && constructorInfo == primaryConstructor,
                MemberParameters =
                [
                    .. constructorParameters.Select(parameterInfo =>
                        CreateParameterDocumentationModel(parameterInfo, isExtensionMethodFirstParameter: false, nullabilityContext))
                ],
                AppliedAttributes = attributeExtractor.BuildAppliedAttributes(constructorInfo),
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage,
                IsObsoleteError = isObsoleteError,
                ObsoleteDiagnosticId = obsoleteDiagnosticId,
                ObsoleteUrlFormat = obsoleteUrlFormat,
                IsExperimental = isExperimental,
                ExperimentalDiagnosticId = experimentalDiagnosticId,
                ExperimentalUrlFormat = experimentalUrlFormat,
                IsEditorBrowsableNever = attributeExtractor.IsEditorBrowsableNever(constructorInfo)
            });
        }

        return constructorModels;
    }

    private List<MemberDocumentationModel> BuildProperties(
        Type targetType,
        string typeFullName,
        bool isRecordType,
        ExtensionBlockDiscovery extensionDiscovery,
        NullabilityInfoContext nullabilityContext)
    {
        var properties = targetType
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .OrderByDescending(propertyInfo => propertyInfo.DeclaringType == targetType)
            .ToArray();
        List<MemberDocumentationModel> propertyModels = [];
        HashSet<string> seenPropertySignatures = [with(StringComparer.Ordinal)];

        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.DeclaringType != null &&
                propertyInfo.DeclaringType != targetType &&
                propertyInfo.DeclaringType.Assembly != targetType.Assembly &&
                propertyInfo.DeclaringType.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
            {
                continue;
            }

            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);

            var isAccessible = (getMethod != null && CSharpSignatureFormatter.IsMethodAccessible(getMethod)) ||
                               (setMethod != null && CSharpSignatureFormatter.IsMethodAccessible(setMethod));

            if (!isAccessible ||
                propertyInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                IsRecordSynthesizedEqualityContract(propertyInfo, getMethod, isRecordType))
            {
                continue;
            }

            var indexParameters = propertyInfo.GetIndexParameters();
            var dedupSignatureKey = $"{propertyInfo.Name}({string.Join(',', indexParameters.Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name))})";
            if (!seenPropertySignatures.Add(dedupSignatureKey))
            {
                continue;
            }

            var representativeMethod = (getMethod != null && CSharpSignatureFormatter.IsMethodAccessible(getMethod))
                ? getMethod
                : setMethod!;
            var isExplicitInterfaceProperty = CSharpSignatureFormatter.IsExplicitInterfaceImplementation(representativeMethod);
            var isIndexer = indexParameters.Length > 0;
            var propertyNullability = nullabilityContext.Create(propertyInfo);
            var propertyTypeName = CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(propertyNullability, propertyInfo);
            var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(propertyInfo);
            var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(propertyInfo);
            var propertyMemberName = isIndexer
                ? isExplicitInterfaceProperty
                    ? CSharpSignatureFormatter.FormatExplicitInterfaceIndexerPrefix(propertyInfo.Name)
                    : "this"
                : isExplicitInterfaceProperty
                    ? CSharpSignatureFormatter.FormatExplicitInterfaceMemberName(propertyInfo.Name)
                    : propertyInfo.Name;

            propertyModels.Add(new MemberDocumentationModel
            {
                MemberName = propertyMemberName,
                XmlMemberKey = BuildPropertyXmlKey(typeFullName, propertyInfo),
                ReturnTypeName = propertyTypeName,
                ResolvedReturnTypeDocumentationLink = ResolveTypeOrGenericLink(propertyNullability, propertyInfo),
                MemberSignature = formatter.BuildPropertySignature(propertyInfo),
                VisibilityModifier = CSharpSignatureFormatter.FormatMemberAccessModifier(representativeMethod),
                IsStaticMember = (getMethod != null && getMethod.IsStatic) || (setMethod != null && setMethod.IsStatic),
                MemberParameters = isIndexer
                    ?
                    [
                        .. indexParameters.Select(parameterInfo =>
                            CreateParameterDocumentationModel(parameterInfo, isExtensionMethodFirstParameter: false, nullabilityContext))
                    ]
                    : [],
                AppliedAttributes = attributeExtractor.BuildAppliedAttributes(propertyInfo),
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage,
                IsObsoleteError = isObsoleteError,
                ObsoleteDiagnosticId = obsoleteDiagnosticId,
                ObsoleteUrlFormat = obsoleteUrlFormat,
                IsExperimental = isExperimental,
                ExperimentalDiagnosticId = experimentalDiagnosticId,
                ExperimentalUrlFormat = experimentalUrlFormat,
                IsEditorBrowsableNever = attributeExtractor.IsEditorBrowsableNever(propertyInfo)
            });
        }

        AppendExtensionPropertyModels(targetType, typeFullName, extensionDiscovery, nullabilityContext, propertyModels);
        return propertyModels;
    }

    private void AppendExtensionPropertyModels(
        Type staticType,
        string typeFullName,
        ExtensionBlockDiscovery extensionDiscovery,
        NullabilityInfoContext nullabilityContext,
        List<MemberDocumentationModel> propertyModels)
    {
        if (extensionDiscovery.Properties.Count == 0)
        {
            return;
        }

        var staticMethods = staticType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var entry in extensionDiscovery.Properties)
        {
            var skeletonProperty = entry.SkeletonProperty;
            var markerInfo = entry.MarkerInfo;
            var expectedGetterParams = markerInfo.IsStaticExtension ? 0 : 1;
            var expectedSetterParams = markerInfo.IsStaticExtension ? 1 : 2;
            var matchingGetter = FindExtensionPropertyAccessor(staticMethods, $"get_{skeletonProperty.Name}", expectedGetterParams, entry.NestedGenericArity);
            var matchingSetter = FindExtensionPropertyAccessor(staticMethods, $"set_{skeletonProperty.Name}", expectedSetterParams, entry.NestedGenericArity);
            var representativeAccessor = matchingGetter ?? matchingSetter;
            if (representativeAccessor == null || !CSharpSignatureFormatter.IsMethodAccessible(representativeAccessor))
            {
                continue;
            }

            var isStaticExtensionProperty = markerInfo.IsStaticExtension;
            var propertyNullability = nullabilityContext.Create(skeletonProperty);
            var propertyTypeName = CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(propertyNullability, skeletonProperty);
            var declaringContainerName = (skeletonProperty.DeclaringType?.FullName ?? typeFullName).Replace('+', '.');

            var appliedAttributes = new List<string>(attributeExtractor.BuildAppliedAttributes(skeletonProperty));
            foreach (var accessorAttribute in attributeExtractor.BuildAppliedAttributes(representativeAccessor))
            {
                if (!appliedAttributes.Contains(accessorAttribute, StringComparer.Ordinal))
                {
                    appliedAttributes.Add(accessorAttribute);
                }
            }

            var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(skeletonProperty);
            if (!isObsolete)
            {
                (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(representativeAccessor);
            }

            var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(skeletonProperty);
            if (!isExperimental)
            {
                (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(representativeAccessor);
            }

            var signature = CSharpSignatureFormatter.BuildExtensionPropertySignature(
                skeletonProperty,
                representativeAccessor,
                matchingGetter,
                matchingSetter);

            propertyModels.Add(new MemberDocumentationModel
            {
                MemberName = skeletonProperty.Name,
                XmlMemberKey = $"P:{declaringContainerName}.{skeletonProperty.Name}",
                ReturnTypeName = propertyTypeName,
                ResolvedReturnTypeDocumentationLink = ResolveTypeOrGenericLink(propertyNullability, skeletonProperty),
                MemberSignature = signature,
                VisibilityModifier = "public",
                IsStaticMember = isStaticExtensionProperty,
                IsExtensionProperty = true,
                ExtensionTargetTypeFullName = ExtractCleanExtensionTargetFullName(markerInfo.ReceiverType),
                AppliedAttributes = appliedAttributes,
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage,
                IsObsoleteError = isObsoleteError,
                ObsoleteDiagnosticId = obsoleteDiagnosticId,
                ObsoleteUrlFormat = obsoleteUrlFormat,
                IsExperimental = isExperimental,
                ExperimentalDiagnosticId = experimentalDiagnosticId,
                ExperimentalUrlFormat = experimentalUrlFormat,
                IsEditorBrowsableNever = attributeExtractor.IsEditorBrowsableNever(skeletonProperty) ||
                                         attributeExtractor.IsEditorBrowsableNever(representativeAccessor)
            });
        }
    }

    private List<MemberDocumentationModel> BuildMethods(
        Type targetType,
        string typeFullName,
        bool isRecordType,
        ExtensionBlockDiscovery extensionDiscovery,
        NullabilityInfoContext nullabilityContext)
    {
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        List<MemberDocumentationModel> methodModels = [];

        foreach (var methodInfo in methods)
        {
            var methodParameters = methodInfo.GetParameters();
            var genericArity = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0;
            ExtensionMethodSignatureKey signatureKey = new(methodInfo.Name, methodParameters.Length, genericArity);

            if (ShouldSkipMethod(methodInfo, isRecordType) ||
                extensionDiscovery.PropertyAccessorKeys.Contains(signatureKey))
            {
                continue;
            }

            var returnNullability = nullabilityContext.Create(methodInfo.ReturnParameter);
            var returnTypeName = CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(returnNullability, methodInfo.ReturnParameter);
            var isAsyncEnumerable = formatter.IsAsyncEnumerableType(methodInfo.ReturnType);
            var isAsyncMethod = isAsyncEnumerable ||
                                formatter.IsAwaitableReturnType(methodInfo.ReturnType) ||
                                formatter.HasAsyncStateMachineAttribute(methodInfo);
            var isClassicExtensionMethod = methodInfo.IsDefined(typeof(ExtensionAttribute), inherit: false);
            _ = extensionDiscovery.ExtensionMethodTargets.TryGetValue(signatureKey, out var markerInfo);

            var extensionTargetFullName = isClassicExtensionMethod && methodParameters.Length > 0
                ? ExtractCleanExtensionTargetFullName(methodParameters[0].ParameterType)
                : markerInfo != null
                    ? ExtractCleanExtensionTargetFullName(markerInfo.ReceiverType)
                    : string.Empty;

            var isExtensionOperator = markerInfo != null &&
                                      CSharpSignatureFormatter.TryGetOperatorDisplayToken(methodInfo.Name, out _, out _);
            var isClassicFirstParameterExtension = !isExtensionOperator &&
                                                   ((isClassicExtensionMethod && markerInfo == null) ||
                                                    (markerInfo != null && !markerInfo.IsStaticExtension));

            var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(methodInfo);
            var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(methodInfo);
            var declaringTypeFullName = (methodInfo.DeclaringType?.FullName ?? typeFullName).Replace('+', '.');

            methodModels.Add(new MemberDocumentationModel
            {
                MemberName = FormatMethodMemberName(methodInfo),
                XmlMemberKey = BuildMethodXmlKey(declaringTypeFullName, methodInfo.Name, genericArity, methodParameters, methodInfo.ReturnType),
                ReturnTypeName = returnTypeName,
                ResolvedReturnTypeDocumentationLink = ResolveTypeOrGenericLink(returnNullability, methodInfo.ReturnParameter),
                MemberSignature = formatter.BuildMethodSignature(methodInfo, isAsyncMethod),
                VisibilityModifier = CSharpSignatureFormatter.FormatMemberAccessModifier(methodInfo),
                IsStaticMember = methodInfo.IsStatic,
                IsAsyncMethod = isAsyncMethod,
                IsAsyncEnumerable = isAsyncEnumerable,
                ExtensionTargetTypeFullName = extensionTargetFullName,
                AppliedAttributes = attributeExtractor.BuildAppliedAttributes(methodInfo),
                ReturnAttributes = attributeExtractor.BuildAppliedAttributes(methodInfo.ReturnParameter),
                GenericTypeParameters = methodInfo.IsGenericMethod
                    ? ExtractGenericTypeParameterDetails(methodInfo.GetGenericArguments())
                    : [],
                MemberParameters =
                [
                    .. methodParameters.Select((parameterInfo, parameterIndex) =>
                        CreateParameterDocumentationModel(parameterInfo, isClassicFirstParameterExtension && parameterIndex == 0, nullabilityContext))
                ],
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage,
                IsObsoleteError = isObsoleteError,
                ObsoleteDiagnosticId = obsoleteDiagnosticId,
                ObsoleteUrlFormat = obsoleteUrlFormat,
                IsExperimental = isExperimental,
                ExperimentalDiagnosticId = experimentalDiagnosticId,
                ExperimentalUrlFormat = experimentalUrlFormat,
                IsEditorBrowsableNever = attributeExtractor.IsEditorBrowsableNever(methodInfo)
            });
        }

        return methodModels;
    }

    private List<MemberDocumentationModel> BuildFields(
        Type targetType,
        string typeFullName,
        NullabilityInfoContext nullabilityContext)
    {
        var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var inlineArrayAttribute = targetType.GetCustomAttribute<InlineArrayAttribute>();
        List<MemberDocumentationModel> fieldModels = [];

        foreach (var fieldInfo in fields)
        {
            var isInlineArrayElementField = inlineArrayAttribute != null && !fieldInfo.IsStatic && !fieldInfo.IsSpecialName;
            if (fieldInfo.IsSpecialName ||
                fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                (!CSharpSignatureFormatter.IsFieldAccessible(fieldInfo) && !isInlineArrayElementField))
            {
                continue;
            }

            var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(fieldInfo);
            var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(fieldInfo);

            if (targetType.IsEnum && fieldInfo.IsLiteral)
            {
                var underlyingEnumType = Enum.GetUnderlyingType(targetType);
                var underlyingTypeName = formatter.FormatDisplayTypeName(underlyingEnumType);
                var numericValueText = Convert.ToString(fieldInfo.GetRawConstantValue(), CultureInfo.InvariantCulture) ?? "0";

                fieldModels.Add(new MemberDocumentationModel
                {
                    MemberName = fieldInfo.Name,
                    XmlMemberKey = $"F:{typeFullName}.{fieldInfo.Name}",
                    ReturnTypeName = underlyingTypeName,
                    ResolvedReturnTypeDocumentationLink = ResolveTypeOrGenericLink(underlyingEnumType),
                    ReturnValueDescription = numericValueText,
                    MemberSignature = $"{fieldInfo.Name} = {numericValueText}",
                    VisibilityModifier = "public",
                    IsStaticMember = true,
                    AppliedAttributes = attributeExtractor.BuildAppliedAttributes(fieldInfo),
                    IsObsolete = isObsolete,
                    ObsoleteMessage = obsoleteMessage,
                    IsObsoleteError = isObsoleteError,
                    ObsoleteDiagnosticId = obsoleteDiagnosticId,
                    ObsoleteUrlFormat = obsoleteUrlFormat,
                    IsExperimental = isExperimental,
                    ExperimentalDiagnosticId = experimentalDiagnosticId,
                    ExperimentalUrlFormat = experimentalUrlFormat
                });
                continue;
            }

            string fieldTypeName;
            string resolvedFieldTypeLink;
            if (fieldInfo.GetCustomAttribute<FixedBufferAttribute>() is { } fixedBufferAttribute)
            {
                var fixedElementName = formatter.FormatDisplayTypeName(fixedBufferAttribute.ElementType);
                fieldTypeName = $"{fixedElementName}[{fixedBufferAttribute.Length}]";
                resolvedFieldTypeLink = $"{ResolveTypeOrGenericLink(fixedBufferAttribute.ElementType)}[{fixedBufferAttribute.Length}]";
            }
            else if (isInlineArrayElementField && inlineArrayAttribute != null)
            {
                var fieldNullability = nullabilityContext.Create(fieldInfo);
                var elementTypeName = CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(fieldNullability, fieldInfo);
                fieldTypeName = $"{elementTypeName}[{inlineArrayAttribute.Length}]";
                resolvedFieldTypeLink = $"{ResolveTypeOrGenericLink(fieldNullability, fieldInfo)}[{inlineArrayAttribute.Length}]";
            }
            else
            {
                var fieldNullability = nullabilityContext.Create(fieldInfo);
                fieldTypeName = CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(fieldNullability, fieldInfo);
                resolvedFieldTypeLink = ResolveTypeOrGenericLink(fieldNullability, fieldInfo);
            }

            fieldModels.Add(new MemberDocumentationModel
            {
                MemberName = fieldInfo.Name,
                XmlMemberKey = $"F:{typeFullName}.{fieldInfo.Name}",
                ReturnTypeName = fieldTypeName,
                ResolvedReturnTypeDocumentationLink = resolvedFieldTypeLink,
                MemberSignature = formatter.BuildFieldSignature(fieldInfo, fieldTypeName),
                VisibilityModifier = CSharpSignatureFormatter.FormatFieldAccessModifier(fieldInfo),
                IsStaticMember = fieldInfo.IsStatic || fieldInfo.IsLiteral,
                AppliedAttributes = attributeExtractor.BuildAppliedAttributes(fieldInfo),
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage,
                IsObsoleteError = isObsoleteError,
                ObsoleteDiagnosticId = obsoleteDiagnosticId,
                ObsoleteUrlFormat = obsoleteUrlFormat,
                IsExperimental = isExperimental,
                ExperimentalDiagnosticId = experimentalDiagnosticId,
                ExperimentalUrlFormat = experimentalUrlFormat,
                IsEditorBrowsableNever = attributeExtractor.IsEditorBrowsableNever(fieldInfo)
            });
        }

        return fieldModels;
    }

    private List<MemberDocumentationModel> BuildEvents(
        Type targetType,
        string typeFullName,
        NullabilityInfoContext nullabilityContext)
    {
        var events = targetType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        List<MemberDocumentationModel> eventModels = [];

        foreach (var eventInfo in events)
        {
            var addMethod = eventInfo.GetAddMethod(true);
            if (addMethod == null ||
                eventInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                !CSharpSignatureFormatter.IsMethodAccessible(addMethod))
            {
                continue;
            }

            var isExplicitInterfaceEvent = CSharpSignatureFormatter.IsExplicitInterfaceImplementation(addMethod);
            var eventHandlerType = eventInfo.EventHandlerType;
            var eventHandlerNullability = eventHandlerType != null ? nullabilityContext.Create(eventInfo) : null;
            var eventHandlerTypeName = eventHandlerNullability != null
                ? CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(eventHandlerNullability, eventInfo)
                : "EventHandler";
            var resolvedEventLink = eventHandlerNullability != null
                ? ResolveTypeOrGenericLink(eventHandlerNullability, eventInfo)
                : linkResolver.ResolveLink("System.EventHandler");
            var (isObsolete, obsoleteMessage, isObsoleteError, obsoleteDiagnosticId, obsoleteUrlFormat) = attributeExtractor.ExtractObsoleteMetadata(eventInfo);
            var (isExperimental, experimentalDiagnosticId, experimentalUrlFormat) = attributeExtractor.ExtractExperimentalMetadata(eventInfo);
            var eventMemberName = isExplicitInterfaceEvent
                ? CSharpSignatureFormatter.FormatExplicitInterfaceMemberName(eventInfo.Name)
                : eventInfo.Name;
            var xmlEventName = FormatXmlMemberMetadataName(eventInfo.Name);

            eventModels.Add(new MemberDocumentationModel
            {
                MemberName = eventMemberName,
                XmlMemberKey = $"E:{typeFullName}.{xmlEventName}",
                ReturnTypeName = eventHandlerTypeName,
                ResolvedReturnTypeDocumentationLink = resolvedEventLink,
                MemberSignature = formatter.BuildEventSignature(eventInfo),
                VisibilityModifier = CSharpSignatureFormatter.FormatMemberAccessModifier(addMethod),
                IsStaticMember = addMethod.IsStatic,
                AppliedAttributes = attributeExtractor.BuildAppliedAttributes(eventInfo),
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage,
                IsObsoleteError = isObsoleteError,
                ObsoleteDiagnosticId = obsoleteDiagnosticId,
                ObsoleteUrlFormat = obsoleteUrlFormat,
                IsExperimental = isExperimental,
                ExperimentalDiagnosticId = experimentalDiagnosticId,
                ExperimentalUrlFormat = experimentalUrlFormat,
                IsEditorBrowsableNever = attributeExtractor.IsEditorBrowsableNever(eventInfo)
            });
        }

        return eventModels;
    }

    private string ResolveTypeOrGenericLink(NullabilityInfo nullabilityInfo, ICustomAttributeProvider? attributeProvider = null)
    {
        var rawTargetType = nullabilityInfo.Type;
        var targetType = rawTargetType.IsByRef
            ? (rawTargetType.GetElementType() ?? rawTargetType)
            : rawTargetType;

        if (targetType.IsFunctionPointer)
        {
            var modifiedFunctionPointerType = CSharpSignatureFormatter.TryExtractModifiedFunctionPointerType(attributeProvider);
            return ResolveFunctionPointerLink(modifiedFunctionPointerType ?? targetType);
        }

        if (targetType.IsPointer)
        {
            var innerLink = nullabilityInfo.ElementType != null
                ? ResolveTypeOrGenericLink(nullabilityInfo.ElementType)
                : ResolveTypeOrGenericLink(targetType.GetElementType()!);
            return $"{innerLink}*";
        }

        var isByRefNullable = rawTargetType.IsByRef && nullabilityInfo.WriteState == NullabilityState.Nullable;
        var nullableSuffix = nullabilityInfo.ReadState == NullabilityState.Nullable || isByRefNullable
            ? "?"
            : string.Empty;

        if (targetType.IsArray)
        {
            var rank = targetType.GetArrayRank();
            var brackets = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            var elementLink = nullabilityInfo.ElementType != null
                ? ResolveTypeOrGenericLink(nullabilityInfo.ElementType)
                : ResolveTypeOrGenericLink(targetType.GetElementType()!);
            return $"{elementLink}{brackets}{nullableSuffix}";
        }

        if (Nullable.GetUnderlyingType(targetType) is { } underlyingValueType)
        {
            var underlyingLink = nullabilityInfo.GenericTypeArguments.Length > 0
                ? ResolveTypeOrGenericLink(nullabilityInfo.GenericTypeArguments[0])
                : ResolveTypeOrGenericLink(underlyingValueType);
            return underlyingLink.EndsWith('?') ? underlyingLink : $"{underlyingLink}?";
        }

        if (targetType.IsGenericParameter)
        {
            return $"`{targetType.Name}`{nullableSuffix}";
        }

        if (CSharpSignatureFormatter.IsValueTupleType(targetType) && attributeProvider != null)
        {
            var tupleNames = CSharpSignatureFormatter.ExtractTupleElementNames(attributeProvider);
            if (tupleNames is { Count: > 0 } && tupleNames.Any(name => !string.IsNullOrWhiteSpace(name)))
            {
                return ResolveNamedValueTupleLink(nullabilityInfo, tupleNames, nullableSuffix);
            }
        }

        if (!targetType.IsGenericType || CSharpSignatureFormatter.ExtractOwnGenericArguments(targetType).Length == 0)
        {
            var fullTypeName = targetType.FullName ?? targetType.Name;
            return $"{linkResolver.ResolveLink(fullTypeName)}{nullableSuffix}";
        }

        var genericTypeDefinition = targetType.GetGenericTypeDefinition();
        var definitionFullName = genericTypeDefinition.FullName ?? genericTypeDefinition.Name;
        var resolvedOuterLink = linkResolver.ResolveLink(definitionFullName);
        var ownCount = CSharpSignatureFormatter.ExtractOwnGenericArguments(targetType).Length;
        IEnumerable<string> resolvedArguments;

        if (nullabilityInfo.GenericTypeArguments.Length == targetType.GetGenericArguments().Length &&
            nullabilityInfo.GenericTypeArguments.Length >= ownCount)
        {
            var startIndex = nullabilityInfo.GenericTypeArguments.Length - ownCount;
            resolvedArguments = nullabilityInfo.GenericTypeArguments[startIndex..].Select(argumentNullability => ResolveTypeOrGenericLink(argumentNullability));
        }
        else
        {
            resolvedArguments = CSharpSignatureFormatter.ExtractOwnGenericArguments(targetType).Select(ResolveTypeOrGenericLink);
        }

        return $"{resolvedOuterLink}<{string.Join(", ", resolvedArguments)}>{nullableSuffix}";
    }

    private string ResolveNamedValueTupleLink(
        NullabilityInfo tupleNullability,
        IReadOnlyList<string?> tupleNames,
        string nullableSuffix)
    {
        List<NullabilityInfo> flattenedElements = [];
        FlattenTupleNullabilityElements(tupleNullability, flattenedElements);
        List<string> linkedElements = [];

        for (var index = 0; index < flattenedElements.Count; index++)
        {
            var elementLink = ResolveTypeOrGenericLink(flattenedElements[index]);
            var elementName = index < tupleNames.Count ? tupleNames[index] : null;
            linkedElements.Add(!string.IsNullOrWhiteSpace(elementName)
                ? $"{elementLink} {elementName}"
                : elementLink);
        }

        return $"({string.Join(", ", linkedElements)}){nullableSuffix}";
    }

    private static void FlattenTupleNullabilityElements(
        NullabilityInfo tupleNullability,
        List<NullabilityInfo> flattenedElements)
    {
        var genericArguments = tupleNullability.GenericTypeArguments;
        for (var index = 0; index < genericArguments.Length; index++)
        {
            if (index == 7 && CSharpSignatureFormatter.IsValueTupleType(genericArguments[index].Type))
            {
                FlattenTupleNullabilityElements(genericArguments[index], flattenedElements);
            }
            else
            {
                flattenedElements.Add(genericArguments[index]);
            }
        }
    }

    private string ResolveFunctionPointerLink(Type functionPointerType)
    {
        var prefix = CSharpSignatureFormatter.FormatFunctionPointerPrefix(functionPointerType);
        var parameterLinks = functionPointerType.GetFunctionPointerParameterTypes().Select(ResolveTypeOrGenericLink);
        var returnLink = ResolveTypeOrGenericLink(functionPointerType.GetFunctionPointerReturnType());
        return $"{prefix}<{string.Join(", ", [.. parameterLinks, returnLink])}>";
    }

    private string ResolveTypeOrGenericLink(Type targetType)
    {
        if (targetType.IsByRef)
        {
            return ResolveTypeOrGenericLink(targetType.GetElementType()!);
        }

        if (targetType.IsFunctionPointer)
        {
            return ResolveFunctionPointerLink(targetType);
        }

        if (targetType.IsPointer)
        {
            return $"{ResolveTypeOrGenericLink(targetType.GetElementType()!)}*";
        }

        if (targetType.IsArray)
        {
            var rank = targetType.GetArrayRank();
            var brackets = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            return $"{ResolveTypeOrGenericLink(targetType.GetElementType()!)}{brackets}";
        }

        if (Nullable.GetUnderlyingType(targetType) is { } underlyingValueType)
        {
            return $"{ResolveTypeOrGenericLink(underlyingValueType)}?";
        }

        if (targetType.IsGenericParameter)
        {
            return $"`{targetType.Name}`";
        }

        var ownArguments = CSharpSignatureFormatter.ExtractOwnGenericArguments(targetType);
        if (ownArguments.Length == 0)
        {
            var fullTypeName = targetType.FullName ?? targetType.Name;
            return linkResolver.ResolveLink(fullTypeName);
        }

        var genericTypeDefinition = targetType.GetGenericTypeDefinition();
        var definitionFullName = genericTypeDefinition.FullName ?? genericTypeDefinition.Name;
        var resolvedOuterLink = linkResolver.ResolveLink(definitionFullName);
        var resolvedArguments = ownArguments.Select(ResolveTypeOrGenericLink);
        return $"{resolvedOuterLink}<{string.Join(", ", resolvedArguments)}>";
    }

    private List<ParameterDocumentationModel> ExtractGenericTypeParameterDetails(Type[] genericArguments)
    {
        List<ParameterDocumentationModel> parameterDetails = [];
        foreach (var genericParameter in genericArguments)
        {
            var variance = genericParameter.GenericParameterAttributes & GenericParameterAttributes.VarianceMask;
            var varianceModifier = variance == GenericParameterAttributes.Covariant
                ? "out"
                : variance == GenericParameterAttributes.Contravariant
                    ? "in"
                    : string.Empty;

            parameterDetails.Add(new ParameterDocumentationModel
            {
                ParameterName = genericParameter.Name,
                ParameterTypeName = genericParameter.Name,
                ParameterModifier = varianceModifier,
                ParameterConstraints = FormatGenericParameterConstraints(genericParameter),
                AppliedAttributes = attributeExtractor.BuildAppliedAttributes(genericParameter)
            });
        }

        return parameterDetails;
    }

    private string FormatGenericParameterConstraints(Type genericParameter)
    {
        if (!genericParameter.IsGenericParameter)
        {
            return string.Empty;
        }

        List<string> constraintsList = [];
        var rawAttributes = genericParameter.GenericParameterAttributes;
        var specialConstraints = rawAttributes & GenericParameterAttributes.SpecialConstraintMask;
        var isValueTypeConstraint = (specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
        var isReferenceTypeConstraint = (specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0;

        if (isValueTypeConstraint)
        {
            var isUnmanaged = genericParameter.GetCustomAttributes(inherit: false)
                .Any(attribute => string.Equals(attribute.GetType().Name, "IsUnmanagedAttribute", StringComparison.Ordinal));
            constraintsList.Add(isUnmanaged
                ? $"[unmanaged]({learnRegistry.UnmanagedConstraintDocUrl})"
                : $"[struct]({learnRegistry.StructConstraintDocUrl})");
        }
        else if (isReferenceTypeConstraint)
        {
            constraintsList.Add($"[class]({learnRegistry.ClassConstraintDocUrl})");
        }
        else if (HasNotNullConstraint(genericParameter))
        {
            constraintsList.Add($"[notnull]({learnRegistry.NotNullConstraintDocUrl})");
        }

        foreach (var typeConstraint in genericParameter.GetGenericParameterConstraints())
        {
            if (isValueTypeConstraint && typeConstraint == typeof(ValueType))
            {
                continue;
            }

            constraintsList.Add(ResolveTypeOrGenericLink(typeConstraint));
        }

        if (!isValueTypeConstraint && (specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
        {
            constraintsList.Add($"[new()]({learnRegistry.NewConstraintDocUrl})");
        }

        if ((rawAttributes & GenericParameterAttributes.AllowByRefLike) != 0)
        {
            constraintsList.Add($"[allows ref struct]({learnRegistry.AllowsRefStructConstraintDocUrl})");
        }

        return string.Join(", ", constraintsList);
    }

    internal static bool HasNotNullConstraint(Type genericParameter)
    {
        var nullableAttribute = genericParameter.GetCustomAttributesData()
            .FirstOrDefault(attributeData => string.Equals(
                attributeData.AttributeType.FullName,
                "System.Runtime.CompilerServices.NullableAttribute",
                StringComparison.Ordinal));

        if (nullableAttribute != null && nullableAttribute.ConstructorArguments.Count > 0)
        {
            var argument = nullableAttribute.ConstructorArguments[0];
            if (argument.Value is byte singleFlag)
            {
                return singleFlag == 1;
            }

            if (argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> arrayFlags &&
                arrayFlags.FirstOrDefault().Value is byte firstFlag)
            {
                return firstFlag == 1;
            }
        }

        MemberInfo? declaringMember = genericParameter.DeclaringMethod ?? (MemberInfo?)genericParameter.DeclaringType;
        while (declaringMember != null)
        {
            var contextAttribute = declaringMember.GetCustomAttributesData()
                .FirstOrDefault(attributeData => string.Equals(
                    attributeData.AttributeType.FullName,
                    "System.Runtime.CompilerServices.NullableContextAttribute",
                    StringComparison.Ordinal));
            if (contextAttribute != null &&
                contextAttribute.ConstructorArguments.Count > 0 &&
                contextAttribute.ConstructorArguments[0].Value is byte contextFlag)
            {
                return contextFlag == 1;
            }

            declaringMember = declaringMember.DeclaringType;
        }

        return false;
    }

    private ParameterDocumentationModel CreateParameterDocumentationModel(
        ParameterInfo parameterInfo,
        bool isExtensionMethodFirstParameter,
        NullabilityInfoContext nullabilityContext)
    {
        var effectiveType = parameterInfo.ParameterType.IsByRef
            ? (parameterInfo.ParameterType.GetElementType() ?? parameterInfo.ParameterType)
            : parameterInfo.ParameterType;
        var paramNullability = nullabilityContext.Create(parameterInfo);
        var paramTypeName = CSharpSignatureFormatter.FormatNullabilityWithCompilerMetadata(paramNullability, parameterInfo);
        var isParams = CSharpSignatureFormatter.IsParamsParameter(parameterInfo);
        var parameterModifier = CSharpSignatureFormatter.DetermineParameterModifier(parameterInfo, isParams, isExtensionMethodFirstParameter);
        var isOptional = parameterInfo.IsOptional;
        var defaultValue = isOptional || parameterInfo.HasDefaultValue
            ? CSharpSignatureFormatter.FormatParameterDefaultValue(parameterInfo, effectiveType)
            : string.Empty;
        var isNullable = DetermineIsNullableParameter(parameterInfo, effectiveType, nullabilityContext);
        var callerInfoAttribute = attributeExtractor.ExtractCallerInformationAttribute(parameterInfo);
        var isCancellationToken = effectiveType == typeof(CancellationToken) ||
                                  Nullable.GetUnderlyingType(effectiveType) == typeof(CancellationToken);
        var isEnumeratorCancellation = parameterInfo.GetCustomAttribute<EnumeratorCancellationAttribute>() != null;

        return new ParameterDocumentationModel
        {
            ParameterName = parameterInfo.Name ?? "param",
            ParameterTypeName = paramTypeName,
            ResolvedTypeDocumentationLink = ResolveTypeOrGenericLink(paramNullability, parameterInfo),
            IsCancellationToken = isCancellationToken,
            IsEnumeratorCancellation = isEnumeratorCancellation,
            IsOptional = isOptional,
            DefaultValue = defaultValue,
            IsParams = isParams,
            ParameterModifier = parameterModifier,
            IsExtensionTarget = isExtensionMethodFirstParameter,
            IsNullable = isNullable,
            CallerInformationAttribute = callerInfoAttribute,
            AppliedAttributes = attributeExtractor.BuildAppliedAttributes(parameterInfo)
        };
    }

    private static bool IsRecordSynthesizedEqualityContract(PropertyInfo propertyInfo, MethodInfo? getMethod, bool isRecordType) =>
        isRecordType &&
        string.Equals(propertyInfo.Name, "EqualityContract", StringComparison.Ordinal) &&
        propertyInfo.PropertyType == typeof(Type) &&
        getMethod != null &&
        getMethod.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    internal static bool ShouldSkipMethod(MethodInfo methodInfo, bool isRecordType) =>
        !CSharpSignatureFormatter.IsMethodAccessible(methodInfo) ||
        methodInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
        (isRecordType && string.Equals(methodInfo.Name, "<Clone>$", StringComparison.Ordinal)) ||
        (methodInfo.IsSpecialName && !CSharpSignatureFormatter.TryGetOperatorDisplayToken(methodInfo.Name, out _, out _));

    internal static string FormatMethodMemberName(MethodInfo methodInfo) =>
        CSharpSignatureFormatter.IsFinalizerMethod(methodInfo)
            ? $"~{CSharpSignatureFormatter.ExtractCleanTypeName(methodInfo.DeclaringType!)}"
            : CSharpSignatureFormatter.TryGetOperatorDisplayToken(methodInfo.Name, out var operatorToken, out var isConversion)
                ? isConversion ? $"operator {operatorToken}" : $"operator {operatorToken}"
                : CSharpSignatureFormatter.IsExplicitInterfaceImplementation(methodInfo)
                    ? CSharpSignatureFormatter.FormatExplicitInterfaceMemberName(methodInfo.Name)
                    : methodInfo.Name;

    private static ExtensionBlockDiscovery DiscoverExtensionBlocks(Type staticType)
    {
        Dictionary<string, ExtensionMarkerInfo> markersByName = [with(StringComparer.Ordinal)];
        Dictionary<ExtensionMethodSignatureKey, ExtensionMarkerInfo> extensionMethodTargets = [];
        HashSet<ExtensionMethodSignatureKey> propertyAccessorKeys = [];
        List<ExtensionPropertyEntry> properties = [];

        foreach (var nestedType in staticType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            if ((!nestedType.IsSpecialName && !nestedType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)) ||
                !nestedType.IsDefined(typeof(ExtensionAttribute), inherit: false))
            {
                continue;
            }

            var nestedGenericArity = nestedType.IsGenericType ? nestedType.GetGenericArguments().Length : 0;

            foreach (var markerType in nestedType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                var markerMethod = markerType.GetMethod("<Extension>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (markerMethod == null)
                {
                    continue;
                }

                var receiverParameter = markerMethod.GetParameters().FirstOrDefault();
                if (receiverParameter == null)
                {
                    continue;
                }

                markersByName[markerType.Name] = new ExtensionMarkerInfo(
                    markerType.Name,
                    string.IsNullOrWhiteSpace(receiverParameter.Name),
                    receiverParameter.ParameterType);
            }

            foreach (var candidateProperty in nestedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var markerName = TryGetExtensionMarkerName(candidateProperty);
                if (string.IsNullOrWhiteSpace(markerName) || !markersByName.TryGetValue(markerName, out var markerInfo))
                {
                    continue;
                }

                properties.Add(new ExtensionPropertyEntry(candidateProperty, markerInfo, nestedGenericArity));
                if (candidateProperty.CanRead)
                {
                    _ = propertyAccessorKeys.Add(new ExtensionMethodSignatureKey(
                        $"get_{candidateProperty.Name}",
                        markerInfo.IsStaticExtension ? 0 : 1,
                        nestedGenericArity));
                }

                if (candidateProperty.CanWrite)
                {
                    _ = propertyAccessorKeys.Add(new ExtensionMethodSignatureKey(
                        $"set_{candidateProperty.Name}",
                        markerInfo.IsStaticExtension ? 1 : 2,
                        nestedGenericArity));
                }
            }

            foreach (var candidateMethod in nestedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (candidateMethod.IsSpecialName && !CSharpSignatureFormatter.TryGetOperatorDisplayToken(candidateMethod.Name, out _, out _))
                {
                    continue;
                }

                var markerName = TryGetExtensionMarkerName(candidateMethod);
                if (string.IsNullOrWhiteSpace(markerName) || !markersByName.TryGetValue(markerName, out var markerInfo))
                {
                    continue;
                }

                var candidateGenericArity = candidateMethod.IsGenericMethod ? candidateMethod.GetGenericArguments().Length : 0;
                var totalGenericArity = nestedGenericArity + candidateGenericArity;
                var expectedImplementationParamCount = markerInfo.IsStaticExtension || candidateMethod.IsStatic
                    ? candidateMethod.GetParameters().Length
                    : candidateMethod.GetParameters().Length + 1;
                extensionMethodTargets[new ExtensionMethodSignatureKey(candidateMethod.Name, expectedImplementationParamCount, totalGenericArity)] = markerInfo;
            }
        }

        return new ExtensionBlockDiscovery(extensionMethodTargets, propertyAccessorKeys, properties);
    }

    private static string? TryGetExtensionMarkerName(MemberInfo memberInfo)
    {
        var markerAttribute = memberInfo.GetCustomAttributesData()
            .FirstOrDefault(attributeData =>
                string.Equals(attributeData.AttributeType.Name, "ExtensionMarkerAttribute", StringComparison.Ordinal) ||
                string.Equals(attributeData.AttributeType.Name, "ExtensionMarkerNameAttribute", StringComparison.Ordinal));
        return markerAttribute?.ConstructorArguments.FirstOrDefault().Value as string;
    }

    private static MethodInfo? FindExtensionPropertyAccessor(
        MethodInfo[] staticMethods,
        string accessorName,
        int expectedParameterCount,
        int expectedGenericArity) =>
        staticMethods.FirstOrDefault(methodInfo =>
            string.Equals(methodInfo.Name, accessorName, StringComparison.Ordinal) &&
            methodInfo.GetParameters().Length == expectedParameterCount &&
            (methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0) == expectedGenericArity);

    private static string ExtractCleanExtensionTargetFullName(Type parameterType)
    {
        var unwrappedType = parameterType.IsByRef ? parameterType.GetElementType()! : parameterType;
        if (unwrappedType.IsGenericType)
        {
            unwrappedType = unwrappedType.GetGenericTypeDefinition();
        }

        var fullName = unwrappedType.FullName ?? unwrappedType.Name;
        var backtickIndex = fullName.IndexOf('`');
        return (backtickIndex >= 0 ? fullName[..backtickIndex] : fullName).Replace('+', '.');
    }

    private static bool DetermineIsNullableParameter(
        ParameterInfo parameterInfo,
        Type effectiveType,
        NullabilityInfoContext nullabilityContext)
    {
        if (Nullable.GetUnderlyingType(effectiveType) != null)
        {
            return true;
        }

        var nullabilityInfo = nullabilityContext.Create(parameterInfo);
        return nullabilityInfo.ReadState == NullabilityState.Nullable;
    }
}
