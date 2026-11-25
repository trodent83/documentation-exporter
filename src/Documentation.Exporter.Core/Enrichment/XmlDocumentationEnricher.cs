using Documentation.Exporter.Abstractions.Enrichment;
using System.Reflection;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;

namespace Documentation.Exporter.Core.Enrichment;

/// <summary>
/// Enriches a <see cref="TypeDocumentationModel"/> with compiler XML documentation comments for the type and its members.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XmlDocumentationEnricher"/> class.
/// </remarks>
/// <param name="xmlDocumentationParser">The XML documentation parser.</param>
public sealed class XmlDocumentationEnricher(IXmlDocumentationParser xmlDocumentationParser) : ITypeDocumentationEnricher
{
    private readonly IXmlDocumentationParser xmlParser = ArgumentGuard.EnsureNotNull(xmlDocumentationParser);

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public Task<TypeDocumentationModel> EnrichAsync(
        Type targetType,
        TypeDocumentationModel typeModel,
        ExporterOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(typeModel);
        ArgumentNullException.ThrowIfNull(options);

        var typeFullName = (targetType.FullName ?? targetType.Name).Replace('+', '.');
        var documentedTypeModel = EnrichType(targetType, typeFullName, typeModel);

        var enrichedModel = documentedTypeModel with
        {
            Constructors = EnrichConstructors(targetType, typeFullName, documentedTypeModel),
            Methods = EnrichMethods(targetType, typeFullName, documentedTypeModel.Methods),
            Properties = EnrichSimpleMembers(targetType, typeFullName, "P", documentedTypeModel.Properties),
            Fields = EnrichSimpleMembers(targetType, typeFullName, "F", documentedTypeModel.Fields),
            Events = EnrichSimpleMembers(targetType, typeFullName, "E", documentedTypeModel.Events)
        };

        return Task.FromResult(enrichedModel);
    }

