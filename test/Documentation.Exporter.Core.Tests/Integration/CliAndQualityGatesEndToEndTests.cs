using System.Text.Json;
using System.Xml.Linq;
using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.DemoContracts.Auditing;
using Documentation.Exporter.DemoLibrary.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests exercising real <c>Program.Main</c> CLI execution, strict quality failure gates,
/// failure-safe SARIF/JSON report flushing, safe <c>--clean</c> asset preservation, <c>DiagramFormat.None</c>, and exclusion filters.
/// </summary>
[IntegrationTest]
public sealed class CliAndQualityGatesEndToEndTests
{
    private static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory != null &&
               !File.Exists(Path.Combine(currentDirectory.FullName, "demo", "Documentation.Exporter.Demo.slnx")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        _ = currentDirectory.Should().NotBeNull("because demo/Documentation.Exporter.Demo.slnx must exist in the repository.");
        return currentDirectory.FullName;
    }

    [Fact]
    public async Task ProgramMainAssemblySolutionConfigAndFailureGateShouldProduceExpectedExitCodesOutputsAndReports()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var demoSolutionPath = Path.Combine(repositoryRoot, "demo", "Documentation.Exporter.Demo.slnx");
        var demoLibraryAssemblyPath = typeof(TagShowcaseProcessor).Assembly.Location;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_CliE2E_{Guid.NewGuid():N}");

        var assemblyOutputDir = Path.Combine(tempRoot, "cli-assembly");
        var assemblyReportPath = Path.Combine(tempRoot, "reports", "assembly-report.md");

        var solutionOutputDir = Path.Combine(tempRoot, "cli-solution");
        var solutionReportPath = Path.Combine(tempRoot, "reports", "solution-report.sarif");

        var configOutputDir = Path.Combine(tempRoot, "cli-config");
        var configReportPath = Path.Combine(configOutputDir, "quality-report.json");
        var configFilePath = Path.Combine(tempRoot, "doc-exporter.test.json");

        var failOutputDir = Path.Combine(tempRoot, "cli-fail");
        var failReportPath = Path.Combine(tempRoot, "reports", "failure-report.json");

        _ = Directory.CreateDirectory(tempRoot);

