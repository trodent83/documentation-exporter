# Security Policy

## Supported Versions

Security fixes and patches are provided for the latest tagged `v*.*.*` release and the `main` development branch:

| Version | Supported |
| :--- | :--- |
| Latest `v*.*.*` release | Yes |
| `main` (pre-release) | Yes |
| Older releases | No (please upgrade to the latest release) |

---

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**

Report vulnerabilities privately through GitHub:

1. Open the repository's **Security** tab on GitHub.
2. Select **Advisories** → **Report a vulnerability** (Private Vulnerability Reporting).
3. Include:
   - A clear description of the vulnerability and affected component (CLI, Core, Abstractions, or a specific plugin)
   - Minimal reproduction steps or sample input (`.dll`, `.xml`, `.json`, or `.md`)
   - Potential impact and any suggested remediation

We aim to acknowledge reports within 5 business days and coordinate a fix and release advisory before public disclosure.

---

## Operational Security Considerations

- **Inspecting Untrusted Assemblies**: `Documentation.Exporter` loads target `.dll` files via `AssemblyLoadContext` (`IsolatedAssemblyLoader`) and discovers plugins via `PluginAssemblyLoadContext` (`DirectoryPluginLoader`). Only run the exporter or load `--plugins-dir` assemblies from trusted builds, or execute inside an isolated container using [`Dockerfile`](Dockerfile) with read-only source mounts.
- **Safe Output Cleanup (`--clean`)**: `--clean` tracks generated output paths via `.docexporter-manifest.json` (falling back to `.md`/`.puml` cleanup if no manifest exists) so hand-authored assets are preserved, but you should always point `--output` to a dedicated documentation directory.
