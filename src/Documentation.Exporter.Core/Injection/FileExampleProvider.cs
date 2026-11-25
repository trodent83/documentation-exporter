using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Injection;

/// <summary>
/// Loads code snippet files asynchronously from an external examples directory with Microsoft Logging support.
/// </summary>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class FileExampleProvider(
    IFileSystem fileSystem,
    ILogger<FileExampleProvider>? logger = null) : IExampleProvider
{
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<FileExampleProvider> logger = logger ?? NullLogger<FileExampleProvider>.Instance;

    [LoggerMessage(EventId = 401, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogLoadingMemberExample(string statusMessage);

    [LoggerMessage(EventId = 402, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogLoadingTypeExample(string statusMessage);

    [LoggerMessage(EventId = 403, Level = LogLevel.Warning, Message = "Failed to enumerate example path '{DirectoryPath}' for '{TargetName}': {ErrorMessage}")]
    private partial void LogEnumerationFailed(string directoryPath, string targetName, string errorMessage);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetExamplesAsync(
        string? examplesDirectoryPath,
        string? typeName,
        string? memberName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(examplesDirectoryPath) ||
            !this.fileSystem.DirectoryExists(examplesDirectoryPath) ||
            string.IsNullOrWhiteSpace(typeName))
        {
            return [];
        }

        foreach (var candidateTypeName in GetCandidateTypeNames(typeName))
        {
            if (string.IsNullOrWhiteSpace(memberName))
            {
                var typeExamples = await TryLoadTypeExampleAsync(examplesDirectoryPath, candidateTypeName, cancellationToken).ConfigureAwait(false);
                if (typeExamples.Count > 0)
                {
                    return typeExamples;
                }
            }
            else
            {
                var typeFolder = Path.Combine(examplesDirectoryPath, candidateTypeName);
                if (!this.fileSystem.DirectoryExists(typeFolder))
                {
                    typeFolder = SafeFindDirectoryCaseInsensitive(examplesDirectoryPath, candidateTypeName) ?? typeFolder;
                }

                var memberExamples = await TryLoadMemberExampleAsync(typeFolder, candidateTypeName, memberName, cancellationToken).ConfigureAwait(false);
                if (memberExamples.Count > 0)
                {
                    return memberExamples;
                }
            }
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

    private async Task<IReadOnlyList<string>> TryLoadMemberExampleAsync(
        string typeFolder,
        string sanitizedTypeName,
        string memberName,
        CancellationToken cancellationToken)
    {
        if (!this.fileSystem.DirectoryExists(typeFolder))
        {
            return [];
        }

        var sanitizedMemberName = CleanName(memberName);
        var memberFileName = $"{sanitizedMemberName}.cs";
        var memberFilePath = Path.Combine(typeFolder, memberFileName);
        if (!this.fileSystem.FileExists(memberFilePath))
        {
            memberFilePath = SafeFindFileCaseInsensitive(typeFolder, memberFileName) ?? memberFilePath;
        }

        if (!this.fileSystem.FileExists(memberFilePath))
        {
            return [];
        }

        var loadingMemberExampleMessage = ExecutionMessagesResources.FormatMessage(
            ExecutionMessagesResources.ExampleProviderLoadingMemberExample,
            sanitizedTypeName,
            sanitizedMemberName);
        LogLoadingMemberExample(loadingMemberExampleMessage);

        var snippetContent = (await this.fileSystem.ReadAllTextAsync(memberFilePath, cancellationToken).ConfigureAwait(false)).Trim();
        return string.IsNullOrWhiteSpace(snippetContent) ? [] : [snippetContent];
    }

    private async Task<IReadOnlyList<string>> TryLoadTypeExampleAsync(
        string examplesDirectoryPath,
        string sanitizedTypeName,
        CancellationToken cancellationToken)
    {
        var typeFileName = $"{sanitizedTypeName}.cs";
        var typeFilePath = Path.Combine(examplesDirectoryPath, typeFileName);
        if (!this.fileSystem.FileExists(typeFilePath))
        {
            typeFilePath = SafeFindFileCaseInsensitive(examplesDirectoryPath, typeFileName) ?? typeFilePath;
        }

        if (!this.fileSystem.FileExists(typeFilePath))
        {
            return [];
        }

        var loadingTypeExampleMessage = ExecutionMessagesResources.FormatMessage(
            ExecutionMessagesResources.ExampleProviderLoadingTypeExample,
            sanitizedTypeName);
        LogLoadingTypeExample(loadingTypeExampleMessage);

        var snippetContent = (await this.fileSystem.ReadAllTextAsync(typeFilePath, cancellationToken).ConfigureAwait(false)).Trim();
        return string.IsNullOrWhiteSpace(snippetContent) ? [] : [snippetContent];
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

    private string? SafeFindDirectoryCaseInsensitive(string parentDirectoryPath, string targetDirectoryName)
    {
        try
        {
            return this.fileSystem.FindDirectoryCaseInsensitive(parentDirectoryPath, targetDirectoryName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogEnumerationFailed(parentDirectoryPath, targetDirectoryName, exception.Message);
            return null;
        }
    }

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
}
