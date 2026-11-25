<#
.SYNOPSIS
    Runs unit and integration tests across Documentation.Exporter assemblies, calculates detailed Cobertura code coverage, and enforces minimum line and branch coverage thresholds.

.DESCRIPTION
    Executes the xUnit test suite via `dotnet test` with the cross-platform Coverlet Cobertura collector (`--collect:"XPlat Code Coverage"`), parses the emitted `coverage.cobertura.xml` report, displays a summary of overall line and branch coverage metrics across Documentation.Exporter.Abstractions, Documentation.Exporter.Core, Documentation.Exporter.Cli, Documentation.Exporter.Plugins.Diagrams, and Documentation.Exporter.Plugins.Navigation.MkDocs, and exits with code 1 if `dotnet test` fails or if either metric falls below its configured threshold.
    Automatically resolves the repository root from the script location (`scripts/manual/`) so it can be invoked from any current working directory.

.PARAMETER MinimumLineCoverage
    The minimum required line coverage percentage (0.0 to 100.0) across [Documentation.Exporter.Abstractions]*, [Documentation.Exporter.Core]*, [Documentation.Exporter.Cli]*, [Documentation.Exporter.Plugins.Diagrams]*, and [Documentation.Exporter.Plugins.Navigation.MkDocs]*.
    Defaults to 90.0. If the measured line coverage is lower than this value, the script fails with exit code 1.

.PARAMETER MinimumBranchCoverage
    The minimum required branch coverage percentage (0.0 to 100.0) across [Documentation.Exporter.Abstractions]*, [Documentation.Exporter.Core]*, [Documentation.Exporter.Cli]*, [Documentation.Exporter.Plugins.Diagrams]*, and [Documentation.Exporter.Plugins.Navigation.MkDocs]*.
    Defaults to 90.0. If the measured branch coverage is lower than this value, the script fails with exit code 1.

.PARAMETER TestProject
    The path (relative to the repository root or absolute) to the xUnit test project (.csproj) to execute.
    Defaults to "test/Documentation.Exporter.Core.Tests/Documentation.Exporter.Core.Tests.csproj".

.PARAMETER ResultsDir
    The directory path (relative to the repository root or absolute) where Coverlet writes the temporary test results and `coverage.cobertura.xml` report.
    Any existing directory at this path is cleaned before running the tests. Defaults to "TestResults".

.PARAMETER Category
    Filters which xUnit test category trait (`Category=<Value>`) to execute.
    Valid values:
      - "All"         : Runs both [UnitTest] and [IntegrationTest] suites (default).
      - "Unit"        : Runs only fast, isolated in-memory unit tests (`--filter "Category=Unit"`).
      - "Integration" : Runs only disk, reflection, and CLI end-to-end integration tests (`--filter "Category=Integration"`).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-CodeCoverage.ps1
    Runs all unit and integration tests and enforces the default 90.0% line and 90.0% branch coverage thresholds.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-CodeCoverage.ps1 -MinimumLineCoverage 92.0 -MinimumBranchCoverage 85.0
    Runs all tests and enforces custom line (92.0%) and branch (85.0%) coverage gates.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-CodeCoverage.ps1 -Category Unit -MinimumLineCoverage 0 -MinimumBranchCoverage 0
    Runs only the [UnitTest] suite and prints its standalone coverage numbers without failing on the full-suite threshold.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/manual/Run-CodeCoverage.ps1 -Category Integration -ResultsDir "artifacts/coverage-integration" -MinimumLineCoverage 0 -MinimumBranchCoverage 0
    Runs only the [IntegrationTest] suite and stores the Cobertura XML report in `artifacts/coverage-integration`.
