using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Integration;

[IntegrationTest]
public sealed class EndToEndExportTests
{
    [Fact]
    public async Task ExportDocumentationAsyncUsingDiContainerShouldGenerateCompleteDocumentationSet()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddDocumentationExporter();
        var serviceProvider = services.BuildServiceProvider();

        var engine = serviceProvider.GetRequiredService<IDocumentationExporterEngine>();

        var tempOutputDir = Path.Combine(Path.GetTempPath(), "DocExporter_E2E_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempOutputDir);

        var currentAssemblyPath = typeof(ExportResult).Assembly.Location;
        var xmlDocumentationPath = Path.ChangeExtension(currentAssemblyPath, ".xml");

        // If XML documentation does not exist at target output, create a minimal dummy file
        var tempXmlCreated = false;
        if (!File.Exists(xmlDocumentationPath))
        {
            xmlDocumentationPath = Path.Combine(tempOutputDir, "TempDocs.xml");
            File.WriteAllText(xmlDocumentationPath, "<doc><assembly><name>Documentation.Exporter.Core</name></assembly><members></members></doc>");
            tempXmlCreated = true;
        }

        var options = new ExporterOptions
        {
            AssemblyFilePath = currentAssemblyPath,
            XmlDocumentationFilePath = xmlDocumentationPath,
            OutputDirectoryPath = tempOutputDir
        };

        try
        {
            // Act
            var result = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = result.Should().NotBeNull();
            _ = result.IsSuccess.Should().BeTrue();
            _ = Directory.GetFiles(tempOutputDir, "*.md").Length.Should().BeGreaterThan(0);
            _ = File.Exists(Path.Combine(tempOutputDir, "index.md")).Should().BeTrue();
        }
        finally
        {
            if (tempXmlCreated && File.Exists(xmlDocumentationPath))
            {
                File.Delete(xmlDocumentationPath);
            }

            if (Directory.Exists(tempOutputDir))
            {
                Directory.Delete(tempOutputDir, recursive: true);
            }
        }
    }
}
