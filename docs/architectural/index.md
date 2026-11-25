# Architecture Overview

`Documentation.Exporter` is a modular .NET 11 contracts base ([`Documentation.Exporter.Abstractions`](../../src/Documentation.Exporter.Abstractions/Documentation.Exporter.Abstractions.csproj)), core engine ([`Documentation.Exporter.Core`](../../src/Documentation.Exporter.Core/Documentation.Exporter.Core.csproj)), dynamic plugin bundles ([`Documentation.Exporter.Plugins.Diagrams`](../../src/Documentation.Exporter.Plugins.Diagrams/Documentation.Exporter.Plugins.Diagrams.csproj) and [`Documentation.Exporter.Plugins.Navigation.MkDocs`](../../src/Documentation.Exporter.Plugins.Navigation.MkDocs/Documentation.Exporter.Plugins.Navigation.MkDocs.csproj)), and console host ([`Documentation.Exporter.Cli`](../../src/Documentation.Exporter.Cli/Documentation.Exporter.Cli.csproj)) engineered around **sequential workflow orchestration**, **immutable domain modeling**, **non-mutating enrichment pipelines**, **strategy-driven rendering**, **plugin isolation via `AssemblyLoadContext`**, and **strict dependency injection**.

Rather than tightly coupling file I/O, reflection, XML parsing, or rendering formats inside monolithic classes, every subsystem exposes fine-grained interfaces composed through `Microsoft.Extensions.DependencyInjection` and executed within per-export `AsyncServiceScope` boundaries with source-generated `[LoggerMessage]` telemetry. This design makes every component independently testable in memory and extensible without modifying Core files.

---

## System Landscape

At runtime, user inputs flow downward from the CLI host (or an embedded caller) into an isolated Core engine scope. Discovery and loading build a clean CLR reflection snapshot paired with assembly reference/dependency metadata; transformation and enrichment enrich immutable records; and generation strategies emit Markdown, diagrams, and MkDocs navigation through an abstracted filesystem:

