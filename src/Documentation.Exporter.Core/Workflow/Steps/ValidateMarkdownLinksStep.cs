using Documentation.Exporter.Abstractions.Workflow;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Scans all emitted Markdown files in the output directory, strips fenced and inline code spans,
/// verifies that relative hyperlinks and <c>#anchor</c> fragments resolve to existing files and headings,
/// and records <c>DOC006</c>/<c>DOC007</c> diagnostics while enforcing strict link validation.
/// </summary>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class ValidateMarkdownLinksStep(
    IFileSystem fileSystem,
    ILogger<ValidateMarkdownLinksStep>? logger = null) : IExportWorkflowStep
{
    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<ValidateMarkdownLinksStep> logger = logger ?? NullLogger<ValidateMarkdownLinksStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameValidateMarkdownLinks;

    [LoggerMessage(EventId = 830, Level = LogLevel.Error, Message = "Strict link validation failed: {BrokenLinkCount} broken relative link(s) detected across generated documentation files.")]
    private partial void LogStrictValidationFailed(int brokenLinkCount);

    [LoggerMessage(EventId = 831, Level = LogLevel.Warning, Message = "Broken relative link found in '{SourceFilePath}': target '{LinkTarget}' does not exist on disk.")]
    private partial void LogBrokenLinkFound(string sourceFilePath, string linkTarget);

    [LoggerMessage(EventId = 832, Level = LogLevel.Information, Message = "Verified {VerifiedLinkCount} relative documentation links across {FileCount} generated Markdown files. No broken links detected.")]
    private partial void LogValidationPassed(int verifiedLinkCount, int fileCount);

    [LoggerMessage(EventId = 833, Level = LogLevel.Warning, Message = "{StatusMessage}")]
    private partial void LogBrokenAnchorFound(string statusMessage);

    /// <inheritdoc />
    public async Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var outputDirectoryPath = context.Options.OutputDirectoryPath;
        if (string.IsNullOrWhiteSpace(outputDirectoryPath) || !this.fileSystem.DirectoryExists(outputDirectoryPath))
        {
            return true;
        }

        var markdownFiles = ResolveTargetMarkdownFiles(outputDirectoryPath);
        ConcurrentDictionary<string, HashSet<string>> headingSlugsCache = new(StringComparer.OrdinalIgnoreCase);
        ConcurrentBag<(string SourceFile, string LinkTarget, int LineNumber, bool IsAnchor, string AnchorSlug)> brokenEntries = [];
        var verifiedLinkCount = 0;

        await Parallel.ForEachAsync(markdownFiles, cancellationToken, async (markdownFilePath, token) =>
        {
            var markdownContent = await this.fileSystem.ReadAllTextAsync(markdownFilePath, token).ConfigureAwait(false);
            var normalizedLines = SplitLinesPreservingNumbers(markdownContent);
            var linesWithoutFences = StripFencedCodeLines(normalizedLines);
            var currentFileSlugs = headingSlugsCache.GetOrAdd(
                this.fileSystem.GetFullPath(markdownFilePath),
                _ => ExtractHeadingSlugs(linesWithoutFences));

            var fileDirectory = Path.GetDirectoryName(markdownFilePath) ?? outputDirectoryPath;

            for (var lineIndex = 0; lineIndex < linesWithoutFences.Length; lineIndex++)
            {
                var lineWithoutInlineCode = StripInlineCodeSpansOnLine(linesWithoutFences[lineIndex]);
                if (string.IsNullOrWhiteSpace(lineWithoutInlineCode))
                {
                    continue;
                }

                var linkMatches = MarkdownLinkRegex().Matches(lineWithoutInlineCode);
                foreach (Match match in linkMatches)
                {
                    var rawLinkTarget = match.Groups[2].Value.Trim();
                    var spaceTitleIndex = rawLinkTarget.IndexOf(" \"", StringComparison.Ordinal);
                    if (spaceTitleIndex > 0)
                    {
                        rawLinkTarget = rawLinkTarget[..spaceTitleIndex].Trim();
                    }

                    if (string.IsNullOrEmpty(rawLinkTarget) || IsExternalLink(rawLinkTarget))
                    {
                        continue;
                    }

                    var lineNumber = lineIndex + 1;
                    var (relativeFilePart, anchorSlug) = SplitTargetAndAnchor(rawLinkTarget);

                    if (string.IsNullOrWhiteSpace(relativeFilePart))
                    {
                        if (string.IsNullOrEmpty(anchorSlug))
                        {
                            continue;
                        }

                        _ = Interlocked.Increment(ref verifiedLinkCount);
                        if (!currentFileSlugs.Contains(anchorSlug))
                        {
                            brokenEntries.Add((markdownFilePath, rawLinkTarget, lineNumber, IsAnchor: true, anchorSlug));
                        }

                        continue;
                    }

                    var relativeOsPath = relativeFilePart.Replace('/', Path.DirectorySeparatorChar);
                    var resolvedTargetPath = this.fileSystem.GetFullPath(Path.Combine(fileDirectory, relativeOsPath));

                    _ = Interlocked.Increment(ref verifiedLinkCount);

                    var fileExists = this.fileSystem.FileExists(resolvedTargetPath);
                    var directoryExists = !fileExists && this.fileSystem.DirectoryExists(resolvedTargetPath);

                    if (!fileExists && !directoryExists)
                    {
                        brokenEntries.Add((markdownFilePath, rawLinkTarget, lineNumber, IsAnchor: false, string.Empty));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(anchorSlug) && fileExists)
                    {
                        var targetSlugs = await GetOrLoadTargetSlugsAsync(resolvedTargetPath, headingSlugsCache, token).ConfigureAwait(false);
                        if (!targetSlugs.Contains(anchorSlug))
                        {
                            brokenEntries.Add((markdownFilePath, rawLinkTarget, lineNumber, IsAnchor: true, anchorSlug));
                        }
                    }
                }
            }
        }).ConfigureAwait(false);

        context.VerifiedLinksCount = verifiedLinkCount;

        var orderedBrokenEntries = brokenEntries
            .OrderBy(entry => entry.SourceFile, StringComparer.Ordinal)
            .ThenBy(entry => entry.LineNumber)
            .ThenBy(entry => entry.LinkTarget, StringComparer.Ordinal)
            .ToList();

        var diagnosticSeverity = context.Options.StrictLinkValidation
            ? DocumentationDiagnosticSeverity.Error
            : DocumentationDiagnosticSeverity.Warning;

        foreach (var (sourceFilePath, linkTarget, lineNumber, isAnchor, anchorSlug) in orderedBrokenEntries)
        {
            var relativeSourcePath = Path.GetRelativePath(outputDirectoryPath, sourceFilePath).Replace('\\', '/');
            if (isAnchor)
            {
                var anchorMessage = ExecutionMessagesResources.FormatMessage(
                    ExecutionMessagesResources.StepBrokenAnchorDetected,
                    relativeSourcePath,
                    lineNumber,
                    linkTarget,
                    anchorSlug);
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    LogBrokenAnchorFound(anchorMessage);
                }

                context.Diagnostics.Add(new DocumentationDiagnostic
                {
                    Code = "DOC007",
                    Kind = DocumentationDiagnosticKind.BrokenMarkdownAnchor,
                    Severity = diagnosticSeverity,
                    FilePath = relativeSourcePath,
                    LineNumber = lineNumber,
                    TargetReference = linkTarget,
                    Message = anchorMessage
                });
            }
            else
            {
                LogBrokenLinkFound(sourceFilePath, linkTarget);

                context.Diagnostics.Add(new DocumentationDiagnostic
                {
                    Code = "DOC006",
                    Kind = DocumentationDiagnosticKind.BrokenMarkdownLink,
                    Severity = diagnosticSeverity,
                    FilePath = relativeSourcePath,
                    LineNumber = lineNumber,
                    TargetReference = linkTarget,
                    Message = $"Broken relative link in '{relativeSourcePath}' (line {lineNumber}): target '{linkTarget}' does not exist on disk."
                });
            }
        }

        if (orderedBrokenEntries.Count > 0)
        {
            if (context.Options.StrictLinkValidation)
            {
                LogStrictValidationFailed(orderedBrokenEntries.Count);
                context.HasFailed = true;
                return false;
            }

            return true;
        }

        LogValidationPassed(verifiedLinkCount, markdownFiles.Length);
        return true;
    }

    private string[] ResolveTargetMarkdownFiles(string outputDirectoryPath)
    {
        var manifestFilePath = Path.Combine(outputDirectoryPath, EnsureOutputDirectoryStep.GeneratedFilesManifestFileName);
        if (this.fileSystem.FileExists(manifestFilePath))
        {
            var fullOutputRoot = Path.TrimEndingDirectorySeparator(this.fileSystem.GetFullPath(outputDirectoryPath)) + Path.DirectorySeparatorChar;
            var manifestEntries = EnsureOutputDirectoryStep.ParseManifestEntries(this.fileSystem.ReadAllText(manifestFilePath));
            var manifestMarkdownFiles = manifestEntries
                .Where(entry => entry.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Select(entry => this.fileSystem.GetFullPath(Path.Combine(outputDirectoryPath, entry.Replace('/', Path.DirectorySeparatorChar))))
                .Where(fullPath => fullPath.StartsWith(fullOutputRoot, StringComparison.OrdinalIgnoreCase) && this.fileSystem.FileExists(fullPath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (manifestMarkdownFiles.Length > 0)
            {
                return manifestMarkdownFiles;
            }
        }

        return [.. this.fileSystem.EnumerateFiles(outputDirectoryPath, "*.md", SearchOption.AllDirectories)];
    }

    private async Task<HashSet<string>> GetOrLoadTargetSlugsAsync(
        string resolvedTargetFilePath,
        ConcurrentDictionary<string, HashSet<string>> headingSlugsCache,
        CancellationToken cancellationToken)
    {
        if (headingSlugsCache.TryGetValue(resolvedTargetFilePath, out var cachedSlugs))
        {
            return cachedSlugs;
        }

        var targetContent = await this.fileSystem.ReadAllTextAsync(resolvedTargetFilePath, cancellationToken).ConfigureAwait(false);
        var linesWithoutFences = StripFencedCodeLines(SplitLinesPreservingNumbers(targetContent));
        var extractedSlugs = ExtractHeadingSlugs(linesWithoutFences);
        return headingSlugsCache.GetOrAdd(resolvedTargetFilePath, extractedSlugs);
    }

    private static string[] SplitLinesPreservingNumbers(string markdownContent) =>
        markdownContent.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

    private static string[] StripFencedCodeLines(string[] lines)
    {
        var strippedLines = new string[lines.Length];
        var activeFenceChar = '\0';
        var activeFenceLength = 0;

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var currentLine = lines[lineIndex];
            var trimmedStart = currentLine.TrimStart();

            if (activeFenceChar == '\0')
            {
                if (TryParseFenceMarker(trimmedStart, out var markerChar, out var markerLength, out _))
                {
                    activeFenceChar = markerChar;
                    activeFenceLength = markerLength;
                    strippedLines[lineIndex] = string.Empty;
                    continue;
                }

                strippedLines[lineIndex] = currentLine;
                continue;
            }

            strippedLines[lineIndex] = string.Empty;
            if (TryParseFenceMarker(trimmedStart, out var closingChar, out var closingLength, out var trailingText) &&
                closingChar == activeFenceChar &&
                closingLength >= activeFenceLength &&
                string.IsNullOrWhiteSpace(trailingText))
            {
                activeFenceChar = '\0';
                activeFenceLength = 0;
            }
        }

        return strippedLines;
    }

    private static bool TryParseFenceMarker(string trimmedLine, out char fenceChar, out int fenceLength, out string trailingText)
    {
        fenceChar = '\0';
        fenceLength = 0;
        trailingText = string.Empty;

        if (trimmedLine.Length < 3 || (trimmedLine[0] != '`' && trimmedLine[0] != '~'))
        {
            return false;
        }

        var candidateChar = trimmedLine[0];
        var count = 0;
        while (count < trimmedLine.Length && trimmedLine[count] == candidateChar)
        {
            count++;
        }

        if (count < 3)
        {
            return false;
        }

        fenceChar = candidateChar;
        fenceLength = count;
        trailingText = trimmedLine[count..];
        return true;
    }

    private static string StripInlineCodeSpansOnLine(string line)
    {
        if (!line.Contains('`'))
        {
            return line;
        }

        StringBuilder resultBuilder = new(line.Length);
        var scanIndex = 0;

        while (scanIndex < line.Length)
        {
            if (line[scanIndex] != '`')
            {
                _ = resultBuilder.Append(line[scanIndex]);
                scanIndex++;
                continue;
            }

            var tickCount = 0;
            while (scanIndex + tickCount < line.Length && line[scanIndex + tickCount] == '`')
            {
                tickCount++;
            }

            var closingIndex = FindMatchingClosingTicks(line, scanIndex + tickCount, tickCount);
            if (closingIndex < 0)
            {
                _ = resultBuilder.Append(line, scanIndex, tickCount);
                scanIndex += tickCount;
                continue;
            }

            var spanLength = closingIndex + tickCount - scanIndex;
            _ = resultBuilder.Append(' ', spanLength);
            scanIndex = closingIndex + tickCount;
        }

        return resultBuilder.ToString();
    }

    private static int FindMatchingClosingTicks(string line, int searchStartIndex, int expectedTickCount)
    {
        var scanIndex = searchStartIndex;
        while (scanIndex < line.Length)
        {
            if (line[scanIndex] != '`')
            {
                scanIndex++;
                continue;
            }

            var tickCount = 0;
            while (scanIndex + tickCount < line.Length && line[scanIndex + tickCount] == '`')
            {
                tickCount++;
            }

            if (tickCount == expectedTickCount)
            {
                return scanIndex;
            }

            scanIndex += tickCount;
        }

        return -1;
    }

    private static HashSet<string> ExtractHeadingSlugs(string[] linesWithoutFences)
    {
        HashSet<string> slugs = [with(StringComparer.OrdinalIgnoreCase)];
        Dictionary<string, int> occurrenceCounts = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (var line in linesWithoutFences)
        {
            var headingMatch = AtxHeadingRegex().Match(line);
            if (!headingMatch.Success)
            {
                continue;
            }

            var rawHeadingText = headingMatch.Groups[1].Value;
            var baseSlug = BuildGitHubHeadingSlug(rawHeadingText);
            if (string.IsNullOrEmpty(baseSlug))
            {
                continue;
            }

            if (!occurrenceCounts.TryGetValue(baseSlug, out var seenCount))
            {
                occurrenceCounts[baseSlug] = 1;
                _ = slugs.Add(baseSlug);
                continue;
            }

            occurrenceCounts[baseSlug] = seenCount + 1;
            _ = slugs.Add($"{baseSlug}-{seenCount}");
            _ = slugs.Add($"{baseSlug}_{seenCount}");
        }

        return slugs;
    }

    private static string BuildGitHubHeadingSlug(string rawHeadingText)
    {
        var withoutLinks = MarkdownLinkRegex().Replace(rawHeadingText, "$1");
        var normalized = withoutLinks
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Trim();

        StringBuilder slugBuilder = new();
        foreach (var character in normalized.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                _ = slugBuilder.Append(character);
            }
            else if (character is ' ' or '-' or '_')
            {
                _ = slugBuilder.Append(character == '_' ? '_' : '-');
            }
        }

        return slugBuilder.ToString();
    }

    private static bool IsExternalLink(string linkTarget) =>
        linkTarget.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        linkTarget.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        linkTarget.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
        linkTarget.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
        linkTarget.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
        linkTarget.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);

    private static (string RelativeFilePart, string AnchorSlug) SplitTargetAndAnchor(string rawLinkTarget)
    {
        var hashIndex = rawLinkTarget.IndexOf('#');
        var rawFilePart = hashIndex >= 0 ? rawLinkTarget[..hashIndex] : rawLinkTarget;
        var questionIndex = rawFilePart.IndexOf('?');
        var cleanFilePart = (questionIndex >= 0 ? rawFilePart[..questionIndex] : rawFilePart).Trim();

        var rawAnchorPart = hashIndex >= 0 ? rawLinkTarget[(hashIndex + 1)..] : string.Empty;
        var decodedAnchor = string.IsNullOrWhiteSpace(rawAnchorPart)
            ? string.Empty
            : Uri.UnescapeDataString(rawAnchorPart).Trim().ToLowerInvariant();

        return (cleanFilePart, decodedAnchor);
    }

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$")]
    private static partial Regex AtxHeadingRegex();

    [GeneratedRegex(@"\[((?:[^\[\]]|\[[^\]]*\])+)\]\(([^)]+)\)")]
    private static partial Regex MarkdownLinkRegex();
}
