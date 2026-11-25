using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Core.Configuration;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

public sealed class DerivedAndMermaidSectionRenderersTests
{
    [Fact]
    [UnitTest]
    public void DerivedTypesSectionRendererShouldRenderDerivedTypesOrKnownImplementations()
    {
        // Arrange
        var renderer = new DerivedTypesSectionRenderer();
        _ = renderer.Order.Should().Be(42);
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));

        var classModel = new TypeDocumentationModel
        {
            TypeName = "BaseProcessor",
            TypeCategory = TypeCategory.Class,
            DerivedTypes = ["[OrderProcessor](OrderProcessor/index.md)", "PlainDerived"]
        };
        var interfaceModel = new TypeDocumentationModel
        {
            TypeName = "IProcessor",
            TypeCategory = TypeCategory.Interface,
            DerivedTypes = ["[OrderProcessor](OrderProcessor/index.md)"]
        };
        var emptyModel = new TypeDocumentationModel { TypeName = "Standalone" };

        // Act & Assert
        _ = renderer.RenderSection(emptyModel).Should().BeEmpty();
        _ = renderer.RenderSection(classModel).Should().Contain("## Derived Types")
            .And.Contain("* [OrderProcessor](OrderProcessor/index.md)")
            .And.Contain("* `PlainDerived`");
        _ = renderer.RenderSection(interfaceModel).Should().Contain("## Known Implementations")
            .And.Contain("* [OrderProcessor](OrderProcessor/index.md)");
    }

    [Fact]
    [UnitTest]
    public void TypeDiagramSectionRendererShouldRenderMermaidClassDiagramForHierarchyAndImplementations()
    {
        // Arrange
        var identifierFormatter = new DiagramIdentifierFormatter();
        var renderer = new TypeDiagramSectionRenderer(
            [new MermaidTypeDiagramGenerator(identifierFormatter), new PlantUmlTypeDiagramGenerator(identifierFormatter)],
            new DiagramGeneratorSelector());
        _ = Assert.Throws<ArgumentNullException>(() => new TypeDiagramSectionRenderer(null!, new DiagramGeneratorSelector()));
        _ = renderer.Order.Should().Be(45);
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));

        var standaloneModel = new TypeDocumentationModel { TypeName = "Standalone" };
        var enumModel = new TypeDocumentationModel { TypeName = "Mode", TypeCategory = TypeCategory.Enum };
        var interfaceModel = new TypeDocumentationModel
        {
            TypeName = "IOrderProcessor",
            TypeCategory = TypeCategory.Interface,
            DerivedTypes = ["[OrderProcessingService](OrderProcessingService/index.md)"]
        };
        var classModel = new TypeDocumentationModel
        {
            TypeName = "AuditableProcessorBase",
            TypeCategory = TypeCategory.Class,
            InheritanceHierarchy = ["System.Object", "[TransactionProcessorBase](TransactionProcessorBase/index.md)"],
            ImplementedInterfaces = ["[IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)"],
            DerivedTypes = ["[OrderProcessingService<T>](OrderProcessingService/index.md)", "App.Services.CustomProcessor"]
        };

        // Act & Assert
        _ = renderer.RenderSection(standaloneModel).Should().BeEmpty();
        _ = renderer.RenderSection(enumModel).Should().BeEmpty();

        var interfaceDiagram = renderer.RenderSection(interfaceModel);
        _ = interfaceDiagram.Should().Contain("## Type Diagram")
            .And.Contain("```mermaid")
            .And.Contain("classDiagram")
            .And.Contain("<<interface>>")
            .And.Contain("IOrderProcessor <|.. OrderProcessingService");

        var classDiagram = renderer.RenderSection(classModel);
        _ = classDiagram.Should().Contain("TransactionProcessorBase <|-- AuditableProcessorBase")
            .And.Contain("IDisposable <|.. AuditableProcessorBase")
            .And.Contain("AuditableProcessorBase <|-- OrderProcessingService")
            .And.Contain("AuditableProcessorBase <|-- CustomProcessor");

        var disabledDiagram = renderer.RenderSection(classModel with { DiagramFormat = DiagramFormat.None });
        _ = disabledDiagram.Should().BeEmpty("because DiagramFormat.None disables type diagram rendering.");

        var plantUmlRichModel = classModel with
        {
            IsAbstractType = true,
            DiagramFormat = DiagramFormat.PlantUml,
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "ServiceName",
                    VisibilityModifier = "public",
                    ReturnTypeName = "String"
                }
            ],
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "RecordAudit",
                    VisibilityModifier = "protected",
                    ReturnTypeName = "void",
                    MemberParameters =
                    [
                        new ParameterDocumentationModel
                        {
                            ParameterName = "entry",
                            ParameterTypeName = "String"
                        }
                    ]
                }
            ],
            Events =
            [
                new MemberDocumentationModel
                {
                    MemberName = "AuditRecorded",
                    VisibilityModifier = "public"
                }
            ]
        };

        var plantUmlRichDiagram = renderer.RenderSection(plantUmlRichModel);
        _ = plantUmlRichDiagram.Should().Contain("```plantuml", "because PlantUml was selected as the preferred format.")
            .And.NotContain("```mermaid", "because only one diagram format should be emitted.")
            .And.Contain("abstract class AuditableProcessorBase {")
            .And.Contain("+ ServiceName : String")
            .And.Contain("# RecordAudit(String) : void")
            .And.Contain("+ <<event>> AuditRecorded")
            .And.Contain("@enduml");

        var sealedStructDiagram = renderer.RenderSection(new TypeDocumentationModel
        {
            TypeName = "MoneyValue",
            TypeCategory = TypeCategory.Struct,
            DiagramFormat = DiagramFormat.PlantUml,
            ImplementedInterfaces = ["IEquatable"]
        });
        _ = sealedStructDiagram.Should().Contain("class MoneyValue <<struct>> {");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExporterConfigurationLoaderShouldValidateAndLoadJsonConfigFiles()
    {
        // Arrange
        var configurationLoader = new ExporterConfigurationLoader(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => configurationLoader.LoadAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentException>(() => configurationLoader.LoadAsync("   "));
        _ = await Assert.ThrowsAsync<FileNotFoundException>(() => configurationLoader.LoadAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json")));

        var tempDir = Path.Combine(Path.GetTempPath(), $"DocExporter_Cfg_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(tempDir);
        try
        {
            var configPath = Path.Combine(tempDir, "doc-exporter.json");
            await File.WriteAllTextAsync(
                configPath,
                """
                {
                  "assemblyFilePaths": ["bin/MyLib.dll"],
                  "xmlDocumentationFilePaths": ["bin/MyLib.xml"],
                  "outputDirectoryPath": "docs_out",
                  "reportFilePath": "reports/quality.md",
                  "reportFormat": "Markdown"
                }
                """);

            var loadedOptions = await configurationLoader.LoadAsync(configPath);
            _ = loadedOptions.AssemblyFilePaths.Should().ContainSingle().Which.Should().EndWith("MyLib.dll");
            _ = loadedOptions.XmlDocumentationFilePaths.Should().ContainSingle().Which.Should().EndWith("MyLib.xml");
            _ = loadedOptions.OutputDirectoryPath.Should().EndWith("docs_out");
            _ = loadedOptions.ReportFilePath.Should().Be(Path.GetFullPath(Path.Combine(tempDir, "reports/quality.md")));
            _ = loadedOptions.ReportFormat.Should().Be(QualityReportFormat.Markdown);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
