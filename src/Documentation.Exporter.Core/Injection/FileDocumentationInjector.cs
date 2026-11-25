using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Injection;

/// <summary>
/// Loads sidecar Markdown files asynchronously from an overrides directory with Microsoft Logging support.
/// </summary>
/// <param name="fileSystem">The file system abstraction used for directory and file I/O.</param>
/// <param name="logger">Optional ILogger instance.</param>
public sealed partial class FileDocumentationInjector(
    IFileSystem fileSystem,
    ILogger<FileDocumentationInjector>? logger = null) : IDocumentationInjector
{
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<FileDocumentationInjector> logger = logger ?? NullLogger<FileDocumentationInjector>.Instance;

    [LoggerMessage(EventId = 501, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogLoadingSidecar(string statusMessage);

    [LoggerMessage(EventId = 502, Level = LogLevel.Warning, Message = "Failed to enumerate sidecar overrides directory '{DirectoryPath}' for '{TargetFileName}': {ErrorMessage}")]
    private partial void LogEnumerationFailed(string directoryPath, string targetFileName, string errorMessage);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentationInjectionModel>> GetInjectionsAsync(
        string? overridesDirectoryPath,
        string? typeName,
        string? memberName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(overridesDirectoryPath) ||
            !this.fileSystem.DirectoryExists(overridesDirectoryPath) ||
            string.IsNullOrWhiteSpace(typeName))
        {
            return [];
        }

        foreach (var candidateTypeName in GetCandidateTypeNames(typeName))
        {
            var fileSearchPattern = string.IsNullOrWhiteSpace(memberName)
                ? $"{candidateTypeName}.md"
                : $"{candidateTypeName}.{CleanName(memberName)}.md";

            var targetFilePath = Path.Combine(overridesDirectoryPath, fileSearchPattern);
            if (!this.fileSystem.FileExists(targetFilePath))
            {
                targetFilePath = SafeFindFileCaseInsensitive(overridesDirectoryPath, fileSearchPattern) ?? targetFilePath;
                if (!this.fileSystem.FileExists(targetFilePath))
                {
                    continue;
                }
            }

            var loadingSidecarMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.InjectorLoadingSidecar, targetFilePath);
            LogLoadingSidecar(loadingSidecarMessage);

            var rawMarkdown = (await this.fileSystem.ReadAllTextAsync(targetFilePath, cancellationToken).ConfigureAwait(false)).Trim();
            return string.IsNullOrWhiteSpace(rawMarkdown)
                ? []
                : [ParseInjectionFile(rawMarkdown, CleanName(typeName))];
        }

        return [];
    }

    private static IEnumerable<string> GetCandidateTypeNames(string rawTypeName)
    {
        var qualifiedClean = CleanPreservingNamespace(rawTypeName);
        var simpleClean = CleanName(rawTypeName);
        if (!string.IsNullOrWhiteSpace(qualifiedClean))
        {
            yield return qualifiedClean;
        }

        if (!string.IsNullOrWhiteSpace(simpleClean) &&
            !string.Equals(qualifiedClean, simpleClean, StringComparison.Ordinal))
        {
            yield return simpleClean;
        }
    }

    private static string CleanPreservingNamespace(string name)
    {
        var genericIndex = name.IndexOf('`');
        var withoutArity = genericIndex >= 0 ? name[..genericIndex] : name;
        return withoutArity.Replace('+', '.').Trim();
    }

    private static DocumentationInjectionModel ParseInjectionFile(string rawMarkdown, string defaultTargetName)
    {
        if (!rawMarkdown.StartsWith("---", StringComparison.Ordinal))
        {
            return new DocumentationInjectionModel
            {
                TargetName = defaultTargetName,
                InjectionMode = InjectionMode.Extend,
                TargetAnchor = SidecarAnchor.AfterRemarks,
                CustomMarkdownContent = rawMarkdown
            };
        }

        var closingFrontmatterIndex = FindClosingFrontmatterLineIndex(rawMarkdown);
        if (closingFrontmatterIndex <= 3)
        {
            return new DocumentationInjectionModel
            {
                TargetName = defaultTargetName,
                InjectionMode = InjectionMode.Extend,
                TargetAnchor = SidecarAnchor.AfterRemarks,
                CustomMarkdownContent = rawMarkdown
            };
        }

        var frontmatterContent = rawMarkdown[3..closingFrontmatterIndex];
        var remainingBody = rawMarkdown[(closingFrontmatterIndex + 3)..].Trim();
        var injectionMode = InjectionMode.Extend;
        var targetAnchor = SidecarAnchor.AfterRemarks;

        foreach (var line in frontmatterContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var frontmatterKey = line[..colonIndex].Trim().ToLowerInvariant();
            var value = line[(colonIndex + 1)..].Trim().ToLowerInvariant();

            if (frontmatterKey == "injection_mode")
            {
                injectionMode = value == "replace" ? InjectionMode.Replace : InjectionMode.Extend;
            }
            else if (frontmatterKey == "target_anchor")
            {
                targetAnchor = ParseTargetAnchor(value);
            }
        }

        return new DocumentationInjectionModel
        {
            TargetName = defaultTargetName,
            InjectionMode = injectionMode,
            TargetAnchor = targetAnchor,
            CustomMarkdownContent = remainingBody
        };
    }

    private static int FindClosingFrontmatterLineIndex(string rawMarkdown)
    {
        var searchIndex = 3;
        while (searchIndex < rawMarkdown.Length)
        {
            var candidateIndex = rawMarkdown.IndexOf("---", searchIndex, StringComparison.Ordinal);
            if (candidateIndex < 0)
            {
                return -1;
            }

            var isAtLineStart = candidateIndex > 0 && rawMarkdown[candidateIndex - 1] is '\n' or '\r';
            var afterDelimiterIndex = candidateIndex + 3;
            var isAtLineEnd = afterDelimiterIndex >= rawMarkdown.Length || rawMarkdown[afterDelimiterIndex] is '\n' or '\r';
            if (isAtLineStart && isAtLineEnd)
            {
                return candidateIndex;
            }

            searchIndex = candidateIndex + 3;
        }

        return -1;
    }

    private static SidecarAnchor ParseTargetAnchor(string rawAnchorValue) =>
        rawAnchorValue switch
        {
            "before_remarks" => SidecarAnchor.BeforeRemarks,
            "after_remarks" => SidecarAnchor.AfterRemarks,
            "after_examples" => SidecarAnchor.AfterExamples,
            "page_append" => SidecarAnchor.PageAppend,
            "replace_summary" => SidecarAnchor.ReplaceSummary,
            _ => SidecarAnchor.AfterRemarks
        };

    private static string CleanName(string name)
    {
        var lastDotIndex = name.LastIndexOf('.');
        if (lastDotIndex >= 0)
        {
            name = name[(lastDotIndex + 1)..];
        }

        var genericIndex = name.IndexOf('`');
        if (genericIndex >= 0)
        {
            name = name[..genericIndex];
        }

        return name.Trim();
    }

    private string? SafeFindFileCaseInsensitive(string directoryPath, string targetFileName)
    {
        try
        {
            return this.fileSystem.FindFileCaseInsensitive(directoryPath, targetFileName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogEnumerationFailed(directoryPath, targetFileName, exception.Message);
            return null;
        }
    }
}
