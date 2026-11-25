# Developer Documentation

This guide explains how every subsystem inside `Documentation.Exporter` works in detail, how engineering mandates and DI guardrails are enforced at compile time, how to run tests and automation scripts, and how to extend any pipeline stage cleanly.

> [!TIP]
> **New to the codebase?** Start with the [Developer Onboarding & New Developer Guide](onboarding.md) for prerequisites, repository navigation, day-1 verification commands, debugging locators, and a step-by-step first PR walkthrough.

---

## Developer Learning Path

```mermaid
flowchart LR
    Onboard["1. Onboarding & Day-1 Commands"] --> Standards["2. Coding Standards & DI Rules"]
    Standards --> DeepDives["3. Dedicated Functionality Specs (1..9)"]
    DeepDives --> Extend["4. Extensibility Recipes"]
    Extend --> Verify["5. Mirrored Tests, Coverage >= 90%, & Demo Export"]
```

---

## Dedicated Functionality Deep-Dive Specifications (`docs/developer/functionalities/`)

Each major capability in `Documentation.Exporter` has its own dedicated specification detailing runtime mechanics, edge-case handling, diagrams, logging `EventId`s, and owning source/test files:

| # | Functionality Deep-Dive | Key Covered Components |
| :--- | :--- | :--- |
| **1** | [CLI Hosting, Subcommands, & JSON Configuration](functionalities/cli-and-configuration.md) | [`CliCommandBuilder`](../../src/Documentation.Exporter.Cli/CliCommandBuilder.cs), `solution`/`assembly`/`config` commands, [`ExporterOptions`](../../src/Documentation.Exporter.Abstractions/Models/ExporterOptions.cs) fallbacks, [`ExporterConfigurationLoader`](../../src/Documentation.Exporter.Core/Configuration/ExporterConfigurationLoader.cs), [`CliPipelineRunner`](../../src/Documentation.Exporter.Cli/CliPipelineRunner.cs) |
| **2** | [Solution & Build Artifact Discovery](functionalities/solution-and-artifact-discovery.md) | [`ExportArtifactDiscoveryService`](../../src/Documentation.Exporter.Core/Parsing/ExportArtifactDiscoveryService.cs), [`SolutionAssemblyResolver`](../../src/Documentation.Exporter.Core/Parsing/SolutionAssemblyResolver.cs), `.sln`/`.slnx` parsing, test heuristics, `build/bin` vs `bin/` probing, `--fail-on-missing` |
| **3** | [Isolated Assembly Loading, Reflection, & Cross-Assembly Indexing](functionalities/assembly-loading-and-reflection.md) | Collectible [`IsolatedAssemblyLoader`](../../src/Documentation.Exporter.Core/Parsing/IsolatedAssemblyLoader.cs), [`AssemblyTypeReflector`](../../src/Documentation.Exporter.Core/Parsing/AssemblyTypeReflector.cs), [`CustomAttributeExtractor`](../../src/Documentation.Exporter.Core/Parsing/CustomAttributeExtractor.cs), [`CSharpSignatureFormatter`](../../src/Documentation.Exporter.Core/Parsing/CSharpSignatureFormatter.cs), cross-assembly nested/derived/extension scanning |
| **4** | [Assembly Reference & Transitive Dependency Tree Analysis](functionalities/assembly-references-and-dependency-tree.md) | [`AssemblyReferenceAnalyzer`](../../src/Documentation.Exporter.Core/Parsing/AssemblyReferenceAnalyzer.cs), IL + `.deps.json` + `.csproj` + [`Directory.Packages.props`](../../Directory.Packages.props) correlation, 4-tier classification, cycle-free 2-pass BFS transitive graph |
| **5** | [XML Documentation Parsing, Tag Transformation, & Link Resolution](functionalities/xml-parsing-and-link-resolution.md) | [`XmlDocumentationParser`](../../src/Documentation.Exporter.Core/Parsing/XmlDocumentationParser.cs), [`XmlTagTransformer`](../../src/Documentation.Exporter.Core/Parsing/XmlTagTransformer.cs), 7 [`IXmlTagHandler`](../../src/Documentation.Exporter.Abstractions/Parsing/Tags/IXmlTagHandler.cs) strategies, [`ConfigureResolutionHandlersStep`](../../src/Documentation.Exporter.Core/Workflow/Steps/ConfigureResolutionHandlersStep.cs), [`INamespaceHandlerProvider`](../../src/Documentation.Exporter.Abstractions/Resolution/INamespaceHandlerProvider.cs), [`TypeDocumentationPaths`](../../src/Documentation.Exporter.Abstractions/Resolution/TypeDocumentationPaths.cs), [`ITypeLinkResolver`](../../src/Documentation.Exporter.Abstractions/Resolution/ITypeLinkResolver.cs), [`MicrosoftLearnReferenceRegistry`](../../src/Documentation.Exporter.Core/Resolution/MicrosoftLearnReferenceRegistry.cs) |
| **6** | [Enrichment Pipeline, External Snippets, & Sidecar Overrides](functionalities/enrichment-snippets-and-overrides.md) | [`TypeDocumentationPipeline`](../../src/Documentation.Exporter.Core/Enrichment/TypeDocumentationPipeline.cs), [`XmlDocumentationEnricher`](../../src/Documentation.Exporter.Core/Enrichment/XmlDocumentationEnricher.cs) (`<inheritdoc/>`), [`FileExampleProvider`](../../src/Documentation.Exporter.Core/Injection/FileExampleProvider.cs), [`FileDocumentationInjector`](../../src/Documentation.Exporter.Core/Injection/FileDocumentationInjector.cs) |
| **7** | [Markdown Emission, Section Renderers, & Single-Diagram Selection](functionalities/markdown-and-diagram-generation.md) | [`TypePageMarkdownGenerator`](../../src/Documentation.Exporter.Core/Generation/TypePageMarkdownGenerator.cs), 16 [`IMarkdownSectionRenderer`](../../src/Documentation.Exporter.Abstractions/Generation/Sections/IMarkdownSectionRenderer.cs) stages, [`MemberDetailMarkdownGenerator`](../../src/Documentation.Exporter.Core/Generation/MemberDetailMarkdownGenerator.cs), [`IndexPageMarkdownGenerator`](../../src/Documentation.Exporter.Core/Generation/IndexPageMarkdownGenerator.cs), [`IDiagramGeneratorSelector`](../../src/Documentation.Exporter.Abstractions/Generation/Diagrams/IDiagramGeneratorSelector.cs), Mermaid & PlantUML strategies |
| **8** | [MkDocs Navigation Merging, Quality Gates, & Localization](functionalities/navigation-quality-gates-and-localization.md) | [`INavigationGenerator`](../../src/Documentation.Exporter.Abstractions/Generation/INavigationGenerator.cs), [`MkDocsNavigationGenerator`](../../src/Documentation.Exporter.Plugins.Navigation.MkDocs/MkDocsNavigationGenerator.cs), [`CalculateDocumentationCoverageStep`](../../src/Documentation.Exporter.Core/Workflow/Steps/CalculateDocumentationCoverageStep.cs), [`IDocumentationQualityAnalyzer`](../../src/Documentation.Exporter.Core/Quality/IDocumentationQualityAnalyzer.cs), [`EnsureOutputDirectoryStep`](../../src/Documentation.Exporter.Core/Workflow/Steps/EnsureOutputDirectoryStep.cs), [`ValidateMarkdownLinksStep`](../../src/Documentation.Exporter.Core/Workflow/Steps/ValidateMarkdownLinksStep.cs), [`EmitQualityReportStep`](../../src/Documentation.Exporter.Core/Workflow/Steps/EmitQualityReportStep.cs), [`IDocumentationQualityReportWriter`](../../src/Documentation.Exporter.Core/Quality/IDocumentationQualityReportWriter.cs), [`IQualityReportFormatter`](../../src/Documentation.Exporter.Abstractions/Quality/IQualityReportFormatter.cs) strategies, `.resx` bundles |
| **9** | [Plugin Architecture, Isolation, & Abstractions Contract](functionalities/plugin-system.md) | [`Documentation.Exporter.Abstractions`](../../src/Documentation.Exporter.Abstractions/Documentation.Exporter.Abstractions.csproj), [`IDocumentationExporterPlugin`](../../src/Documentation.Exporter.Abstractions/Plugins/IDocumentationExporterPlugin.cs), [`DirectoryPluginLoader`](../../src/Documentation.Exporter.Core/Plugins/DirectoryPluginLoader.cs), [`PluginAssemblyLoadContext`](../../src/Documentation.Exporter.Core/Plugins/PluginAssemblyLoadContext.cs), [`StandardDiagramsPlugin`](../../src/Documentation.Exporter.Plugins.Diagrams/StandardDiagramsPlugin.cs), [`MkDocsNavigationPlugin`](../../src/Documentation.Exporter.Plugins.Navigation.MkDocs/MkDocsNavigationPlugin.cs), extensible [`DiagramFormat`](../../src/Documentation.Exporter.Abstractions/Models/DiagramFormat.cs) / [`NavigationFormat`](../../src/Documentation.Exporter.Abstractions/Models/NavigationFormat.cs) / [`QualityReportFormat`](../../src/Documentation.Exporter.Abstractions/Models/QualityReportFormat.cs) |

