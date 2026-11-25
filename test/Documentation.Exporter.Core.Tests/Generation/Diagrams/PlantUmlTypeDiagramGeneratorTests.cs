using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Diagrams;

[UnitTest]
public sealed class PlantUmlTypeDiagramGeneratorTests
{
    [Fact]
    public void ConstructorAndMethodWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var generator = new PlantUmlTypeDiagramGenerator(new DiagramIdentifierFormatter());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new PlantUmlTypeDiagramGenerator(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.CanGenerate(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateTypeDiagram(null!));
        _ = generator.Format.Should().Be(DiagramFormat.PlantUml);
    }

    [Fact]
    public void GenerateTypeDiagramShouldRenderCompartmentsStereotypesAndRelationships()
    {
        // Arrange
        var generator = new PlantUmlTypeDiagramGenerator(new DiagramIdentifierFormatter());
        var enumModel = new TypeDocumentationModel { TypeName = "Mode", TypeCategory = TypeCategory.Enum };
        var model = new TypeDocumentationModel
        {
            TypeName = "AuditableProcessorBase",
            TypeCategory = TypeCategory.Class,
            IsAbstractType = true,
            InheritanceHierarchy = ["TransactionProcessorBase"],
            ImplementedInterfaces = ["IDisposable"],
            Properties = [new MemberDocumentationModel { MemberName = "ServiceName", VisibilityModifier = "public", ReturnTypeName = "String" }],
            Methods = [new MemberDocumentationModel { MemberName = "RecordAudit", VisibilityModifier = "protected", ReturnTypeName = "void", MemberParameters = [new ParameterDocumentationModel { ParameterName = "entry", ParameterTypeName = "String" }] }],
            Events = [new MemberDocumentationModel { MemberName = "AuditRecorded", VisibilityModifier = "public" }]
        };

        // Act
        var diagram = generator.GenerateTypeDiagram(model);

        // Assert
        _ = generator.CanGenerate(enumModel).Should().BeFalse("because PlantUML type diagram skips enums.");
        _ = generator.CanGenerate(model).Should().BeTrue("because model has inheritance and interface relationships.");
        _ = diagram.Should().Contain("```plantuml")
            .And.Contain("@startuml")
            .And.Contain("abstract class AuditableProcessorBase {")
            .And.Contain("+ ServiceName : String")
            .And.Contain("# RecordAudit(String) : void")
            .And.Contain("+ <<event>> AuditRecorded")
            .And.Contain("@enduml");
    }
}
