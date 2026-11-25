<#
.SYNOPSIS
    Builds the showcase demo solution (demo/Documentation.Exporter.Demo.slnx), builds the Documentation Exporter Linux Docker container image from Dockerfile, and runs the containerized CLI to export unified multi-assembly Markdown documentation into build/.demo-output/.

.DESCRIPTION
    Mirrors the behavior of `scripts/manual/Run-DemoExport.ps1` across four stages, executing the documentation export inside an isolated Linux Docker container rather than invoking the local host binary directly:
      1. Compiles `demo/Documentation.Exporter.Demo.slnx` on the host so compiled `.dll`, `.xml`, and `.deps.json` artifacts exist under `build/bin/`.
      2. Builds the multi-stage .NET 11 Linux container image from the repository root `Dockerfile`.
      3. Cleans the host output directory, seeds `mkdocs.nav.yml` from `demo/mkdocs.base.nav.yml`, and runs `docker run --rm` mounting the repository read-only at `/workspace:ro` and the output directory read-write at `/output:rw`.
      4. Enumerates all generated Markdown files and `quality-report.json` in the host output folder with byte sizes.
    Prerequisites: Docker Engine / Docker Desktop must be installed and running in Linux container mode.

.PARAMETER Configuration
    The MSBuild build configuration used to compile `demo/Documentation.Exporter.Demo.slnx` and passed to `--configuration` inside the container.
    Valid values: "Debug" (default) or "Release".

.PARAMETER OutputDirectory
    The host directory path (relative to the repository root) bind-mounted to `/output` inside the container where generated Markdown files, `mkdocs.nav.yml`, and `quality-report.json` will be written.
    Note: Any existing contents in this directory are deleted prior to export. Defaults to "build/.demo-output".

.PARAMETER LogLevel
    The minimum console logging verbosity passed to `--log-level` on the containerized `Documentation.Exporter.Cli`.
    Common values: "Trace", "Debug", "Information" (default), "Warning", "Error", "Critical", "None".

.PARAMETER DiagramFormat
    The preferred diagram syntax passed to `--diagram-format` for type relationship diagrams, assembly dependency trees, and architecture diagrams.
    Valid values:
      - "Mermaid"  : Emits inline ```mermaid fenced code blocks (default).
      - "PlantUml" : Emits inline ```plantuml fenced code blocks and standalone `architecture.puml` files.
      - "None"     : Suppresses diagram generation across all pages.

.PARAMETER ImageTag
    The Docker image name and tag applied during `docker build -t <ImageTag>` and executed by `docker run`.
    Defaults to "documentation-exporter:demo".

.PARAMETER NoCache
    When specified (`-NoCache`), passes `--no-cache` to `docker build` to force a clean rebuild of all Docker image layers without using cached layers.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport-Docker.ps1
    Builds the demo solution in Debug mode, builds the `documentation-exporter:demo` Docker image, and exports the demo documentation with Mermaid diagrams into `build/.demo-output/`.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport-Docker.ps1 -NoCache -Configuration Release -DiagramFormat PlantUml
    Forces a clean uncached Docker image build, compiles the demo in Release mode, and exports PlantUML diagrams into `build/.demo-output/`.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-DemoExport-Docker.ps1 -OutputDirectory "build/.docker-demo-out" -ImageTag "docexporter:local" -LogLevel Debug
    Builds and runs a custom-tagged container (`docexporter:local`) with Debug logging and writes output to `build/.docker-demo-out/`.
#>
[CmdletBinding()]
param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$OutputDirectory = "build/.demo-output",
    [string]$LogLevel = "Information",
    [ValidateSet("Mermaid", "PlantUml", "None")]
    [string]$DiagramFormat = "Mermaid",
    [string]$ImageTag = "documentation-exporter:demo",
    [switch]$NoCache
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repositoryRoot

