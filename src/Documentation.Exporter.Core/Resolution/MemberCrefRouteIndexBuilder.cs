using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;

namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Default implementation of <see cref="IMemberCrefRouteIndexBuilder"/> indexing overloaded methods, constructors,
/// indexers, operators, properties, fields, and events using exact reflection signatures.
/// </summary>
/// <param name="signatureFormatter">The C# signature formatter used for parameter display types and modifiers.</param>
/// <param name="memberFileNameFormatter">The member detail filename and title formatter.</param>
public sealed class MemberCrefRouteIndexBuilder(
    ICSharpSignatureFormatter signatureFormatter,
    IMemberFileNameFormatter memberFileNameFormatter) : IMemberCrefRouteIndexBuilder
{
    private readonly ICSharpSignatureFormatter formatter = ArgumentGuard.EnsureNotNull(signatureFormatter);
    private readonly IMemberFileNameFormatter fileNameFormatter = ArgumentGuard.EnsureNotNull(memberFileNameFormatter);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, (string DisplayName, string RelativeUrl)> BuildMemberCrefRoutes(
        IReadOnlyDictionary<string, Type> exportedTypesMap,
        bool useAssemblySubdirectories,
        bool excludeEditorBrowsableNever = true)
    {
        ArgumentNullException.ThrowIfNull(exportedTypesMap);

        Dictionary<string, (string DisplayName, string RelativeUrl)> routes = [with(StringComparer.Ordinal)];
        var collidingCleanNames = LocalProjectNamespaceHandler.ComputeCollidingCleanTypeNames(exportedTypesMap.Values);

        foreach (var (_, targetType) in exportedTypesMap)
        {
            if (targetType == null ||
                targetType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            var typeDirectoryUrl = LocalProjectNamespaceHandler.ComputeRuntimeTypeDirectoryUrlPath(
                targetType,
                useAssemblySubdirectories,
                collidingCleanNames);
            var typeFullName = (targetType.FullName ?? targetType.Name).Replace('+', '.');
            var cleanTypeName = formatter.GetCleanTypeName(targetType);

            IndexConstructors(routes, targetType, typeFullName, cleanTypeName, typeDirectoryUrl, excludeEditorBrowsableNever);
            IndexMethods(routes, targetType, typeFullName, typeDirectoryUrl, excludeEditorBrowsableNever);
            IndexPropertiesAndIndexers(routes, targetType, typeFullName, typeDirectoryUrl, excludeEditorBrowsableNever);
            IndexFields(routes, targetType, typeFullName, typeDirectoryUrl, excludeEditorBrowsableNever);
            IndexEvents(routes, targetType, typeFullName, typeDirectoryUrl, excludeEditorBrowsableNever);
        }

        return routes;
    }

    private void IndexConstructors(
        Dictionary<string, (string DisplayName, string RelativeUrl)> routes,
        Type targetType,
        string typeFullName,
        string cleanTypeName,
        string typeDirectoryUrl,
        bool excludeEditorBrowsableNever)
    {
        List<ConstructorInfo> visibleConstructors =
        [
            .. targetType
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(constructorInfo => CSharpSignatureFormatter.IsMethodAccessible(constructorInfo) &&
                                          !constructorInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) &&
                                          (!excludeEditorBrowsableNever || !IsEditorBrowsableNever(constructorInfo)))
        ];

        var isOverloaded = visibleConstructors.Count > 1;
        foreach (var constructorInfo in visibleConstructors)
        {
            var parameters = constructorInfo.GetParameters();
            var xmlMemberKey = AssemblyTypeReflector.BuildConstructorXmlKey(typeFullName, parameters);
            var lightModel = new MemberDocumentationModel
            {
                MemberName = cleanTypeName,
                MemberParameters = CreateLightParameterModels(parameters)
            };

            var fileName = fileNameFormatter.GetMemberDetailFileName(lightModel, isOverloaded, isConstructor: true);
            var displayName = isOverloaded
                ? fileNameFormatter.FormatMemberHeaderTitle(lightModel, isOverloaded: true)
                : cleanTypeName;

            AddRouteIfAbsent(routes, xmlMemberKey, displayName, $"{typeDirectoryUrl}/{fileName}");
        }
    }

    private void IndexMethods(
        Dictionary<string, (string DisplayName, string RelativeUrl)> routes,
        Type targetType,
        string typeFullName,
        string typeDirectoryUrl,
        bool excludeEditorBrowsableNever)
    {
        var isRecordType = CSharpSignatureFormatter.IsRecordType(targetType);
        List<MethodInfo> visibleMethods =
        [
            .. targetType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(methodInfo => !AssemblyTypeReflector.ShouldSkipMethod(methodInfo, isRecordType) &&
                                     (!excludeEditorBrowsableNever || !IsEditorBrowsableNever(methodInfo)))
        ];

        var memberCountsByName = visibleMethods
            .GroupBy(AssemblyTypeReflector.FormatMethodMemberName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        foreach (var methodInfo in visibleMethods)
        {
            var memberName = AssemblyTypeReflector.FormatMethodMemberName(methodInfo);
            var isOverloaded = memberCountsByName.TryGetValue(memberName, out var memberCount) && memberCount > 1;
            var methodParameters = methodInfo.GetParameters();
            var genericArity = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0;
            var declaringTypeFullName = (methodInfo.DeclaringType?.FullName ?? typeFullName).Replace('+', '.');
            var xmlMemberKey = AssemblyTypeReflector.BuildMethodXmlKey(
                declaringTypeFullName,
                methodInfo.Name,
                genericArity,
                methodParameters,
                methodInfo.ReturnType);

            var lightModel = new MemberDocumentationModel
            {
                MemberName = memberName,
                ReturnTypeName = formatter.FormatDisplayTypeName(methodInfo.ReturnType),
                GenericTypeParameters = methodInfo.IsGenericMethod
                    ? [.. methodInfo.GetGenericArguments().Select(argument => new ParameterDocumentationModel { ParameterName = argument.Name })]
                    : [],
                MemberParameters = CreateLightParameterModels(methodParameters)
            };

            var fileName = fileNameFormatter.GetMemberDetailFileName(lightModel, isOverloaded, isConstructor: false);
            var displayName = fileNameFormatter.FormatMemberHeaderTitle(lightModel, isOverloaded);
            AddRouteIfAbsent(routes, xmlMemberKey, displayName, $"{typeDirectoryUrl}/{fileName}");
        }
    }

    private void IndexPropertiesAndIndexers(
        Dictionary<string, (string DisplayName, string RelativeUrl)> routes,
        Type targetType,
        string typeFullName,
        string typeDirectoryUrl,
        bool excludeEditorBrowsableNever)
    {
        List<(PropertyInfo Property, ParameterInfo[] IndexParameters)> accessibleProperties = [];
        foreach (var propertyInfo in targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);
            var isAccessible = (getMethod != null && CSharpSignatureFormatter.IsMethodAccessible(getMethod)) ||
                               (setMethod != null && CSharpSignatureFormatter.IsMethodAccessible(setMethod));
            if (!isAccessible ||
                propertyInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                (excludeEditorBrowsableNever && IsEditorBrowsableNever(propertyInfo)))
            {
                continue;
            }

            accessibleProperties.Add((propertyInfo, propertyInfo.GetIndexParameters()));
        }

        var indexerCount = accessibleProperties.Count(entry => entry.IndexParameters.Length > 0);
        foreach (var (propertyInfo, indexParameters) in accessibleProperties)
        {
            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);
            var representativeMethod = (getMethod != null && CSharpSignatureFormatter.IsMethodAccessible(getMethod))
                ? getMethod
                : setMethod!;
            var isExplicitInterfaceProperty = CSharpSignatureFormatter.IsExplicitInterfaceImplementation(representativeMethod);
            var isIndexer = indexParameters.Length > 0;
            var isOverloaded = isIndexer && indexerCount > 1;
            var xmlMemberKey = AssemblyTypeReflector.BuildPropertyXmlKey(typeFullName, propertyInfo);
            var memberName = isIndexer
                ? isExplicitInterfaceProperty
                    ? CSharpSignatureFormatter.FormatExplicitInterfaceIndexerPrefix(propertyInfo.Name)
                    : "this"
                : isExplicitInterfaceProperty
                    ? CSharpSignatureFormatter.FormatExplicitInterfaceMemberName(propertyInfo.Name)
                    : propertyInfo.Name;
            var lightModel = new MemberDocumentationModel
            {
                MemberName = memberName,
                MemberParameters = isIndexer ? CreateLightParameterModels(indexParameters) : []
            };

            var fileName = fileNameFormatter.GetMemberDetailFileName(lightModel, isOverloaded, isConstructor: false);
            var displayName = isIndexer
                ? fileNameFormatter.FormatMemberHeaderTitle(lightModel, isOverloaded)
                : memberName;

            AddRouteIfAbsent(routes, xmlMemberKey, displayName, $"{typeDirectoryUrl}/{fileName}");
        }
    }

    private static void IndexFields(
        Dictionary<string, (string DisplayName, string RelativeUrl)> routes,
        Type targetType,
        string typeFullName,
        string typeDirectoryUrl,
        bool excludeEditorBrowsableNever)
    {
        foreach (var fieldInfo in targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (fieldInfo.IsSpecialName ||
                fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                !CSharpSignatureFormatter.IsFieldAccessible(fieldInfo) ||
                (excludeEditorBrowsableNever && IsEditorBrowsableNever(fieldInfo)))
            {
                continue;
            }

            var xmlMemberKey = $"F:{typeFullName}.{fieldInfo.Name}";
            var relativeUrl = targetType.IsEnum && fieldInfo.IsLiteral
                ? $"{typeDirectoryUrl}/index.md#values"
                : $"{typeDirectoryUrl}/{fieldInfo.Name}.md";
            AddRouteIfAbsent(routes, xmlMemberKey, fieldInfo.Name, relativeUrl);
        }
    }

    private void IndexEvents(
        Dictionary<string, (string DisplayName, string RelativeUrl)> routes,
        Type targetType,
        string typeFullName,
        string typeDirectoryUrl,
        bool excludeEditorBrowsableNever)
    {
        foreach (var eventInfo in targetType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var addMethod = eventInfo.GetAddMethod(true);
            if (addMethod == null ||
                eventInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                !CSharpSignatureFormatter.IsMethodAccessible(addMethod) ||
                (excludeEditorBrowsableNever && IsEditorBrowsableNever(eventInfo)))
            {
                continue;
            }

            var isExplicitInterfaceEvent = CSharpSignatureFormatter.IsExplicitInterfaceImplementation(addMethod);
            var eventMemberName = isExplicitInterfaceEvent
                ? CSharpSignatureFormatter.FormatExplicitInterfaceMemberName(eventInfo.Name)
                : eventInfo.Name;
            var xmlEventName = AssemblyTypeReflector.FormatXmlMemberMetadataName(eventInfo.Name);
            var xmlMemberKey = $"E:{typeFullName}.{xmlEventName}";
            var lightEventModel = new MemberDocumentationModel { MemberName = eventMemberName };
            var fileName = fileNameFormatter.GetMemberDetailFileName(lightEventModel, isOverloaded: false, isConstructor: false);
            AddRouteIfAbsent(routes, xmlMemberKey, eventMemberName, $"{typeDirectoryUrl}/{fileName}");
        }
    }

    private static bool IsEditorBrowsableNever(MemberInfo memberInfo) =>
        memberInfo.GetCustomAttribute<EditorBrowsableAttribute>(inherit: false)?.State == EditorBrowsableState.Never;

    private List<ParameterDocumentationModel> CreateLightParameterModels(ParameterInfo[] parameters) =>
    [
        .. parameters.Select(parameterInfo =>
        {
            var isParams = CSharpSignatureFormatter.IsParamsParameter(parameterInfo);
            return new ParameterDocumentationModel
            {
                ParameterName = parameterInfo.Name ?? "param",
                ParameterTypeName = formatter.FormatDisplayTypeName(parameterInfo.ParameterType),
                ParameterModifier = CSharpSignatureFormatter.DetermineParameterModifier(parameterInfo, isParams, isExtensionMethodFirstParameter: false),
                IsParams = isParams
            };
        })
    ];

    private static void AddRouteIfAbsent(
        Dictionary<string, (string DisplayName, string RelativeUrl)> routes,
        string rawXmlKey,
        string displayName,
        string relativeUrl)
    {
        var unprefixedKey = rawXmlKey.Length > 2 && rawXmlKey[1] == ':'
            ? rawXmlKey[2..]
            : rawXmlKey;
        var normalizedKey = unprefixedKey.Replace('+', '.');
        _ = routes.TryAdd(normalizedKey, (displayName, relativeUrl));
    }
}
