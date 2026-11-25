<#
.SYNOPSIS
    Builds the demo showcase C# solution (demo/Documentation.Exporter.Demo.slnx) and runs the standalone Documentation.Exporter.Cli console executable against it, emitting unified multi-assembly Markdown documentation into .demo-output/.

.DESCRIPTION
    Performs a complete three-stage local end-to-end verification of Documentation.Exporter:
      1. Compiles `demo/Documentation.Exporter.Demo.slnx` and `src/Documentation.Exporter.Cli/Documentation.Exporter.Cli.csproj` under the selected build configuration.
      2. Cleans the target output directory, seeds `mkdocs.nav.yml` from `demo/mkdocs.base.nav.yml`, and runs the compiled `Documentation.Exporter.Cli` binary in `solution` mode with `--strict` relative-link and anchor validation, external `.cs` examples, `.md` sidecar overrides, and JSON quality report emission (`quality-report.json`).
      3. Lists every generated Markdown file and the quality report with their byte sizes.
    Automatically resolves the repository root from `scripts/manual/` so it can be run from any working directory.

.PARAMETER Configuration
    The MSBuild build configuration used to compile the demo solution, compile the CLI project, and locate the output assemblies under `build/bin/`.
    Valid values: "Debug" (default) or "Release".

.PARAMETER OutputDir
    The target directory path (relative to the repository root) where generated Markdown files, `mkdocs.nav.yml`, and `quality-report.json` will be written.
    Note: Any existing contents in this directory are deleted prior to export. Defaults to ".demo-output".

.PARAMETER LogLevel
    The minimum console logging verbosity passed to `--log-level` on `Documentation.Exporter.Cli`.
    Common values: "Trace", "Debug", "Information" (default), "Warning", "Error", "Critical", "None".

.PARAMETER DiagramFormat
    The preferred diagram syntax passed to `--diagram-format` for type relationship diagrams, assembly dependency trees, and architecture diagrams.
    Valid values:
      - "Mermaid"  : Emits inline ```mermaid fenced code blocks (default).
      - "PlantUml" : Emits inline ```plantuml fenced code blocks and standalone `architecture.puml` files.
      - "None"     : Suppresses diagram generation across all pages.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport.ps1
    Builds the demo solution and CLI in Debug mode and exports the showcase documentation with Mermaid diagrams into `.demo-output/`.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport.ps1 -Configuration Release -DiagramFormat PlantUml
    Builds in Release mode and exports the showcase documentation using PlantUML diagrams (including standalone `architecture.puml` files).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport.ps1 -OutputDir ".demo-output-debug" -LogLevel Debug
    Exports the demo documentation into `.demo-output-debug/` with verbose Debug console telemetry.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport.ps1 -DiagramFormat None -LogLevel Warning
    Exports the demo documentation without diagrams and prints only warnings or errors to the console.
#>
[CmdletBinding()]
param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$OutputDir = ".demo-output",
    [string]$LogLevel = "Information",
    [ValidateSet("Mermaid", "PlantUml", "None")]
    [string]$DiagramFormat = "Mermaid"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot

try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host " Running Documentation.Exporter Demo Solution Showcase" -ForegroundColor Cyan
    Write-Host " Configuration : $Configuration" -ForegroundColor Cyan
    Write-Host " Diagram Format : $DiagramFormat" -ForegroundColor Cyan
    Write-Host " Output Folder : $OutputDir" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    $demoSolution = Join-Path $repoRoot "demo/Documentation.Exporter.Demo.slnx"
    $cliProject = Join-Path $repoRoot "src/Documentation.Exporter.Cli/Documentation.Exporter.Cli.csproj"

    Write-Host "`n[1/3] Building Demo Solution and Documentation Exporter CLI..." -ForegroundColor Yellow
    dotnet build $demoSolution -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build Demo solution ($demoSolution)."
    }

    dotnet build $cliProject -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build Documentation.Exporter.Cli project."
    }

    $configFolder = $Configuration.ToLowerInvariant()
    $cliBuildDir = Join-Path $repoRoot "build/bin/Documentation.Exporter.Cli/$configFolder"
    $cliExecutableName = if ($IsWindows -or $env:OS -eq "Windows_NT") { "Documentation.Exporter.Cli.exe" } else { "Documentation.Exporter.Cli" }
    $cliExecutablePath = Join-Path $cliBuildDir $cliExecutableName
    $examplesDir = Join-Path $repoRoot "demo/Documentation.Exporter.DemoLibrary/docs_extra/examples"
    $overridesDir = Join-Path $repoRoot "demo/Documentation.Exporter.DemoLibrary/docs_extra/overrides"
    $resolvedOutputDir = Join-Path $repoRoot $OutputDir

    if (Test-Path $resolvedOutputDir) {
        Remove-Item -Path $resolvedOutputDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resolvedOutputDir -Force | Out-Null
    $baseNavTemplate = Join-Path $repoRoot "demo/mkdocs.base.nav.yml"
    $targetNavFile = Join-Path $resolvedOutputDir "mkdocs.nav.yml"
    $targetReportFile = Join-Path $resolvedOutputDir "quality-report.json"
    if (Test-Path $baseNavTemplate) {
        Copy-Item -Path $baseNavTemplate -Destination $targetNavFile -Force
    }

    Write-Host "`n[2/3] Executing $cliExecutableName solution against Demo.slnx..." -ForegroundColor Yellow
    & $cliExecutablePath solution `
        --solution $demoSolution `
        --configuration $Configuration `
        --exclude "*.Tests" "*.Benchmarks" `
        --diagram-format $DiagramFormat `
        --strict `
        --output $resolvedOutputDir `
        --examples-dir $examplesDir `
        --overrides-dir $overridesDir `
        --nav-file $targetNavFile `
        --nav-entry-point "Developer Portal/API Reference" `
        --report-file $targetReportFile `
        --report-format Json `
        --log-level $LogLevel

    if ($LASTEXITCODE -ne 0) {
        throw "Documentation.Exporter.Cli execution failed with exit code $LASTEXITCODE."
    }

    Write-Host "`n[3/3] Generated Markdown Documentation Files in '$OutputDir':" -ForegroundColor Green
    Get-ChildItem -Path $resolvedOutputDir -Filter "*.md" -Recurse | Sort-Object FullName | ForEach-Object {
        $relativePath = $_.FullName.Substring($resolvedOutputDir.Length).TrimStart('\', '/').Replace('\', '/')
        Write-Host ("  - {0} ({1} bytes)" -f $relativePath, $_.Length) -ForegroundColor White
    }

    if (Test-Path $targetReportFile) {
        $reportSize = (Get-Item $targetReportFile).Length
        Write-Host ("`n  - quality-report.json ({0} bytes)" -f $reportSize) -ForegroundColor Cyan
    }

    Write-Host "`n[SUCCESS] Demo documentation export completed successfully!" -ForegroundColor Green
}
finally {
    Pop-Location
}