try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host " Running Documentation.Exporter Docker Demo Solution Showcase" -ForegroundColor Cyan
    Write-Host " Configuration   : $Configuration" -ForegroundColor Cyan
    Write-Host " Diagram Format  : $DiagramFormat" -ForegroundColor Cyan
    Write-Host " Output Folder   : $OutputDirectory" -ForegroundColor Cyan
    Write-Host " Docker Image    : $ImageTag" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    $demoSolutionPath = Join-Path $repositoryRoot "demo/Documentation.Exporter.Demo.slnx"
    $dockerfilePath = Join-Path $repositoryRoot "Dockerfile"

    # 1. Compile the demo showcase solution locally so the compiled .dll and .xml files are available for volume mounting
    Write-Host "`n[1/4] Building Demo Solution assemblies..." -ForegroundColor Yellow
    dotnet build $demoSolutionPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build Demo solution ($demoSolutionPath)."
    }

    # 2. Build the Docker container image
    Write-Host "`n[2/4] Building Documentation Exporter Docker Image ($ImageTag)..." -ForegroundColor Yellow
    $dockerBuildArguments = @("build", "-t", $ImageTag, "-f", $dockerfilePath, $repositoryRoot)
    if ($NoCache) {
        $dockerBuildArguments += "--no-cache"
    }

    & docker @dockerBuildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed with exit code $LASTEXITCODE."
    }

    # 3. Prepare output directory
    $resolvedOutputDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
    if (Test-Path $resolvedOutputDirectory) {
        Remove-Item -Path $resolvedOutputDirectory -Recurse -Force
    }
    New-Item -Path $resolvedOutputDirectory -ItemType Directory -Force | Out-Null
    $baseNavTemplatePath = Join-Path $repositoryRoot "demo/mkdocs.base.nav.yml"
    $targetNavFilePath = Join-Path $resolvedOutputDirectory "mkdocs.nav.yml"
    if (Test-Path $baseNavTemplatePath) {
        Copy-Item -Path $baseNavTemplatePath -Destination $targetNavFilePath -Force
    }

    # 4. Execute the documentation conversion inside the container via volume mount
    Write-Host "`n[3/4] Executing Documentation.Exporter solution command inside Docker container..." -ForegroundColor Yellow

    $containerSolutionPath = "/workspace/demo/Documentation.Exporter.Demo.slnx"
    $containerExamplesPath = "/workspace/demo/Documentation.Exporter.DemoLibrary/docs_extra/examples"
    $containerOverridesPath = "/workspace/demo/Documentation.Exporter.DemoLibrary/docs_extra/overrides"
    $containerOutputPath = "/output"
    $containerNavFilePath = "/output/mkdocs.nav.yml"

    $dockerRunArguments = @(
        "run", "--rm",
        "-v", "${repositoryRoot}:/workspace:ro",
        "-v", "${resolvedOutputDirectory}:${containerOutputPath}:rw",
        $ImageTag,
        "solution",
        "--solution", $containerSolutionPath,
        "--configuration", $Configuration,
        "--exclude", "*.Tests", "*.Benchmarks",
        "--diagram-format", $DiagramFormat,
        "--output", $containerOutputPath,
        "--examples-dir", $containerExamplesPath,
        "--overrides-dir", $containerOverridesPath,
        "--nav-file", $containerNavFilePath,
        "--nav-entry-point", "Developer Portal/API Reference",
        "--strict",
        "--report-file", "/output/quality-report.json",
        "--report-format", "Json",
        "--log-level", $LogLevel
    )

    & docker @dockerRunArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Containerized Documentation.Exporter.Cli failed with exit code $LASTEXITCODE."
    }

    # 5. Report generated files
    Write-Host "`n[4/4] Generated Markdown Documentation Files in '$OutputDirectory':" -ForegroundColor Green
    $generatedFiles = Get-ChildItem -Path $resolvedOutputDirectory -Filter "*.md" -Recurse | Sort-Object FullName
    foreach ($fileItem in $generatedFiles) {
        $relativeFilePath = $fileItem.FullName.Substring($resolvedOutputDirectory.Length).TrimStart('\', '/').Replace('\', '/')
        Write-Host ("  - {0} ({1} bytes)" -f $relativeFilePath, $fileItem.Length) -ForegroundColor White
    }

    $qualityReportPath = Join-Path $resolvedOutputDirectory "quality-report.json"
    if (Test-Path $qualityReportPath) {
        $qualityReportFile = Get-Item $qualityReportPath
        Write-Host ("`n  - quality-report.json ({0} bytes)" -f $qualityReportFile.Length) -ForegroundColor Cyan
    }

    Write-Host "`n[SUCCESS] Docker demo documentation export completed successfully!" -ForegroundColor Green
}
finally {
    Pop-Location
}
