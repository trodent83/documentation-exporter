using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;

namespace Documentation.Exporter.Plugins.Navigation.MkDocs;

/// <summary>
/// Built-in MkDocs implementation of <see cref="INavigationGenerator"/> supporting standalone generation,
/// top-level merging, nested section insertion, and placeholder comment replacement.
/// </summary>
public sealed class MkDocsNavigationGenerator : INavigationGenerator
{
    /// <inheritdoc />
    public NavigationFormat Format => NavigationFormat.MkDocs;

    /// <inheritdoc />
    public string DefaultFileName => "mkdocs.nav.yml";

    /// <inheritdoc />
    public string GenerateNavigation(
        IReadOnlyList<TypeDocumentationModel> types,
        bool useAssemblySubdirectories = false,
        string? existingNavigationContent = null,
        string? navigationEntryPoint = null,
        string? navigationUrlPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(types);

        var materializedTypes = types.ToList();
        var itemLines = BuildNavigationItemLines(materializedTypes, useAssemblySubdirectories, navigationUrlPrefix);
        var trimmedEntryPoint = navigationEntryPoint?.Trim();
        var isCommentMarker = !string.IsNullOrWhiteSpace(trimmedEntryPoint) && trimmedEntryPoint.StartsWith('#');
        var entrySegments = !string.IsNullOrWhiteSpace(trimmedEntryPoint) && !isCommentMarker
            ? trimmedEntryPoint.Split(['/', '>'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

        if (string.IsNullOrWhiteSpace(existingNavigationContent))
        {
            return BuildStandaloneNavYaml(entrySegments, itemLines);
        }

        var lines = existingNavigationContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .ToList();

        while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        if (isCommentMarker && !string.IsNullOrWhiteSpace(trimmedEntryPoint))
        {
            var endMarker = $"{trimmedEntryPoint}-END";
            var markerIndex = lines.FindIndex(line => string.Equals(line.Trim(), trimmedEntryPoint, StringComparison.Ordinal));
            if (markerIndex >= 0)
            {
                var markerIndent = CountLeadingSpaces(lines[markerIndex]);
                var markerIndentPrefix = new string(' ', markerIndent);
                var endMarkerIndex = lines.FindIndex(markerIndex + 1, line => string.Equals(line.Trim(), endMarker, StringComparison.Ordinal));
                if (endMarkerIndex >= 0)
                {
                    lines.RemoveRange(markerIndex + 1, endMarkerIndex - markerIndex - 1);
                    lines.InsertRange(markerIndex + 1, itemLines.Select(line => $"{markerIndentPrefix}{line}"));
                }
                else
                {
                    List<string> markerRegionLines =
                    [
                        .. itemLines.Select(line => $"{markerIndentPrefix}{line}"),
                        $"{markerIndentPrefix}{endMarker}"
                    ];
                    lines.InsertRange(markerIndex + 1, markerRegionLines);
                }

                return string.Join(Environment.NewLine, lines) + Environment.NewLine;
            }
        }

        var navLineIndex = lines.FindIndex(line =>
            CountLeadingSpaces(line) == 0 &&
            (string.Equals(line.Trim(), "nav:", StringComparison.Ordinal) ||
             line.Trim().StartsWith("nav:", StringComparison.Ordinal)));

        if (navLineIndex < 0)
        {
            AppendNavRootBlock(lines, entrySegments, itemLines);
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        var navBlockEndIndex = FindNavBlockEndIndex(lines, navLineIndex);
        if (entrySegments.Length > 0)
        {
            InsertOrReplaceAtEntrySegments(lines, navLineIndex + 1, navBlockEndIndex, entrySegments, itemLines);
        }
        else
        {
            MergeTopLevelNavItems(lines, navLineIndex + 1, navBlockEndIndex, itemLines);
        }

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string BuildStandaloneNavYaml(string[] entrySegments, List<string> itemLines)
    {
        StringBuilder yamlBuilder = new();
        _ = yamlBuilder.AppendLine("nav:");
        var currentIndent = 2;
        foreach (var segment in entrySegments)
        {
            _ = yamlBuilder.AppendLine(CultureInfo.InvariantCulture, $"{new string(' ', currentIndent)}- {segment}:");
            currentIndent += 4;
        }

        var indentPrefix = new string(' ', currentIndent);
        foreach (var itemLine in itemLines)
        {
            _ = yamlBuilder.AppendLine(CultureInfo.InvariantCulture, $"{indentPrefix}{itemLine}");
        }

        return yamlBuilder.ToString();
    }

    private static void AppendNavRootBlock(List<string> lines, string[] entrySegments, List<string> itemLines)
    {
        lines.Add("nav:");
        var currentIndent = 2;
        foreach (var segment in entrySegments)
        {
            lines.Add($"{new string(' ', currentIndent)}- {segment}:");
            currentIndent += 4;
        }

        var indentPrefix = new string(' ', currentIndent);
        lines.AddRange(itemLines.Select(line => $"{indentPrefix}{line}"));
    }

    private static List<string> BuildNavigationItemLines(
        List<TypeDocumentationModel> materializedTypes,
        bool includeAssemblyDirectory,
        string? navigationUrlPrefix)
    {
        var normalizedPrefix = string.IsNullOrWhiteSpace(navigationUrlPrefix)
            ? string.Empty
            : $"{navigationUrlPrefix.Trim().Replace('\\', '/').Trim('/')}/";

        List<string> itemLines = [$"- Overview: {normalizedPrefix}index.md"];

        if (includeAssemblyDirectory)
        {
            var groupedByAssembly = materializedTypes
                .GroupBy(typeModel => typeModel.AssemblyName, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal);

            foreach (var assemblyGroup in groupedByAssembly)
            {
                var assemblyKey = FormatYamlNavKey(assemblyGroup.Key, "DefaultAssembly");
                itemLines.Add($"- {assemblyKey}:");
                if (!string.IsNullOrWhiteSpace(assemblyGroup.Key))
                {
                    itemLines.Add($"    - Overview: {normalizedPrefix}{assemblyGroup.Key}/index.md");
                }

                foreach (var namespaceGroup in assemblyGroup.GroupBy(typeModel => typeModel.NamespaceName).OrderBy(group => group.Key, StringComparer.Ordinal))
                {
                    var namespaceKey = FormatYamlNavKey(namespaceGroup.Key, "Global");
                    itemLines.Add($"    - {namespaceKey}:");
                    foreach (var typeModel in namespaceGroup
                                 .OrderBy(typeModel => typeModel.TypeName, StringComparer.Ordinal)
                                 .ThenBy(typeModel => typeModel.GenericTypeParameters.Count))
                    {
                        var typeUrl = TypeDocumentationPaths.GetTypeRelativeUrlPath(typeModel, includeAssemblyDirectory: true);
                        var typeKey = FormatYamlNavKey(typeModel.DisplayTypeName, typeModel.TypeName);
                        itemLines.Add($"        - {typeKey}: {normalizedPrefix}{typeUrl}");
                    }
                }
            }

            return itemLines;
        }

        foreach (var namespaceGroup in materializedTypes.GroupBy(typeModel => typeModel.NamespaceName).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var namespaceKey = FormatYamlNavKey(namespaceGroup.Key, "Global");
            itemLines.Add($"- {namespaceKey}:");
            foreach (var typeModel in namespaceGroup
                         .OrderBy(typeModel => typeModel.TypeName, StringComparer.Ordinal)
                         .ThenBy(typeModel => typeModel.GenericTypeParameters.Count))
            {
                var typeUrl = TypeDocumentationPaths.GetTypeRelativeUrlPath(typeModel, includeAssemblyDirectory: false);
                var typeKey = FormatYamlNavKey(typeModel.DisplayTypeName, typeModel.TypeName);
                itemLines.Add($"    - {typeKey}: {normalizedPrefix}{typeUrl}");
            }
        }

        return itemLines;
    }

    private static string FormatYamlNavKey(string? rawKey, string fallbackKey)
    {
        var key = string.IsNullOrWhiteSpace(rawKey) ? fallbackKey : rawKey.Trim();
        return key.IndexOfAny([':', '#']) >= 0
            ? $"\"{key.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : key;
    }

    private static int FindNavBlockEndIndex(List<string> lines, int navLineIndex)
    {
        var scanIndex = navLineIndex + 1;
        while (scanIndex < lines.Count)
        {
            var candidateLine = lines[scanIndex];
            if (!string.IsNullOrWhiteSpace(candidateLine) &&
                !candidateLine.TrimStart().StartsWith('#') &&
                CountLeadingSpaces(candidateLine) == 0)
            {
                break;
            }

            scanIndex++;
        }

        while (scanIndex > navLineIndex + 1 && string.IsNullOrWhiteSpace(lines[scanIndex - 1]))
        {
            scanIndex--;
        }

        return scanIndex;
    }

    private static void InsertOrReplaceAtEntrySegments(
        List<string> lines,
        int navStartIndex,
        int navEndIndex,
        string[] entrySegments,
        List<string> itemLines)
    {
        var topLevelIndent = DetectListIndent(lines, navStartIndex, navEndIndex, defaultIndent: 2);
        var searchStart = navStartIndex;
        var searchEnd = navEndIndex;
        var currentExpectedIndent = topLevelIndent;

        for (var segmentIndex = 0; segmentIndex < entrySegments.Length; segmentIndex++)
        {
            var targetSegment = entrySegments[segmentIndex];
            var matchedLineIndex = FindMatchingNavSectionLine(
                lines,
                searchStart,
                searchEnd,
                targetSegment,
                currentExpectedIndent,
                allowAnyIndentFallback: segmentIndex == 0);

            if (matchedLineIndex >= 0)
            {
                var matchedIndent = CountLeadingSpaces(lines[matchedLineIndex]);
                lines[matchedLineIndex] = $"{new string(' ', matchedIndent)}- {targetSegment}:";
                var childBlockEnd = FindChildBlockEnd(lines, matchedLineIndex + 1, searchEnd, matchedIndent);

                if (segmentIndex == entrySegments.Length - 1)
                {
                    lines.RemoveRange(matchedLineIndex + 1, childBlockEnd - (matchedLineIndex + 1));
                    var childIndentPrefix = new string(' ', matchedIndent + 4);
                    lines.InsertRange(matchedLineIndex + 1, itemLines.Select(line => $"{childIndentPrefix}{line}"));
                    return;
                }

                currentExpectedIndent = DetectListIndent(lines, matchedLineIndex + 1, childBlockEnd, matchedIndent + 4);
                searchStart = matchedLineIndex + 1;
                searchEnd = childBlockEnd;
                continue;
            }

            List<string> linesToInsert = [];
            var nextIndent = currentExpectedIndent;
            for (var remainingIndex = segmentIndex; remainingIndex < entrySegments.Length; remainingIndex++)
            {
                linesToInsert.Add($"{new string(' ', nextIndent)}- {entrySegments[remainingIndex]}:");
                nextIndent += 4;
            }

            var itemIndentPrefix = new string(' ', nextIndent);
            linesToInsert.AddRange(itemLines.Select(line => $"{itemIndentPrefix}{line}"));
            lines.InsertRange(searchEnd, linesToInsert);
            return;
        }
    }

    private static void MergeTopLevelNavItems(
        List<string> lines,
        int navStartIndex,
        int navEndIndex,
        List<string> itemLines)
    {
        var topLevelIndent = DetectListIndent(lines, navStartIndex, navEndIndex, defaultIndent: 2);
        var topLevelBlocks = SplitIntoTopLevelBlocks(itemLines);
        var currentNavEnd = navEndIndex;

        foreach (var (sectionKey, blockLines) in topLevelBlocks)
        {
            var indentedBlockLines = blockLines.Select(line => $"{new string(' ', topLevelIndent)}{line}").ToList();
            var existingLineIndex = FindMatchingNavSectionLine(
                lines,
                navStartIndex,
                currentNavEnd,
                sectionKey,
                topLevelIndent,
                allowAnyIndentFallback: false);

            if (existingLineIndex >= 0)
            {
                if (string.Equals(sectionKey, "Overview", StringComparison.OrdinalIgnoreCase) &&
                    blockLines.Count == 1 &&
                    !IsGeneratedOverviewScalarLine(lines[existingLineIndex], blockLines[0]))
                {
                    continue;
                }

                var existingIndent = CountLeadingSpaces(lines[existingLineIndex]);
                var childBlockEnd = FindChildBlockEnd(lines, existingLineIndex + 1, currentNavEnd, existingIndent);
                var removeCount = childBlockEnd - existingLineIndex;
                lines.RemoveRange(existingLineIndex, removeCount);
                lines.InsertRange(existingLineIndex, indentedBlockLines);
                currentNavEnd += indentedBlockLines.Count - removeCount;
            }
            else
            {
                lines.InsertRange(currentNavEnd, indentedBlockLines);
                currentNavEnd += indentedBlockLines.Count;
            }
        }
    }

    private static List<(string SectionKey, List<string> BlockLines)> SplitIntoTopLevelBlocks(List<string> itemLines)
    {
        List<(string SectionKey, List<string> BlockLines)> blocks = [];
        List<string>? currentBlock = null;
        var currentKey = string.Empty;

        foreach (var line in itemLines)
        {
            if (CountLeadingSpaces(line) == 0 && line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (currentBlock != null)
                {
                    blocks.Add((currentKey, currentBlock));
                }

                var colonIndex = line.IndexOf(':', 2);
                currentKey = colonIndex > 2 ? line[2..colonIndex].Trim() : line[2..].Trim();
                currentBlock = [line];
            }
            else
            {
                currentBlock?.Add(line);
            }
        }

        if (currentBlock != null)
        {
            blocks.Add((currentKey, currentBlock));
        }

        return blocks;
    }

    private static int FindMatchingNavSectionLine(
        List<string> lines,
        int searchStart,
        int searchEnd,
        string targetSegment,
        int expectedIndent,
        bool allowAnyIndentFallback)
    {
        for (var index = searchStart; index < searchEnd; index++)
        {
            if (CountLeadingSpaces(lines[index]) == expectedIndent &&
                MatchesNavSectionHeader(lines[index], targetSegment))
            {
                return index;
            }
        }

        if (!allowAnyIndentFallback)
        {
            return -1;
        }

        for (var index = searchStart; index < searchEnd; index++)
        {
            if (MatchesNavSectionHeader(lines[index], targetSegment))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsGeneratedOverviewScalarLine(string existingLine, string generatedLine)
    {
        var existingColon = existingLine.IndexOf(':', StringComparison.Ordinal);
        var generatedColon = generatedLine.IndexOf(':', StringComparison.Ordinal);
        if (existingColon < 0 || generatedColon < 0)
        {
            return true;
        }

        var existingValue = existingLine[(existingColon + 1)..].Trim().Trim('"', '\'');
        var generatedValue = generatedLine[(generatedColon + 1)..].Trim().Trim('"', '\'');
        return string.IsNullOrEmpty(existingValue) ||
               string.Equals(existingValue, generatedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesNavSectionHeader(string line, string segmentName)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            return false;
        }

        var colonIndex = trimmed.IndexOf(':', 2);
        if (colonIndex < 0)
        {
            return false;
        }

        var rawKey = trimmed[2..colonIndex].Trim().Trim('"', '\'');
        return string.Equals(rawKey, segmentName, StringComparison.OrdinalIgnoreCase);
    }

    private static int FindChildBlockEnd(List<string> lines, int startIndex, int maxEndIndex, int parentIndent)
    {
        var scanIndex = startIndex;
        while (scanIndex < maxEndIndex)
        {
            var candidate = lines[scanIndex];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                scanIndex++;
                continue;
            }

            if (CountLeadingSpaces(candidate) <= parentIndent)
            {
                break;
            }

            scanIndex++;
        }

        while (scanIndex > startIndex && string.IsNullOrWhiteSpace(lines[scanIndex - 1]))
        {
            scanIndex--;
        }

        return scanIndex;
    }

    private static int DetectListIndent(List<string> lines, int startIndex, int endIndex, int defaultIndent)
    {
        var detectedIndent = int.MaxValue;
        for (var index = startIndex; index < endIndex; index++)
        {
            var trimmed = lines[index].TrimStart();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var indent = CountLeadingSpaces(lines[index]);
                if (indent < detectedIndent)
                {
                    detectedIndent = indent;
                }
            }
        }

        return detectedIndent == int.MaxValue ? defaultIndent : detectedIndent;
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ')
        {
            count++;
        }

        return count;
    }
}