```mermaid
flowchart TD
    subgraph Inputs["Inputs & Configuration"]
        SolutionFile["C# Solution (.sln / .slnx)"]
        AssemblyFiles["Compiled Assemblies (.dll) & XML Docs (.xml)"]
        ConfigFile["Declarative Config (doc-exporter.json)"]
        Extras["External Examples (.cs) & Sidecars (.md)"]
        BaseNav["Existing MkDocs Nav (mkdocs.yml / mkdocs.nav.yml)"]
    end

    subgraph Host["CLI Host (Documentation.Exporter.Cli)"]
        CliEntry["Program & CliPluginDirectoryResolver (--plugins-dir)"]
        CliDI["CliServiceCollectionExtensions"]
        CmdBuilder["ICliCommandBuilder & ICliSubcommand (solution / assembly / config)"]
        SharedOpts["ISharedCliOptions & IConsoleOutput"]
        Runner["ICliPipelineRunner & CliLoggingLevelSwitch"]
    end

    subgraph PluginLayer["Plugin Architecture (Documentation.Exporter.Abstractions + plugins/)"]
        Abstractions["Documentation.Exporter.Abstractions (IDocumentationExporterPlugin, Subsystem SPI, Diagram/Nav/ReportFormats, Models)"]
        PluginLoader["IPluginLoader (DirectoryPluginLoader + PluginAssemblyLoadContext)"]
        DiagPlugin["Documentation.Exporter.Plugins.Diagrams (Mermaid & PlantUML Generators)"]
        NavPlugin["Documentation.Exporter.Plugins.Navigation.MkDocs (MkDocs YAML INavigationGenerator)"]
    end

    subgraph CoreInfra["Core Engine & Workflow (Documentation.Exporter.Core)"]
        Engine["IDocumentationExporterEngine (AsyncServiceScope)"]
        Workflow["IExportWorkflow (10 Sequential IExportWorkflowStep Stages)"]
        Context["ExportWorkflowContext (Shared Mutable Stage State)"]
        FileSys["IFileSystem (PhysicalFileSystem)"]
    end

    subgraph Analysis["Discovery, Loading, Analysis & Enrichment"]
        Discovery["ISolutionAssemblyResolver & IExportArtifactDiscoveryService"]
        RefAnalyzer["IAssemblyReferenceAnalyzer (References & Dependency Trees)"]
        LoaderReflect["IAssemblyLoader (Collectible ALC) & IAssemblyTypeReflector"]
        XmlStack["IXmlDocumentationParser & IXmlTagTransformer (7 IXmlTagHandler)"]
        Links["ITypeLinkResolver (INamespaceHandler + IMemberCrefRouteIndexBuilder) & IMicrosoftLearnReferenceRegistry"]
        Enrichers["ITypeDocumentationPipeline (3 ITypeDocumentationEnricher Stages)"]
    end

    subgraph Generation["Generation, Single-Diagram, & Quality Reporting"]
        TypeGen["ITypePageMarkdownGenerator (16 IMarkdownSectionRenderer Strategies)"]
        MemberGen["IMemberDetailMarkdownGenerator & Formatting Helpers"]
        IndexGen["IIndexPageMarkdownGenerator & INavigationGenerator (via plugins/)"]
        DiagramSel["IDiagramGeneratorSelector (Preferred Format + Fallback)"]
        DiagramStrats["Mermaid & PlantUML Strategies (Type, Architecture, Dependency Tree)"]
        QualitySvc["IDocumentationQualityAnalyzer & IDocumentationQualityReportWriter (Json / Markdown / Sarif)"]
    end

    subgraph Outputs["Generated Artifacts"]
        MdPages["Root, Assembly, Type, & Member index.md / .md Pages"]
        PumlFiles["Standalone architecture.puml (When PlantUML Selected)"]
        NavOut["Generated or In-Place Updated mkdocs.nav.yml"]
        ReportOut["Quality & Diagnostics Report (quality-report.json / .md / .sarif)"]
    end

    Inputs --> Host
    ConfigFile --> Host
    CliEntry --> CliDI
    CliDI --> PluginLoader
    PluginLoader -->|Loads Isolated ALC| DiagPlugin
    PluginLoader -->|Loads Isolated ALC| NavPlugin
    DiagPlugin -.->|Implements SPI| Abstractions
    NavPlugin -.->|Implements SPI| Abstractions
    CliDI --> CmdBuilder
    CmdBuilder --> SharedOpts
    CmdBuilder --> Runner
    Runner --> Engine

    Engine --> Workflow
    Workflow --> Context
    Workflow --> Analysis
    Workflow --> Generation
    CoreInfra -.-> FileSys

    Discovery --> LoaderReflect
    Discovery --> RefAnalyzer
    LoaderReflect --> Enrichers
    XmlStack --> Enrichers
    Links --> XmlStack
    Links --> LoaderReflect
    Extras --> Enrichers

    Enrichers --> TypeGen
    Enrichers --> MemberGen
    Enrichers --> IndexGen
    RefAnalyzer --> IndexGen
    TypeGen --> DiagramSel
    IndexGen --> DiagramSel
    DiagramSel --> DiagramStrats

    TypeGen --> MdPages
    MemberGen --> MdPages
    IndexGen --> MdPages
    DiagramStrats --> PumlFiles
    BaseNav --> IndexGen
    IndexGen --> NavOut
```

---

## Layered Dependency & Isolation Boundaries

Dependencies point strictly inward toward [`Documentation.Exporter.Abstractions`](../../src/Documentation.Exporter.Abstractions/Documentation.Exporter.Abstractions.csproj), while plugin implementations ([`Documentation.Exporter.Plugins.Diagrams`](../../src/Documentation.Exporter.Plugins.Diagrams/Documentation.Exporter.Plugins.Diagrams.csproj), [`Documentation.Exporter.Plugins.Navigation.MkDocs`](../../src/Documentation.Exporter.Plugins.Navigation.MkDocs/Documentation.Exporter.Plugins.Navigation.MkDocs.csproj), and external extensions) reference **only** `Abstractions` without coupling to Core or CLI:

```mermaid
flowchart LR
    CLI["Documentation.Exporter.Cli (Commands, --plugins-dir, plugins/ Staging, Exit Codes)"] --> CoreEngine["Documentation.Exporter.Core (Engine, Plugin Loader, Workflow Steps, Enrichers, Generators)"]
    CLI --> Abstractions["Documentation.Exporter.Abstractions (Plugin SPI, Subsystem Interfaces, Models, Formats, Validation .resx)"]
    CoreEngine --> Abstractions
    DiagPlugin["Documentation.Exporter.Plugins.Diagrams (Mermaid & PlantUML Plugin)"] -->|"Private=false (Shared Default ALC Identity)"| Abstractions
    NavPlugin["Documentation.Exporter.Plugins.Navigation.MkDocs (MkDocs Navigation Plugin)"] -->|"Private=false (Shared Default ALC Identity)"| Abstractions
    Abstractions --> NetRuntime[".NET 11 BCL (AssemblyLoadContext, XDocument, DataAnnotations)"]
    TestProj["Documentation.Exporter.Core.Tests (xUnit, AwesomeAssertions/FluentAssertions, Moq)"] -.-> CLI
    TestProj -.-> CoreEngine
    TestProj -.-> Abstractions
    TestProj -.-> DiagPlugin
    TestProj -.-> NavPlugin
```

Because built-in plugins reference `Abstractions` with `<Private>false</Private>` and `<ExcludeAssets>runtime</ExcludeAssets>`, [`PluginAssemblyLoadContext`](../../src/Documentation.Exporter.Core/Plugins/PluginAssemblyLoadContext.cs) shares `Abstractions` and `Microsoft.Extensions.*` contracts from `AssemblyLoadContext.Default` while isolating plugin-private assemblies inside per-plugin load contexts.

---

## Key Architectural Principles & Rationale

