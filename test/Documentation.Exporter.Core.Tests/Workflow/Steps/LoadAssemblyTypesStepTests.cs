using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

public sealed class LoadAssemblyTypesStepTests
{
    private sealed class EmptyAssemblyLoader : IAssemblyLoader
    {
        public bool TryLoadExportedTypes(string? assemblyFilePath, out IReadOnlyDictionary<string, Type>? exportedTypesMap)
        {
            exportedTypesMap = new Dictionary<string, Type>();
            return true;
        }

        public bool TryLoadExportedTypes(IEnumerable<string> assemblyFilePaths, out IReadOnlyDictionary<string, Type>? exportedTypesMap)
        {
            exportedTypesMap = new Dictionary<string, Type>();
            return true;
        }
    }

    private static LoadAssemblyTypesStep CreateStep(IAssemblyLoader? assemblyLoader = null)
    {
        var linkResolver = new TypeLinkResolver();
        var fileSystem = new Documentation.Exporter.Core.IO.PhysicalFileSystem();
        return new LoadAssemblyTypesStep(
            assemblyLoader ?? new IsolatedAssemblyLoader(fileSystem),
            new ExportArtifactDiscoveryService(new SolutionAssemblyResolver(fileSystem)),
            new AssemblyReferenceAnalyzer(fileSystem),
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter());
    }

    [Fact]
    [UnitTest]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var step = CreateStep();

        // Assert
        _ = step.StepName.Should().Be("Load Assembly Types");
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var fileSystem = new Documentation.Exporter.Core.IO.PhysicalFileSystem();
        var loader = new IsolatedAssemblyLoader(fileSystem);
        var discovery = new ExportArtifactDiscoveryService(new SolutionAssemblyResolver(fileSystem));
        var analyzer = new AssemblyReferenceAnalyzer(fileSystem);
        var extractor = new CustomAttributeExtractor(new TypeLinkResolver());
        var formatter = new CSharpSignatureFormatter();
        var step = new LoadAssemblyTypesStep(loader, discovery, analyzer, extractor, formatter);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new LoadAssemblyTypesStep(null!, discovery, analyzer, extractor, formatter));
        _ = Assert.Throws<ArgumentNullException>(() => new LoadAssemblyTypesStep(loader, null!, analyzer, extractor, formatter));
        _ = Assert.Throws<ArgumentNullException>(() => new LoadAssemblyTypesStep(loader, discovery, null!, extractor, formatter));
        _ = Assert.Throws<ArgumentNullException>(() => new LoadAssemblyTypesStep(loader, discovery, analyzer, null!, formatter));
        _ = Assert.Throws<ArgumentNullException>(() => new LoadAssemblyTypesStep(loader, discovery, analyzer, extractor, null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null!));

        var emptyStep = CreateStep(new EmptyAssemblyLoader());
        var emptyResult = await emptyStep.ExecuteAsync(new ExportWorkflowContext(new ExporterOptions()));
        _ = emptyResult.Should().BeFalse("because zero exported types should fail the step.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenAssemblyValidShouldPopulateExportedTypesMap()
    {
        // Arrange
        var step = CreateStep();
        var currentAssemblyPath = typeof(DocumentationExporterEngine).Assembly.Location;
        var options = new ExporterOptions
        {
            AssemblyFilePath = currentAssemblyPath
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because DocumentationExporterEngine assembly exists and exports public types.");
        _ = context.ExportedTypesMap.Should().NotBeNull();
        _ = context.ExportedTypesMap!.ContainsKey(typeof(DocumentationExporterEngine).FullName!).Should().BeTrue();
        _ = context.AssemblyReferencesByAssemblyName.Should().ContainKey("Documentation.Exporter.Core");
        _ = context.AssemblyDependencyEdgesByAssemblyName.Should().ContainKey("Documentation.Exporter.Core");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenAssemblyNotFoundShouldReturnFalse()
    {
        // Arrange
        var step = CreateStep();
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "NonExistent", "MissingAssembly.dll")
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse("because assembly does not exist.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWithExcludedTypesAndNamespacesShouldFilterTypes()
    {
        // Arrange
        var step = CreateStep();
        var currentAssemblyPath = typeof(ExportResult).Assembly.Location;
        var options = new ExporterOptions
        {
            AssemblyFilePath = currentAssemblyPath,
            ExcludedTypes = ["*Result*"],
            ExcludedNamespaces = ["Documentation.Exporter.Core.Workflow*"]
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because unexcluded types remain.");
        _ = context.ExportedTypesMap.Should().NotBeNull();
        _ = context.ExportedTypesMap!.ContainsKey(typeof(ExportResult).FullName!).Should().BeFalse("because ExportResult matches *Result* exclusion.");
        _ = context.ExportedTypesMap!.Values.Any(runtimeType => runtimeType.Namespace?.StartsWith("Documentation.Exporter.Core.Workflow", StringComparison.Ordinal) == true)
            .Should().BeFalse("because workflow namespace is excluded.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenAllTypesExcludedShouldReturnFalse()
    {
        // Arrange
        var step = CreateStep();
        var currentAssemblyPath = typeof(ExportResult).Assembly.Location;
        var options = new ExporterOptions
        {
            AssemblyFilePath = currentAssemblyPath,
            ExcludedNamespaces = ["*"]
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse("because excluding all namespaces results in zero exported types.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWithMultipleAssembliesShouldLoadAllTypes()
    {
        // Arrange
        var step = CreateStep();
        var currentAssemblyPath = typeof(ExportResult).Assembly.Location;
        var options = new ExporterOptions
        {
            AssemblyFilePaths = [currentAssemblyPath, currentAssemblyPath]
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because assemblies are loaded successfully.");
        _ = context.ExportedTypesMap.Should().NotBeNull();
        _ = context.ExportedTypesMap!.ContainsKey(typeof(ExportResult).FullName!).Should().BeTrue();
    }

    [Fact]
    [IntegrationTest]
    public async Task ExecuteAsyncWhenFailOnMissingProjectArtifactsAndSolutionMissingShouldReturnFalse()
    {
        // Arrange
        var step = CreateStep();
        var missingSolutionPath = Path.Combine(Path.GetTempPath(), $"Missing_{Guid.NewGuid():N}.slnx");
        var options = new ExporterOptions
        {
            SolutionFilePath = missingSolutionPath,
            FailOnMissingProjectArtifacts = true
        };
        var context = new ExportWorkflowContext(options);

        // Act
        var result = await step.ExecuteAsync(context);
        var nonExistentWithoutFailFlagResult = await step.ExecuteAsync(new ExportWorkflowContext(new ExporterOptions
        {
            SolutionFilePath = missingSolutionPath,
            FailOnMissingProjectArtifacts = false
        }));

        // Assert
        _ = result.Should().BeFalse("because missing solution artifacts with FailOnMissingProjectArtifacts=true should fail the step gracefully instead of throwing.");
        _ = nonExistentWithoutFailFlagResult.Should().BeFalse("because a solution yielding zero discovered assemblies must fail with NoTypesFound rather than attempting to load the .slnx file as a CLR assembly.");
    }
}
