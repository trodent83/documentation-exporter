## Summary

<!-- Describe what this PR changes and why. -->

Closes #

## Change Type (Aligns with `.github/release.yml` Labels & Auto-SemVer Bump on `main`)

- [ ] `feature` / `enhancement`: New capability or enhancement *(auto **minor** bump)*
- [ ] `bug`: Bug fix *(auto **patch** bump)*
- [ ] `plugin`: Plugin SPI, loader, or built-in plugin change *(auto **patch** bump)*
- [ ] `documentation`: Documentation-only update *(auto **patch** bump, or add `skip-release` to skip releasing)*
- [ ] `ci` / `build` / `dependencies`: Build system, workflows, Docker, or dependency update *(auto **patch** bump, or add `skip-release`)*
- [ ] `breaking-change`: Breaking change to CLI flags, JSON schema, or `Abstractions` contracts *(auto **major** bump)*
- [ ] `skip-release`: Internal/docs/chore merge that should not cut a release on merge to `main`

## Verification Checklist

- [ ] `dotnet build documentation-exporter.slnx --configuration Release` succeeds with **0 warnings** and **0 errors**.
- [ ] `dotnet test documentation-exporter.slnx --configuration Release --no-build` passes, and every new test has `[UnitTest]` or `[IntegrationTest]`.
- [ ] `scripts/manual/Run-CodeCoverage.ps1` passes coverage gates (`Line >= 90%`).
- [ ] `scripts/manual/Run-DemoExport.ps1` passes `--strict` link/anchor validation (if output, CLI, or demo behavior changed).
- [ ] All affected guides under `docs/architectural/`, `docs/developer/`, `docs/user/`, and `docs/index.md` are updated to stay in sync.
- [ ] Appropriate PR label applied for release-note categorization (or `ignore-for-release` for internal-only changes).
