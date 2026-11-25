using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.DemoContracts.Auditing;
using Documentation.Exporter.DemoLibrary.Auditing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Integration;

public sealed class MultiAssemblyAndSolutionEndToEndTests
{
    [Fact]
    [IntegrationTest]
    public async Task ExportDocumentationAsyncWithMultiAssemblyAndCustomLinkRulesShouldEmitPerAssemblySubdirectoriesAndCentralIndex()
    {
        // Arrange
        var demoContractsAssemblyPath = typeof(ISettlementAuditSink).Assembly.Location;
        var demoContractsXmlPath = Path.ChangeExtension(demoContractsAssemblyPath, ".xml");
        var demoLibraryAssemblyPath = typeof(CrossAssemblyAuditReporter).Assembly.Location;
        var demoLibraryXmlPath = Path.ChangeExtension(demoLibraryAssemblyPath, ".xml");

        var tempOutputDir = Path.Combine(Path.GetTempPath(), $"DocExporter_MultiAssembly_{Guid.NewGuid():N}");

        try
        {
            ServiceCollection services = new();
            _ = services.AddDocumentationExporter();
            await using var provider = services.BuildServiceProvider();
            var engine = provider.GetRequiredService<IDocumentationExporterEngine>();

            ExporterOptions options = new()
            {
                AssemblyFilePaths = [demoContractsAssemblyPath, demoLibraryAssemblyPath],
                XmlDocumentationFilePaths = [demoContractsXmlPath, demoLibraryXmlPath],
                OutputDirectoryPath = tempOutputDir,
                StrictLinkValidation = true,
                ExternalDocLinkRules =
                [
                    new ExternalDocLinkRule
                    {
                        NamespacePrefix = "System.Threading",
                        UrlTemplate = "https://custom.threading.docs/api/{fullNameLower}.html"
                    }
                ]
            };

            // Act
            var exportResult = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = exportResult.IsSuccess.Should().BeTrue("because multi-assembly export should succeed.");

            var centralIndexPath = Path.Combine(tempOutputDir, "index.md");
            var contractsIndexPath = Path.Combine(tempOutputDir, "Documentation.Exporter.DemoContracts", "index.md");
            var libraryIndexPath = Path.Combine(tempOutputDir, "Documentation.Exporter.DemoLibrary", "index.md");
            var reporterTypePagePath = Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Auditing",
                "CrossAssemblyAuditReporter",
                "index.md");
            var reporterMethodPagePath = Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Auditing",
                "CrossAssemblyAuditReporter",
                "WriteAuditEvent.md");
            var orderServiceMethodPagePath = Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "ProcessOrderAsync.md");

            _ = File.Exists(centralIndexPath).Should().BeTrue("because a central root index.md must be generated.");
            _ = File.Exists(contractsIndexPath).Should().BeTrue("because DemoContracts must have its own subdirectory index.md.");
            _ = File.Exists(libraryIndexPath).Should().BeTrue("because DemoLibrary must have its own subdirectory index.md.");
            _ = File.Exists(reporterTypePagePath).Should().BeTrue();
            _ = File.Exists(reporterMethodPagePath).Should().BeTrue();

            var centralIndexMarkdown = await File.ReadAllTextAsync(centralIndexPath);
            _ = centralIndexMarkdown.Should().Contain("[Documentation.Exporter.DemoContracts](Documentation.Exporter.DemoContracts/index.md)")
                .And.Contain("[Documentation.Exporter.DemoLibrary](Documentation.Exporter.DemoLibrary/index.md)")
                .And.NotContain("## Namespace:", "because the central root index.md should only list assemblies while each assembly's index.md lists its namespaces and types.");

            var contractsIndexMarkdown = await File.ReadAllTextAsync(contractsIndexPath);
            _ = contractsIndexMarkdown.Should().Contain("[ISettlementAuditSink](Auditing/ISettlementAuditSink/index.md)");

            var libraryIndexMarkdown = await File.ReadAllTextAsync(libraryIndexPath);
            _ = libraryIndexMarkdown.Should().Contain("[CrossAssemblyAuditReporter](Auditing/CrossAssemblyAuditReporter/index.md)")
                .And.Contain("[ISettlementAuditSink](../Documentation.Exporter.DemoContracts/Auditing/ISettlementAuditSink/index.md)",
                    "because cross-assembly links in an assembly index summary must traverse up one directory to the peer assembly directory.");

            var reporterTypeMarkdown = await File.ReadAllTextAsync(reporterTypePagePath);
            _ = reporterTypeMarkdown.Should().Contain("**Assembly:** [Documentation.Exporter.DemoLibrary](../../index.md)")
                .And.Contain("[ISettlementAuditSink](../../../Documentation.Exporter.DemoContracts/Auditing/ISettlementAuditSink/index.md)",
                    "because cross-assembly references must resolve to relative paths into the target assembly's subdirectory.")
                .And.Contain("**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [StabilityTierAttribute](../../Attributes/StabilityTierAttribute/index.md)`(\"Enterprise-Audit\")`",
                    "because custom attributes and their constructor arguments must be rendered and linked.")
                .And.Contain("## Type Diagram")
                .And.Contain("```mermaid")
                .And.Contain("ISettlementAuditSink <|.. CrossAssemblyAuditReporter")
                .And.Contain("## Extension Methods")
                .And.Contain("[TryWriteVerifiedAudit](../../Extensions/OrderRequestExtensions/TryWriteVerifiedAudit.md)");

            var sinkInterfacePagePath = Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoContracts",
                "Auditing",
                "ISettlementAuditSink",
                "index.md");
            var sinkInterfaceMarkdown = await File.ReadAllTextAsync(sinkInterfacePagePath);
            _ = sinkInterfaceMarkdown.Should().Contain("## Known Implementations")
                .And.Contain("[CrossAssemblyAuditReporter](../../../Documentation.Exporter.DemoLibrary/Auditing/CrossAssemblyAuditReporter/index.md)")
                .And.Contain("## Extension Methods")
                .And.Contain("[TryWriteVerifiedAudit](../../../Documentation.Exporter.DemoLibrary/Extensions/OrderRequestExtensions/TryWriteVerifiedAudit.md)");

            var auditableBasePagePath = Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "AuditableProcessorBase",
                "index.md");
            var auditableBaseMarkdown = await File.ReadAllTextAsync(auditableBasePagePath);
            _ = auditableBaseMarkdown.Should().Contain("## Derived Types")
                .And.Contain("[OrderProcessingService](../OrderProcessingService/index.md)");

            var orderRequestPagePath = Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Models",
                "OrderRequest",
                "index.md");
            var orderRequestMarkdown = await File.ReadAllTextAsync(orderRequestPagePath);
            _ = orderRequestMarkdown.Should().Contain("## Extension Methods")
                .And.Contain("[IsHighValueOrder](../../Extensions/OrderRequestExtensions/IsHighValueOrder.md)");

            var navigationYamlPath = Path.Combine(tempOutputDir, "mkdocs.nav.yml");
            _ = File.Exists(navigationYamlPath).Should().BeTrue("because mkdocs.nav.yml should be generated automatically.");
            var navigationYaml = await File.ReadAllTextAsync(navigationYamlPath);
            _ = navigationYaml.Should().Contain("nav:")
                .And.Contain("Documentation.Exporter.DemoContracts:")
                .And.Contain("Documentation.Exporter.DemoLibrary:");

            var reporterMethodMarkdown = await File.ReadAllTextAsync(reporterMethodPagePath);
            _ = reporterMethodMarkdown.Should().Contain("Records a settlement audit correlation event into the underlying sink.",
                    "because <inheritdoc /> must migrate summary across assembly boundaries.")
                .And.Contain("The unique transaction correlation identifier.",
                    "because <inheritdoc /> must migrate parameter descriptions across assembly boundaries.")
                .And.Contain("Thrown when `correlationIdentifier` is `null`.",
                    "because <inheritdoc /> must migrate documented exceptions across assembly boundaries.");

            var orderServiceMethodMarkdown = await File.ReadAllTextAsync(orderServiceMethodPagePath);
            _ = orderServiceMethodMarkdown.Should().Contain("https://custom.threading.docs/api/system.threading.cancellationtoken.html",
                    "because custom ExternalDocLinkRules must override matched third-party namespaces.")
                .And.Contain("https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception",
                    "because unmatched external types must continue falling back to Microsoft Learn.");
        }
        finally
        {
            if (Directory.Exists(tempOutputDir))
            {
                Directory.Delete(tempOutputDir, recursive: true);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public void ExporterOptionsWithSiblingXmlInferenceShouldResolvePathsAutomatically()
    {
        // Arrange
        var demoLibraryAssemblyPath = typeof(CrossAssemblyAuditReporter).Assembly.Location;
        var expectedXmlPath = Path.ChangeExtension(demoLibraryAssemblyPath, ".xml");

        var options = new ExporterOptions
        {
            AssemblyFilePaths = [demoLibraryAssemblyPath],
            OutputDirectoryPath = Path.GetTempPath()
        };
        var discoveryService = new Documentation.Exporter.Core.Parsing.ExportArtifactDiscoveryService(
            new Documentation.Exporter.Core.Parsing.SolutionAssemblyResolver(
                new Documentation.Exporter.Core.IO.PhysicalFileSystem()));

        // Act
        var resolvedAssemblies = discoveryService.DiscoverAssemblyFilePaths(options);
        var resolvedXmlFiles = discoveryService.DiscoverXmlDocumentationFilePaths(options);

        // Assert
        _ = options.AssemblyFilePath.Should().Be(demoLibraryAssemblyPath);
        _ = options.XmlDocumentationFilePath.Should().Be(expectedXmlPath);
        _ = resolvedAssemblies.Should().ContainSingle().Which.Should().Be(demoLibraryAssemblyPath);
        _ = resolvedXmlFiles.Should().ContainSingle().Which.Should().Be(expectedXmlPath);
    }

    [Fact]
    [IntegrationTest]
    public async Task ExportDocumentationAsyncWithSolutionFilePathShouldResolveProjectsAndExportUnifiedDocs()
    {
        // Arrange
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "demo", "Documentation.Exporter.Demo.slnx")))
        {
            currentDir = currentDir.Parent;
        }

        _ = currentDir.Should().NotBeNull("because demo/Documentation.Exporter.Demo.slnx must exist in the repository.");
        var demoConfigPath = Path.Combine(currentDir.FullName, "demo", "doc-exporter.demo.json");
        var tempOutputDir = Path.Combine(Path.GetTempPath(), $"DocExporter_SlnE2E_{Guid.NewGuid():N}");
        var plantUmlOutputDir = Path.Combine(Path.GetTempPath(), $"DocExporter_SlnPumlE2E_{Guid.NewGuid():N}");

        try
        {
            ServiceCollection services = new();
            _ = services.AddDocumentationExporter();
            await using var provider = services.BuildServiceProvider();
            var engine = provider.GetRequiredService<IDocumentationExporterEngine>();
            var configurationLoader = provider.GetRequiredService<Documentation.Exporter.Core.Configuration.IExporterConfigurationLoader>();

            var options = await configurationLoader.LoadAsync(demoConfigPath);
            options.OutputDirectoryPath = tempOutputDir;
            options.ReportFilePath = Path.Combine(tempOutputDir, "quality-report.json");
            var targetNavPath = Path.Combine(tempOutputDir, "mkdocs.nav.yml");
            options.NavigationFilePath = targetNavPath;
            _ = Directory.CreateDirectory(tempOutputDir);
            var baseNavTemplatePath = Path.Combine(currentDir.FullName, "demo", "mkdocs.base.nav.yml");
            File.Copy(baseNavTemplatePath, targetNavPath, overwrite: true);

            // Act (First run: inserts into Developer Portal/API Reference with default Mermaid format; Second run: updates in-place idempotently)
            var exportResult = await engine.ExportDocumentationAsync(options);
            var secondExportResult = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = exportResult.IsSuccess.Should().BeTrue("because solution export should discover built demo projects and succeed.");
            _ = secondExportResult.IsSuccess.Should().BeTrue("because re-running export on an already-extended mkdocs.nav.yml must succeed.");
            _ = File.Exists(Path.Combine(tempOutputDir, "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(tempOutputDir, "architecture.puml")).Should().BeFalse("because DiagramFormat.Mermaid should emit only Mermaid and not standalone architecture.puml.");
            _ = File.Exists(Path.Combine(tempOutputDir, "Documentation.Exporter.DemoContracts", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(tempOutputDir, "Documentation.Exporter.DemoContracts", "architecture.puml")).Should().BeFalse("because DiagramFormat.Mermaid must not emit PlantUML standalone files.");
            _ = File.Exists(Path.Combine(tempOutputDir, "Documentation.Exporter.DemoLibrary", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(tempOutputDir, "Documentation.Exporter.DemoLibrary", "architecture.puml")).Should().BeFalse("because DiagramFormat.Mermaid must not emit PlantUML standalone files.");

            var demoLibraryIndexContent = await File.ReadAllTextAsync(Path.Combine(tempOutputDir, "Documentation.Exporter.DemoLibrary", "index.md"));
            _ = demoLibraryIndexContent.Should().Contain(
                    "**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [StabilityTierAttribute](Attributes/StabilityTierAttribute/index.md)`(\"Stable\")`",
                    "because assembly-level custom attributes on DemoLibrary must render and link on its assembly index.md.")
                .And.Contain("## Assembly References")
                .And.Contain("| [Documentation.Exporter.DemoContracts](../Documentation.Exporter.DemoContracts/index.md) | Project | `1.0.0.0` | Linked (Project) | No |",
                    "because DemoContracts is part of the same solution export and must link to its assembly index.md.")
                .And.Contain("| `System.Runtime` | Framework |")
                .And.Contain("Yes (`")
                .And.Contain("## Assembly Dependency Tree", "because DemoLibrary has non-framework project dependencies.")
                .And.Contain("flowchart LR")
                .And.Contain("Linked (Project)")
                .And.Contain("linkStyle 0 stroke:#757575,stroke-width:2px,stroke-dasharray:5 5",
                    "because DemoContracts is unsigned and should render with dashed gray styling.")
                .And.Contain("## Architecture Diagram")
                .And.Contain("```mermaid", "because demo configuration defaults to Mermaid.")
                .And.NotContain("```plantuml", "because only one diagram format should be emitted per diagram section.")
                .And.Contain("([DEMO002](https://docs.example.com/diagnostics/DEMO002))",
                    "because assembly index type tables must include [Obsolete(DiagnosticId, UrlFormat)] badges.");

            var demoContractsIndexContent = await File.ReadAllTextAsync(Path.Combine(tempOutputDir, "Documentation.Exporter.DemoContracts", "index.md"));
            _ = demoContractsIndexContent.Should().NotContain("## Assembly Dependency Tree",
                "because DemoContracts has only BCL framework references which are excluded from the dependency tree.");

            var rootIndexContent = await File.ReadAllTextAsync(Path.Combine(tempOutputDir, "index.md"));
            _ = rootIndexContent.Should().Contain("## Architecture Diagram")
                .And.Contain("```mermaid", "because root index should render the preferred Mermaid architecture diagram.")
                .And.NotContain("```plantuml", "because duplicate PlantUML diagrams must not be generated alongside Mermaid.")
                .And.NotContain("## Assembly Dependency Tree", "because multi-assembly solution root index omits assembly dependency trees.");

            var orderProcessingTypeContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "index.md"));
            _ = orderProcessingTypeContent.Should().Contain("## Type Diagram")
                .And.Contain("```mermaid")
                .And.NotContain("```plantuml", "because only one diagram format should be emitted on type pages.")
                .And.Contain("// External snippet injected from docs_extra/examples/OrderProcessingService.cs",
                    "because external type-level .cs snippet files must appear on the generated type page.")
                .And.Contain("**Sidecar Override (`after_remarks`)**:",
                    "because type-level after_remarks sidecar overrides must appear on the generated type page.");

            var paymentStatusContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Models",
                "PaymentStatus",
                "index.md"));
            _ = paymentStatusContent.Should().Contain(
                    "**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [StabilityTierAttribute](../../Attributes/StabilityTierAttribute/index.md)`(\"Stable\")`",
                    "because enum types must display their custom attributes with clickable links.")
                .And.Contain(
                    "[DisplayAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.displayattribute)`(Name = \"Pending Verification\")`",
                    "because enum values must display their custom attributes with clickable links.")
                .And.Contain("## State Transition Matrix (Sidecar `page_append`)",
                    "because enum page_append sidecar overrides must appear on PaymentStatus/index.md.");

            var legacyAdapterContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Adapters",
                "LegacyInvoiceAdapter",
                "index.md"));
            _ = legacyAdapterContent.Should().Contain("**Sidecar Migration Notice (`before_remarks`)**",
                "because before_remarks sidecar overrides must appear on LegacyInvoiceAdapter/index.md.");

            var orderRequestContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Models",
                "OrderRequest",
                "index.md"));
            _ = orderRequestContent.Should().Contain(
                "[RequiredAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)",
                "because member overview tables on OrderRequest/index.md must surface custom attributes with links.");

            var ctorParamContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "OrderProcessingService.ctor_String.md"));
            _ = ctorParamContent.Should().Contain(
                "[RequiredAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)",
                "because constructor parameters must render linked custom attributes in the Parameters table.");

            var auditableEnvelopeIndexContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Models",
                "AuditableEnvelope",
                "index.md"));
            var typeParametersSectionIndex = auditableEnvelopeIndexContent.IndexOf("#### Type Parameters", StringComparison.Ordinal);
            _ = typeParametersSectionIndex.Should().BeGreaterThan(0,
                "because AuditableEnvelope<TPayload> must document its generic type parameter.");
            var constructorsSectionIndex = auditableEnvelopeIndexContent.IndexOf("## Constructors", StringComparison.Ordinal);
            _ = constructorsSectionIndex.Should().BeGreaterThan(typeParametersSectionIndex);
            _ = auditableEnvelopeIndexContent[typeParametersSectionIndex..constructorsSectionIndex].Should().Contain(
                "[StabilityTierAttribute](../../Attributes/StabilityTierAttribute/index.md)`(\"Stable\")`",
                "because type-level generic parameter custom attributes must render with clickable links inside #### Type Parameters.");

            var projectPayloadContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Models",
                "AuditableEnvelope",
                "ProjectPayload.md"));
            _ = projectPayloadContent.Should().Contain("#### Type Parameters",
                    "because ProjectPayload<TMetric> must document its method-level generic type parameter.")
                .And.Contain(
                    "[StabilityTierAttribute](../../Attributes/StabilityTierAttribute/index.md)`(\"Preview\")`",
                    "because method-level generic parameter custom attributes must render with clickable links.");

            var fieldMemberContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "MaxInstantSettlementThreshold.md"));
            var eventMemberContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "OrderStatusChanged.md"));

            const string expectedMemberStabilityAttributeLine =
                "**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [StabilityTierAttribute](../../Attributes/StabilityTierAttribute/index.md)`(\"Stable\")`";

            _ = orderProcessingTypeContent.Should().Contain(
                "[StabilityTierAttribute](../../Attributes/StabilityTierAttribute/index.md)`(\"Stable\")`",
                "because member overview tables on OrderProcessingService/index.md must surface custom attributes with links.");
            _ = fieldMemberContent.Should().Contain(
                expectedMemberStabilityAttributeLine,
                "because field member pages must render linked custom attributes.");
            _ = eventMemberContent.Should().Contain(
                expectedMemberStabilityAttributeLine,
                "because event member pages must render linked custom attributes.");
            _ = ctorParamContent.Should().Contain(
                expectedMemberStabilityAttributeLine,
                "because constructor member pages must render linked custom attributes.");

            var processOrderMethodContent = await File.ReadAllTextAsync(Path.Combine(
                tempOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "ProcessOrderAsync.md"));
            var returnsSectionIndex = processOrderMethodContent.IndexOf("#### Returns", StringComparison.Ordinal);
            _ = returnsSectionIndex.Should().BeGreaterThan(0, "because ProcessOrderAsync.md must include a #### Returns section.");
            _ = processOrderMethodContent[..returnsSectionIndex].Should().Contain(
                expectedMemberStabilityAttributeLine,
                "because method-level custom attributes must render with clickable links in the member header.");
            _ = processOrderMethodContent[returnsSectionIndex..].Should().Contain(
                expectedMemberStabilityAttributeLine,
                "because method return-value custom attributes must render with clickable links in #### Returns.");
            _ = processOrderMethodContent.Should().Contain(
                "// External member-level snippet injected from docs_extra/examples/OrderProcessingService/ProcessOrderAsync.cs",
                "because external member-level .cs snippet files must appear on the generated member page.");

            // Act & Assert - Explicit PlantUML preference emits single PlantUML diagram and standalone .puml files
            var plantUmlOptions = await configurationLoader.LoadAsync(demoConfigPath);
            plantUmlOptions.DiagramFormat = DiagramFormat.PlantUml;
            plantUmlOptions.OutputDirectoryPath = plantUmlOutputDir;
            plantUmlOptions.NavigationFilePath = Path.Combine(plantUmlOutputDir, "mkdocs.nav.yml");
            var plantUmlResult = await engine.ExportDocumentationAsync(plantUmlOptions);

            _ = plantUmlResult.IsSuccess.Should().BeTrue("because PlantUML solution export should succeed.");
            _ = File.Exists(Path.Combine(plantUmlOutputDir, "architecture.puml")).Should().BeTrue("because PlantUml emits standalone root architecture.puml.");
            _ = File.Exists(Path.Combine(plantUmlOutputDir, "Documentation.Exporter.DemoLibrary", "architecture.puml")).Should().BeTrue("because PlantUml emits per-assembly architecture.puml.");
            var plantUmlDemoLibraryIndex = await File.ReadAllTextAsync(Path.Combine(plantUmlOutputDir, "Documentation.Exporter.DemoLibrary", "index.md"));
            _ = plantUmlDemoLibraryIndex.Should().Contain("## Assembly Dependency Tree")
                .And.Contain("-[#757575,dashed]->", "because PlantUML dependency tree must render dashed gray arrows for unsigned DemoContracts.");
            var plantUmlRootIndex = await File.ReadAllTextAsync(Path.Combine(plantUmlOutputDir, "index.md"));
            _ = plantUmlRootIndex.Should().Contain("## Architecture Diagram")
                .And.Contain("```plantuml")
                .And.NotContain("```mermaid", "because PlantUML preference should emit only PlantUML diagrams.");
            var plantUmlTypeContent = await File.ReadAllTextAsync(Path.Combine(
                plantUmlOutputDir,
                "Documentation.Exporter.DemoLibrary",
                "Services",
                "OrderProcessingService",
                "index.md"));
            _ = plantUmlTypeContent.Should().Contain("## Type Diagram")
                .And.Contain("```plantuml")
                .And.NotContain("```mermaid");

            var excludedOptions = await configurationLoader.LoadAsync(demoConfigPath);
            excludedOptions.ExcludedProjects = ["*DemoContracts*"];
            var discoveryService = provider.GetRequiredService<Documentation.Exporter.Core.Parsing.IExportArtifactDiscoveryService>();
            var filteredAssemblies = discoveryService.DiscoverAssemblyFilePaths(excludedOptions);
            _ = filteredAssemblies.Should().ContainSingle("because *DemoContracts* should be excluded by wildcard pattern.")
                .Which.Should().Contain("Documentation.Exporter.DemoLibrary");

            var mergedNavYaml = await File.ReadAllTextAsync(targetNavPath);
            _ = mergedNavYaml.Should().Contain("site_name: Contoso Settlement Platform Documentation")
                .And.Contain("- Home: home.md")
                .And.Contain("- Developer Portal:")
                .And.Contain("- API Reference:")
                .And.Contain("          - Overview: index.md")
                .And.Contain("          - Documentation.Exporter.DemoContracts:")
                .And.Contain("          - Documentation.Exporter.DemoLibrary:")
                .And.Contain("- Release Notes: changelog.md");

            // Act & Assert - Modern C# 9-14 support and zero compiler metadata leaks
            foreach (var markdownFilePath in Directory.EnumerateFiles(tempOutputDir, "*.md", SearchOption.AllDirectories))
            {
                var markdownContent = await File.ReadAllTextAsync(markdownFilePath);
                _ = markdownContent.Should().NotContain("<G>$", $"because {markdownFilePath} must not leak C# 14 extension marker types.")
                    .And.NotContain("<M>$", $"because {markdownFilePath} must not leak C# 14 extension marker containers.")
                    .And.NotContain("EqualityContract", $"because {markdownFilePath} must not leak synthesized record EqualityContract.")
                    .And.NotContain("AsyncIteratorStateMachineAttribute", $"because {markdownFilePath} must not leak iterator state machine attributes.")
                    .And.NotContain("ParamCollectionAttribute", $"because {markdownFilePath} must not leak ParamCollectionAttribute.")
                    .And.NotContain("ExtensionMarkerAttribute", $"because {markdownFilePath} must not leak ExtensionMarkerAttribute.")
                    .And.NotContain("Constructors of types with required members are not supported", $"because {markdownFilePath} must not leak Roslyn required-member obsolete metadata.")
                    .And.NotContain("Types with embedded references are not supported", $"because {markdownFilePath} must not leak Roslyn ref-struct obsolete metadata.")
                    .And.NotContain("DefaultMemberAttribute", $"because {markdownFilePath} must not leak indexer DefaultMemberAttribute metadata.");
            }

            var demoLibRoot = Path.Combine(tempOutputDir, "Documentation.Exporter.DemoLibrary");
            var orderRequestIndex = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "Models", "OrderRequest", "index.md"));
            _ = orderRequestIndex.Should().Contain("## Extension Properties")
                .And.Contain("CreateHighValueSample");

            var itemSkuContent = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "Models", "OrderRequest", "ItemLine", "ItemSku.md"));
            _ = itemSkuContent.Should().Contain("init;");

            var monetaryValueIndex = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "Models", "MonetaryValue", "index.md"));
            _ = monetaryValueIndex.Should().Contain("**[ReadOnly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly):** `Yes`");

            var settlementRecordBaseIndex = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "ModernCSharp", "SettlementRecordBase", "index.md"));
            _ = settlementRecordBaseIndex.Should().Contain("**[Record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record):** `Yes`")
                .And.NotContain("[Obsolete");

            var orderSpanReaderIndex = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "ModernCSharp", "OrderSpanReader", "index.md"));
            _ = orderSpanReaderIndex.Should().Contain("**[ReadOnly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly):** `Yes`")
                .And.Contain("**[Ref Struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct):** `Yes`")
                .And.NotContain("[Obsolete");

            var experimentalPreviewIndex = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "ModernCSharp", "ExperimentalSettlementPreview", "index.md"));
            _ = experimentalPreviewIndex.Should().Contain("> [!IMPORTANT]")
                .And.Contain("DEMO001");

            var combineScopedContent = await File.ReadAllTextAsync(Path.Combine(demoLibRoot, "ModernCSharp", "SettlementLedger", "CombineScopedTag.md"));
            _ = combineScopedContent.Should().Contain("[scoped]")
                .And.Contain("[ref readonly]");

            _ = File.Exists(Path.Combine(demoLibRoot, "ModernCSharp", "SpanFormatter", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(demoLibRoot, "ModernCSharp", "IMetricConverter", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(demoLibRoot, "ModernCSharp", "ISettlementMetric", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(demoLibRoot, "ModernCSharp", "SettlementTransformHandler", "index.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(demoLibRoot, "ModernCSharp", "SettlementLedger", "MetricCategory.md")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempOutputDir))
            {
                Directory.Delete(tempOutputDir, recursive: true);
            }

            if (Directory.Exists(plantUmlOutputDir))
            {
                Directory.Delete(plantUmlOutputDir, recursive: true);
            }
        }
    }

    [Fact]
    [UnitTest]
    public void GenerateNavigationYamlWhenExtendingExistingYamlWithVariousEntryPointsShouldMergeCorrectly()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddDocumentationExporter();
        using var provider = services.BuildServiceProvider();
        var generator = provider.GetServices<Documentation.Exporter.Abstractions.Generation.INavigationGenerator>()
            .First(candidate => candidate.Format == NavigationFormat.MkDocs);

        TypeDocumentationModel[] models =
        [
            new()
            {
                AssemblyName = "Contoso.Core",
                NamespaceName = "Contoso.Core.Services",
                TypeName = "OrderEngine"
            }
        ];

        var yamlWithMarker = """
            site_name: Marker Docs
            nav:
              - Home: index.md
              - Reference:
                  # API-NAV-MARKER
              - Footer: footer.md
            """;

        var yamlWithoutNav = "site_name: Minimal Site";

        var yamlWithTopLevelNav = """
            nav:
              - Home: home.md
              - Contoso.Core.Services:
                  - OldType: old.md
            """;

        // Act
        var markerMerged = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithMarker,
            navigationEntryPoint: "# API-NAV-MARKER",
            navigationUrlPrefix: "api");

        var appendedNav = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithoutNav,
            navigationEntryPoint: "Docs/API Reference",
            navigationUrlPrefix: "/reference/api/");

        var topLevelMerged = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithTopLevelNav,
            navigationEntryPoint: null,
            navigationUrlPrefix: null);

        var missingEntryAppended = generator.GenerateNavigation(
            models,
            useAssemblySubdirectories: false,
            existingNavigationContent: yamlWithTopLevelNav,
            navigationEntryPoint: "New Section > API",
            navigationUrlPrefix: null);

        // Assert
        _ = markerMerged.Should().Contain("- Overview: api/index.md")
            .And.Contain("- OrderEngine: api/Services/OrderEngine/index.md")
            .And.Contain("# API-NAV-MARKER")
            .And.Contain("# API-NAV-MARKER-END")
            .And.Contain("- Footer: footer.md");

        _ = appendedNav.Should().Contain("site_name: Minimal Site")
            .And.Contain("nav:")
            .And.Contain("- Docs:")
            .And.Contain("- API Reference:")
            .And.Contain("- Overview: reference/api/index.md");

        _ = topLevelMerged.Should().Contain("- Home: home.md")
            .And.Contain("- OrderEngine: Services/OrderEngine/index.md")
            .And.NotContain("OldType");

        _ = missingEntryAppended.Should().Contain("- New Section:")
            .And.Contain("      - API:")
            .And.Contain("          - Overview: index.md");
    }

    [Fact]
    [IntegrationTest]
    public async Task ConsecutiveExportsWithSameServiceProviderShouldNotLeakStateOrHandlers()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddDocumentationExporter();
        await using var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IDocumentationExporterEngine>();

        var demoContractsAssemblyPath = typeof(ISettlementAuditSink).Assembly.Location;
        var demoContractsXmlPath = Path.ChangeExtension(demoContractsAssemblyPath, ".xml");

        var demoLibraryAssemblyPath = typeof(CrossAssemblyAuditReporter).Assembly.Location;
        var demoLibraryXmlPath = Path.ChangeExtension(demoLibraryAssemblyPath, ".xml");

        var outputDirectory1 = Path.Combine(Path.GetTempPath(), $"DocExporter_Run1_{Guid.NewGuid():N}");
        var outputDirectory2 = Path.Combine(Path.GetTempPath(), $"DocExporter_Run2_{Guid.NewGuid():N}");

        try
        {
            // Act 1: Export only DemoContracts
            ExporterOptions firstRunOptions = new()
            {
                AssemblyFilePath = demoContractsAssemblyPath,
                XmlDocumentationFilePath = demoContractsXmlPath,
                OutputDirectoryPath = outputDirectory1
            };
            var firstRunResult = await engine.ExportDocumentationAsync(firstRunOptions);

            // Act 2: Export only DemoLibrary using the same engine and service provider
            ExporterOptions secondRunOptions = new()
            {
                AssemblyFilePath = demoLibraryAssemblyPath,
                XmlDocumentationFilePath = demoLibraryXmlPath,
                OutputDirectoryPath = outputDirectory2
            };
            var secondRunResult = await engine.ExportDocumentationAsync(secondRunOptions);

            // Assert
            _ = firstRunResult.IsSuccess.Should().BeTrue("because the first export must succeed.");
            _ = secondRunResult.IsSuccess.Should().BeTrue("because the second export must succeed.");

            var run1IndexMarkdown = await File.ReadAllTextAsync(Path.Combine(outputDirectory1, "index.md"));
            _ = run1IndexMarkdown.Should().Contain("ISettlementAuditSink")
                .And.NotContain("CrossAssemblyAuditReporter", "because run 1 only exported DemoContracts.");

            var run2IndexMarkdown = await File.ReadAllTextAsync(Path.Combine(outputDirectory2, "index.md"));
            _ = run2IndexMarkdown.Should().Contain("CrossAssemblyAuditReporter")
                .And.NotContain("Auditing/ISettlementAuditSink/index.md", "because run 2 only exported DemoLibrary and local links to DemoContracts must not exist.");
        }
        finally
        {
            if (Directory.Exists(outputDirectory1))
            {
                Directory.Delete(outputDirectory1, recursive: true);
            }

            if (Directory.Exists(outputDirectory2))
            {
                Directory.Delete(outputDirectory2, recursive: true);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ExportDocumentationWithDemoConfigAndAllQualityReportFormatsShouldEmitJsonMarkdownAndSarifReports()
    {
        // Arrange
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "demo", "Documentation.Exporter.Demo.slnx")))
        {
            currentDir = currentDir.Parent;
        }

        _ = currentDir.Should().NotBeNull("because demo/Documentation.Exporter.Demo.slnx must exist in the repository.");
        var demoConfigPath = Path.Combine(currentDir!.FullName, "demo", "doc-exporter.demo.json");
        var tempRootDir = Path.Combine(Path.GetTempPath(), $"DocExporter_QualityFormats_{Guid.NewGuid():N}");

        try
        {
            ServiceCollection services = new();
            _ = services.AddDocumentationExporter();
            await using var provider = services.BuildServiceProvider();
            var engine = provider.GetRequiredService<IDocumentationExporterEngine>();
            var configurationLoader = provider.GetRequiredService<Documentation.Exporter.Core.Configuration.IExporterConfigurationLoader>();

            var loadedDemoOptions = await configurationLoader.LoadAsync(demoConfigPath);
            _ = loadedDemoOptions.StrictLinkValidation.Should().BeTrue("because demo/doc-exporter.demo.json enables strict link validation.");
            _ = loadedDemoOptions.ReportFormat.Should().Be(QualityReportFormat.Json, "because demo/doc-exporter.demo.json defaults quality reporting to Json.");
            _ = loadedDemoOptions.ReportFilePath.Should().NotBeNullOrWhiteSpace("because demo/doc-exporter.demo.json configures reportFilePath.")
                .And.EndWith("quality-report.json");

            var formatsToVerify = new[]
            {
                (Format: QualityReportFormat.Json, FileName: "quality-report.json"),
                (Format: QualityReportFormat.Markdown, FileName: "quality-report.md"),
                (Format: QualityReportFormat.Sarif, FileName: "quality-report.sarif")
            };

            foreach (var (format, fileName) in formatsToVerify)
            {
                var formatOutputDir = Path.Combine(tempRootDir, format.ToString());
                var formatReportPath = Path.Combine(formatOutputDir, fileName);
                var runOptions = new ExporterOptions
                {
                    SolutionFilePath = loadedDemoOptions.SolutionFilePath,
                    SolutionConfiguration = loadedDemoOptions.SolutionConfiguration,
                    ExcludedProjects = [.. loadedDemoOptions.ExcludedProjects],
                    DiagramFormat = DiagramFormat.Mermaid,
                    OutputDirectoryPath = formatOutputDir,
                    ExamplesDirectoryPath = loadedDemoOptions.ExamplesDirectoryPath,
                    OverridesDirectoryPath = loadedDemoOptions.OverridesDirectoryPath,
                    ExternalDocLinkRules = [.. loadedDemoOptions.ExternalDocLinkRules],
                    StrictLinkValidation = true,
                    ReportFilePath = formatReportPath,
                    ReportFormat = format
                };

                // Act
                var result = await engine.ExportDocumentationAsync(runOptions);

                // Assert
                _ = result.IsSuccess.Should().BeTrue($"because strict demo export with {format} quality report format must succeed.");
                _ = result.QualityReport.Should().NotBeNull($"because {format} export must attach QualityReport to ExportResult.");
                _ = result.Diagnostics.Should().BeEquivalentTo(result.QualityReport!.Diagnostics,
                    "because ExportResult.Diagnostics should mirror ExportResult.QualityReport.Diagnostics.");
                _ = File.Exists(formatReportPath).Should().BeTrue($"because Step 10 must persist {fileName} to disk.");

                var rawContent = await File.ReadAllTextAsync(formatReportPath);
                if (format == QualityReportFormat.Json)
                {
                    using var jsonDocument = System.Text.Json.JsonDocument.Parse(rawContent);
                    _ = jsonDocument.RootElement.GetProperty("verifiedLinksCount").GetInt32().Should().BeGreaterThan(0);
                    _ = jsonDocument.RootElement.GetProperty("errorCount").GetInt32().Should().Be(0);
                }
                else if (format == QualityReportFormat.Markdown)
                {
                    _ = rawContent.Should().Contain("# Documentation Quality Report")
                        .And.Contain("| Verified Links |");
                }
                else
                {
                    using var sarifDocument = System.Text.Json.JsonDocument.Parse(rawContent);
                    _ = sarifDocument.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");
                    _ = sarifDocument.RootElement.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver").GetProperty("rules").GetArrayLength()
                        .Should().Be(7);
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempRootDir))
            {
                Directory.Delete(tempRootDir, recursive: true);
            }
        }
    }
}