#>
[CmdletBinding()]
param (
    [double]$MinimumLineCoverage = 90.0,
    [double]$MinimumBranchCoverage = 90.0,
    [string]$TestProject = "test/Documentation.Exporter.Core.Tests/Documentation.Exporter.Core.Tests.csproj",
    [string]$ResultsDir = "TestResults",
    [ValidateSet("All", "Unit", "Integration")]
    [string]$Category = "All"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot

try {
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host " Running Code Coverage Analysis for Documentation.Exporter" -ForegroundColor Cyan
    Write-Host " Test Category Filter   : $Category" -ForegroundColor Cyan
    Write-Host " Target Line Coverage   : $MinimumLineCoverage%" -ForegroundColor Cyan
    Write-Host " Target Branch Coverage : $MinimumBranchCoverage%" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    if (Test-Path $ResultsDir) {
        Remove-Item -Path $ResultsDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "`nExecuting dotnet test with Coverlet Cobertura collector..." -ForegroundColor Yellow
    if ($Category -eq "All") {
        dotnet test $TestProject --collect:"XPlat Code Coverage" --results-directory $ResultsDir -- 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute' 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile=**/*.g.cs' 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include=[Documentation.Exporter.Abstractions]*,[Documentation.Exporter.Core]*,[Documentation.Exporter.Cli]*,[Documentation.Exporter.Plugins.Diagrams]*,[Documentation.Exporter.Plugins.Navigation.MkDocs]*'
    } else {
        dotnet test $TestProject --filter "Category=$Category" --collect:"XPlat Code Coverage" --results-directory $ResultsDir -- 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute' 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile=**/*.g.cs' 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include=[Documentation.Exporter.Abstractions]*,[Documentation.Exporter.Core]*,[Documentation.Exporter.Cli]*,[Documentation.Exporter.Plugins.Diagrams]*,[Documentation.Exporter.Plugins.Navigation.MkDocs]*'
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet test execution failed. Aborting coverage calculation."
        exit $LASTEXITCODE
    }

    $coverageFiles = Get-ChildItem -Path $ResultsDir -Filter "coverage.cobertura.xml" -Recurse
    if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
        Write-Error "No Cobertura coverage file found in $ResultsDir."
        exit 1
    }

    $coverageFilePath = $coverageFiles[0].FullName
    [xml]$coberturaXml = Get-Content -Path $coverageFilePath

    $root = $coberturaXml.coverage
    $linesCovered = [int]$root.'lines-covered'
    $linesValid = [int]$root.'lines-valid'
    $branchesCovered = [int]$root.'branches-covered'
    $branchesValid = [int]$root.'branches-valid'

    $lineRatePct = if ($linesValid -gt 0) { [math]::Round(($linesCovered / $linesValid) * 100, 2) } else { 100.0 }
    $branchRatePct = if ($branchesValid -gt 0) { [math]::Round(($branchesCovered / $branchesValid) * 100, 2) } else { 100.0 }

    Write-Host "`n------------------------------------------------------------" -ForegroundColor Cyan
    Write-Host " Overall Solution Code Coverage Metrics" -ForegroundColor Cyan
    Write-Host "------------------------------------------------------------" -ForegroundColor Cyan
    Write-Host ("Line Coverage   : {0}% ({1} / {2} lines)" -f $lineRatePct, $linesCovered, $linesValid)
    Write-Host ("Branch Coverage : {0}% ({1} / {2} branches)" -f $branchRatePct, $branchesCovered, $branchesValid)
    Write-Host "------------------------------------------------------------" -ForegroundColor Cyan

    $linePassed = $lineRatePct -ge $MinimumLineCoverage
    $branchPassed = $branchRatePct -ge $MinimumBranchCoverage

    if (-not $linePassed -or -not $branchPassed) {
        if (-not $linePassed) {
            Write-Host "`n[FAILURE] Overall Line Coverage ($lineRatePct%) is below required threshold ($MinimumLineCoverage%)!" -ForegroundColor Red
        }
        if (-not $branchPassed) {
            Write-Host "`n[FAILURE] Overall Branch Coverage ($branchRatePct%) is below required threshold ($MinimumBranchCoverage%)!" -ForegroundColor Red
        }
        exit 1
    }

    Write-Host "`n[SUCCESS] Code Coverage check passed! (Lines: $lineRatePct% >= $MinimumLineCoverage%, Branches: $branchRatePct% >= $MinimumBranchCoverage%)" -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
