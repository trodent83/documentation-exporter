using System.Runtime.Loader;
using Documentation.Exporter.Abstractions.Generation.Diagrams;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Plugins;
using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Core.IO;
using Documentation.Exporter.Core.Plugins;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Plugins;

public sealed class DirectoryPluginLoaderTests
{
    private sealed class CustomD2DiagramGenerator : ITypeDiagramGenerator, IArchitectureDiagramGenerator
    {
        public DiagramFormat Format => DiagramFormat.Custom("D2");

        public string? StandaloneFileName => "architecture.d2";

        public bool CanGenerate(TypeDocumentationModel typeModel) =>
            !string.IsNullOrWhiteSpace(typeModel.TypeName);

        public string GenerateTypeDiagram(TypeDocumentationModel typeModel) =>
            $"```d2\n{typeModel.TypeName}\n```\n\n";

        public bool CanGenerate(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            types.Count > 0;

        public string GenerateEmbeddedMarkdown(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            "```d2\ndirection: right\n```\n\n";

        public string GenerateStandaloneContent(IReadOnlyList<TypeDocumentationModel> types, bool includeAssemblyDirectory) =>
            "direction: right\n";
    }

    private sealed class CustomD2DiagramsPlugin : IDocumentationExporterPlugin
    {
        public string Name => "Custom D2 Diagrams Plugin";

        public void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddSingleton<ITypeDiagramGenerator, CustomD2DiagramGenerator>();
            _ = services.AddSingleton<IArchitectureDiagramGenerator, CustomD2DiagramGenerator>();
        }
    }

    [Fact]
    [UnitTest]
    public void ConstructorAndLoadPluginsWhenArgumentsInvalidShouldThrowExpectedExceptions()
    {
        // Arrange
        DirectoryPluginLoader loader = new(new PhysicalFileSystem());
        ServiceCollection services = new();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new DirectoryPluginLoader(null!));
        _ = Assert.Throws<ArgumentNullException>(() => loader.LoadPluginsFromDirectory(null!, "plugins"));
        _ = Assert.Throws<ArgumentNullException>(() => loader.LoadPluginsFromDirectory(services, null!));
        _ = Assert.Throws<ArgumentException>(() => loader.LoadPluginsFromDirectory(services, "   "));
    }

    [Fact]
    [UnitTest]
    public void LoadPluginsFromDirectoryWhenDirectoryDoesNotExistShouldReturnEmptyListWithoutThrowing()
    {
        // Arrange
        var missingPluginsDirectory = Path.Combine(Path.GetTempPath(), "missing-plugins");
        Mock<IFileSystem> mockFileSystem = new();
        _ = mockFileSystem.Setup(fileSystem => fileSystem.GetFullPath(It.IsAny<string>())).Returns(missingPluginsDirectory);
        _ = mockFileSystem.Setup(fileSystem => fileSystem.DirectoryExists(missingPluginsDirectory)).Returns(false);

        Mock<ILogger<DirectoryPluginLoader>> mockLogger = new();
        _ = mockLogger.Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        DirectoryPluginLoader loader = new(mockFileSystem.Object, mockLogger.Object);
        ServiceCollection services = new();

        // Act
        var discoveredPlugins = loader.LoadPluginsFromDirectory(services, "missing-plugins");

        // Assert
        _ = discoveredPlugins.Should().BeEmpty("because a missing plugins directory should gracefully result in zero plugins.");
    }

    [Fact]
    [UnitTest]
    public void LoadPluginsFromDirectoryWhenOnlySharedHostAssembliesPresentShouldSkipThemSafely()
    {
        // Arrange
        var pluginsDirectory = Path.Combine(Path.GetTempPath(), "plugins");
        Mock<IFileSystem> mockFileSystem = new();
        _ = mockFileSystem.Setup(fileSystem => fileSystem.GetFullPath("plugins")).Returns(pluginsDirectory);
        _ = mockFileSystem.Setup(fileSystem => fileSystem.DirectoryExists(pluginsDirectory)).Returns(true);
        _ = mockFileSystem
            .Setup(fileSystem => fileSystem.EnumerateFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories))
            .Returns(
            [
                Path.Combine(pluginsDirectory, "Documentation.Exporter.Abstractions.dll"),
                Path.Combine(pluginsDirectory, "Documentation.Exporter.Core.dll"),
                Path.Combine(pluginsDirectory, "Documentation.Exporter.Cli.dll"),
                Path.Combine(pluginsDirectory, "de", "Documentation.Exporter.Core.resources.dll")
            ]);

        DirectoryPluginLoader loader = new(mockFileSystem.Object);
        ServiceCollection services = new();

        // Act
        var discoveredPlugins = loader.LoadPluginsFromDirectory(services, "plugins");

