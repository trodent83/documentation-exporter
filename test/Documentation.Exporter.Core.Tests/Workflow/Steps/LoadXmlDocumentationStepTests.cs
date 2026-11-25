using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

public sealed class LoadXmlDocumentationStepTests
{
    private static XmlDocumentationParser CreateDefaultParser()
    {
        var linkResolver = new TypeLinkResolver();
        var transformer = new XmlTagTransformer([]);
        return new XmlDocumentationParser(transformer, linkResolver, new Documentation.Exporter.Core.IO.PhysicalFileSystem());
    }

    private static ExportArtifactDiscoveryService CreateDiscoveryService() =>
        new(new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem()));

    [Fact]
    [UnitTest]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var parser = CreateDefaultParser();
        var step = new LoadXmlDocumentationStep(parser, CreateDiscoveryService());

        // Assert
        _ = step.StepName.Should().Be("Load XML Documentation");
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var parser = CreateDefaultParser();
        var discoveryService = CreateDiscoveryService();
        var step = new LoadXmlDocumentationStep(parser, discoveryService);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new LoadXmlDocumentationStep(null!, discoveryService));
        _ = Assert.Throws<ArgumentNullException>(() => new LoadXmlDocumentationStep(parser, null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null!));
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenXmlValidShouldReturnTrue()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "<doc><assembly><name>Test</name></assembly><members></members></doc>");
        var parser = CreateDefaultParser();
        var step = new LoadXmlDocumentationStep(parser, CreateDiscoveryService());
        var options = new ExporterOptions { XmlDocumentationFilePath = tempFilePath };
        var context = new ExportWorkflowContext(options);

        try
        {
            // Act
            var result = await step.ExecuteAsync(context);

            // Assert
            _ = result.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenXmlNotFoundShouldReturnFalse()
    {
        // Arrange
        var parser = CreateDefaultParser();
        var step = new LoadXmlDocumentationStep(parser, CreateDiscoveryService());
        var options = new ExporterOptions
        {
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "NonExistent", "MissingXml.xml")
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);
        var nullPathResult = await step.ExecuteAsync(new ExportWorkflowContext(new ExporterOptions { XmlDocumentationFilePath = null! }));

        // Assert
        _ = result.Should().BeFalse("because XML documentation file does not exist.");
        _ = nullPathResult.Should().BeFalse();
    }
}
