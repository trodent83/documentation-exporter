using System.Text;
using System.Text.RegularExpressions;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Generation.Diagrams;

/// <summary>
/// Default implementation of <see cref="IDiagramIdentifierFormatter"/>.
/// </summary>
public sealed partial class DiagramIdentifierFormatter : IDiagramIdentifierFormatter
{
    /// <inheritdoc />
    public string DependencyTreeLegend => MarkdownGenerationResources.AssemblyDependencyTreeLegend;

    /// <inheritdoc />
    public string SanitizeIdentifier(string rawName)
    {
        ArgumentNullException.ThrowIfNull(rawName);

        StringBuilder builder = new();
        foreach (var character in rawName)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                _ = builder.Append(character);
            }
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public string ExtractSimpleTypeIdentifier(string rawEntry)
    {
        if (string.IsNullOrWhiteSpace(rawEntry))
        {
            return string.Empty;
        }

        var trimmed = rawEntry.Trim();
        if (trimmed.StartsWith('['))
        {
            var closeBracket = trimmed.IndexOf(']');
            if (closeBracket > 1)
            {
                trimmed = trimmed[1..closeBracket];
            }
        }

        var angleIndex = trimmed.IndexOf('<');
        if (angleIndex > 0)
        {
            trimmed = trimmed[..angleIndex];
        }

        var lastDotIndex = trimmed.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex < trimmed.Length - 1)
        {
            trimmed = trimmed[(lastDotIndex + 1)..];
        }