        // Assert
        _ = discoveredPlugins.Should().BeEmpty("because host-shared and satellite resource assemblies must be skipped.");
    }

    [Fact]
    [IntegrationTest]
    public void LoadPluginsFromDirectoryWithRealAndCorruptDllsShouldIsolateFailuresAndLoadValidPlugin()
    {
        // Arrange
        var tempPluginsRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_PluginsTest_{Guid.NewGuid():N}");
        var validSubdir = Path.Combine(tempPluginsRoot, "StandardDiagrams");
        var sourcePluginDir = Path.Combine(AppContext.BaseDirectory, "plugins", "Documentation.Exporter.Plugins.Diagrams");
        _ = Directory.CreateDirectory(validSubdir);

        try
        {
            foreach (var sourceFile in Directory.GetFiles(sourcePluginDir))
            {
                File.Copy(sourceFile, Path.Combine(validSubdir, Path.GetFileName(sourceFile)), overwrite: true);
            }

            File.WriteAllText(Path.Combine(tempPluginsRoot, "CorruptPlugin.dll"), "not a valid PE binary");

            ServiceCollection services = new();
            _ = services.AddCoreInfrastructure();
            _ = services.AddDiagramGenerators();

            Mock<ILogger<DirectoryPluginLoader>> mockLogger = new();
            _ = mockLogger.Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            DirectoryPluginLoader loader = new(new PhysicalFileSystem(), mockLogger.Object);

            // Act
            var loadedPlugins = loader.LoadPluginsFromDirectory(services, tempPluginsRoot);
            using var provider = services.BuildServiceProvider();

            // Assert
            _ = loadedPlugins.Should().ContainSingle("because the valid StandardDiagramsPlugin must still load despite the corrupt sibling DLL.");
            _ = loadedPlugins[0].PluginName.Should().Be("Standard Diagrams (Mermaid & PlantUML)");
            _ = loadedPlugins[0].AssemblyName.Should().Be("Documentation.Exporter.Plugins.Diagrams");

            var typeGenerators = provider.GetServices<ITypeDiagramGenerator>().ToList();
            _ = typeGenerators.Should().HaveCount(2);
            _ = typeGenerators.Should().AllSatisfy(generator =>
                AssemblyLoadContext.GetLoadContext(generator.GetType().Assembly).Should().NotBeSameAs(AssemblyLoadContext.Default));

            mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(eventId => eventId.Id == 1104),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                "because the corrupt plugin DLL failure must be logged with EventId 1104 without aborting discovery.");
        }
        finally
        {
            if (Directory.Exists(tempPluginsRoot))
            {
                try
                {
                    Directory.Delete(tempPluginsRoot, recursive: true);
                }
                catch (Exception cleanupException) when (cleanupException is UnauthorizedAccessException or IOException)
                {
                    // Windows locks loaded PE files in non-collectible AssemblyLoadContexts until process exit.
                }
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ExportDocumentationWithCustomDiagramFormatPluginShouldRenderCustomSyntaxAndStandaloneFile()
    {
        // Arrange
        var tempOutputDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_CustomD2_{Guid.NewGuid():N}");
        var emptyPluginsDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_Empty_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(emptyPluginsDirectory);

        try
        {
            ServiceCollection services = new();
            _ = services
                .AddDocumentationExporter(emptyPluginsDirectory)
                .AddDocumentationExporterPlugin<CustomD2DiagramsPlugin>();

            await using var provider = services.BuildServiceProvider();
            var engine = provider.GetRequiredService<IDocumentationExporterEngine>();

            var demoLibraryAssembly = typeof(Documentation.Exporter.DemoLibrary.Services.OrderProcessingService).Assembly.Location;
            var demoLibraryXml = Path.ChangeExtension(demoLibraryAssembly, ".xml");

            ExporterOptions options = new()
            {
                AssemblyFilePath = demoLibraryAssembly,
                XmlDocumentationFilePath = demoLibraryXml,
                OutputDirectoryPath = tempOutputDirectory,
                DiagramFormat = DiagramFormat.Custom("D2"),
                CleanOutputDirectory = true
            };

            // Act
            var exportResult = await engine.ExportDocumentationAsync(options);

            // Assert
            _ = exportResult.IsSuccess.Should().BeTrue("because custom plugin diagram formats must be supported end-to-end.");
            var indexMarkdown = await File.ReadAllTextAsync(Path.Combine(tempOutputDirectory, "index.md"));
            var standaloneD2Path = Path.Combine(tempOutputDirectory, "architecture.d2");
            _ = indexMarkdown.Should().Contain("```d2", "because CustomD2DiagramGenerator embeds D2 diagrams.");
            _ = File.Exists(standaloneD2Path).Should().BeTrue("because CustomD2DiagramGenerator declares StandaloneFileName = architecture.d2.");
        }
        finally
        {
            if (Directory.Exists(tempOutputDirectory))
            {
                Directory.Delete(tempOutputDirectory, recursive: true);
            }

            if (Directory.Exists(emptyPluginsDirectory))
            {
                Directory.Delete(emptyPluginsDirectory, recursive: true);
            }
        }
    }
}