---

## Core Engineering & Operations Guides

| Guide | Purpose |
| :--- | :--- |
| [Developer Onboarding & New Developer Guide](onboarding.md) | Environment prerequisites, repository tour, day-1 verification commands, debugging map, first-contribution walkthrough, and analyzer/style guardrails |
| [Coding Standards, Mandates, & DI Setup](coding-standards-and-di.md) | Engineering mandates (zero hardcoded component lists, primary constructors, throw helpers, naming, [`IFileSystem`](../../src/Documentation.Exporter.Abstractions/IO/IFileSystem.cs), cross-platform rules) and programmatic DI registration |
| [Extensibility Guide](extensibility-guide.md) | Concrete C# recipes for registering custom workflow steps, enrichers, section renderers, XML tag handlers, diagram generators, namespace handlers, and CLI subcommands |
| [Testing Architecture & Code Coverage](testing-and-coverage.md) | Mirrored test suite structure, defensive `FluentAssertions` patterns, virtual [`OverlayFileSystem`](../../test/Documentation.Exporter.Core.Tests/Parsing/AssemblyReferenceAnalyzerTests.cs) testing, and enforcing the `90%` line/branch coverage bar |
| [Scripts, Build System, & Containerization](scripts-and-automation.md) | Complete reference for [`scripts/manual/Run-CodeCoverage.ps1`](../../scripts/manual/Run-CodeCoverage.ps1), [`scripts/manual/Run-DemoExport.ps1`](../../scripts/manual/Run-DemoExport.ps1), [`scripts/manual/Run-DemoExport-Docker.ps1`](../../scripts/manual/Run-DemoExport-Docker.ps1), [`Dockerfile`](../../Dockerfile), [`Directory.Build.props`](../../Directory.Build.props), MinVer Semantic Versioning ([`src/Directory.Build.props`](../../src/Directory.Build.props)), Central Package Management, and GitHub Actions workflows ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml), [`.github/workflows/release.yml`](../../.github/workflows/release.yml)) |
| [Structured Logging & Localization](logging-and-localization.md) | Component `[LoggerMessage]` `EventId` range map (`100`–`1099`), logger switch, `.resx` resource bundles, and adding new cultures |

Related references:
- [Documentation Portal Home](../index.md)
- [Architecture Documentation](../architectural/index.md)
- [User Documentation](../user/index.md)
