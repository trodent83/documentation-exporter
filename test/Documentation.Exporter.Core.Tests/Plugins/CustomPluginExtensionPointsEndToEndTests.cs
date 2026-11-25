using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Plugins;
using Documentation.Exporter.Abstractions.Quality;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Plugins;

[IntegrationTest]
public sealed class CustomPluginExtensionPointsEndToEndTests
{
    private sealed class CustomSuitePlugin : IDocumentationExporterPlugin
    {
        public string Name => "Custom Suite Extension Plugin";

        public void ConfigureServices(IServiceCollection serviceCollection) =>
            _ = serviceCollection
                .AddNamespaceHandler<ExternalSystemOverrideHandler>()
                .AddQualityReportFormatter<JUnitQualityReportFormatter>()
                .AddNavigationGenerator<DocusaurusNavigationGenerator>();
    }

    private sealed class ExternalSystemOverrideHandler : INamespaceHandler
    {
        public int Order => -2000;

        public bool CanHandleNamespace(string fullTypeName) =>
            string.Equals(fullTypeName, "System.ArgumentException", StringComparison.Ordinal) ||
            string.Equals(fullTypeName, "T:System.ArgumentException", StringComparison.Ordinal);

        public string ResolveTypeLink(string fullTypeName) =>
            "[System.ArgumentException](https://custom-api.example.org/dotnet/System.ArgumentException)";
    }

    private sealed class JUnitQualityReportFormatter : IQualityReportFormatter
    {
        public QualityReportFormat Format => QualityReportFormat.Custom("JUnit");

        public string FormatReport(DocumentationQualityReport report) =>
            $"<testsuite name=\"Documentation.Exporter\" tests=\"{report.TotalDocumentableSymbols}\" coverage=\"{report.DocumentationCoveragePercentage:F1}\" />";
    }

    private sealed class DocusaurusNavigationGenerator : INavigationGenerator
    {
        public NavigationFormat Format => NavigationFormat.Custom("Docusaurus");

        public string DefaultFileName => "sidebars.js";

        public string GenerateNavigation(
            IReadOnlyList<TypeDocumentationModel> types,
            bool useAssemblySubdirectories = false,
            string? existingNavigationContent = null,
            string? navigationEntryPoint = null,
            string? navigationUrlPrefix = null)
        {
            var entryNames = string.Join(", ", types.Select(typeModel => $"'{typeModel.TypeName}'"));
            return $"module.exports = {{ apiSidebar: [{entryNames}] }};";
        }
    }

    [Fact]
    public async Task ExportDocumentationWithCustomPluginExtensionsShouldDriveLinkResolutionNavigationAndQualityReport()
    {
        // Arrange
        var demoAssemblyPath = typeof(Documentation.Exporter.DemoLibrary.Services.OrderProcessingService).Assembly.Location;
        var demoXmlPath = Path.ChangeExtension(demoAssemblyPath, ".xml");
        var outputDir = Path.Combine(Path.GetTempPath(), $"DocExporter_CustomPluginE2E_{Guid.NewGuid():N}");
        var reportPath = Path.Combine(outputDir, "junit-report.xml");

        ServiceCollection services = new();
        _ = services.AddDocumentationExporter();
        _ = services.AddDocumentationExporterPlugin<CustomSuitePlugin>();

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IDocumentationExporterEngine>();

        ExporterOptions options = new()
        {
            AssemblyFilePath = demoAssemblyPath,
            XmlDocumentationFilePath = demoXmlPath,
            OutputDirectoryPath = outputDir,
            NavigationFormat = NavigationFormat.Custom("Docusaurus"),
            ReportFilePath = reportPath,
            ReportFormat = QualityReportFormat.Custom("JUnit")
        };

        try
        {
            // Act
            var result = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = result.IsSuccess.Should().BeTrue("because custom navigation, link resolution, and quality report extensions should succeed end-to-end.");

            var sidebarsPath = Path.Combine(outputDir, "sidebars.js");
            _ = File.Exists(sidebarsPath).Should().BeTrue("because the Docusaurus plugin specifies DefaultFileName='sidebars.js'.");
            _ = File.Exists(Path.Combine(outputDir, "mkdocs.nav.yml")).Should().BeFalse(
                "because selecting Docusaurus format must not emit mkdocs.nav.yml.");
            var sidebarContent = await File.ReadAllTextAsync(sidebarsPath);
            _ = sidebarContent.Should().Contain("OrderProcessingService");

            _ = File.Exists(reportPath).Should().BeTrue("because custom JUnit quality report format must write to ReportFilePath.");
            var junitContent = await File.ReadAllTextAsync(reportPath);
            _ = junitContent.Should().StartWith("<testsuite name=\"Documentation.Exporter\"");

            // Verify high-precedence custom INamespaceHandler (-2000) overrode default MicrosoftDocs link for System.ArgumentException
            var ctorDocPath = Path.Combine(outputDir, "Services", "OrderProcessingService", "OrderProcessingService.ctor_String.md");
            _ = File.Exists(ctorDocPath).Should().BeTrue("because the documented OrderProcessingService(string) constructor page must be generated.");
            var ctorContent = await File.ReadAllTextAsync(ctorDocPath);
            _ = ctorContent.Should().Contain("https://custom-api.example.org/dotnet/System.ArgumentException",
                "because ExternalSystemOverrideHandler registered at Order=-2000 must override MicrosoftDocsNamespaceHandler.");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