        try
        {
            await File.WriteAllTextAsync(
                configFilePath,
                $$"""
                {
                  "solutionFilePath": "{{demoSolutionPath.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                  "outputDirectoryPath": "{{configOutputDir.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                  "excludedProjects": [ "*.Tests", "*.Benchmarks" ],
                  "diagramFormat": "Mermaid",
                  "strictLinkValidation": true,
                  "reportFilePath": "{{configReportPath.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                  "reportFormat": "Json"
                }
                """);

            // Act 1: Real CLI `assembly` subcommand with --strict and Markdown quality report
            var assemblyExitCode = await Documentation.Exporter.Cli.Program.Main(
            [
                "assembly",
                "--assembly", demoLibraryAssemblyPath,
                "--output", assemblyOutputDir,
                "--strict",
                "--report-file", assemblyReportPath,
                "--report-format", "Markdown",
                "--log-level", "None"
            ]);

            // Act 2: Real CLI `solution` subcommand with --exclude *DemoContracts*, --diagram-format None, --strict, Sarif report
            var solutionExitCode = await Documentation.Exporter.Cli.Program.Main(
            [
                "solution",
                "--solution", demoSolutionPath,
                "--output", solutionOutputDir,
                "--exclude", "*.Tests", "*DemoContracts*",
                "--diagram-format", "None",
                "--strict",
                "--report-file", solutionReportPath,
                "--report-format", "Sarif",
                "--log-level", "None"
            ]);

            // Act 3: Real CLI `config` subcommand
            var configExitCode = await Documentation.Exporter.Cli.Program.Main(
            [
                "config",
                "--config", configFilePath,
                "--log-level", "None"
            ]);

            // Act 4: Real CLI coverage failure gate (--fail-on-undocumented 99.9) still flushes quality report
            var failGateExitCode = await Documentation.Exporter.Cli.Program.Main(
            [
                "assembly",
                "--assembly", demoLibraryAssemblyPath,
                "--output", failOutputDir,
                "--fail-on-undocumented", "99.9",
                "--report-file", failReportPath,
                "--report-format", "Json",
                "--log-level", "None"
            ]);

            // Assert - `assembly`
            _ = assemblyExitCode.Should().Be(0, "because valid CLI assembly export with --strict must return exit code 0.");
            _ = File.Exists(Path.Combine(assemblyOutputDir, "index.md")).Should().BeTrue();
            _ = File.Exists(assemblyReportPath).Should().BeTrue();
            var assemblyMarkdownReport = await File.ReadAllTextAsync(assemblyReportPath);
            _ = assemblyMarkdownReport.Should().Contain("# Documentation Quality Report")
                .And.Contain("| Verified Links |");

            // Assert - `solution` (--exclude + DiagramFormat.None + Sarif)
            _ = solutionExitCode.Should().Be(0, "because solution export excluding DemoContracts with DiagramFormat.None must succeed under --strict.");
            _ = Directory.Exists(Path.Combine(solutionOutputDir, "Documentation.Exporter.DemoLibrary")).Should().BeTrue();
            _ = Directory.Exists(Path.Combine(solutionOutputDir, "Documentation.Exporter.DemoContracts")).Should().BeFalse(
                "because *DemoContracts* was excluded via --exclude.");
            _ = Directory.GetFiles(solutionOutputDir, "*.puml", SearchOption.AllDirectories).Should().BeEmpty(
                "because DiagramFormat.None must not emit standalone .puml files.");
            foreach (var markdownFile in Directory.EnumerateFiles(solutionOutputDir, "*.md", SearchOption.AllDirectories))
            {
                var content = await File.ReadAllTextAsync(markdownFile);
                _ = content.Should().NotContain("```mermaid", $"because DiagramFormat.None must omit Mermaid blocks in '{markdownFile}'.")
                    .And.NotContain("```plantuml", $"because DiagramFormat.None must omit PlantUML blocks in '{markdownFile}'.");
            }

            _ = File.Exists(solutionReportPath).Should().BeTrue();
            using var sarifDocument = JsonDocument.Parse(await File.ReadAllTextAsync(solutionReportPath));
            _ = sarifDocument.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");

            // Assert - `config`
            _ = configExitCode.Should().Be(0, "because valid config subcommand invocation must return exit code 0.");
            _ = File.Exists(Path.Combine(configOutputDir, "index.md")).Should().BeTrue();
            _ = File.Exists(configReportPath).Should().BeTrue();
            using var configReportDocument = JsonDocument.Parse(await File.ReadAllTextAsync(configReportPath));
            _ = configReportDocument.RootElement.GetProperty("errorCount").GetInt32().Should().Be(0);

            // Assert - failure gate exit code 1 + flushed failure report without emitting docs
            _ = failGateExitCode.Should().Be(1, "because failing --fail-on-undocumented gate must return exit code 1.");
            _ = Directory.Exists(failOutputDir).Should().BeFalse(
                "because Step 6 aborts before Step 7 ensures/creates output directory.");
            _ = File.Exists(failReportPath).Should().BeTrue(
                "because failure quality reports must still be persisted when coverage gates abort.");
            using var failureReportDocument = JsonDocument.Parse(await File.ReadAllTextAsync(failReportPath));
            _ = failureReportDocument.RootElement.GetProperty("warningCount").GetInt32().Should().BeGreaterThan(0);
            _ = failureReportDocument.RootElement.GetProperty("diagnostics").GetArrayLength().Should().BeGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportDocumentationAsyncWhenStrictLinksFailShouldReturnFailureAndFlushSarifReportWithDoc006AndDoc007()
    {
        // Arrange
        var demoLibraryAssemblyPath = typeof(TagShowcaseProcessor).Assembly.Location;
        var demoLibraryXmlPath = Path.ChangeExtension(demoLibraryAssemblyPath, ".xml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_StrictFailE2E_{Guid.NewGuid():N}");
        var overridesDir = Path.Combine(tempRoot, "overrides");
        var outputDir = Path.Combine(tempRoot, "docs");
        var sarifReportPath = Path.Combine(tempRoot, "reports", "strict-failure.sarif");

        _ = Directory.CreateDirectory(overridesDir);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(overridesDir, "LegacyInvoiceAdapter.md"),
                """
                ---
                injection_mode: extend
                target_anchor: page_append
                ---

                See [Broken File](DoesNotExist.md) and [Broken Heading](index.md#non-existent-heading).
                """);

            ServiceCollection services = new();
            _ = services.AddDocumentationExporter();
            await using var serviceProvider = services.BuildServiceProvider();
            var engine = serviceProvider.GetRequiredService<IDocumentationExporterEngine>();

            ExporterOptions options = new()
            {
                AssemblyFilePath = demoLibraryAssemblyPath,
                XmlDocumentationFilePath = demoLibraryXmlPath,
                OutputDirectoryPath = outputDir,
                OverridesDirectoryPath = overridesDir,
                StrictLinkValidation = true,
                ReportFilePath = sarifReportPath,
                ReportFormat = QualityReportFormat.Sarif
            };

            // Act
            var exportResult = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = exportResult.IsSuccess.Should().BeFalse("because strict validation must fail on injected broken links and broken anchors.");
            _ = exportResult.QualityReport.Should().NotBeNull("because failed exports must still attach QualityReport.");
            _ = exportResult.QualityReport!.ErrorCount.Should().BeGreaterThanOrEqualTo(2);
            _ = exportResult.Diagnostics.Should().Contain(diagnostic =>
                diagnostic.Code == "DOC006" &&
                diagnostic.Kind == DocumentationDiagnosticKind.BrokenMarkdownLink &&
                diagnostic.Severity == DocumentationDiagnosticSeverity.Error &&
                diagnostic.TargetReference == "DoesNotExist.md" &&
                diagnostic.LineNumber > 0);
            _ = exportResult.Diagnostics.Should().Contain(diagnostic =>
                diagnostic.Code == "DOC007" &&
                diagnostic.Kind == DocumentationDiagnosticKind.BrokenMarkdownAnchor &&
                diagnostic.Severity == DocumentationDiagnosticSeverity.Error &&
                diagnostic.TargetReference == "../../index.md#non-existent-heading" &&
                diagnostic.LineNumber > 0);

            _ = File.Exists(sarifReportPath).Should().BeTrue("because SARIF diagnostics must be flushed even when Step 9 fails.");
            using var sarifDocument = JsonDocument.Parse(await File.ReadAllTextAsync(sarifReportPath));
            var sarifResults = sarifDocument.RootElement.GetProperty("runs")[0].GetProperty("results");
            var ruleIds = Enumerable.Range(0, sarifResults.GetArrayLength())
                .Select(resultIndex => sarifResults[resultIndex].GetProperty("ruleId").GetString())
                .ToList();
            _ = ruleIds.Should().Contain("DOC006").And.Contain("DOC007");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportDocumentationAsyncWithCleanOutputDirectoryShouldDeleteStaleGeneratedFilesAndPreserveCustomAssets()
    {
        // Arrange
        var demoContractsAssemblyPath = typeof(ISettlementAuditSink).Assembly.Location;
        var demoContractsXmlPath = Path.ChangeExtension(demoContractsAssemblyPath, ".xml");
        var tempOutputDir = Path.Combine(Path.GetTempPath(), $"DocExporter_CleanE2E_{Guid.NewGuid():N}");
        var staleSubDir = Path.Combine(tempOutputDir, "StaleSubdir");
        var customAssetDir = Path.Combine(tempOutputDir, "styles");
        _ = Directory.CreateDirectory(staleSubDir);
        _ = Directory.CreateDirectory(customAssetDir);

        try
        {
            var staleRootMarkdown = Path.Combine(tempOutputDir, "StalePage.md");
            var staleNestedMarkdown = Path.Combine(staleSubDir, "OldType.md");
            var stalePlantUml = Path.Combine(tempOutputDir, "stale.puml");
            var preservedImage = Path.Combine(tempOutputDir, "custom-logo.png");
            var preservedCss = Path.Combine(customAssetDir, "extra.css");

            await File.WriteAllTextAsync(staleRootMarkdown, "# Stale");
            await File.WriteAllTextAsync(staleNestedMarkdown, "# Old Type");
            await File.WriteAllTextAsync(stalePlantUml, "@startuml\n@enduml");
            await File.WriteAllTextAsync(preservedImage, "PNG-PLACEHOLDER");
            await File.WriteAllTextAsync(preservedCss, "body { margin: 0; }");

            ServiceCollection services = new();
            _ = services.AddDocumentationExporter();
            await using var serviceProvider = services.BuildServiceProvider();
            var engine = serviceProvider.GetRequiredService<IDocumentationExporterEngine>();

            ExporterOptions options = new()
            {
                AssemblyFilePath = demoContractsAssemblyPath,
                XmlDocumentationFilePath = demoContractsXmlPath,
                OutputDirectoryPath = tempOutputDir,
                CleanOutputDirectory = true,
                StrictLinkValidation = true
            };

            // Act
            var exportResult = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = exportResult.IsSuccess.Should().BeTrue("because cleaning stale generated files and exporting DemoContracts must succeed.");
            _ = File.Exists(staleRootMarkdown).Should().BeFalse("because --clean deletes stale .md files.");
            _ = File.Exists(staleNestedMarkdown).Should().BeFalse("because --clean deletes nested stale .md files.");
            _ = File.Exists(stalePlantUml).Should().BeFalse("because --clean deletes stale .puml files.");
            _ = Directory.Exists(staleSubDir).Should().BeFalse("because empty stale subdirectories should be removed.");
            _ = File.Exists(preservedImage).Should().BeTrue("because non-generated asset files must be preserved.");
            _ = File.Exists(preservedCss).Should().BeTrue("because custom CSS directories and assets must be preserved.");
            _ = File.Exists(Path.Combine(tempOutputDir, "index.md")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempOutputDir))
            {
                Directory.Delete(tempOutputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportDocumentationAsyncWithTypeNamespaceXmlExcludeFiltersAndFailOnMissingShouldBehaveCorrectly()
    {
        // Arrange
        var demoLibraryAssemblyPath = typeof(TagShowcaseProcessor).Assembly.Location;
        var sourceXmlPath = Path.ChangeExtension(demoLibraryAssemblyPath, ".xml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_FiltersE2E_{Guid.NewGuid():N}");
        var modifiedXmlPath = Path.Combine(tempRoot, "DemoLibrary.Modified.xml");
        var outputDir = Path.Combine(tempRoot, "docs");
        var brokenSolutionPath = Path.Combine(tempRoot, "MissingProject.slnx");
        var failOnMissingOutputDir = Path.Combine(tempRoot, "missing-out");

        _ = Directory.CreateDirectory(tempRoot);

        try
        {
            var xmlDocument = XDocument.Load(sourceXmlPath);
            var previewMemberElement = xmlDocument.Descendants("member")
                .FirstOrDefault(memberElement => string.Equals(
                    memberElement.Attribute("name")?.Value,
                    "T:Documentation.Exporter.DemoLibrary.ModernCSharp.ExperimentalSettlementPreview",
                    StringComparison.Ordinal));
            _ = previewMemberElement.Should().NotBeNull("because DemoLibrary.xml contains ExperimentalSettlementPreview.");
            previewMemberElement!.Add(new XElement("exclude"));
            xmlDocument.Save(modifiedXmlPath);

            await File.WriteAllTextAsync(
                brokenSolutionPath,
                """
                <Solution>
                  <Project Path="src/DoesNotExist/Missing.csproj" />
                </Solution>
                """);

            ServiceCollection services = new();
            _ = services.AddDocumentationExporter();
            await using var serviceProvider = services.BuildServiceProvider();
            var engine = serviceProvider.GetRequiredService<IDocumentationExporterEngine>();

            // Act 1: Export with <exclude/> XML tag + ExcludedNamespaces + ExcludedTypes + StrictLinkValidation
            var filteredResult = await engine.ExportDocumentationAsync(new ExporterOptions
            {
                AssemblyFilePath = demoLibraryAssemblyPath,
                XmlDocumentationFilePath = modifiedXmlPath,
                OutputDirectoryPath = outputDir,
                ExcludedNamespaces = ["Documentation.Exporter.DemoLibrary.Adapters"],
                ExcludedTypes = ["*CurrencyPair*"],
                StrictLinkValidation = true
            });

            // Act 2: Export solution with FailOnMissingProjectArtifacts = true when project artifact is missing
            var failOnMissingResult = await engine.ExportDocumentationAsync(new ExporterOptions
            {
                SolutionFilePath = brokenSolutionPath,
                OutputDirectoryPath = failOnMissingOutputDir,
                FailOnMissingProjectArtifacts = true
            });

            // Assert - Exclusions
            _ = filteredResult.IsSuccess.Should().BeTrue(
                "because excluding unreferenced types via <exclude/>, ExcludedNamespaces, and ExcludedTypes must leave strict links valid.");
            _ = File.Exists(Path.Combine(outputDir, "ModernCSharp", "ExperimentalSettlementPreview", "index.md")).Should().BeFalse(
                "because types tagged with <exclude/> must be omitted.");
            _ = File.Exists(Path.Combine(outputDir, "Adapters", "LegacyInvoiceAdapter", "index.md")).Should().BeFalse(
                "because ExcludedNamespaces must filter out matching namespaces.");
            _ = File.Exists(Path.Combine(outputDir, "ModernCSharp", "CurrencyPair", "index.md")).Should().BeFalse(
                "because ExcludedTypes wildcard patterns must filter out matching types.");
            _ = File.Exists(Path.Combine(outputDir, "Diagnostics", "TagShowcaseProcessor", "index.md")).Should().BeTrue(
                "because non-excluded types must still be emitted.");

            // Assert - FailOnMissingProjectArtifacts
            _ = failOnMissingResult.IsSuccess.Should().BeFalse(
                "because FailOnMissingProjectArtifacts=true must fail when a solution project artifact cannot be located.");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