    private TypeDocumentationModel EnrichType(Type targetType, string typeFullName, TypeDocumentationModel typeModel)
    {
        var documentedModel = xmlParser.ApplyTypeDocumentation(typeFullName, typeModel);
        documentedModel = PromoteOverloadedPrimaryConstructorIfMatched(documentedModel);

        if (!documentedModel.HasInheritDoc && !string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
        {
            return documentedModel;
        }

        if (!string.IsNullOrWhiteSpace(documentedModel.InheritDocCref))
        {
            var resolvedCrefKey = xmlParser.ResolveCrefKey(documentedModel.InheritDocCref, typeFullName, "T") ?? documentedModel.InheritDocCref;
            return MergeInheritedTypeDocumentation(resolvedCrefKey, documentedModel);
        }

        foreach (var candidateType in EnumerateAncestorAndInterfaceTypes(targetType))
        {
            var candidateFullName = ResolveCandidateTypeFullName(candidateType);
            var candidateXmlKey = $"T:{candidateFullName}";
            documentedModel = MergeInheritedTypeDocumentation(candidateXmlKey, documentedModel);
            if (!string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
            {
                break;
            }
        }

        return documentedModel;
    }

    private TypeDocumentationModel MergeInheritedTypeDocumentation(string candidateXmlKey, TypeDocumentationModel targetTypeModel)
    {
        var inheritedModel = xmlParser.ApplyTypeDocumentation(candidateXmlKey, new TypeDocumentationModel());

        return targetTypeModel with
        {
            SummaryDescription = PreferExistingText(targetTypeModel.SummaryDescription, inheritedModel.SummaryDescription),
            RemarksDescription = PreferExistingText(targetTypeModel.RemarksDescription, inheritedModel.RemarksDescription),
            DocumentedExceptions = AddMissingExceptions(targetTypeModel.DocumentedExceptions, inheritedModel.DocumentedExceptions),
            CodeExamples = targetTypeModel.CodeExamples.Count == 0 ? [.. inheritedModel.CodeExamples] : targetTypeModel.CodeExamples,
            SeeAlsoReferences = targetTypeModel.SeeAlsoReferences.Count == 0 ? [.. inheritedModel.SeeAlsoReferences] : targetTypeModel.SeeAlsoReferences,
            GenericTypeParameterDetails = FillMissingGenericDescriptions(targetTypeModel.GenericTypeParameterDetails, inheritedModel.GenericTypeParameterDetails)
        };
    }

    private List<MemberDocumentationModel> EnrichConstructors(Type targetType, string typeFullName, TypeDocumentationModel typeModel)
    {
        var constructors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return
        [
            .. typeModel.Constructors.Select(constructorModel =>
            {
                string constructorXmlKey;
                if (!string.IsNullOrWhiteSpace(constructorModel.XmlMemberKey))
                {
                    constructorXmlKey = constructorModel.XmlMemberKey;
                }
                else
                {
                    var matchingConstructor = constructors.FirstOrDefault(candidate => MatchesParameterList(candidate.GetParameters(), constructorModel.MemberParameters));
                    constructorXmlKey = matchingConstructor != null
                        ? AssemblyTypeReflector.BuildConstructorXmlKey(typeFullName, matchingConstructor.GetParameters())
                        : $"M:{typeFullName}.#ctor";
                }

                var documentedConstructor = xmlParser.ApplyMemberDocumentation(constructorXmlKey, constructorModel);

                if (documentedConstructor.HasInheritDoc && !string.IsNullOrWhiteSpace(documentedConstructor.InheritDocCref))
                {
                    var resolvedCrefKey = xmlParser.ResolveCrefKey(documentedConstructor.InheritDocCref, typeFullName, "M") ?? documentedConstructor.InheritDocCref;
                    documentedConstructor = MergeInheritedMemberDocumentation(resolvedCrefKey, documentedConstructor);
                }

                // If constructor is a primary constructor or default constructor without explicit M:#ctor docs,
                // merge documentation and parameter descriptions from the declaring type element (T:TypeName)
                // and fall back to the class SummaryDescription.
                if (string.IsNullOrWhiteSpace(documentedConstructor.SummaryDescription) ||
                    documentedConstructor.MemberParameters.Any(parameter => string.IsNullOrWhiteSpace(parameter.ParameterDescription)))
                {
                    documentedConstructor = MergeInheritedMemberDocumentation($"T:{typeFullName}", documentedConstructor);
                }

                return string.IsNullOrWhiteSpace(documentedConstructor.SummaryDescription) &&
                       !string.IsNullOrWhiteSpace(typeModel.SummaryDescription)
                    ? documentedConstructor with { SummaryDescription = typeModel.SummaryDescription }
                    : documentedConstructor;
            })
        ];
    }

    private List<MemberDocumentationModel> EnrichMethods(Type targetType, string typeFullName, IReadOnlyList<MemberDocumentationModel> methodModels)
    {
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        return [.. methodModels.Select(methodModel => EnrichMethod(targetType, typeFullName, methods, methodModel))];
    }

    private MemberDocumentationModel EnrichMethod(
        Type targetType,
        string typeFullName,
        MethodInfo[] methods,
        MemberDocumentationModel methodModel)
    {
        var matchingMethod = !string.IsNullOrWhiteSpace(methodModel.XmlMemberKey)
            ? methods.FirstOrDefault(candidate =>
            {
                var declaringTypeFullName = (candidate.DeclaringType?.FullName ?? typeFullName).Replace('+', '.');
                var genericArity = candidate.IsGenericMethod ? candidate.GetGenericArguments().Length : 0;
                var candidateKey = AssemblyTypeReflector.BuildMethodXmlKey(declaringTypeFullName, candidate.Name, genericArity, candidate.GetParameters(), candidate.ReturnType);
                return string.Equals(candidateKey, methodModel.XmlMemberKey, StringComparison.Ordinal);
            })
            : methods.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, methodModel.MemberName, StringComparison.Ordinal) &&
                MatchesParameterList(candidate.GetParameters(), methodModel.MemberParameters));

        var methodXmlKey = !string.IsNullOrWhiteSpace(methodModel.XmlMemberKey)
            ? methodModel.XmlMemberKey
            : matchingMethod != null
                ? AssemblyTypeReflector.BuildMethodXmlKey(
                    (matchingMethod.DeclaringType?.FullName ?? typeFullName).Replace('+', '.'),
                    matchingMethod.Name,
                    matchingMethod.IsGenericMethod ? matchingMethod.GetGenericArguments().Length : 0,
                    matchingMethod.GetParameters(),
                    matchingMethod.ReturnType)
                : $"M:{typeFullName}.{methodModel.MemberName}";

        var documentedModel = xmlParser.ApplyMemberDocumentation(methodXmlKey, methodModel);

        if (documentedModel.HasInheritDoc && !string.IsNullOrWhiteSpace(documentedModel.InheritDocCref))
        {
            var crefQualifyingType = matchingMethod?.DeclaringType?.FullName ?? typeFullName;
            var resolvedCrefKey = xmlParser.ResolveCrefKey(documentedModel.InheritDocCref, crefQualifyingType, "M") ?? documentedModel.InheritDocCref;
            return MergeInheritedMemberDocumentation(resolvedCrefKey, documentedModel);
        }

        return matchingMethod != null && (documentedModel.HasInheritDoc || string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
            ? InheritMethodDocumentation(targetType, matchingMethod, documentedModel)
            : documentedModel;
    }

    private List<MemberDocumentationModel> EnrichSimpleMembers(
        Type targetType,
        string typeFullName,
        string memberPrefix,
        IReadOnlyList<MemberDocumentationModel> memberModels) =>
        [.. memberModels.Select(memberModel => EnrichSimpleMember(targetType, typeFullName, memberPrefix, memberModel))];

    private MemberDocumentationModel EnrichSimpleMember(
        Type targetType,
        string typeFullName,
        string memberPrefix,
        MemberDocumentationModel memberModel)
    {
        var memberXmlKey = !string.IsNullOrWhiteSpace(memberModel.XmlMemberKey)
            ? memberModel.XmlMemberKey
            : $"{memberPrefix}:{typeFullName}.{memberModel.MemberName}";
        var documentedModel = xmlParser.ApplyMemberDocumentation(memberXmlKey, memberModel);

        if (documentedModel.HasInheritDoc && !string.IsNullOrWhiteSpace(documentedModel.InheritDocCref))
        {
            var resolvedCrefKey = xmlParser.ResolveCrefKey(documentedModel.InheritDocCref, typeFullName, memberPrefix) ?? documentedModel.InheritDocCref;
            return MergeInheritedMemberDocumentation(resolvedCrefKey, documentedModel);
        }

        var memberKeySuffix = ExtractMemberKeySuffix(memberModel.XmlMemberKey, memberPrefix, typeFullName, documentedModel.MemberName);
        return documentedModel.HasInheritDoc || string.IsNullOrWhiteSpace(documentedModel.SummaryDescription)
            ? InheritMemberDocumentationByPrefix(targetType, memberPrefix, memberKeySuffix, documentedModel)
            : documentedModel;
    }

    private static string ExtractMemberKeySuffix(
        string? xmlMemberKey,
        string memberPrefix,
        string typeFullName,
        string fallbackMemberName)
    {
        var expectedPrefix = $"{memberPrefix}:{typeFullName}.";
        return !string.IsNullOrWhiteSpace(xmlMemberKey) &&
               xmlMemberKey.StartsWith(expectedPrefix, StringComparison.Ordinal) &&
               xmlMemberKey.Length > expectedPrefix.Length
            ? xmlMemberKey[expectedPrefix.Length..]
            : fallbackMemberName;
    }

    private MemberDocumentationModel InheritMethodDocumentation(Type targetType, MethodInfo methodInfo, MemberDocumentationModel methodModel)
    {
        var methodParameters = methodInfo.GetParameters();
        var genericArity = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0;
        var documentedModel = methodModel;
        foreach (var candidateType in EnumerateAncestorAndInterfaceTypes(targetType))
        {
            var candidateFullName = ResolveCandidateTypeFullName(candidateType);
            var candidateXmlKey = AssemblyTypeReflector.BuildMethodXmlKey(candidateFullName, methodInfo.Name, genericArity, methodParameters, methodInfo.ReturnType);
            documentedModel = MergeInheritedMemberDocumentation(candidateXmlKey, documentedModel);
            if (!string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
            {
                return documentedModel;
            }

            if (candidateType.IsGenericType && !candidateType.IsGenericTypeDefinition)
            {
                var definitionMethod = TryResolveGenericDefinitionMethod(targetType, methodInfo, candidateType);
                if (definitionMethod != null)
                {
                    var definitionXmlKey = AssemblyTypeReflector.BuildMethodXmlKey(
                        candidateFullName,
                        definitionMethod.Name,
                        genericArity,
                        definitionMethod.GetParameters(),
                        definitionMethod.ReturnType);
                    documentedModel = MergeInheritedMemberDocumentation(definitionXmlKey, documentedModel);
                    if (!string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
                    {
                        return documentedModel;
                    }
                }
            }
        }

        return documentedModel;
    }

    private static MethodInfo? TryResolveGenericDefinitionMethod(Type targetType, MethodInfo methodInfo, Type constructedCandidateType)
    {
        var candidateDefinition = constructedCandidateType.GetGenericTypeDefinition();
        MethodInfo? matchedConstructedMethod = null;

        if (constructedCandidateType.IsInterface && !targetType.IsInterface)
        {
            try
            {
                var interfaceMap = targetType.GetInterfaceMap(constructedCandidateType);
                for (var mapIndex = 0; mapIndex < interfaceMap.TargetMethods.Length; mapIndex++)
                {
                    if (interfaceMap.TargetMethods[mapIndex] == methodInfo)
                    {
                        matchedConstructedMethod = interfaceMap.InterfaceMethods[mapIndex];
                        break;
                    }
                }
            }
            catch (ArgumentException)
            {
                // Target type does not directly map the interface; fall back to signature scan below.
            }
        }

        var methodParamTypes = methodInfo.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var methodGenericArity = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0;
        matchedConstructedMethod ??= constructedCandidateType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, methodInfo.Name, StringComparison.Ordinal) &&
                (candidate.IsGenericMethod ? candidate.GetGenericArguments().Length : 0) == methodGenericArity &&
                candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(methodParamTypes));

        if (matchedConstructedMethod != null)
        {
            var tokenMatch = candidateDefinition
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(candidate => candidate.MetadataToken == matchedConstructedMethod.MetadataToken);
            if (tokenMatch != null)
            {
                return tokenMatch;
            }
        }

        return candidateDefinition
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, methodInfo.Name, StringComparison.Ordinal) &&
                (candidate.IsGenericMethod ? candidate.GetGenericArguments().Length : 0) == methodGenericArity &&
                candidate.GetParameters().Length == methodParamTypes.Length);
    }

    private static string ResolveCandidateTypeFullName(Type candidateType)
    {
        var candidateDefinition = candidateType.IsGenericType ? candidateType.GetGenericTypeDefinition() : candidateType;
        return (candidateDefinition.FullName ?? candidateDefinition.Name).Replace('+', '.');
    }

    private static TypeDocumentationModel PromoteOverloadedPrimaryConstructorIfMatched(TypeDocumentationModel documentedModel)
    {
        if (documentedModel.Constructors.Count <= 1 ||
            documentedModel.Constructors.Any(constructor => constructor.IsPrimaryConstructor))
        {
            return documentedModel;
        }

        var matchedConstructor = documentedModel.Constructors.FirstOrDefault(constructor =>
            constructor.MemberParameters.Count > 0 &&
            constructor.MemberParameters.All(parameter => !string.IsNullOrWhiteSpace(parameter.ParameterDescription)) &&
            string.Equals(constructor.SummaryDescription, documentedModel.SummaryDescription, StringComparison.Ordinal));

        if (matchedConstructor == null)
        {
            return documentedModel;
        }

        var updatedConstructors = documentedModel.Constructors
            .Select(constructor => ReferenceEquals(constructor, matchedConstructor)
                ? constructor with { IsPrimaryConstructor = true }
                : constructor)
            .ToList();

        var parametersSummary = string.Join(
            ", ",
            matchedConstructor.MemberParameters.Select(parameter => $"{parameter.ParameterTypeName} {parameter.ParameterName}"));

        return documentedModel with
        {
            Constructors = updatedConstructors,
            ConstructorKind = $"Primary ({parametersSummary})"
        };
    }

    private MemberDocumentationModel InheritMemberDocumentationByPrefix(
        Type targetType,
        string memberPrefix,
        string memberKeySuffix,
        MemberDocumentationModel memberModel)
    {
        var documentedModel = memberModel;
        foreach (var candidateType in EnumerateAncestorAndInterfaceTypes(targetType))
        {
            var candidateFullName = ResolveCandidateTypeFullName(candidateType);
            var candidateXmlKey = $"{memberPrefix}:{candidateFullName}.{memberKeySuffix}";
            documentedModel = MergeInheritedMemberDocumentation(candidateXmlKey, documentedModel);
            if (!string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
            {
                return documentedModel;
            }

            if (candidateType.IsGenericType &&
                !candidateType.IsGenericTypeDefinition &&
                string.Equals(memberPrefix, "P", StringComparison.Ordinal) &&
                memberModel.MemberParameters.Count > 0)
            {
                var candidateDefinition = candidateType.GetGenericTypeDefinition();
                var candidateIndexers = candidateDefinition
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(property =>
                    {
                        var indexParams = property.GetIndexParameters();
                        return indexParams.Length == memberModel.MemberParameters.Count &&
                               indexParams.Any(parameter => parameter.ParameterType.ContainsGenericParameters);
                    });
                foreach (var definitionIndexer in candidateIndexers)
                {
                    var definitionIndexerKey = AssemblyTypeReflector.BuildPropertyXmlKey(candidateFullName, definitionIndexer);
                    documentedModel = MergeInheritedMemberDocumentation(definitionIndexerKey, documentedModel);
                    if (!string.IsNullOrWhiteSpace(documentedModel.SummaryDescription))
                    {
                        return documentedModel;
                    }
                }
            }
        }

        return documentedModel;
    }

    private MemberDocumentationModel MergeInheritedMemberDocumentation(string candidateXmlKey, MemberDocumentationModel targetMemberModel)
    {
        MemberDocumentationModel inheritedSeedModel = new()
        {
            MemberName = targetMemberModel.MemberName,
            MemberParameters = [.. targetMemberModel.MemberParameters.Select(parameter => new ParameterDocumentationModel
            {
                ParameterName = parameter.ParameterName,
                ParameterTypeName = parameter.ParameterTypeName,
                ResolvedTypeDocumentationLink = parameter.ResolvedTypeDocumentationLink
            })]
        };

        var inheritedModel = xmlParser.ApplyMemberDocumentation(candidateXmlKey, inheritedSeedModel);

        return targetMemberModel with
        {
            SummaryDescription = PreferExistingText(targetMemberModel.SummaryDescription, inheritedModel.SummaryDescription),
            RemarksDescription = PreferExistingText(targetMemberModel.RemarksDescription, inheritedModel.RemarksDescription),
            ReturnValueDescription = PreferExistingText(targetMemberModel.ReturnValueDescription, inheritedModel.ReturnValueDescription),
            MemberParameters = MergeInheritedParameters(targetMemberModel.MemberParameters, inheritedModel.MemberParameters),
            DocumentedExceptions = AddMissingExceptions(targetMemberModel.DocumentedExceptions, inheritedModel.DocumentedExceptions),
            CodeExamples = targetMemberModel.CodeExamples.Count == 0 ? [.. inheritedModel.CodeExamples] : targetMemberModel.CodeExamples,
            SeeAlsoReferences = targetMemberModel.SeeAlsoReferences.Count == 0 ? [.. inheritedModel.SeeAlsoReferences] : targetMemberModel.SeeAlsoReferences,
            GenericTypeParameters = FillMissingGenericDescriptions(targetMemberModel.GenericTypeParameters, inheritedModel.GenericTypeParameters)
        };
    }

    private static List<ParameterDocumentationModel> MergeInheritedParameters(
        IReadOnlyList<ParameterDocumentationModel> targetParameters,
        IReadOnlyList<ParameterDocumentationModel> inheritedParameters)
    {
        var unmatchedOffset = inheritedParameters.Count > targetParameters.Count ? targetParameters.Count : 0;
        return
        [
            .. targetParameters.Select((parameter, index) =>
            {
                if (!string.IsNullOrWhiteSpace(parameter.ParameterDescription))
                {
                    return parameter;
                }

                if (index < inheritedParameters.Count &&
                    !string.IsNullOrWhiteSpace(inheritedParameters[index].ParameterDescription))
                {
                    return parameter with { ParameterDescription = inheritedParameters[index].ParameterDescription };
                }

                var fallbackIndex = unmatchedOffset + index;
                return unmatchedOffset > 0 &&
                       fallbackIndex < inheritedParameters.Count &&
                       !string.IsNullOrWhiteSpace(inheritedParameters[fallbackIndex].ParameterDescription)
                    ? parameter with { ParameterDescription = inheritedParameters[fallbackIndex].ParameterDescription }
                    : parameter;
            })
        ];
    }

    private static string PreferExistingText(string existingText, string inheritedText) =>
        string.IsNullOrWhiteSpace(existingText) && !string.IsNullOrWhiteSpace(inheritedText)
            ? inheritedText
            : existingText;

    private static Dictionary<string, string> AddMissingExceptions(
        IReadOnlyDictionary<string, string> existingExceptions,
        IReadOnlyDictionary<string, string> inheritedExceptions)
    {
        Dictionary<string, string> mergedExceptions = [with(StringComparer.Ordinal)];
        foreach (var exceptionEntry in existingExceptions.Concat(inheritedExceptions))
        {
            _ = mergedExceptions.TryAdd(exceptionEntry.Key, exceptionEntry.Value);
        }

        return mergedExceptions;
    }

    private static List<ParameterDocumentationModel> FillMissingGenericDescriptions(
        IReadOnlyList<ParameterDocumentationModel> targetParameters,
        IReadOnlyList<ParameterDocumentationModel> inheritedParameters)
    {
        var unmatchedOffset = inheritedParameters.Count > targetParameters.Count ? targetParameters.Count : 0;
        return
        [
            .. targetParameters.Select((parameter, index) =>
            {
                if (!string.IsNullOrWhiteSpace(parameter.ParameterDescription))
                {
                    return parameter;
                }

                var matchingInherited = inheritedParameters
                    .FirstOrDefault(candidateParameter => string.Equals(candidateParameter.ParameterName, parameter.ParameterName, StringComparison.Ordinal));
                if (matchingInherited != null && !string.IsNullOrWhiteSpace(matchingInherited.ParameterDescription))
                {
                    return parameter with { ParameterDescription = matchingInherited.ParameterDescription };
                }

                var fallbackIndex = unmatchedOffset > 0 ? unmatchedOffset + index : index;
                return fallbackIndex < inheritedParameters.Count &&
                       !string.IsNullOrWhiteSpace(inheritedParameters[fallbackIndex].ParameterDescription)
                    ? parameter with { ParameterDescription = inheritedParameters[fallbackIndex].ParameterDescription }
                    : parameter;
            })
        ];
    }

    private static IEnumerable<Type> EnumerateAncestorAndInterfaceTypes(Type targetType)
    {
        foreach (var interfaceType in targetType.GetInterfaces())
        {
            yield return interfaceType;
        }

        var currentBase = targetType.BaseType;
        while (currentBase != null)
        {
            yield return currentBase;
            currentBase = currentBase.BaseType;
        }
    }

    private static bool MatchesParameterList(ParameterInfo[] reflectionParameters, IReadOnlyList<ParameterDocumentationModel> modelParameters)
    {
        if (reflectionParameters.Length != modelParameters.Count)
        {
            return false;
        }

        for (var index = 0; index < reflectionParameters.Length; index++)
        {
            var reflectionParamName = reflectionParameters[index].Name ?? "param";
            if (!string.Equals(reflectionParamName, modelParameters[index].ParameterName, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
