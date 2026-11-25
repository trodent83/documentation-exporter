using Documentation.Exporter.Abstractions.Resolution;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Handles link resolution for framework types in System.* and Microsoft.* namespaces, linking to Microsoft Learn.
/// </summary>
/// <param name="baseMicrosoftDocsUrl">The base URL for Microsoft Learn API documentation.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class MicrosoftDocsNamespaceHandler(
    string baseMicrosoftDocsUrl = "https://learn.microsoft.com/en-us/dotnet/api/",
    ILogger<MicrosoftDocsNamespaceHandler>? logger = null) : INamespaceHandler
{
    private static readonly ConcurrentDictionary<string, string> DynamicTypeResolutionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<MicrosoftDocsNamespaceHandler> logger = logger ?? NullLogger<MicrosoftDocsNamespaceHandler>.Instance;

    [LoggerMessage(EventId = 701, Level = LogLevel.Debug, Message = "Skipped assembly '{AssemblyName}' while resolving framework type '{TypeName}': {ErrorMessage}")]
    private partial void LogAssemblyReflectionSkipped(string assemblyName, string typeName, string errorMessage);

    // C# language primitive keyword aliases (since language keywords like 'int' are not CLR runtime types)
    private static readonly Dictionary<string, string> CSharpPrimitiveKeywordAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bool"] = "System.Boolean",
        ["byte"] = "System.Byte",
        ["sbyte"] = "System.SByte",
        ["char"] = "System.Char",
        ["decimal"] = "System.Decimal",
        ["double"] = "System.Double",
        ["float"] = "System.Single",
        ["int"] = "System.Int32",
        ["uint"] = "System.UInt32",
        ["long"] = "System.Int64",
        ["ulong"] = "System.UInt64",
        ["short"] = "System.Int16",
        ["ushort"] = "System.UInt16",
        ["object"] = "System.Object",
        ["string"] = "System.String",
        ["void"] = "System.Void",
        ["nint"] = "System.IntPtr",
        ["nuint"] = "System.UIntPtr"
    };

    private readonly string baseMicrosoftDocsUrl = FormatBaseUrl(baseMicrosoftDocsUrl);

    private static string FormatBaseUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return url.EndsWith('/') ? url : url + "/";
    }

    /// <inheritdoc />
    public bool CanHandleNamespace(string fullTypeName) => !string.IsNullOrWhiteSpace(fullTypeName);

    /// <inheritdoc />
    public string ResolveTypeLink(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return string.Empty;
        }

        var (canonicalTypeName, displayTypeName) = CanonicalizeType(fullTypeName);
        var urlSlug = BuildMicrosoftDocsUrlSlug(canonicalTypeName);
        var targetUrl = $"{baseMicrosoftDocsUrl}{urlSlug}";

        return $"[{displayTypeName}]({targetUrl})";
    }

    private (string CanonicalTypeName, string DisplayTypeName) CanonicalizeType(string rawTypeName)
    {
        var cleanedTypeName = rawTypeName.Trim();
        if (cleanedTypeName.Length > 2 && cleanedTypeName[1] == ':')
        {
            cleanedTypeName = cleanedTypeName[2..];
        }

        if (CSharpPrimitiveKeywordAliases.TryGetValue(cleanedTypeName, out var primitiveFullName))
        {
            return (primitiveFullName, cleanedTypeName);
        }

        var bracketIndex = cleanedTypeName.IndexOf("[[", StringComparison.Ordinal);
        if (bracketIndex >= 0)
        {
            cleanedTypeName = cleanedTypeName[..bracketIndex];
        }

        var parenIndex = cleanedTypeName.IndexOf('(');
        if (parenIndex >= 0)
        {
            cleanedTypeName = cleanedTypeName[..parenIndex];
        }

        cleanedTypeName = cleanedTypeName.Replace('+', '.');
        cleanedTypeName = NormalizeGenericSyntax(cleanedTypeName);

        if (cleanedTypeName.Contains('.'))
        {
            var displayName = GetSimpleDisplayName(cleanedTypeName);
            return (cleanedTypeName, displayName);
        }

        var canonicalName = ResolveUnqualifiedTypeDynamically(cleanedTypeName);
        var simpleDisplay = GetSimpleDisplayName(cleanedTypeName);
        return (canonicalName, simpleDisplay);
    }

    private string ResolveUnqualifiedTypeDynamically(string unqualifiedName) =>
        DynamicTypeResolutionCache.GetOrAdd(unqualifiedName, name =>
        {
            var baseName = name;
            var explicitAritySuffix = string.Empty;
            var backtickIndex = name.IndexOf('`');
            if (backtickIndex >= 0)
            {
                baseName = name[..backtickIndex];
                explicitAritySuffix = name[backtickIndex..];
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var candidateType = assembly.GetType(name, false, true)
                    ?? assembly.GetType($"System.{name}", false, true)
                    ?? assembly.GetType($"System.Collections.Generic.{name}", false, true)
                    ?? assembly.GetType($"System.Threading.Tasks.{name}", false, true);

                if (candidateType?.FullName != null)
                {
                    return candidateType.FullName;
                }

                try
                {
                    foreach (var exportedType in assembly.GetExportedTypes())
                    {
                        if (string.Equals(exportedType.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return exportedType.FullName ?? name;
                        }

                        if (exportedType.IsGenericType &&
                            exportedType.Name.StartsWith(baseName + "`", StringComparison.OrdinalIgnoreCase))
                        {
                            var genericDefinitionFullName = exportedType.FullName ?? exportedType.Name;
                            return string.IsNullOrEmpty(explicitAritySuffix)
                                ? genericDefinitionFullName
                                : $"{genericDefinitionFullName.Split('`')[0]}{explicitAritySuffix}";
                        }
                    }
                }
                catch (Exception exception) when (exception is ReflectionTypeLoadException or FileLoadException or BadImageFormatException or IOException or TypeLoadException or NotSupportedException)
                {
                    LogAssemblyReflectionSkipped(assembly.FullName ?? string.Empty, name, exception.Message);
                }
            }

            return $"System.{name}";
        });

    private static string NormalizeGenericSyntax(string typeName)
    {
        var curlyOpenIndex = typeName.IndexOf('{');
        var curlyCloseIndex = typeName.LastIndexOf('}');
        if (curlyOpenIndex >= 0 && curlyCloseIndex > curlyOpenIndex)
        {
            var typeArguments = typeName[(curlyOpenIndex + 1)..curlyCloseIndex];
            var arity = CountTopLevelGenericArguments(typeArguments);
            return $"{typeName[..curlyOpenIndex]}`{arity}";
        }

        var angleOpenIndex = typeName.IndexOf('<');
        var angleCloseIndex = typeName.LastIndexOf('>');
        if (angleOpenIndex >= 0 && angleCloseIndex > angleOpenIndex)
        {
            var typeArguments = typeName[(angleOpenIndex + 1)..angleCloseIndex];
            var arity = CountTopLevelGenericArguments(typeArguments);
            return $"{typeName[..angleOpenIndex]}`{arity}";
        }

        return typeName;
    }

    private static int CountTopLevelGenericArguments(string typeArguments)
    {
        var nestingDepth = 0;
        var argumentCount = 1;

        foreach (var character in typeArguments)
        {
            switch (character)
            {
                case '<':
                case '{':
                    nestingDepth++;
                    break;
                case '>':
                case '}':
                    if (nestingDepth > 0)
                    {
                        nestingDepth--;
                    }

                    break;
                case ',' when nestingDepth == 0:
                    argumentCount++;
                    break;
                default:
                    break;
            }
        }

        return argumentCount;
    }

    private static string GetSimpleDisplayName(string typeName)
    {
        var backtickIndex = typeName.IndexOf('`');
        var nameWithoutArity = backtickIndex >= 0 ? typeName[..backtickIndex] : typeName;
        var lastDotIndex = nameWithoutArity.LastIndexOf('.');
        return lastDotIndex < 0 ? nameWithoutArity : nameWithoutArity[(lastDotIndex + 1)..];
    }

    private static string BuildMicrosoftDocsUrlSlug(string canonicalTypeName)
    {
        var slug = canonicalTypeName.Replace('`', '-');
        return slug.ToLowerInvariant();
    }
}