        return SanitizeIdentifier(trimmed);
    }

    /// <inheritdoc />
    public string ExtractDisplayTypeLabel(string? rawTypeLink)
    {
        if (string.IsNullOrWhiteSpace(rawTypeLink))
        {
            return string.Empty;
        }

        var trimmed = rawTypeLink.Trim();
        if (trimmed.Contains("](", StringComparison.Ordinal))
        {
            return MarkdownLinkLabelRegex().Replace(trimmed, "$1");
        }

        if (trimmed.StartsWith('['))
        {
            var closeBracket = trimmed.IndexOf(']');
            if (closeBracket > 1)
            {
                return trimmed[1..closeBracket];
            }
        }

        return trimmed;
    }

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
    private static partial Regex MarkdownLinkLabelRegex();

    /// <inheritdoc />
    public (IReadOnlyList<string> BaseNames, IReadOnlyList<string> InterfaceNames, IReadOnlyList<string> DerivedNames) ExtractTypeDiagramRelationships(
        TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        IReadOnlyList<string> baseNames =
        [
            .. typeModel.InheritanceHierarchy
                .Select(ExtractSimpleTypeIdentifier)
                .Where(identifier => !string.IsNullOrEmpty(identifier) &&
                                     !string.Equals(identifier, "Object", StringComparison.OrdinalIgnoreCase) &&
                                     !string.Equals(identifier, "System_Object", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
        ];

        IReadOnlyList<string> interfaceNames =
        [
            .. typeModel.ImplementedInterfaces
                .Select(ExtractSimpleTypeIdentifier)
                .Where(identifier => !string.IsNullOrEmpty(identifier))
                .Distinct(StringComparer.Ordinal)
        ];

        IReadOnlyList<string> derivedNames =
        [
            .. typeModel.DerivedTypes
                .Select(ExtractSimpleTypeIdentifier)
                .Where(identifier => !string.IsNullOrEmpty(identifier))
                .Distinct(StringComparer.Ordinal)
        ];

        return (baseNames, interfaceNames, derivedNames);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> CollectArchitectureRelationships(IReadOnlyList<TypeDocumentationModel> materializedTypes)
    {
        ArgumentNullException.ThrowIfNull(materializedTypes);

        HashSet<string> relations = [with(StringComparer.Ordinal)];
        foreach (var typeModel in materializedTypes)
        {
            var nodeNameSource = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
                ? typeModel.DirectorySegmentName
                : typeModel.TypeName;
            var currentNode = SanitizeIdentifier(nodeNameSource);
            if (string.IsNullOrEmpty(currentNode))
            {
                continue;
            }

            var baseNames = typeModel.InheritanceHierarchy
                .Select(ExtractSimpleTypeIdentifier)
                .Where(name => !string.IsNullOrEmpty(name) &&
                               !string.Equals(name, "Object", StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(name, "ValueType", StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(name, "Enum", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (baseNames.Count > 0)
            {
                _ = relations.Add($"{baseNames[^1]} <|-- {currentNode}");
            }

            foreach (var interfaceEntry in typeModel.ImplementedInterfaces)
            {
                var interfaceNode = ExtractSimpleTypeIdentifier(interfaceEntry);
                if (!string.IsNullOrEmpty(interfaceNode))
                {
                    _ = relations.Add($"{interfaceNode} <|.. {currentNode}");
                }
            }
        }

        return [.. relations.OrderBy(item => item, StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public string FormatDependencyEdgeLabel(AssemblyDependencyEdgeModel dependencyEdge)
    {
        ArgumentNullException.ThrowIfNull(dependencyEdge);

        var kindLabel = dependencyEdge.VersionLinkingKind switch
        {
            AssemblyVersionLinkingKind.Compiled => MarkdownGenerationResources.VersionLinkingCompiled,
            AssemblyVersionLinkingKind.ProjectLinked => MarkdownGenerationResources.VersionLinkingProjectLinked,
            AssemblyVersionLinkingKind.Fixed => MarkdownGenerationResources.VersionLinkingFixed,
            AssemblyVersionLinkingKind.Minimum => MarkdownGenerationResources.VersionLinkingMinimum,
            AssemblyVersionLinkingKind.Range => MarkdownGenerationResources.VersionLinkingRange,
            AssemblyVersionLinkingKind.Floating => MarkdownGenerationResources.VersionLinkingFloating,
            AssemblyVersionLinkingKind.Framework => MarkdownGenerationResources.VersionLinkingFramework,
            _ => MarkdownGenerationResources.VersionLinkingCompiled
        };

        if (dependencyEdge.VersionLinkingKind == AssemblyVersionLinkingKind.ProjectLinked)
        {
            return kindLabel.Replace("\"", "'", StringComparison.Ordinal);
        }

        var versionDetail = !string.IsNullOrWhiteSpace(dependencyEdge.VersionConstraintExpression)
            ? dependencyEdge.VersionConstraintExpression
            : dependencyEdge.ResolvedVersion;

        var rawLabel = string.IsNullOrWhiteSpace(versionDetail)
            ? kindLabel
            : $"{kindLabel} ({versionDetail})";

        return rawLabel.Replace("\"", "'", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public (IReadOnlyList<(string NodeName, string NodeIdentifier)> OrderedNodes, IReadOnlyDictionary<string, string> IdentifierByNodeName) BuildUniqueNodeIdentifiers(
        string rootAssemblyName,
        IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootAssemblyName);
        ArgumentNullException.ThrowIfNull(dependencyEdges);

        var trimmedRootName = rootAssemblyName.Trim();
        List<string> orderedNames = [trimmedRootName];

        var remainingNames = dependencyEdges
            .SelectMany(dependencyEdge => new[] { dependencyEdge.SourceAssemblyName, dependencyEdge.TargetAssemblyName })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Where(name => !string.Equals(name, trimmedRootName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        orderedNames.AddRange(remainingNames);

        List<(string NodeName, string NodeIdentifier)> orderedNodes = [];
        Dictionary<string, string> identifierByNodeName = [with(StringComparer.OrdinalIgnoreCase)];
        HashSet<string> usedIdentifiers = [with(StringComparer.Ordinal)];

        foreach (var nodeName in orderedNames)
        {
            var baseIdentifier = SanitizeIdentifier(nodeName);
            if (string.IsNullOrEmpty(baseIdentifier))
            {
                baseIdentifier = "Assembly";
            }

            var uniqueIdentifier = baseIdentifier;
            var suffix = 2;
            while (!usedIdentifiers.Add(uniqueIdentifier))
            {
                uniqueIdentifier = $"{baseIdentifier}_{suffix}";
                suffix++;
            }

            orderedNodes.Add((nodeName, uniqueIdentifier));
            identifierByNodeName[nodeName] = uniqueIdentifier;
        }

        return (orderedNodes, identifierByNodeName);
    }
}
