using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Generates PlantUML package and class hierarchy architecture diagrams for index pages and standalone <c>architecture.puml</c> files.
/// </summary>
/// <param name="identifierFormatter">The diagram identifier formatter.</param>
public sealed class PlantUmlArchitectureDiagramGenerator(IDiagramIdentifierFormatter identifierFormatter) : IArchitectureDiagramGenerator
{
    private readonly IDiagramIdentifierFormatter formatter = PluginArgumentGuard.EnsureNotNull(identifierFormatter);

    /// <inheritdoc />
    public DiagramFormat Format => DiagramFormat.PlantUml;

    /// <inheritdoc />
    public string? StandaloneFileName => "architecture.puml";

    /// <inheritdoc />
    public bool CanGenerate(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory)
    {
        ArgumentNullException.ThrowIfNull(types);

        return types.Any(typeModel => !string.IsNullOrWhiteSpace(typeModel.TypeName));
    }

    /// <inheritdoc />
    public string GenerateEmbeddedMarkdown(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory)
    {
        ArgumentNullException.ThrowIfNull(types);

        var plantUmlContent = GenerateStandaloneContent(types, includeAssemblyDirectory).TrimEnd();
        if (string.IsNullOrEmpty(plantUmlContent))
        {
            return string.Empty;
        }

        StringBuilder outputBuilder = new();
        _ = outputBuilder.AppendLine("```plantuml");
        _ = outputBuilder.AppendLine(plantUmlContent);
        _ = outputBuilder.AppendLine("```");
        _ = outputBuilder.AppendLine();
        return outputBuilder.ToString();
    }

    /// <inheritdoc />
    public string GenerateStandaloneContent(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory)
    {
        ArgumentNullException.ThrowIfNull(types);

        var materializedTypes = types
            .Where(typeModel => !string.IsNullOrWhiteSpace(typeModel.TypeName))
            .ToList();

        if (materializedTypes.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder pumlBuilder = new();
        _ = pumlBuilder.AppendLine("@startuml");
        _ = pumlBuilder.AppendLine("skinparam classAttributeIconSize 0");
        _ = pumlBuilder.AppendLine("skinparam packageStyle rectangle");

        if (includeAssemblyDirectory)
        {
            AppendAssemblyGroupedPackages(pumlBuilder, materializedTypes);
        }
        else
        {
            AppendNamespaceGroupedPackages(pumlBuilder, materializedTypes, indentPrefix: string.Empty);
        }

        foreach (var relationLine in formatter.CollectArchitectureRelationships(materializedTypes))
        {
            _ = pumlBuilder.AppendLine(relationLine);
        }

        _ = pumlBuilder.AppendLine("@enduml");
        return pumlBuilder.ToString();
    }

    private void AppendAssemblyGroupedPackages(StringBuilder pumlBuilder, List<TypeDocumentationModel> materializedTypes)
    {
        var groupedByAssembly = materializedTypes
            .GroupBy(typeModel => string.IsNullOrWhiteSpace(typeModel.AssemblyName) ? "DefaultAssembly" : typeModel.AssemblyName, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var assemblyGroup in groupedByAssembly)
        {
            _ = pumlBuilder.AppendLine(CultureInfo.InvariantCulture, $"package \"{assemblyGroup.Key}\" {{");
            AppendNamespaceGroupedPackages(pumlBuilder, assemblyGroup, indentPrefix: "    ");
            _ = pumlBuilder.AppendLine("}");
        }
    }

    private void AppendNamespaceGroupedPackages(
        StringBuilder pumlBuilder,
        IEnumerable<TypeDocumentationModel> types,
        string indentPrefix)
    {
        foreach (var namespaceGroup in types.GroupBy(typeModel => typeModel.NamespaceName).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            _ = pumlBuilder.AppendLine(CultureInfo.InvariantCulture, $"{indentPrefix}package \"{namespaceGroup.Key}\" {{");
            foreach (var typeModel in namespaceGroup.OrderBy(typeModel => typeModel.TypeName, StringComparer.Ordinal))
            {
                _ = pumlBuilder.AppendLine(CultureInfo.InvariantCulture, $"{indentPrefix}    {FormatCompactPlantUmlTypeLine(typeModel)}");
            }

            _ = pumlBuilder.AppendLine(CultureInfo.InvariantCulture, $"{indentPrefix}}}");
        }
    }

    private string FormatCompactPlantUmlTypeLine(TypeDocumentationModel typeModel)
    {
        var nodeNameSource = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
            ? typeModel.DirectorySegmentName
            : typeModel.TypeName;
        var safeName = formatter.SanitizeIdentifier(nodeNameSource);
        return typeModel switch
        {
            { TypeCategory: TypeCategory.Interface } => $"interface {safeName}",
            { TypeCategory: TypeCategory.Enum } => $"enum {safeName}",
            { IsAbstractType: true, IsStaticType: false } => $"abstract class {safeName}",
            _ => $"class {safeName}"
        };
    }
}
