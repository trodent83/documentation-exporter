# Recommended AI Agent Rules for Any C# / .NET Project (`AGENTS.md`)

> **Related Guides:** [User Guide Hub](index.md) · [AI Agent Command for Authoring C# XML Documentation](ai-agent-documentation-command.md) · [Authoring XML Documentation Comments](xml-documentation-authoring.md) · [Coding Standards, Mandates, & DI Setup](../developer/coding-standards-and-di.md)

This guide provides a **project-agnostic, copy-ready AI Agent Rules file** distilled from the engineering mandates used to build and verify `Documentation.Exporter`.

You can drop this rules file directly into **any C# / .NET repository** as:
- **Antigravity / Open Agent Standard**: `.agents/AGENTS.md` (or root `AGENTS.md`)
- **Claude Code**: `CLAUDE.md`
- **Cursor**: `.cursor/rules/csharp-mandates.mdc` or `.cursorrules`
- **GitHub Copilot**: `.github/copilot-instructions.md`

---

## Copy-Ready Universal C# Agent Rules (`AGENTS.md`)

````markdown
# Project Engineering Rules — C# & .NET

All code, tests, and documentation authored or modified in this repository MUST strictly adhere to the following mandates without exception.

---

## 1. Documentation Architecture, Depth, & XML Comment Synchronization Mandate

Whenever modifying, adding, or refactoring source code or scripts in this repository:

1. **Always Update Corresponding Documentation**: All corresponding architectural, developer, and user documentation files (`docs/index.md`, `docs/architectural/`, `docs/developer/`, `docs/user/`) MUST be updated in tandem to accurately reflect the changes.
2. **Never Leave Documentation Out of Sync**: Editing source code or scripts without updating related documentation is strictly prohibited.
3. **Mandatory Three-Tier Documentation Structure & Depth**:
   Maintain a modular, cross-linked documentation portal under `docs/` organized into three audience tiers, with a central `docs/index.md` portal routing readers by role:
   - **`docs/architectural/` (System & Design Architecture)**:
     - `index.md`: System landscape diagram, layer dependency boundaries, design rationales, and architectural trade-offs.
     - `functionality-catalog.md`: Complete capability matrix organized by functional domain, mapping every feature to its owning interfaces, classes, inputs, outputs, and dedicated developer deep-dive.
     - `component-interactions.md`: End-to-end runtime sequence diagrams, pipeline state handoffs, concurrency/parallelism boundaries, and scoped lifetime isolation.
     - `domain-models-and-lifetimes.md`: Immutable domain models (`sealed record`), class diagrams, strong enums, DataAnnotations rules, and `Singleton` / `Scoped` / `Transient` DI lifetime tables.
     - Language or domain feature support matrices mapping supported constructs to code entry points and tests.
   - **`docs/developer/` (Engineering & Subsystem Deep-Dives)**:
     - `index.md`: Developer learning path and index of all engineering guides and subsystem specifications.
     - `onboarding.md`: Day-1 prerequisites, repository anatomy table, build/test/demo verification loop, symptom-based debugging locator map, step-by-step first PR checklist, and common compiler/analyzer traps.
     - `coding-standards-and-di.md`: Core engineering mandates (with prohibited vs. compliant C# code examples) and modular Dependency Injection container composition.
     - `functionalities/*.md`: One dedicated deep-dive specification per functional domain explaining runtime mechanics, edge-case algorithms, Mermaid flowcharts, and owning source/test files.
     - `extensibility-guide.md`: Copy-ready C# implementation recipes and DI registration examples for every extension seam in the architecture.
     - `testing-and-coverage.md`: Mirrored test suite hierarchy, `[UnitTest]` vs. `[IntegrationTest]` categorization, defensive assertion rules, virtual filesystem/mocking patterns, and `>= 90%` line/branch coverage enforcement.
     - `scripts-and-automation.md`: Parameter tables, stage diagrams, and usage examples for all PowerShell/automation scripts, `Dockerfile`, `Directory.Build.props`, and Central Package Management (`Directory.Packages.props`).
     - `logging-and-localization.md`: Source-generated `[LoggerMessage]` `EventId` range allocations by component and `.resx` localization bundle architecture.
   - **`docs/user/` (End-User & Operations Guides)**:
     - `index.md`: End-to-end user workflow diagram, execution mode comparison table, and user guide map.
     - `getting-started-and-demo.md`: Prerequisites, building the application, running the included demo/showcase locally or in Docker, and a guided tour of generated outputs.
     - `cli-and-configuration.md`: Subcommands, full CLI flag reference table, declarative JSON configuration schema, console telemetry, exit-code table, and troubleshooting.
     - Domain authoring, customization, and CI/CD integration guides (`mkdocs`, GitHub Actions, quality gate rule codes).
   - **Mandatory Formatting & Visual Standards Across All Docs**:
     - Include **Mermaid diagrams** (`flowchart`, `sequenceDiagram`, `classDiagram`) to visualize workflows, decision trees, and relationships.
     - Use **Markdown tables** for parameters, options, exit codes, and contract mappings.
     - Use **GitHub Alert callouts** (`> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`, `> [!CAUTION]`) for key takeaways.
     - Include **clickable relative Markdown links** to every referenced source file (`src/...`), test file (`test/...`), script (`scripts/...`), and companion guide.
4. **Complete Public XML Documentation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`)**:
   - Every public interface, class, record, struct, enum, delegate, and member MUST carry complete C# XML documentation comments (`<summary>`, `<param>`, `<typeparam>`, `<returns>`/`<value>`, `<exception cref="...">`, and `<remarks>`/`<example>` on contracts) so automated documentation generators (such as `Documentation.Exporter.Cli`) emit complete, warning-free API reference pages.
   - Concrete implementations of documented interfaces SHOULD use `<inheritdoc />` and add implementation-specific `<remarks>` or `<example>` blocks when behavior warrants additional detail.

---

## 2. Defensive Testing Mandate

Whenever creating, modifying, or auditing automated unit and integration tests in `test/`:

1. **Always Program Defensively**: Never write forgiving tests that silently skip assertions using conditional checks (`if (result != null)`) or swallow uncaught exceptions.
2. **Mandatory Fluent Assertions**: Every output, domain state, returned collection, and exception MUST be explicitly asserted using FluentAssertions / AwesomeAssertions (for example, `.Should().NotBeNull("because [descriptive reason]")`, `.Should().BeEquivalentTo(...)`) so tests fail immediately with detailed diagnostics if logic behaves unexpectedly.
3. **No Silent Null Fallbacks**: Tests must fail fast with explanatory failure messages describing exactly which component or service failed to initialize or execute.
4. **Explicit Test Categorization**: Every test class or test method MUST be categorized (for example, `[UnitTest]` or `[IntegrationTest]`) so CI pipelines can filter fast unit tests vs. end-to-end integration tests deterministically.

---

## 3. Early Return Pattern & Human-Friendly Naming Mandate

1. **Early Return Pattern & Guard Clauses**: Code MUST follow the early return pattern—invert preconditions and edge-case checks upfront (`if (!condition) { return; }`), avoid redundant `else` blocks after `return`/`throw`/`continue`, and keep the primary execution path flat rather than deeply nested conditional blocks.
2. **Human-Friendly Naming Policy**: Single-letter or short 1–3 character abbreviations for variables, method parameters, and lambda arguments are **STRICTLY PROHIBITED** (for example, `c` for `category`, `s` for `service`, `m` for `viewModel`, `ex` for `exception`, `fs` for `fileSystem`, `sb` for `stringBuilder` are not allowed). All names MUST be fully descriptive, self-explanatory, multi-character words (`category`, `service`, `account`, `repository`, `exception`, `configuration`, `fileSystem`, `stringBuilder`).

---

## 4. Immutable Domain Models & DataAnnotations Input Validation Mandate

1. **Mandatory DataAnnotations Attributes**: All request models, DTOs, domain options, and configuration classes MUST use `System.ComponentModel.DataAnnotations` attributes (`[Required]`, `[MinLength]`, `[Range]`, `[EnumDataType]`, `[EmailAddress]`) to validate inputs before executing core domain logic.
2. **Immutable Records by Default**: Domain data models and value objects SHOULD be declared as `public sealed record` types with `init`-only properties and read-only collection contracts (`IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`), using non-destructive `with` expressions for state transformations.

---

## 5. Modern C# Argument Throw Helpers Mandate

1. **Use Modern Argument Throw Helpers**: When validating parameter preconditions in C# constructors or method contracts, code MUST use modern static throw helpers such as `ArgumentNullException.ThrowIfNull(parameter)`, `ArgumentException.ThrowIfNullOrWhiteSpace(parameter)`, and `ArgumentOutOfRangeException.ThrowIfNegative(parameter)` instead of verbose manual `if (parameter == null) throw new ArgumentNullException(...)` conditional blocks.

---

## 6. Primary Constructors Mandate

1. **Prefer Primary Constructors Everywhere Possible**: All C# classes and types with constructor parameters MUST use C# 12+ primary constructor syntax (`public sealed class MyService(IDependency dependency)`) instead of verbose explicit constructor bodies. Enforce this in `.editorconfig` via:
   - `csharp_style_prefer_primary_constructors = true:warning`
   - `dotnet_diagnostic.IDE0290.severity = warning`

---

## 7. Structured Logging `EventId` Ranges & Localization Mandate

1. **Source-Generated `[LoggerMessage]` with Explicit `EventId` Ranges**: All production logging MUST use compile-time source-generated `[LoggerMessage(EventId = ..., Level = ..., Message = "...")]` partial methods rather than runtime string interpolation (`logger.LogInformation($"...")`).
2. **Dedicated Component `EventId` Ranges**: Assign non-overlapping `EventId` integer ranges per subsystem (for example, `100–199` for Data/Loading, `200–299` for Parsing, `300–399` for Core Engine, `800–899` for Workflow Steps, `1000–1099` for CLI/Host) and document the allocation table in developer documentation.
3. **Resource-Backed Localization (`.resx`)**: Externalize user-facing error messages, validation messages, and CLI/UI text into strongly-typed `.resx` resource files rather than hardcoding magic strings in domain logic.

---

## 8. Prohibition of Hardcoded Default Component Lists Mandate

1. **No Hardcoded Default Component Lists**: Methods returning hardcoded lists of components, handlers, renderers, validators, or enrichers (such as `GetDefaultRenderers()` or `GetDefaultHandlers()`) are **STRICTLY PROHIBITED** in the codebase.
2. **Mandatory Dependency Injection Resolution**: All extensible components (pipeline steps, handlers, strategies, renderers, enrichers) MUST be registered exclusively through Dependency Injection (`IServiceCollection`) and injected into consuming services via primary constructors (`IEnumerable<TStrategy>`). Internal fallback collections or instantiating default dependencies inside services is forbidden.

---

## 9. SOLID Design Principles Mandate

All classes, interfaces, modules, and extensions MUST adhere to the **SOLID** principles:

1. **Single Responsibility Principle (SRP)**: Every class, workflow step, enricher, handler, and renderer MUST own a single, cohesive responsibility and a single reason to change.
2. **Open/Closed Principle (OCP)**: Extend system behavior by registering new strategy implementations (`IWorkflowStep`, `IHandler`, `IRenderer`, `IValidator`) rather than modifying existing orchestrators with branching `if`/`switch` chains.
3. **Liskov Substitution Principle (LSP)**: Derived types and interface implementations MUST honor base contracts, invariants, and documented exception behavior without requiring callers to type-check or downcast concrete implementations (`if (service is ConcreteService)` is prohibited).
4. **Interface Segregation Principle (ISP)**: Keep interfaces focused, minimal, and role-specific rather than introducing monolithic interfaces that force implementers or callers to depend on unused members.
5. **Dependency Inversion Principle (DIP)**: High-level policies and orchestrators MUST depend on abstractions (interfaces such as `IFileSystem`, `IClock`, `IRepository`) rather than concrete implementations or static I/O calls (`System.IO.File`, `System.IO.Directory`).

---

## 10. Dependency Injection First & Cross-Platform Discipline Mandate

1. **Constructor Injection Over Direct Instantiation**: Production services MUST receive all collaborating dependencies as interface abstractions via primary constructor injection; instantiating collaborating services directly via `new ConcreteService(...)` inside domain/application logic is prohibited.
2. **Explicit Composition Root & Lifetimes**: All service registrations and `Singleton` / `Scoped` / `Transient` lifetime policies MUST live in dedicated DI composition roots (`*ServiceCollectionExtensions`). Avoid the Service Locator anti-pattern (`IServiceProvider.GetService(...)`) inside domain logic—limit explicit scope resolution strictly to lifecycle boundary orchestrators (`IServiceScopeFactory`).
3. **Zero Warnings & Cross-Platform I/O**:
   - Maintain `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` in `Directory.Build.props`.
   - Abstract file and environment operations behind interfaces (`IFileSystem`) and use `Path.Combine` / forward-slash-normalized relative URLs so all code runs identically on Windows, Linux, and Docker containers.

---

## 11. Automation & PowerShell Script Documentation Mandate

Whenever creating, modifying, or organizing PowerShell (`.ps1`) or automation scripts under `scripts/`:

1. **Directory Separation (`scripts/manual/` vs. Build/CI Scripts)**:
   - Place developer-invoked and manual verification scripts under `scripts/manual/` so they remain cleanly separated from automated CI/CD or build-pipeline scripts (such as `scripts/build/` or `scripts/ci/`).
2. **Mandatory In-File Comment-Based Help (`.SYNOPSIS`, `.DESCRIPTION`, `.PARAMETER`, `.EXAMPLE`)**:
   - Every `.ps1` script MUST start with a comprehensive PowerShell comment-based help block (`<# ... #>`) compatible with `Get-Help -Full` so anyone opening the script file immediately understands how to call it:
     - `.SYNOPSIS`: A concise one-sentence summary of the script's purpose.
     - `.DESCRIPTION`: A step-by-step explanation of the stages executed by the script, required prerequisites, and exit-code behavior.
     - `.PARAMETER <ParameterName>`: A dedicated section for **every** parameter in `param (...)` explaining what the property represents, its valid values (`[ValidateSet(...)]`), and its default value.
     - `.EXAMPLE`: Multiple copy-ready invocation examples demonstrating default usage and common parameter combinations.
3. **Working-Directory Independence**:
   - Every script MUST resolve the repository root dynamically from `$PSScriptRoot` and wrap execution in `Push-Location $repoRoot` / `try { ... } finally { Pop-Location }` so it runs reliably from any directory.
````
