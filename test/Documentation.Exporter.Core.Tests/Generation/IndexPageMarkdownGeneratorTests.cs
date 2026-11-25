using Documentation.Exporter.Plugins.Diagrams;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Diagrams;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class IndexPageMarkdownGeneratorTests
{
    private sealed class StubArchitectureDiagramGenerator(
        DiagramFormat format,
        bool isSupported,
        string embeddedMarkdown = "",
        string standaloneContent = "",
        string? standaloneFileName = null) : IArchitectureDiagramGenerator
    {
        public DiagramFormat Format => format;

        public string? StandaloneFileName => standaloneFileName;

        public bool CanGenerate(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) => isSupported;

        public string GenerateEmbeddedMarkdown(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            embeddedMarkdown;

        public string GenerateStandaloneContent(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            standaloneContent;
    }

    private sealed class StubAssemblyDependencyDiagramGenerator(
        DiagramFormat format,
        bool isSupported,
        string renderedMarkdown = "") : IAssemblyDependencyDiagramGenerator
    {
        public DiagramFormat Format => format;

        public bool CanGenerate(string rootAssemblyName, IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges) =>
            isSupported;

        public string GenerateDependencyTreeMarkdown(string rootAssemblyName, IReadOnlyList<AssemblyDependencyEdgeModel> dependencyEdges) =>
            renderedMarkdown;
    }

    private static IndexPageMarkdownGenerator CreateGenerator()
    {
        var identifierFormatter = new DiagramIdentifierFormatter();
        return new IndexPageMarkdownGenerator(
            [
                new MermaidArchitectureDiagramGenerator(identifierFormatter),
                new PlantUmlArchitectureDiagramGenerator(identifierFormatter)
            ],
            [
                new MermaidAssemblyDependencyDiagramGenerator(identifierFormatter),
                new PlantUmlAssemblyDependencyDiagramGenerator(identifierFormatter)
            ],
            new DiagramGeneratorSelector(),
            new RelativeLinkRewriter(),
            new MicrosoftLearnReferenceRegistry());
    }

    [Fact]
    public void ConstructorAndMethodWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        DiagramGeneratorSelector diagramSelector = new();
        var linkRewriter = new RelativeLinkRewriter();
        var referenceRegistry = new MicrosoftLearnReferenceRegistry();
        var generator = CreateGenerator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new IndexPageMarkdownGenerator(null!, [], diagramSelector, linkRewriter, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new IndexPageMarkdownGenerator([], null!, diagramSelector, linkRewriter, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new IndexPageMarkdownGenerator([], [], null!, linkRewriter, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new IndexPageMarkdownGenerator([], [], diagramSelector, null!, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new IndexPageMarkdownGenerator([], [], diagramSelector, linkRewriter, null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateIndexMarkdown(null!));
    }

    [Fact]
    public void GenerateIndexMarkdownShouldGroupTypesByNamespaceAndAdjustCrossAssemblySummaryLinks()
    {
        // Arrange
        var generator = CreateGenerator();
        TypeDocumentationModel[] models =
        [
            new()
            {
                TypeName = "OrderService",
                NamespaceName = "App.Services",
                AssemblyName = "App",
                SummaryDescription = "Processes orders with [AuditSink](App.Contracts/Auditing/AuditSink/index.md)."
            },
            new()
            {
                TypeName = "InvoiceService",
                NamespaceName = "App.Services",
                AssemblyName = "App",
                IsObsolete = true,
                ObsoleteMessage = "Deprecated invoice service.",
                SummaryDescription = new string('A', 140)
            },
            new()
            {
                TypeName = "Customer",
                NamespaceName = "App.Models",
                AssemblyName = "App",
                IsObsolete = true,
                SummaryDescription = "Customer entity."
            }
        ];

        // Act
        var indexMarkdown = generator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: false,
            diagramFormat: DiagramFormat.Mermaid,
            isAssemblySubdirectoryIndex: true);

        // Assert
        _ = indexMarkdown.Should().Contain("# API Reference")
            .And.Contain("## Namespace: `App.Models`")
            .And.Contain("## Namespace: `App.Services`")
            .And.Contain("[OrderService](Services/OrderService/index.md)")
            .And.Contain("[AuditSink](../App.Contracts/Auditing/AuditSink/index.md)",
                "because assembly subdirectory index summaries must rewrite cross-assembly links upward.")
            .And.Contain("Deprecated invoice service.")
            .And.Contain("[Customer](Models/Customer/index.md)")
            .And.Contain("## Architecture Diagram")
            .And.Contain("```mermaid", "because Mermaid was selected as the preferred diagram generator.")
            .And.NotContain("```plantuml", "because only one architecture diagram should be emitted per index page.");
    }

    [Fact]
    public void GenerateIndexMarkdownWhenIncludeAssemblyDirectoryTrueShouldListAssemblies()
    {
        // Arrange
        var generator = CreateGenerator();
        TypeDocumentationModel[] models =
        [
            new() { TypeName = "TypeOne", AssemblyName = "Alpha.Assembly", NamespaceName = "Alpha" },
            new() { TypeName = "TypeTwo", AssemblyName = "Beta.Assembly", NamespaceName = "Beta" }
        ];

        // Act
        var markdown = generator.GenerateIndexMarkdown(models, includeAssemblyDirectory: true, diagramFormat: DiagramFormat.None);

        // Assert
        _ = markdown.Should().Contain("## Assembly")
            .And.Contain("* [Alpha.Assembly](Alpha.Assembly/index.md)")
            .And.Contain("* [Beta.Assembly](Beta.Assembly/index.md)")
            .And.NotContain("## Architecture Diagram");
    }

    [Fact]
    public void GenerateIndexMarkdownWithAssemblyReferencesShouldRenderTableLinkAnalyzedSiblingsAndDisplayPublicKeyTokens()
    {
        // Arrange
        var generator = CreateGenerator();
        TypeDocumentationModel[] models =
        [
            new() { TypeName = "OrderService", AssemblyName = "App.Services", NamespaceName = "App.Services" }
        ];
        AssemblyReferenceModel[] references =
        [
            new()
            {
                ReferenceName = "App.Services",
                ReferenceKind = AssemblyReferenceKind.Project,
                AssemblyVersion = "1.0.0.0",
                VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked,
                IsStrongNamed = false
            },
            new()
            {
                ReferenceName = "App.Contracts",
                ReferenceKind = AssemblyReferenceKind.Project,
                AssemblyVersion = "1.0.0.0",
                VersionLinkingKind = AssemblyVersionLinkingKind.ProjectLinked,
                IsStrongNamed = false
            },
            new()
            {
                ReferenceName = "ExcludedLib",
                ReferenceKind = AssemblyReferenceKind.Project,
                PackageVersion = "2.1.0",
                AssemblyVersion = "2.0.0.0",
                VersionLinkingKind = AssemblyVersionLinkingKind.Range,
                VersionConstraintExpression = "[2.0.0, 3.0.0)",
                IsStrongNamed = true,
                PublicKeyToken = "1234567890abcdef"
            },
            new()
            {
                ReferenceName = "Newtonsoft.Json",
                ReferenceKind = AssemblyReferenceKind.Package,
                PackageVersion = "13.0.3",
                AssemblyVersion = "13.0.3",
                VersionLinkingKind = AssemblyVersionLinkingKind.Minimum,
                VersionConstraintExpression = ">= 13.0.3",
                IsStrongNamed = true,
                PublicKeyToken = string.Empty
            },
            new()
            {
                ReferenceName = "FixedPkg",
                ReferenceKind = AssemblyReferenceKind.Package,
                PackageVersion = "1.2.0",
                VersionLinkingKind = AssemblyVersionLinkingKind.Fixed,
                VersionConstraintExpression = "[1.2.0]"
            },
            new()
            {
                ReferenceName = "FloatPkg",
                ReferenceKind = AssemblyReferenceKind.Package,
                PackageVersion = "4.*",
                VersionLinkingKind = AssemblyVersionLinkingKind.Floating,
                VersionConstraintExpression = "4.*"
            },
            new()
            {
                ReferenceName = "CustomBinary",
                ReferenceKind = AssemblyReferenceKind.Assembly,
                VersionLinkingKind = AssemblyVersionLinkingKind.Compiled
            },
            new()
            {
                ReferenceName = "System.Runtime",
                ReferenceKind = AssemblyReferenceKind.Framework,
                AssemblyVersion = "11.0.0.0",
                VersionLinkingKind = AssemblyVersionLinkingKind.Framework,
                VersionConstraintExpression = "net11.0",
                IsStrongNamed = true,
                PublicKeyToken = "b03f5f7f11d50a3a"
            }
        ];
        HashSet<string> analyzedAssemblies = new(["App.Services", "App.Contracts"], StringComparer.OrdinalIgnoreCase);

        // Act
        var subdirectoryMarkdown = generator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: false,
            diagramFormat: DiagramFormat.None,
            isAssemblySubdirectoryIndex: true,
            assemblyReferences: references,
            analyzedAssemblyNames: analyzedAssemblies);

        var singleAssemblyMarkdown = generator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: false,
            diagramFormat: DiagramFormat.None,
            isAssemblySubdirectoryIndex: false,
            assemblyReferences: references,
            analyzedAssemblyNames: analyzedAssemblies);

        // Assert
        _ = subdirectoryMarkdown.Should().Contain("## Assembly References")
            .And.Contain("| Reference | Kind | Version | Version Linking | Strong-Named |")
            .And.Contain("| `App.Services` | Project | `1.0.0.0` | Linked (Project) | No |",
                "because an assembly index should never emit a self-link.")
            .And.Contain("| [App.Contracts](../App.Contracts/index.md) | Project | `1.0.0.0` | Linked (Project) | No |",
                "because analyzed sibling assemblies in the current export must link to their index.md.")
            .And.Contain("| `ExcludedLib` | Project | `2.1.0` (`2.0.0.0`) | Range (`[2.0.0, 3.0.0)`) | Yes (`1234567890abcdef`) |",
                "because unanalyzed projects must stay plain code spans instead of broken links.")
            .And.Contain("| `Newtonsoft.Json` | Package | `13.0.3` | Minimum (`>= 13.0.3`) | Yes |")
            .And.Contain("| `FixedPkg` | Package | `1.2.0` | Fixed (`[1.2.0]`) | No |")
            .And.Contain("| `FloatPkg` | Package | `4.*` | Floating (`4.*`) | No |")
            .And.Contain("| `CustomBinary` | Assembly | `-` | Compiled | No |")
            .And.Contain("| `System.Runtime` | Framework | `11.0.0.0` | Framework (`net11.0`) | Yes (`b03f5f7f11d50a3a`) |");

        _ = singleAssemblyMarkdown.Should().NotContain("../App.Contracts/index.md",
            "because single-assembly root exports do not generate per-assembly subdirectories.");
    }

    [Fact]
    public void GenerateIndexMarkdownWithPlantUmlPreferredOrFallbackShouldRenderOnlySelectedDiagram()
    {
        // Arrange
        var identifierFormatter = new DiagramIdentifierFormatter();
        var defaultGenerator = CreateGenerator();
        TypeDocumentationModel[] models =
        [
            new() { TypeName = "OrderService", AssemblyName = "App.Services", NamespaceName = "App.Services" }
        ];

        IndexPageMarkdownGenerator fallbackGenerator = new(
            [
                new StubArchitectureDiagramGenerator(DiagramFormat.Mermaid, isSupported: false, "```mermaid\nignored\n```\n"),
                new PlantUmlArchitectureDiagramGenerator(identifierFormatter)
            ],
            [
                new StubAssemblyDependencyDiagramGenerator(DiagramFormat.Mermaid, isSupported: false, "ignored"),
                new PlantUmlAssemblyDependencyDiagramGenerator(identifierFormatter)
            ],
            new DiagramGeneratorSelector(),
            new RelativeLinkRewriter(),
            new MicrosoftLearnReferenceRegistry());

        IndexPageMarkdownGenerator emptyBlockGenerator = new(
            [new StubArchitectureDiagramGenerator(DiagramFormat.Mermaid, isSupported: true, embeddedMarkdown: "   ")],
            [new StubAssemblyDependencyDiagramGenerator(DiagramFormat.Mermaid, isSupported: true, renderedMarkdown: "   ")],
            new DiagramGeneratorSelector(),
            new RelativeLinkRewriter(),
            new MicrosoftLearnReferenceRegistry());

        AssemblyDependencyEdgeModel[] sampleDependencyEdges =
        [
            new()
            {
                SourceAssemblyName = "App.Services",
                TargetAssemblyName = "Newtonsoft.Json",
                ReferenceKind = AssemblyReferenceKind.Package,
                VersionLinkingKind = AssemblyVersionLinkingKind.Range,
                VersionConstraintExpression = "[13.0.1, 14.0.0)",
                IsTargetStrongNamed = true,
                TargetPublicKeyToken = "30ad4fe6b2a6aeed"
            }
        ];

        // Act
        var plantUmlMarkdown = defaultGenerator.GenerateIndexMarkdown(
            models,
            diagramFormat: DiagramFormat.PlantUml,
            assemblyDependencyEdges: sampleDependencyEdges);
        var mermaidSubdirMarkdown = defaultGenerator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: false,
            diagramFormat: DiagramFormat.Mermaid,
            isAssemblySubdirectoryIndex: true,
            assemblyDependencyEdges: sampleDependencyEdges);
        var multiAssemblyRootMarkdown = defaultGenerator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: true,
            diagramFormat: DiagramFormat.Mermaid,
            assemblyDependencyEdges: sampleDependencyEdges);
        var fallbackMarkdown = fallbackGenerator.GenerateIndexMarkdown(
            models,
            diagramFormat: DiagramFormat.Mermaid,
            assemblyDependencyEdges: sampleDependencyEdges);
        var emptyBlockMarkdown = emptyBlockGenerator.GenerateIndexMarkdown(
            models,
            diagramFormat: DiagramFormat.Mermaid,
            assemblyDependencyEdges: sampleDependencyEdges);

        // Assert
        _ = plantUmlMarkdown.Should().Contain("## Architecture Diagram")
            .And.Contain("## Assembly Dependency Tree")
            .And.Contain("```plantuml", "because PlantUML was explicitly preferred.")
            .And.NotContain("```mermaid", "because only the selected diagram format should render.");
        _ = mermaidSubdirMarkdown.Should().Contain("## Assembly Dependency Tree")
            .And.Contain("flowchart LR")
            .And.Contain("Range ([13.0.1, 14.0.0))")
            .And.Contain("linkStyle 0 stroke:#2E7D32")
            .And.NotContain("```plantuml");
        _ = multiAssemblyRootMarkdown.Should().NotContain("## Assembly Dependency Tree",
            "because multi-assembly solution root index pages do not render per-assembly dependency trees.");
        _ = fallbackMarkdown.Should().Contain("## Architecture Diagram")
            .And.Contain("## Assembly Dependency Tree")
            .And.Contain("```plantuml", "because unsupported Mermaid should fall back to PlantUML.")
            .And.NotContain("```mermaid", "because the unsupported Mermaid generator must be skipped.");
        _ = emptyBlockMarkdown.Should().NotContain("## Architecture Diagram",
            "because an empty embedded block must not emit a blank Architecture Diagram heading.")
            .And.NotContain("## Assembly Dependency Tree",
                "because an empty dependency block must not emit a blank Assembly Dependency Tree heading.");
    }

    [Fact]
    public void GenerateIndexMarkdownWithAssemblyAndTypeAppliedAttributesShouldRenderAndRewriteLinksSafely()
    {
        // Arrange
        var generator = CreateGenerator();
        TypeDocumentationModel[] models =
        [
            new()
            {
                TypeName = "OrderService",
                NamespaceName = "App.Services",
                AssemblyName = "App",
                SummaryDescription = "Processes orders.",
                AppliedAttributes = ["[TierAttribute](App/Attributes/TierAttribute/index.md)`(\"Stable | v1\")`"]
            },
            new()
            {
                TypeName = "EmptyDocAttributed",
                NamespaceName = "App.Services",
                AssemblyName = "App",
                AppliedAttributes = ["PlainCustomAttribute"]
            }
        ];
        string[] assemblyAttributes =
        [
            "[TierAttribute](App/Attributes/TierAttribute/index.md)`(\"Stable\")`"
        ];

        // Act
        var subdirectoryMarkdown = generator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: false,
            diagramFormat: DiagramFormat.None,
            isAssemblySubdirectoryIndex: true,
            assemblyAttributes: assemblyAttributes);

        var rootMultiAssemblyMarkdown = generator.GenerateIndexMarkdown(
            models,
            includeAssemblyDirectory: true,
            diagramFormat: DiagramFormat.None,
            assemblyAttributes: assemblyAttributes);

        // Assert
        _ = subdirectoryMarkdown.Should().Contain(
                "**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [TierAttribute](Attributes/TierAttribute/index.md)`(\"Stable\")`",
                "because assembly-level custom attributes must rewrite local links relative to the assembly index directory.")
            .And.Contain(
                "Processes orders.<br/>**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [TierAttribute](Attributes/TierAttribute/index.md)`(\"Stable \\| v1\")`",
                "because type-level attributes in namespace tables must rewrite links and escape pipe characters.")
            .And.Contain(
                "| [EmptyDocAttributed](Services/EmptyDocAttributed/index.md) | [Class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class) | **[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** `[PlainCustomAttribute]` |",
                "because types without summaries still display their applied custom attributes.");

        _ = rootMultiAssemblyMarkdown.Should().NotContain(
            "**[Attributes]",
            "because multi-assembly solution root index pages list assemblies rather than single-assembly attributes.");
    }

    [Fact]
    public void GenerateIndexMarkdownShouldRenderExperimentalPrefixInNamespaceTable()
    {
        // Arrange
        var generator = CreateGenerator();
        IReadOnlyList<TypeDocumentationModel> models =
        [
            new TypeDocumentationModel
            {
                AssemblyName = "SampleAssembly",
                NamespaceName = "SampleAssembly.Preview",
                TypeName = "ExperimentalPreviewClient",
                TypeCategory = TypeCategory.Class,
                SummaryDescription = "Preview client.",
                IsExperimental = true,
                ExperimentalDiagnosticId = "EXP007"
            }
        ];

        // Act
        var markdown = generator.GenerateIndexMarkdown(models, includeAssemblyDirectory: false, diagramFormat: DiagramFormat.None);

        // Assert
        _ = markdown.Should().Contain("**[Experimental](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute)** (`EXP007`) — Preview client.");
    }

    [Fact]
    public void GenerateIndexMarkdownShouldHandleCombinedObsoleteExperimentalBadgesAndEmptyAssemblies()
    {
        // Arrange
        var generator = CreateGenerator();
        IReadOnlyList<TypeDocumentationModel> combinedModels =
        [
            new TypeDocumentationModel
            {
                AssemblyName = string.Empty,
                NamespaceName = "Sample.Edge",
                TypeName = "ObsoleteExperimentalFallback",
                TypeCategory = TypeCategory.Class,
                SummaryDescription = "Fallback summary.",
                IsObsolete = true,
                ObsoleteMessage = string.Empty,
                IsExperimental = true,
                ExperimentalDiagnosticId = string.Empty
            },
            new TypeDocumentationModel
            {
                AssemblyName = string.Empty,
                NamespaceName = "Sample.Edge",
                TypeName = "BareExperimental",
                TypeCategory = TypeCategory.Class,
                SummaryDescription = string.Empty,
                IsExperimental = true,
                ExperimentalDiagnosticId = string.Empty
            },
            new TypeDocumentationModel
            {
                AssemblyName = string.Empty,
                NamespaceName = "Sample.Edge",
                TypeName = "ObsoleteWithDiagnosticId",
                TypeCategory = TypeCategory.Class,
                SummaryDescription = "Has diagnostic badge.",
                IsObsolete = true,
                ObsoleteMessage = "Replaced type.",
                ObsoleteDiagnosticId = "APP010",
                ObsoleteUrlFormat = "https://docs.example.com/errors/{0}"
            }
        ];

        // Act
        var namespaceMarkdown = generator.GenerateIndexMarkdown(
            combinedModels,
            includeAssemblyDirectory: false,
            diagramFormat: DiagramFormat.None);
        var emptyAssembliesMarkdown = generator.GenerateIndexMarkdown(
            combinedModels,
            includeAssemblyDirectory: true,
            diagramFormat: DiagramFormat.None);

        // Assert
        _ = namespaceMarkdown.Should().Contain("**[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute):** Fallback summary.<br/>**[Experimental](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute)**")
            .And.Contain("| [BareExperimental](Edge/BareExperimental/index.md) | [Class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class) | **[Experimental](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute)** |")
            .And.Contain("**[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute) ([APP010](https://docs.example.com/errors/APP010)):** Replaced type.",
                "because assembly index type rows should include linked Obsolete DiagnosticId badges when present.");
        _ = emptyAssembliesMarkdown.Should().NotContain("## Assembly");
    }
}