1. **Zero Hardcoded Component Lists & Subdirectory Plugin Discovery**: All extensible strategies (workflow steps, type enrichers, XML tag handlers, Markdown section renderers, diagram generators, navigation generators, namespace handler providers, quality report formatters, CLI subcommands) are registered exclusively in `IServiceCollection` (via [`DocumentationExporterServiceCollectionExtensions`](../../src/Documentation.Exporter.Core/DependencyInjection/DocumentationExporterServiceCollectionExtensions.cs), [`CliServiceCollectionExtensions`](../../src/Documentation.Exporter.Cli/CliServiceCollectionExtensions.cs), and dynamically loaded [`IDocumentationExporterPlugin`](../../src/Documentation.Exporter.Abstractions/Plugins/IDocumentationExporterPlugin.cs) assemblies). Concrete Mermaid & PlantUML emitters live in [`Documentation.Exporter.Plugins.Diagrams`](../../src/Documentation.Exporter.Plugins.Diagrams/Documentation.Exporter.Plugins.Diagrams.csproj), while MkDocs YAML navigation generation lives in [`Documentation.Exporter.Plugins.Navigation.MkDocs`](../../src/Documentation.Exporter.Plugins.Navigation.MkDocs/Documentation.Exporter.Plugins.Navigation.MkDocs.csproj)—both discovered automatically from `plugins/` (or `--plugins-dir`) via [`DirectoryPluginLoader`](../../src/Documentation.Exporter.Core/Plugins/DirectoryPluginLoader.cs) and [`PluginAssemblyLoadContext`](../../src/Documentation.Exporter.Core/Plugins/PluginAssemblyLoadContext.cs). *Why:* New diagram syntaxes (such as D2), site navigation formats (such as Docusaurus or DocFX), custom link resolvers, and custom quality reports plug in cleanly without recompiling Core or CLI.
2. **Scoped State Isolation**: Every invocation of [`IDocumentationExporterEngine.ExportDocumentationAsync`](../../src/Documentation.Exporter.Core/IDocumentationExporterEngine.cs) creates a fresh `AsyncServiceScope` via `IServiceScopeFactory`. Stateful per-run components ([`IsolatedAssemblyLoader`](../../src/Documentation.Exporter.Core/Parsing/IsolatedAssemblyLoader.cs), [`XmlDocumentationParser`](../../src/Documentation.Exporter.Core/Parsing/XmlDocumentationParser.cs), [`TypeLinkResolver`](../../src/Documentation.Exporter.Core/Resolution/TypeLinkResolver.cs)) are `Scoped` and cleaned/unloaded per run. *Why:* Back-to-back exports (such as multi-test suites or long-running build workers) never leak XML dictionaries, namespace rules, or assembly handles across runs.
3. **Immutable Domain Records**: Reflected and enriched metadata flows downstream as immutable `sealed record` types in [`Documentation.Exporter.Abstractions`](../../src/Documentation.Exporter.Abstractions/Documentation.Exporter.Abstractions.csproj) ([`TypeDocumentationModel`](../../src/Documentation.Exporter.Abstractions/Models/TypeDocumentationModel.cs), [`MemberDocumentationModel`](../../src/Documentation.Exporter.Abstractions/Models/MemberDocumentationModel.cs), [`AssemblyReferenceModel`](../../src/Documentation.Exporter.Abstractions/Models/AssemblyReferenceModel.cs), [`AssemblyDependencyEdgeModel`](../../src/Documentation.Exporter.Abstractions/Models/AssemblyDependencyEdgeModel.cs), [`PluginDescriptor`](../../src/Documentation.Exporter.Abstractions/Plugins/PluginDescriptor.cs)) updated via non-destructive `with` expressions. *Why:* Step 5 and Step 8 process types concurrently across threads using `Parallel.ForEachAsync` with zero shared-state race conditions.
4. **Collectible Assembly Loading & File System Abstraction**: Target `.dll` binaries are loaded in a collectible `AssemblyLoadContext` ([`IsolatedAssemblyLoader`](../../src/Documentation.Exporter.Core/Parsing/IsolatedAssemblyLoader.cs)) to release OS file locks on teardown and isolate dependency graphs. All disk access flows through [`IFileSystem`](../../src/Documentation.Exporter.Abstractions/IO/IFileSystem.cs) ([`PhysicalFileSystem`](../../src/Documentation.Exporter.Core/IO/PhysicalFileSystem.cs)) so tests can mock or overlay `.csproj`, `.deps.json`, `.xml`, and Markdown files safely in memory.
5. **Extensible Single-Diagram Strategy with Controlled Fallback**: Consumers never emit duplicate diagram blocks in competing syntaxes for the same artifact. Instead, [`IDiagramGeneratorSelector`](../../src/Documentation.Exporter.Abstractions/Generation/Diagrams/IDiagramGeneratorSelector.cs) selects at most one [`IDiagramGenerator`](../../src/Documentation.Exporter.Abstractions/Generation/Diagrams/IDiagramGenerator.cs) matching the extensible [`DiagramFormat`](../../src/Documentation.Exporter.Abstractions/Models/DiagramFormat.cs) (`Mermaid` by default, `PlantUml`, `None`, or custom plugin format names via `DiagramFormat.Custom(...)`) and falls back to another registered generator only when `CanGenerate(...)` returns `false`.
6. **Localized Feedback & Precondition Validation**: Input contracts use localized `System.ComponentModel.DataAnnotations` attributes ([`ValidationMessagesResources`](../../src/Documentation.Exporter.Abstractions/Resources/ValidationMessagesResources.cs)), argument preconditions use `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`, and all user-facing headings, CLI prompts, and log messages are `.resx`-backed ([`CliCommandResources`](../../src/Documentation.Exporter.Core/Resources/CliCommandResources.resx), [`ExecutionMessagesResources`](../../src/Documentation.Exporter.Core/Resources/ExecutionMessagesResources.resx), [`MarkdownGenerationResources`](../../src/Documentation.Exporter.Core/Resources/MarkdownGenerationResources.resx), [`ValidationMessagesResources`](../../src/Documentation.Exporter.Abstractions/Resources/ValidationMessagesResources.resx)).

---

## Architecture Documentation Sections

| Section | Focus |
| :--- | :--- |
| [Functionality Catalog](functionality-catalog.md) | Complete capability matrix across all 9 functional domains with links to dedicated per-functionality deep-dives |
| [.NET & C# Language Support Matrix](dotnet-language-support-matrix.md) | Exhaustive inventory of supported .NET & C# 1–14 constructs linked directly to their functional descriptions, code entry points, and known exclusions |
| [Component Interactions & Data Flow](component-interactions.md) | Sequence diagrams, plugin discovery handshake, 10-step [`ExportWorkflowContext`](../../src/Documentation.Exporter.Abstractions/Workflow/ExportWorkflowContext.cs) state transitions, parallel enrichment fan-out, and scoped isolation |
| [Domain Models, Strong Enums, & DI Lifetimes](domain-models-and-lifetimes.md) | Reference of immutable records, relationship class diagram, strong enums, DataAnnotations validation, and DI lifetimes |
| [Developer Documentation](../developer/index.md) | Dedicated per-functionality deep-dives, automation scripts guide, coding standards, extensibility recipes, and onboarding |
| [User Documentation](../user/index.md) | Task-oriented guides for running the CLI, authoring XML docs, configuring diagrams, and integrating with MkDocs |
