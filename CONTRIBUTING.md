# Contributing to Documentation.Exporter

Thank you for contributing to **Documentation.Exporter**! Before opening a pull request, please review our engineering rules and run the local verification checks below.

---

## 1. Prerequisites & Local Setup

- **.NET 11 SDK** (`net11.0` across all projects)
- **PowerShell 5.1+ or 7+ (`pwsh`)** *(optional, for helper scripts in [`scripts/manual/`](scripts/manual))*
- **Docker** *(optional, for Linux container verification via [`Dockerfile`](Dockerfile))*

For a complete repository tour, debugging locator map, and first-PR walkthrough, read the **[Developer Onboarding Guide](docs/developer/onboarding.md)**.

---

## 2. Local Build, Test, & Demo Verification

Run all checks from the repository root before submitting a pull request:

```powershell
# 1. Build in Release (TreatWarningsAsErrors + EnforceCodeStyleInBuild are active)
dotnet build documentation-exporter.slnx --configuration Release

# 2. Run the full xUnit unit & integration test suite
dotnet test documentation-exporter.slnx --configuration Release --no-build

# 3. Check Cobertura line & branch coverage
powershell -ExecutionPolicy Bypass -File scripts/manual/Run-CodeCoverage.ps1 -MinimumLineCoverage 90.0 -MinimumBranchCoverage 85.0

# 4. Run the end-to-end Demo showcase with --strict link & anchor validation
powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport.ps1 -Configuration Release -DiagramFormat Mermaid
powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport.ps1 -Configuration Release -DiagramFormat PlantUml
```

---

## 3. Engineering & Documentation Standards

Every pull request must comply with [`.agents/AGENTS.md`](.agents/AGENTS.md) and our [Coding Standards, Mandates, & DI Setup](docs/developer/coding-standards-and-di.md):

- **SOLID & Dependency Injection First**: Register all extensible components via `IServiceCollection`; never introduce hardcoded default component lists or direct `new ConcreteService(...)` instantiation inside domain logic.
- **Modern C# Conventions**: Use C# 12+ primary constructors, early return guard clauses, modern throw helpers (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`), `DataAnnotations` validation on options/DTOs, and human-friendly multi-character variable/parameter names.
- **Plugin & Abstractions Isolation**: External extension contracts belong in [`Documentation.Exporter.Abstractions`](src/Documentation.Exporter.Abstractions/Documentation.Exporter.Abstractions.csproj) with zero coupling to `Core` or `Cli`. See the [Extensibility Guide](docs/developer/extensibility-guide.md) and [Plugin System Specification](docs/developer/functionalities/plugin-system.md).
- **Categorized Defensive Tests**: Mirror the source directory structure under [`test/Documentation.Exporter.Core.Tests/`](test/Documentation.Exporter.Core.Tests/Documentation.Exporter.Core.Tests.csproj), use explicit `AwesomeAssertions` / `FluentAssertions` checks, and decorate every test with exactly one of `[UnitTest]` or `[IntegrationTest]` (enforced by [`TestCategoryGovernanceTests`](test/Documentation.Exporter.Core.Tests/TestCategories/TestCategoryGovernanceTests.cs)).
- **Synchronized Three-Tier Documentation**: Any change to `src/` or `scripts/` **must** update the corresponding [`docs/architectural/`](docs/architectural/index.md), [`docs/developer/`](docs/developer/index.md), [`docs/user/`](docs/user/index.md), and [`docs/index.md`](docs/index.md) guides in the same PR.

---

## 4. Pull Requests, CI Gate, & Release Labels

1. Fork or branch from `main` and keep changes focused.
2. Fill out the checklist in [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md).
3. All pull requests targeting `main` must pass **`CI Gate`** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)), which verifies:
   - Cross-platform Release builds (`ubuntu-latest` & `windows-latest`) with zero warnings/errors
   - CLI publishing and `plugins/` staging without duplicating `Documentation.Exporter.Abstractions.dll`
   - Full test execution and Cobertura coverage gates
   - End-to-end `--strict` demo export for both Mermaid and PlantUML
4. Apply the matching PR label so [`.github/release.yml`](.github/release.yml) categorizes your change cleanly in generated release notes (`breaking-change`, `feature`, `enhancement`, `bug`, `plugin`, `documentation`, `ci`, `build`, `dependencies`).

---

## 5. Community & Security

- Participation in this project is governed by our [Code of Conduct](CODE_OF_CONDUCT.md).
- Please report security issues privately using the process in [Security Policy](SECURITY.md).
