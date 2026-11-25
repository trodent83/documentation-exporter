using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Plugins.Navigation.MkDocs;
using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

[IntegrationTest]
public sealed class EmitMarkdownDocumentationStepTests
{
    private sealed class UnsupportedMermaidArchitectureGenerator : IArchitectureDiagramGenerator
    {
        public DiagramFormat Format => DiagramFormat.Mermaid;

        public string? StandaloneFileName => null;

        public bool CanGenerate(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) => false;

        public string GenerateEmbeddedMarkdown(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            string.Empty;

        public string GenerateStandaloneContent(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            string.Empty;
    }

    private sealed class CustomDocusaurusNavigationGenerator : INavigationGenerator
    {
        public NavigationFormat Format => NavigationFormat.Custom("Docusaurus");

        public string DefaultFileName => "sidebars.js";

        public string GenerateNavigation(
            IReadOnlyList<TypeDocumentationModel> types,
            bool useAssemblySubdirectories = false,
            string? existingNavigationContent = null,
            string? navigationEntryPoint = null,
            string? navigationUrlPrefix = null) =>
            $"// Generated Docusaurus sidebar for {types.Count} types";
    }

    private static EmitMarkdownDocumentationStep CreateStep(IEnumerable<INavigationGenerator>? customNavigationGenerators = null)
    {
        var linkRewriter = new RelativeLinkRewriter();
        var fileNameFormatter = new MemberFileNameFormatter();
        var exceptionFormatter = new ExceptionTableFormatter();
        var referenceRegistry = new MicrosoftLearnReferenceRegistry();
        var identifierFormatter = new DiagramIdentifierFormatter();
        DiagramGeneratorSelector diagramSelector = new();
        IArchitectureDiagramGenerator[] diagramGenerators =
        [
            new MermaidArchitectureDiagramGenerator(identifierFormatter),
            new PlantUmlArchitectureDiagramGenerator(identifierFormatter)
        ];
        IAssemblyDependencyDiagramGenerator[] dependencyGenerators =
        [
            new MermaidAssemblyDependencyDiagramGenerator(identifierFormatter),
            new PlantUmlAssemblyDependencyDiagramGenerator(identifierFormatter)
        ];
        INavigationGenerator[] navigationGenerators = customNavigationGenerators != null
            ? [.. customNavigationGenerators]
            : [new MkDocsNavigationGenerator()];

        return new EmitMarkdownDocumentationStep(
            new TypePageMarkdownGenerator([], linkRewriter),
            new MemberDetailMarkdownGenerator(fileNameFormatter, linkRewriter, exceptionFormatter, referenceRegistry),
            new IndexPageMarkdownGenerator(diagramGenerators, dependencyGenerators, diagramSelector, linkRewriter, referenceRegistry),
            diagramGenerators,
            diagramSelector,
            navigationGenerators,
            new Documentation.Exporter.Core.IO.PhysicalFileSystem());
    }

    [Fact]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var step = CreateStep();

        // Assert
        _ = step.StepName.Should().Be("Emit Markdown Documentation");
    }

