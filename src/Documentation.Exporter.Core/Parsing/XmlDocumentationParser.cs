using System.Xml.Linq;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Parses compiler-generated XML documentation files (.xml) and extracts structured XML comment data with Microsoft Logging support.
/// </summary>
/// <param name="xmlTagTransformer">The XML tag transformer instance.</param>
/// <param name="typeLinkResolver">The type link resolver used for exception crefs.</param>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class XmlDocumentationParser(
    IXmlTagTransformer xmlTagTransformer,
    ITypeLinkResolver typeLinkResolver,
    IFileSystem fileSystem,
    ILogger<XmlDocumentationParser>? logger = null) : IXmlDocumentationParser
{
    private readonly Dictionary<string, XElement> memberElementsMap = [with(StringComparer.Ordinal)];
    private readonly IXmlTagTransformer xmlTagTransformer = ArgumentGuard.EnsureNotNull(xmlTagTransformer);
    private readonly ITypeLinkResolver linkResolver = ArgumentGuard.EnsureNotNull(typeLinkResolver);
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<XmlDocumentationParser> logger = logger ?? NullLogger<XmlDocumentationParser>.Instance;

    [LoggerMessage(EventId = 201, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogXmlFilePathMissing(string statusMessage);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogXmlFileNotFound(string statusMessage);

    [LoggerMessage(EventId = 203, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogLoadingXmlFile(string statusMessage);

    [LoggerMessage(EventId = 204, Level = LogLevel.Warning, Message = "{StatusMessage}")]
    private partial void LogXmlNoMembersRoot(string statusMessage);

    [LoggerMessage(EventId = 205, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogIndexedMembers(string statusMessage);

    [LoggerMessage(EventId = 206, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogFailedParsingXml(Exception exception, string statusMessage);

    /// <summary>
    /// Tries to load and parse the specified compiler XML documentation file asynchronously, logging any errors directly.
    /// </summary>
    /// <param name="xmlDocumentationFilePath">File path to the XML documentation file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if the file was loaded successfully; otherwise false.</returns>
    public async Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(xmlDocumentationFilePath))
        {
            LogXmlFilePathMissing(ExecutionMessagesResources.XmlParserFilePathMissing);
            return false;
        }

        if (!this.fileSystem.FileExists(xmlDocumentationFilePath))
        {
            LogXmlFileNotFound(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.XmlParserFileNotFound, xmlDocumentationFilePath));
            return false;
        }

        try
        {
            var loadingXmlMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.XmlParserLoadingFile, xmlDocumentationFilePath);
            LogLoadingXmlFile(loadingXmlMessage);

            await using var xmlStream = this.fileSystem.OpenRead(xmlDocumentationFilePath);
            var xmlDocument = await XDocument.LoadAsync(xmlStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            var membersElement = xmlDocument.Root?.Element("members");

            if (membersElement == null)
            {
                LogXmlNoMembersRoot(ExecutionMessagesResources.XmlParserNoMembersRoot);
                return true;
            }

            foreach (var memberElement in membersElement.Elements("member"))
            {
                var nameAttributeValue = memberElement.Attribute("name")?.Value;
                if (!string.IsNullOrWhiteSpace(nameAttributeValue))
                {
                    memberElementsMap[nameAttributeValue] = memberElement;
                }
            }

            var indexedMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.XmlParserIndexedMembers, memberElementsMap.Count);
            LogIndexedMembers(indexedMessage);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogFailedParsingXml(exception, ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.XmlParserFailedParsing, xmlDocumentationFilePath));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xmlDocumentationFilePaths);

        var pathList = xmlDocumentationFilePaths.ToList();
        if (pathList.Count == 0)
        {
            return false;
        }

        foreach (var filePath in pathList)
        {
            if (!await TryLoadXmlDocumentationAsync(filePath, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void Clear() => memberElementsMap.Clear();

    /// <inheritdoc />
    public TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel)
    {
        ArgumentNullException.ThrowIfNull(typeDocumentationModel);

        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return typeDocumentationModel;
        }

        var typeMemberKey = $"T:{CleanTypeName(fullTypeName)}";

        if (!memberElementsMap.TryGetValue(typeMemberKey, out var memberElement))
        {
            return typeDocumentationModel;
        }

        var inheritDocElement = memberElement.Element("inheritdoc");
        var summaryElement = memberElement.Element("summary");
        var remarksElement = memberElement.Element("remarks");
        var hasExcludeTag = memberElement.Element("exclude") != null;

        var documentedModel = typeDocumentationModel with
        {
            HasInheritDoc = inheritDocElement != null || typeDocumentationModel.HasInheritDoc,
            IsExcludedByXmlDoc = hasExcludeTag || typeDocumentationModel.IsExcludedByXmlDoc,
            InheritDocCref = inheritDocElement != null
                ? inheritDocElement.Attribute("cref")?.Value
                : typeDocumentationModel.InheritDocCref,
            SummaryDescription = summaryElement != null
                ? xmlTagTransformer.TransformXmlToMarkdown(summaryElement)
                : typeDocumentationModel.SummaryDescription,
            RemarksDescription = remarksElement != null
                ? xmlTagTransformer.TransformXmlToMarkdown(remarksElement)
                : typeDocumentationModel.RemarksDescription,
            CodeExamples = [.. typeDocumentationModel.CodeExamples, .. ParseMarkdownElements(memberElement, "example")],
            SeeAlsoReferences = [.. typeDocumentationModel.SeeAlsoReferences, .. ParseMarkdownElements(memberElement, "seealso")],
            GenericTypeParameterDetails = MergeParameterDescriptions(
                memberElement.Elements("typeparam"),
                typeDocumentationModel.GenericTypeParameterDetails,
                useNameAsTypeName: true),
            DocumentedExceptions = MergeExceptions(memberElement, typeDocumentationModel.DocumentedExceptions)
        };

        return documentedModel with
        {
            Constructors = ApplyConstructorDocumentation(memberElement, documentedModel)
        };
    }

    /// <inheritdoc />
    public MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel)
    {
        ArgumentNullException.ThrowIfNull(memberDocumentationModel);

        if (string.IsNullOrWhiteSpace(xmlMemberKey))
        {
            return memberDocumentationModel;
        }

        if (!memberElementsMap.TryGetValue(xmlMemberKey, out var memberElement))
        {
            return memberDocumentationModel;
        }

        var inheritDocElement = memberElement.Element("inheritdoc");
        var summaryElement = memberElement.Element("summary");
        var remarksElement = memberElement.Element("remarks");
        var valueElement = memberElement.Element("value") ?? memberElement.Element("returns");
        var includesGenericTypeParameters = !xmlMemberKey.StartsWith("T:", StringComparison.Ordinal) &&
                                            !xmlMemberKey.Contains(".#ctor", StringComparison.Ordinal);

        return memberDocumentationModel with
        {
            HasInheritDoc = inheritDocElement != null || memberDocumentationModel.HasInheritDoc,
            IsExcludedByXmlDoc = memberElement.Element("exclude") != null || memberDocumentationModel.IsExcludedByXmlDoc,
            InheritDocCref = inheritDocElement != null
                ? inheritDocElement.Attribute("cref")?.Value
                : memberDocumentationModel.InheritDocCref,
            SummaryDescription = summaryElement != null
                ? xmlTagTransformer.TransformXmlToMarkdown(summaryElement)
                : memberDocumentationModel.SummaryDescription,
            RemarksDescription = remarksElement != null
                ? xmlTagTransformer.TransformXmlToMarkdown(remarksElement)
                : memberDocumentationModel.RemarksDescription,
            ReturnValueDescription = valueElement != null
                ? xmlTagTransformer.TransformXmlToMarkdown(valueElement)
                : memberDocumentationModel.ReturnValueDescription,
            GenericTypeParameters = includesGenericTypeParameters
                ? MergeParameterDescriptions(
                    memberElement.Elements("typeparam"),
                    memberDocumentationModel.GenericTypeParameters,
                    useNameAsTypeName: true)
                : memberDocumentationModel.GenericTypeParameters,
            MemberParameters = MergeParameterDescriptions(
                memberElement.Elements("param"),
                memberDocumentationModel.MemberParameters,
                useNameAsTypeName: false),
            DocumentedExceptions = MergeExceptions(memberElement, memberDocumentationModel.DocumentedExceptions),
            CodeExamples = [.. memberDocumentationModel.CodeExamples, .. ParseMarkdownElements(memberElement, "example")],
            SeeAlsoReferences = [.. memberDocumentationModel.SeeAlsoReferences, .. ParseMarkdownElements(memberElement, "seealso")]
        };
    }

    private List<string> ParseMarkdownElements(XElement parentElement, string elementName) =>
    [
        .. parentElement.Elements(elementName)
            .Select(xmlTagTransformer.TransformXmlToMarkdown)
            .Where(markdown => !string.IsNullOrWhiteSpace(markdown))
    ];

    private List<ParameterDocumentationModel> MergeParameterDescriptions(
        IEnumerable<XElement> parameterElements,
        IReadOnlyList<ParameterDocumentationModel> existingParameters,
        bool useNameAsTypeName)
    {
        List<ParameterDocumentationModel> mergedParameters = [.. existingParameters];
        foreach (var parameterElement in parameterElements)
        {
            var parameterName = parameterElement.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                continue;
            }

            var parameterDescription = xmlTagTransformer.TransformXmlToMarkdown(parameterElement);
            var existingIndex = mergedParameters.FindIndex(parameter =>
                string.Equals(parameter.ParameterName, parameterName, StringComparison.Ordinal));

            if (existingIndex >= 0)
            {
                mergedParameters[existingIndex] = mergedParameters[existingIndex] with { ParameterDescription = parameterDescription };
                continue;
            }

            mergedParameters.Add(new ParameterDocumentationModel
            {
                ParameterName = parameterName,
                ParameterTypeName = useNameAsTypeName ? parameterName : string.Empty,
                ParameterDescription = parameterDescription
            });
        }

        return mergedParameters;
    }

    private List<MemberDocumentationModel> ApplyConstructorDocumentation(XElement typeMemberElement, TypeDocumentationModel typeDocumentationModel)
    {
        if (typeDocumentationModel.Constructors.Count == 0)
        {
            return [];
        }

        var typeParamElements = typeMemberElement.Elements("param").ToList();
        var primaryConstructor =
            typeDocumentationModel.Constructors.FirstOrDefault(candidate => candidate.IsPrimaryConstructor)
            ?? (typeParamElements.Count > 0
                ? typeDocumentationModel.Constructors.FirstOrDefault(candidate => candidate.MemberParameters.Count == typeParamElements.Count)
                  ?? typeDocumentationModel.Constructors.FirstOrDefault(candidate => candidate.MemberParameters.Count > 0)
                : null);

        var isSingleParameterlessConstructorType =
            typeDocumentationModel.Constructors.Count == 1 &&
            typeDocumentationModel.Constructors[0].MemberParameters.Count == 0;

        return
        [
            .. typeDocumentationModel.Constructors.Select(constructorModel =>
            {
                var isPrimaryOrSingleDefaultConstructor =
                    ReferenceEquals(constructorModel, primaryConstructor) ||
                    isSingleParameterlessConstructorType;

                var documentedConstructor = ReferenceEquals(constructorModel, primaryConstructor)
                    ? ApplyPrimaryConstructorDocumentation(typeMemberElement, typeParamElements, constructorModel)
                    : constructorModel;

                return isPrimaryOrSingleDefaultConstructor &&
                       string.IsNullOrWhiteSpace(documentedConstructor.SummaryDescription) &&
                       !string.IsNullOrWhiteSpace(typeDocumentationModel.SummaryDescription)
                    ? documentedConstructor with { SummaryDescription = typeDocumentationModel.SummaryDescription }
                    : documentedConstructor;
            })
        ];
    }

    private MemberDocumentationModel ApplyPrimaryConstructorDocumentation(
        XElement typeMemberElement,
        List<XElement> typeParamElements,
        MemberDocumentationModel primaryConstructor)
    {
        var documentedParameters = primaryConstructor.MemberParameters
            .Select(parameter =>
            {
                if (!string.IsNullOrWhiteSpace(parameter.ParameterDescription))
                {
                    return parameter;
                }

                var matchingElement = typeParamElements.FirstOrDefault(parameterElement =>
                    string.Equals(parameterElement.Attribute("name")?.Value, parameter.ParameterName, StringComparison.Ordinal));
                return matchingElement == null
                    ? parameter
                    : parameter with { ParameterDescription = xmlTagTransformer.TransformXmlToMarkdown(matchingElement) };
            })
            .ToList();

        return primaryConstructor with
        {
            MemberParameters = documentedParameters,
            DocumentedExceptions = MergeExceptions(typeMemberElement, primaryConstructor.DocumentedExceptions)
        };
    }

    private Dictionary<string, string> MergeExceptions(XElement documentedElement, IReadOnlyDictionary<string, string> existingExceptions)
    {
        Dictionary<string, string> mergedExceptions = new(existingExceptions, StringComparer.Ordinal);
        foreach (var exceptionElement in documentedElement.Elements("exception"))
        {
            var exceptionCref = exceptionElement.Attribute("cref")?.Value;
            if (string.IsNullOrWhiteSpace(exceptionCref))
            {
                continue;
            }

            var cleanedExceptionType = CleanTypeName(exceptionCref);
            var resolvedExceptionLink = linkResolver.ResolveLink(cleanedExceptionType);
            var exceptionKey = resolvedExceptionLink.Contains("](", StringComparison.Ordinal)
                ? resolvedExceptionLink
                : cleanedExceptionType;

            var exceptionCondition = xmlTagTransformer.TransformXmlToMarkdown(exceptionElement);
            mergedExceptions[exceptionKey] = MergeExceptionCondition(
                mergedExceptions.GetValueOrDefault(exceptionKey),
                exceptionCondition);
        }

        return mergedExceptions;
    }

    private static string MergeExceptionCondition(string? existingCondition, string exceptionCondition)
    {
        if (existingCondition == null)
        {
            return exceptionCondition;
        }

        // Several <exception> tags may share the same cref (one per failure condition); keep every distinct condition.
        var existingConditions = existingCondition.Split("<br/>", StringSplitOptions.None);
        return existingConditions.Contains(exceptionCondition, StringComparer.Ordinal)
            ? existingCondition
            : $"{existingCondition}<br/>{exceptionCondition}";
    }

    private static string CleanTypeName(string typeName)
    {
        var text = typeName.Trim();
        if (text.Length > 2 && text[1] == ':')
        {
            text = text[2..];
        }

        return text.Replace('+', '.');
    }

    /// <inheritdoc />
    public string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M")
    {
        if (string.IsNullOrWhiteSpace(cref))
        {
            return null;
        }

        var trimmedCref = cref.Trim();

        if (memberElementsMap.ContainsKey(trimmedCref))
        {
            return trimmedCref;
        }

        var prefixedCref = trimmedCref.Contains(':') ? trimmedCref : $"{defaultPrefix}:{trimmedCref}";
        if (memberElementsMap.ContainsKey(prefixedCref))
        {
            return prefixedCref;
        }

        if (!string.IsNullOrWhiteSpace(declaringTypeFullName))
        {
            string[] candidatePrefixes = [defaultPrefix, "M", "P", "F", "E", "T"];
            foreach (var candidatePrefix in candidatePrefixes.Distinct(StringComparer.Ordinal))
            {
                var qualifiedCref = $"{candidatePrefix}:{declaringTypeFullName}.{trimmedCref}";
                if (memberElementsMap.ContainsKey(qualifiedCref))
                {
                    return qualifiedCref;
                }

                var matchingPrefixedKey = memberElementsMap.Keys.FirstOrDefault(candidateKey =>
                    candidateKey.StartsWith($"{candidatePrefix}:{declaringTypeFullName}.{trimmedCref}(", StringComparison.Ordinal));
                if (matchingPrefixedKey != null)
                {
                    return matchingPrefixedKey;
                }
            }
        }

        var suffixMatch = memberElementsMap.Keys.FirstOrDefault(candidateKey =>
            candidateKey.EndsWith($".{trimmedCref}", StringComparison.Ordinal) ||
            candidateKey.EndsWith($":{trimmedCref}", StringComparison.Ordinal) ||
            candidateKey.Contains($".{trimmedCref}(", StringComparison.Ordinal));

        return suffixMatch;
    }
}
