using System.Globalization;
using System.Text;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Plugins.Diagrams;

/// <summary>
/// Generates PlantUML <c>@startuml ... @enduml</c> class diagram blocks for single-type documentation pages.
/// </summary>
/// <param name="identifierFormatter">The diagram identifier formatter.</param>
public sealed class PlantUmlTypeDiagramGenerator(IDiagramIdentifierFormatter identifierFormatter) : ITypeDiagramGenerator
{
    private readonly IDiagramIdentifierFormatter formatter = PluginArgumentGuard.EnsureNotNull(identifierFormatter);

    /// <inheritdoc />
    public DiagramFormat Format => DiagramFormat.PlantUml;

    /// <inheritdoc />
    public bool CanGenerate(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum ||
            string.IsNullOrWhiteSpace(typeModel.TypeName))
        {
            return false;
        }

        var (baseNames, interfaceNames, derivedNames) = formatter.ExtractTypeDiagramRelationships(typeModel);
        return baseNames.Count > 0 || interfaceNames.Count > 0 || derivedNames.Count > 0;
    }

    /// <inheritdoc />
    public string GenerateTypeDiagram(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);

        if (typeModel.TypeCategory == TypeCategory.Enum ||
            string.IsNullOrWhiteSpace(typeModel.TypeName))
        {
            return string.Empty;
        }

        var (baseNames, interfaceNames, derivedNames) = formatter.ExtractTypeDiagramRelationships(typeModel);
        if (baseNames.Count == 0 && interfaceNames.Count == 0 && derivedNames.Count == 0)
        {
            return string.Empty;
        }

        var nodeNameSource = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
            ? typeModel.DirectorySegmentName
            : typeModel.TypeName;
        var currentIdentifier = formatter.SanitizeIdentifier(nodeNameSource);
        var isCurrentInterface = typeModel.TypeCategory == TypeCategory.Interface;

        StringBuilder sectionBuilder = new();
        _ = sectionBuilder.AppendLine("```plantuml");
        _ = sectionBuilder.AppendLine("@startuml");
        _ = sectionBuilder.AppendLine("skinparam classAttributeIconSize 0");

        var declarationHeader = GetPlantUmlTypeDeclarationHeader(typeModel, currentIdentifier);
        _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"{declarationHeader} {{");

        AppendProperties(sectionBuilder, typeModel.Properties);
        AppendMethods(sectionBuilder, typeModel.Methods);
        AppendEvents(sectionBuilder, typeModel.Events);

        _ = sectionBuilder.AppendLine("}");

        for (var index = 0; index < baseNames.Count; index++)
        {
            var parentNode = baseNames[index];
            var childNode = index + 1 < baseNames.Count ? baseNames[index + 1] : currentIdentifier;
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"{parentNode} <|-- {childNode}");
        }

        foreach (var interfaceNode in interfaceNames)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"interface {interfaceNode}");
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"{interfaceNode} <|.. {currentIdentifier}");
        }

        var arrowToken = isCurrentInterface ? "<|.." : "<|--";
        foreach (var derivedNode in derivedNames)
        {
            _ = sectionBuilder.AppendLine(CultureInfo.InvariantCulture, $"{currentIdentifier} {arrowToken} {derivedNode}");
        }

        _ = sectionBuilder.AppendLine("@enduml");
        _ = sectionBuilder.AppendLine("```");
        _ = sectionBuilder.AppendLine();
        return sectionBuilder.ToString();
    }

    private void AppendProperties(StringBuilder sectionBuilder, IReadOnlyList<MemberDocumentationModel> properties)
    {
        foreach (var propertyModel in properties)
        {
            var visibility = GetPlantUmlVisibilitySymbol(propertyModel.VisibilityModifier);
            var propertyType = formatter.ExtractDisplayTypeLabel(
                !string.IsNullOrWhiteSpace(propertyModel.ReturnTypeName)
                    ? propertyModel.ReturnTypeName
                    : propertyModel.ResolvedReturnTypeDocumentationLink);
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    {visibility} {propertyModel.MemberName} : {(string.IsNullOrEmpty(propertyType) ? "Object" : propertyType)}");
        }
    }

    private void AppendMethods(StringBuilder sectionBuilder, IReadOnlyList<MemberDocumentationModel> methods)
    {
        foreach (var methodModel in methods)
        {
            var visibility = GetPlantUmlVisibilitySymbol(methodModel.VisibilityModifier);
            var parameterTypes = string.Join(
                ", ",
                methodModel.MemberParameters.Select(parameter => formatter.ExtractDisplayTypeLabel(parameter.ParameterTypeName)));
            var returnType = formatter.ExtractDisplayTypeLabel(
                !string.IsNullOrWhiteSpace(methodModel.ReturnTypeName)
                    ? methodModel.ReturnTypeName
                    : methodModel.ResolvedReturnTypeDocumentationLink);
            var returnSuffix = string.IsNullOrEmpty(returnType) ? "void" : returnType;
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    {visibility} {methodModel.MemberName}({parameterTypes}) : {returnSuffix}");
        }
    }

    private static void AppendEvents(StringBuilder sectionBuilder, IReadOnlyList<MemberDocumentationModel> events)
    {
        foreach (var eventModel in events)
        {
            var visibility = GetPlantUmlVisibilitySymbol(eventModel.VisibilityModifier);
            _ = sectionBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    {visibility} <<event>> {eventModel.MemberName}");
        }
    }

    private static string GetPlantUmlTypeDeclarationHeader(TypeDocumentationModel typeModel, string currentIdentifier) =>
        typeModel switch
        {
            { TypeCategory: TypeCategory.Interface } => $"interface {currentIdentifier}",
            { IsStaticType: true } => $"class {currentIdentifier} <<static>>",
            { IsAbstractType: true } => $"abstract class {currentIdentifier}",
            { TypeCategory: TypeCategory.Struct } => $"class {currentIdentifier} <<struct>>",
            { IsSealedType: true } => $"class {currentIdentifier} <<sealed>>",
            _ => $"class {currentIdentifier}"
        };

    private static string GetPlantUmlVisibilitySymbol(string? declarationSignature) =>
        !string.IsNullOrWhiteSpace(declarationSignature) &&
        declarationSignature.TrimStart().StartsWith("protected", StringComparison.Ordinal)
            ? "#"
            : "+";
}