    [Fact]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var linkRewriter = new RelativeLinkRewriter();
        var typeGenerator = new TypePageMarkdownGenerator([], linkRewriter);
        var memberGenerator = new MemberDetailMarkdownGenerator(
            new MemberFileNameFormatter(),
            linkRewriter,
            new ExceptionTableFormatter(),
            new MicrosoftLearnReferenceRegistry());
        var identifierFormatter = new DiagramIdentifierFormatter();
        DiagramGeneratorSelector diagramSelector = new();
        IArchitectureDiagramGenerator[] diagramGenerators = [new PlantUmlArchitectureDiagramGenerator(identifierFormatter)];
        IAssemblyDependencyDiagramGenerator[] dependencyGenerators = [new PlantUmlAssemblyDependencyDiagramGenerator(identifierFormatter)];
        var indexGenerator = new IndexPageMarkdownGenerator(diagramGenerators, dependencyGenerators, diagramSelector, linkRewriter, new MicrosoftLearnReferenceRegistry());
        INavigationGenerator[] navigationGenerators = [new MkDocsNavigationGenerator()];
        var fileSystem = new Documentation.Exporter.Core.IO.PhysicalFileSystem();
        var step = new EmitMarkdownDocumentationStep(typeGenerator, memberGenerator, indexGenerator, diagramGenerators, diagramSelector, navigationGenerators, fileSystem);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(null!, memberGenerator, indexGenerator, diagramGenerators, diagramSelector, navigationGenerators, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(typeGenerator, null!, indexGenerator, diagramGenerators, diagramSelector, navigationGenerators, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(typeGenerator, memberGenerator, null!, diagramGenerators, diagramSelector, navigationGenerators, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(typeGenerator, memberGenerator, indexGenerator, null!, diagramSelector, navigationGenerators, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(typeGenerator, memberGenerator, indexGenerator, diagramGenerators, null!, navigationGenerators, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(typeGenerator, memberGenerator, indexGenerator, diagramGenerators, diagramSelector, null!, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new EmitMarkdownDocumentationStep(typeGenerator, memberGenerator, indexGenerator, diagramGenerators, diagramSelector, navigationGenerators, null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsyncWithCompiledModelsShouldGenerateTypeSubdirectoryAndMemberMarkdownFiles()
    {
        // Arrange
        var step = CreateStep();
        var tempDir = Path.Combine(Path.GetTempPath(), "EmitStepTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var options = new ExporterOptions
        {
            OutputDirectoryPath = tempDir,
            DiagramFormat = DiagramFormat.PlantUml
        };
        var context = new ExportWorkflowContext(options);
        context.CompiledTypeModels.Add(new TypeDocumentationModel
        {
            TypeName = "SampleService",
            NamespaceName = "SampleNamespace",
            TypeCategory = TypeCategory.Class,
            Methods = [new MemberDocumentationModel { MemberName = "Execute", SummaryDescription = "Executes action." }]
        });

        try
        {
            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue("because emit step should write type, member, index, and PlantUML files.");
            _ = File.Exists(Path.Combine(tempDir, "SampleService", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(tempDir, "SampleService", "Execute.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(tempDir, "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(tempDir, "architecture.puml")).Should().BeTrue("because standalone architecture.puml must be emitted for PlantUml format.");
            var indexContent = await File.ReadAllTextAsync(Path.Combine(tempDir, "index.md"));
            _ = indexContent.Should().Contain("## Architecture Diagram")
                .And.Contain("```plantuml", "because PlantUml was selected as the preferred format.")
                .And.NotContain("```mermaid", "because only one diagram format should be emitted on the index page.");
            _ = context.GeneratedFilesCount.Should().Be(3);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsyncWithSubnamespaceModelsShouldCreateNestedTypeSubdirectories()
    {
        // Arrange
        var step = CreateStep();
        var tempDir = Path.Combine(Path.GetTempPath(), "EmitStepSubDirTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDir);

        var options = new ExporterOptions { OutputDirectoryPath = tempDir };
        var context = new ExportWorkflowContext(options);
        context.CompiledTypeModels.Add(new TypeDocumentationModel
        {
            TypeName = "OrderProcessingService",
            NamespaceName = "DemoLibrary.Services.Orders",
            AssemblyName = "DemoLibrary",
            TypeCategory = TypeCategory.Class,
            Properties = [new MemberDocumentationModel { MemberName = "GatewayName" }]
        });

        try
        {
            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue();
            var expectedIndex = Path.Combine(tempDir, "Services", "Orders", "OrderProcessingService", "index.md");
            var expectedMember = Path.Combine(tempDir, "Services", "Orders", "OrderProcessingService", "GatewayName.md");
            _ = File.Exists(expectedIndex).Should().BeTrue("because each type must have an index.md inside its dedicated subdirectory.");
            _ = File.Exists(expectedMember).Should().BeTrue("because each member must have a dedicated .md file inside the type's subdirectory.");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsyncWithMermaidAndFallbackShouldRenderSingleArchitectureDiagramAndStandaloneFilesWhenFallingBack()
    {
        // Arrange
        var linkRewriter = new RelativeLinkRewriter();
        var fileNameFormatter = new MemberFileNameFormatter();
        var exceptionFormatter = new ExceptionTableFormatter();
        var referenceRegistry = new MicrosoftLearnReferenceRegistry();
        var identifierFormatter = new DiagramIdentifierFormatter();
        DiagramGeneratorSelector diagramSelector = new();
        var physicalFileSystem = new Documentation.Exporter.Core.IO.PhysicalFileSystem();

        var mermaidStep = CreateStep();
        IArchitectureDiagramGenerator[] fallbackGenerators =
        [
            new UnsupportedMermaidArchitectureGenerator(),
            new PlantUmlArchitectureDiagramGenerator(identifierFormatter)
        ];
        IAssemblyDependencyDiagramGenerator[] fallbackDependencyGenerators =
        [
            new PlantUmlAssemblyDependencyDiagramGenerator(identifierFormatter)
        ];
        EmitMarkdownDocumentationStep fallbackStep = new(
            new TypePageMarkdownGenerator([], linkRewriter),
            new MemberDetailMarkdownGenerator(fileNameFormatter, linkRewriter, exceptionFormatter, referenceRegistry),
            new IndexPageMarkdownGenerator(fallbackGenerators, fallbackDependencyGenerators, diagramSelector, linkRewriter, referenceRegistry),
            fallbackGenerators,
            diagramSelector,
            [new MkDocsNavigationGenerator()],
            physicalFileSystem);

        var mermaidTempDir = Path.Combine(Path.GetTempPath(), "EmitStepMermaidTest_" + Guid.NewGuid().ToString("N"));
        var fallbackTempDir = Path.Combine(Path.GetTempPath(), "EmitStepFallbackTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(mermaidTempDir);
        _ = Directory.CreateDirectory(fallbackTempDir);

        TypeDocumentationModel sampleModel = new()
        {
            TypeName = "SampleService",
            NamespaceName = "SampleNamespace",
            AssemblyName = "SampleAssembly",
            TypeCategory = TypeCategory.Class
        };

        Dictionary<string, IReadOnlyList<AssemblyDependencyEdgeModel>> sampleEdgesMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["SampleAssembly"] =
                [
                    new()
                    {
                        SourceAssemblyName = "SampleAssembly",
                        TargetAssemblyName = "SampleContracts",
                        ReferenceKind = AssemblyReferenceKind.Project,
                        VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked,
                        IsTargetStrongNamed = false
                    }
                ]
            };

        try
        {
            ExportWorkflowContext mermaidContext = new(new ExporterOptions
            {
                OutputDirectoryPath = mermaidTempDir,
                DiagramFormat = DiagramFormat.Mermaid
            })
            {
                AssemblyAttributesByAssemblyName = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SampleAssembly"] = ["[AssemblyMarkerAttribute](Attributes/AssemblyMarkerAttribute/index.md)"]
                }
            };
            mermaidContext.CompiledTypeModels.Add(sampleModel);
            mermaidContext.AssemblyDependencyEdgesByAssemblyName = sampleEdgesMap;

            ExportWorkflowContext fallbackContext = new(new ExporterOptions
            {
                OutputDirectoryPath = fallbackTempDir,
                DiagramFormat = DiagramFormat.Mermaid
            });
            fallbackContext.CompiledTypeModels.Add(sampleModel);
            fallbackContext.AssemblyDependencyEdgesByAssemblyName = sampleEdgesMap;

            // Act
            var mermaidSucceeded = await mermaidStep.ExecuteAsync(mermaidContext);
            var fallbackSucceeded = await fallbackStep.ExecuteAsync(fallbackContext);

            // Assert
            _ = mermaidSucceeded.Should().BeTrue();
            var mermaidIndexContent = await File.ReadAllTextAsync(Path.Combine(mermaidTempDir, "index.md"));
            _ = mermaidIndexContent.Should().Contain("## Architecture Diagram")
                .And.Contain("**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [AssemblyMarkerAttribute](Attributes/AssemblyMarkerAttribute/index.md)",
                    "because single-assembly root index emits assembly attributes from ExportWorkflowContext.")
                .And.Contain("## Assembly Dependency Tree", "because single-assembly root index emits the assembly dependency tree.")
                .And.Contain("flowchart LR")
                .And.Contain("```mermaid", "because default Mermaid preference should emit a Mermaid architecture diagram on index.md.")
                .And.NotContain("```plantuml", "because only the preferred Mermaid diagram should be emitted.");
            _ = File.Exists(Path.Combine(mermaidTempDir, "architecture.puml")).Should().BeFalse(
                "because standalone architecture.puml must not be written when Mermaid is selected.");

            _ = fallbackSucceeded.Should().BeTrue();
            var fallbackIndexContent = await File.ReadAllTextAsync(Path.Combine(fallbackTempDir, "index.md"));
            _ = fallbackIndexContent.Should().Contain("## Architecture Diagram")
                .And.Contain("## Assembly Dependency Tree")
                .And.Contain("```plantuml", "because unsupported Mermaid falls back to the supporting PlantUML generator.")
                .And.NotContain("```mermaid");
            _ = File.Exists(Path.Combine(fallbackTempDir, "architecture.puml")).Should().BeTrue(
                "because falling back to PlantUML should also emit standalone architecture.puml.");
        }
        finally
        {
            if (Directory.Exists(mermaidTempDir))
            {
                Directory.Delete(mermaidTempDir, recursive: true);
            }

            if (Directory.Exists(fallbackTempDir))
            {
                Directory.Delete(fallbackTempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsyncWithNavigationFormatNoneCustomOrMissingGeneratorShouldRespectSelectedStrategy()
    {
        // Arrange
        var customTempDir = Path.Combine(Path.GetTempPath(), $"EmitStepCustomNav_{Guid.NewGuid():N}");
        var noneTempDir = Path.Combine(Path.GetTempPath(), $"EmitStepNoneNav_{Guid.NewGuid():N}");
        var missingTempDir = Path.Combine(Path.GetTempPath(), $"EmitStepMissingNav_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(customTempDir);
        _ = Directory.CreateDirectory(noneTempDir);
        _ = Directory.CreateDirectory(missingTempDir);

        TypeDocumentationModel sampleModel = new()
        {
            TypeName = "SampleService",
            NamespaceName = "SampleNamespace",
            TypeCategory = TypeCategory.Class
        };

        var customStep = CreateStep([new MkDocsNavigationGenerator(), new CustomDocusaurusNavigationGenerator()]);

        try
        {
            ExportWorkflowContext customContext = new(new ExporterOptions
            {
                OutputDirectoryPath = customTempDir,
                NavigationFormat = NavigationFormat.Custom("Docusaurus")
            });
            customContext.CompiledTypeModels.Add(sampleModel);

            ExportWorkflowContext noneContext = new(new ExporterOptions
            {
                OutputDirectoryPath = noneTempDir,
                NavigationFormat = NavigationFormat.None
            });
            noneContext.CompiledTypeModels.Add(sampleModel);

            ExportWorkflowContext missingContext = new(new ExporterOptions
            {
                OutputDirectoryPath = missingTempDir,
                NavigationFormat = NavigationFormat.Custom("UnregisteredFormat")
            });
            missingContext.CompiledTypeModels.Add(sampleModel);

            // Act
            var customResult = await customStep.ExecuteAsync(customContext);
            var noneResult = await customStep.ExecuteAsync(noneContext);
            var missingResult = await customStep.ExecuteAsync(missingContext);

            // Assert
            _ = customResult.Should().BeTrue();
            var sidebarsPath = Path.Combine(customTempDir, "sidebars.js");
            _ = File.Exists(sidebarsPath).Should().BeTrue("because custom navigation generator supplies its own DefaultFileName.");
            _ = (await File.ReadAllTextAsync(sidebarsPath)).Should().Contain("Generated Docusaurus sidebar for 1 types");
            _ = File.Exists(Path.Combine(customTempDir, "mkdocs.nav.yml")).Should().BeFalse(
                "because MkDocs generator must not run when Docusaurus format is selected.");

            _ = noneResult.Should().BeTrue();
            _ = File.Exists(Path.Combine(noneTempDir, "mkdocs.nav.yml")).Should().BeFalse(
                "because NavigationFormat.None disables navigation generation.");

            _ = missingResult.Should().BeTrue("because missing navigation generator logs a warning and continues without throwing.");
            _ = File.Exists(Path.Combine(missingTempDir, "mkdocs.nav.yml")).Should().BeFalse();
        }
        finally
        {
            foreach (var directoryPath in new[] { customTempDir, noneTempDir, missingTempDir })
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
        }
    }
}
