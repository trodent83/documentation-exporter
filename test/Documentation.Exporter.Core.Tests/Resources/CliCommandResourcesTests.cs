using System.Globalization;
using Documentation.Exporter.Core.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resources;

[UnitTest]
public sealed class CliCommandResourcesTests
{
    [Fact]
    public void ResourceManagerShouldNotBeNull() =>
        _ = CliCommandResources.ResourceManager.Should().NotBeNull("because the embedded CliCommandResources manifest must be accessible.");

    [Fact]
    public void ResourceCultureWhenSetAndResetShouldPersistProperly()
    {
        try
        {
            // Arrange
            var targetCulture = new CultureInfo("en-US");

            // Act
            CliCommandResources.ResourceCulture = targetCulture;

            // Assert
            _ = CliCommandResources.ResourceCulture.Should().Be(targetCulture, "because ResourceCulture should store the assigned CultureInfo.");
            _ = CliCommandResources.RootCommandDescription.Should().Be("C# XML Documentation and Reflection Exporter into MkDocs Markdown");
        }
        finally
        {
            CliCommandResources.ResourceCulture = null;
        }
    }

    [Fact]
    public void AllCliCommandResourcePropertiesShouldReturnExpectedNeutralStrings()
    {
        // Act & Assert
        _ = CliCommandResources.RootCommandDescription.Should().Be(
            "C# XML Documentation and Reflection Exporter into MkDocs Markdown",
            "because RootCommandDescription must return the neutral resource value.");
        _ = CliCommandResources.AssemblyOptionDescription.Should().Be(
            "Path to the compiled target assembly file (.dll)",
            "because AssemblyOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.XmlOptionDescription.Should().Be(
            "Path to the compiler-generated XML documentation file (.xml)",
            "because XmlOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.OutputOptionDescription.Should().Be(
            "Path to the output directory for generated Markdown files",
            "because OutputOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.ExamplesOptionDescription.Should().Be(
            "Optional directory containing external code example snippets",
            "because ExamplesOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.OverridesOptionDescription.Should().Be(
            "Optional directory containing supplementary markdown sidecar override files",
            "because OverridesOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.MsDocsOptionDescription.Should().Be(
            "Base URL for Microsoft Learn API documentation links",
            "because MsDocsOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.LogLevelOptionDescription.Should().Be(
            "Console logging minimum level (Trace, Debug, Information, Warning, Error, Critical, None)",
            "because LogLevelOptionDescription must return the neutral resource value.");
        _ = CliCommandResources.AssemblyCommandDescription.Should().Be(
            "Export documentation for one or more compiled assemblies (.dll)");
        _ = CliCommandResources.SolutionCommandDescription.Should().Be(
            "Export unified documentation for all projects in a C# solution (.sln or .slnx)");
        _ = CliCommandResources.SolutionOptionDescription.Should().Be(
            "Path to the C# solution file (.sln or .slnx)");
        _ = CliCommandResources.ConfigurationOptionDescription.Should().Be(
            "Build configuration used to locate compiled project outputs (Debug or Release)");
        _ = CliCommandResources.IncludeTestsOptionDescription.Should().Be(
            "Include test projects when exporting a solution");
        _ = CliCommandResources.ExternalDocsOptionDescription.Should().Be(
            "Custom third-party documentation link mappings in NamespacePrefix=UrlTemplate format");
        _ = CliCommandResources.ConfigCommandDescription.Should().Be(
            "Export documentation using a declarative JSON configuration file");
        _ = CliCommandResources.ConfigOptionDescription.Should().Be(
            "Path to the JSON configuration file (doc-exporter.json)");
        _ = CliCommandResources.NavFileOptionDescription.Should().Be(
            "Optional path to an existing or target MkDocs YAML file (mkdocs.yml or mkdocs.nav.yml) to generate or extend");
        _ = CliCommandResources.NavEntryPointOptionDescription.Should().Be(
            "Optional navigation entry point section path (e.g. 'API Reference' or 'Developer Guide/API Reference') or comment placeholder where generated navigation should be inserted");
        _ = CliCommandResources.NavUrlPrefixOptionDescription.Should().Be(
            "Optional relative URL prefix prepended to generated Markdown links in the MkDocs navigation (e.g. 'api')");
        _ = CliCommandResources.ExcludeOptionDescription.Should().Be(
            "Optional project or assembly names and wildcard patterns (e.g. '*.Tests', '*.Benchmarks') to exclude when exporting");
        _ = CliCommandResources.DiagramFormatOptionDescription.Should().Be(
            "Preferred diagram generator format for type and index pages (for example Mermaid, PlantUml, None, or any custom plugin format; defaults to Mermaid and falls back to another registered generator when a diagram type is unsupported)");
        _ = CliCommandResources.CleanOptionDescription.Should().Be(
            "Clean existing Markdown (.md) and PlantUML (.puml) files in the output directory prior to export");
        _ = CliCommandResources.FailOnMissingOptionDescription.Should().Be(
            "Fail export execution if any included project artifact (.dll or .xml) in the solution is missing");
        _ = CliCommandResources.FailOnUndocumentedOptionDescription.Should().Be(
            "Minimum required documentation coverage percentage (0.0 to 100.0). Export fails if coverage is below this threshold");
        _ = CliCommandResources.StrictOptionDescription.Should().Be(
            "Enable strict relative link validation; fails export if any relative Markdown link cannot be resolved");
        _ = CliCommandResources.ExcludeTypesOptionDescription.Should().Be(
            "Optional type names or wildcard patterns (e.g. '*Internal*') to exclude from generated documentation");
        _ = CliCommandResources.ExcludeNamespacesOptionDescription.Should().Be(
            "Optional namespace names or wildcard patterns (e.g. '*.Migrations') to exclude from generated documentation");
        _ = CliCommandResources.ExcludeEditorBrowsableNeverOptionDescription.Should().Be(
            "Automatically excludes types and members decorated with [EditorBrowsable(EditorBrowsableState.Never)].");
        _ = CliCommandResources.InvalidExternalDocRuleFormat.Should().Be(
            "Invalid external documentation link rule format: '{0}'. Expected format: '<NamespacePattern>=<UrlTemplate>' (for example, 'Serilog*=https://serilog.net/api/{{type}}').");
        _ = CliCommandResources.InvalidFailOnUndocumentedPercentage.Should().Be(
            "Cannot parse argument '{0}' for option '--fail-on-undocumented' as a valid percentage number.");
        _ = CliCommandResources.ConfigFileNotFound.Should().Be(
            "Configuration file not found: '{0}'.");
        _ = CliCommandResources.ConfigFileLoadFailed.Should().Be(
            "Failed to load configuration file '{0}': {1}");
        _ = CliCommandResources.ReportFileOptionDescription.Should().Be(
            "Optional output file path for the structured documentation quality and diagnostics report.");
        _ = CliCommandResources.ReportFormatOptionDescription.Should().Be(
            "Output format for the documentation quality report: Json (default), Markdown, Sarif, or a custom plugin format name.");
        _ = CliCommandResources.NavFormatOptionDescription.Should().Be(
            "Target static-site navigation generator format (MkDocs by default, None to disable, or any custom plugin format name).");
        _ = CliCommandResources.PluginsDirOptionDescription.Should().Be(
            "Optional directory path containing external plugin assemblies (.dll) implementing IDocumentationExporterPlugin.");
        _ = CliCommandResources.InvalidDiagramFormat.Should().Be(
            "Cannot parse argument '{0}' for option '--diagram-format' as a valid diagram format name.");
        _ = CliCommandResources.InvalidReportFormat.Should().Be(
            "Cannot parse argument '{0}' for option '--report-format' as a valid quality report format name.");
        _ = CliCommandResources.InvalidNavigationFormat.Should().Be(
            "Cannot parse argument '{0}' for option '--nav-format' as a valid navigation format name.");
    }
}
