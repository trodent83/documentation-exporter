# Project Rules - Documentation.Exporter

## 1. Documentation Synchronization, Structure, & Depth Mandate

Whenever modifying, adding, or refactoring source code or scripts in this repository:

1. **Always Update Corresponding Documentation**: All corresponding architectural, developer, and user documentation files (`docs/architectural/`, `docs/developer/`, `docs/user/`, and `docs/index.md`) MUST be updated in tandem to accurately reflect the changes.
2. **Never Leave Documentation Out of Sync**: Editing source code or scripts without updating related documentation is strictly prohibited.
3. **Mandatory Three-Tier Documentation Architecture & Formatting Depth**:
   - **`docs/index.md`**: Central documentation portal with audience routing, a visual Mermaid portal map, and complete index tables for every guide.
   - **`docs/architectural/`**: System landscape & boundaries (`index.md`), subsystem capability matrix (`functionality-catalog.md`), C#/.NET feature support matrix (`dotnet-language-support-matrix.md`), runtime sequences & data flow (`component-interactions.md`), and immutable domain contracts & DI lifetimes (`domain-models-and-lifetimes.md`).
   - **`docs/developer/`**: Developer hub (`index.md`), day-1 onboarding (`onboarding.md`), coding mandates & DI composition (`coding-standards-and-di.md`), dedicated subsystem specifications (`docs/developer/functionalities/*.md`), copy-ready extensibility recipes (`extensibility-guide.md`), mirrored testing & `>= 90%` coverage gates (`testing-and-coverage.md`), scripts & containerization (`scripts-and-automation.md`), and `[LoggerMessage]` `EventId` ranges & `.resx` localization (`logging-and-localization.md`).
   - **`docs/user/`**: User guide hub (`index.md`), getting started & demo walkthrough (`getting-started-and-demo.md`), CLI commands & JSON configuration (`cli-and-configuration.md`), XML comment authoring (`xml-documentation-authoring.md`), external examples & sidecars (`examples-overrides-and-external-links.md`), diagrams & assembly insights (`diagrams-and-assembly-insights.md`), MkDocs & CI quality gates (`mkdocs-and-ci-integration.md`), and reusable AI agent commands/rules.
   - **Rich Visual & Traceable Content**: Every documentation guide MUST include visual Mermaid diagrams (`flowchart`, `sequenceDiagram`, `classDiagram`), parameter/contract Markdown tables, GitHub Alert callouts (`> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`), runnable code/CLI examples, and clickable relative Markdown links to owning source (`src/`) and test (`test/`) files.

---

## 2. Defensive Testing Mandate

Whenever creating, modifying, or auditing automated unit and integration tests in test/:

1. **Always Program Defensively**: Never write forgiving tests that silently skip assertions using conditional checks or swallow uncaught exceptions.
2. **Mandatory Fluent Assertions**: Every output, domain state, returned collection, and exception MUST be explicitly asserted using AwesomeAssertions/FluentAssertions (e.g., .Should().NotBeNull("because [descriptive reason]"), .Should().BeEquivalentTo(...)) so tests fail immediately with detailed diagnostics if logic behaves unexpectedly.
3. **No Silent Null Fallbacks**: Tests must fail fast with explanatory error messages describing exactly which component or service failed to initialize or execute.

---

## 3. Early Return Pattern & Human-Friendly Naming Mandate

1. **Early Return Pattern & Guard Clauses**: Code MUST follow the early return pattern—invert preconditions and edge-case checks upfront (`if (!condition) return;`), avoid redundant `else` blocks after `return`/`throw`/`continue`, and keep the primary execution path flat rather than deeply nested conditional blocks.
2. **Human-Friendly Naming Policy**: Single-letter or short 1-3 character abbreviations for variables, method parameters, and lambda arguments are STRICTLY PROHIBITED (e.g. c for category, s for service, m for viewModel, cc for account, ex for exception are not allowed). All names MUST be fully descriptive, self-explanatory, multi-character words (category, service, account, repository, exception, configuration).

---

## 4. Data Annotations & Model Input Validation Mandate

1. **Mandatory DataAnnotations Attributes**: All request models, DTOs, domain options, and configuration classes MUST use System.ComponentModel.DataAnnotations attributes ([Required], [MinLength], [Range], [EmailAddress]) to validate inputs before executing core domain logic.

---

## 5. Modern C# Argument Throw Helpers Mandate

1. **Use Modern Argument Throw Helpers**: When validating parameter preconditions in C# constructors or method contracts, code MUST use modern static throw helpers such as `ArgumentNullException.ThrowIfNull(parameter)` and `ArgumentException.ThrowIfNullOrWhiteSpace(parameter)` instead of verbose manual `if (param == null) throw new ArgumentNullException(...)` conditional blocks.

---

## 6. Primary Constructors Mandate

1. **Prefer Primary Constructors Everywhere Possible**: All C# classes and types with constructor parameters MUST use C# 12+ primary constructor syntax (`public sealed class MyService(IDependency dependency)`) instead of verbose explicit constructor bodies. This is enforced via `.editorconfig` (`csharp_style_prefer_primary_constructors = true:warning` and `dotnet_diagnostic.IDE0290.severity = warning`).

---

## 7. Structured Logging EventId Range Mandate

1. **Mandatory EventId Ranges for [LoggerMessage]**: When adding structured log delegates, code MUST use explicit `EventId` integers within the component's designated range (`100-199` for `AssemblyTypeReflector`, `200-299` for `XmlDocumentationParser`, `300-399` for `DocumentationExporterEngine`, `400-499` for `FileExampleProvider`, `500-599` for `FileDocumentationInjector`, `1000-1099` for CLI). Refer to `docs/developer/logging-and-localization.md` for full component range mappings.

---

## 8. Prohibition of Hardcoded Default Component Lists Mandate

1. **No Hardcoded Default Component Lists**: Methods returning hardcoded lists of components, handlers, renderers, or enrichers (such as `GetDefaultRenderers()` or `GetDefaultHandlers()`) are STRICTLY PROHIBITED in the codebase.
2. **Mandatory Dependency Injection Resolution**: All extensible components (section renderers, XML tag handlers, type enrichers, workflow steps, etc.) MUST be registered exclusively through Dependency Injection (`IServiceCollection`) and injected into consuming services via primary constructors (`IEnumerable<T>`). Internal fallback collections or instantiating default dependencies inside services is forbidden.

---

## 9. SOLID Design Principles Mandate

All classes, interfaces, modules, and extensions MUST adhere to the **SOLID** principles:

1. **Single Responsibility Principle (SRP)**: Every class, workflow step, enricher, handler, and renderer MUST own a single, cohesive responsibility and a single reason to change.
2. **Open/Closed Principle (OCP)**: Extend system behavior by registering new strategy implementations (`IExportWorkflowStep`, `ITypeDocumentationEnricher`, `IMarkdownSectionRenderer`, `IXmlTagHandler`, `IDiagramGenerator`, `INamespaceHandler`, `ICliSubcommand`) rather than modifying existing orchestrators with branching `if`/`switch` chains.
3. **Liskov Substitution Principle (LSP)**: Derived types and interface implementations MUST honor base contracts, invariants, and documented exception behavior without requiring callers to type-check concrete implementations.
4. **Interface Segregation Principle (ISP)**: Keep interfaces focused, minimal, and role-specific rather than introducing monolithic interfaces that force implementers or callers to depend on unused members.
5. **Dependency Inversion Principle (DIP)**: High-level policies and orchestrators MUST depend on abstractions (interfaces such as `IFileSystem`, `ITypeLinkResolver`, `IXmlDocumentationParser`) rather than concrete implementations or static I/O calls.

---

## 10. Dependency Injection First Mandate

1. **Constructor Injection Over Direct Instantiation**: Production services MUST receive all collaborating dependencies as interface abstractions via primary constructor injection; instantiating collaborating services directly via `new ConcreteService(...)` inside domain/application logic is prohibited.
2. **Explicit Composition Root & Lifetimes**: All service registrations and `Singleton` / `Scoped` / `Transient` lifetime policies MUST live in dedicated DI composition roots (`DocumentationExporterServiceCollectionExtensions`, `CliServiceCollectionExtensions`). Avoid the Service Locator anti-pattern (`IServiceProvider.GetService(...)`) inside domain logic—limit explicit scope resolution strictly to lifecycle boundary orchestrators (e.g., `IServiceScopeFactory` inside `DocumentationExporterEngine`).

---

## 11. Automation & PowerShell Script Documentation Mandate

Whenever creating, modifying, or organizing PowerShell (`.ps1`) or automation scripts under `scripts/`:

1. **Directory Separation (`scripts/manual/` vs. Build Scripts)**: Interactive developer and verification scripts MUST be kept in `scripts/manual/` so they remain cleanly separated from automated CI/CD or build-pipeline scripts.
2. **Mandatory Comment-Based Help Header (`.SYNOPSIS`, `.DESCRIPTION`, `.PARAMETER`, `.EXAMPLE`)**: Every `.ps1` script MUST begin with a complete PowerShell comment-based help block (`<# ... #>`) compatible with `Get-Help -Full` that includes:
   - `.SYNOPSIS`: A concise summary of what the script does.
   - `.DESCRIPTION`: A detailed explanation of the execution stages, prerequisites, and exit-code behavior.
   - `.PARAMETER <ParameterName>`: A dedicated entry for **every** script parameter explaining what the parameter controls, its valid values (`[ValidateSet(...)]`), and its default value, so anyone opening the `.ps1` file immediately understands every flag.
   - `.EXAMPLE`: At least 3–4 realistic invocation examples covering default execution and common parameter combinations.
3. **Working-Directory Independence**: Scripts MUST resolve the repository root dynamically from `$PSScriptRoot` and wrap execution in `Push-Location $repoRoot` / `try { ... } finally { Pop-Location }` so they execute reliably from any current directory.
