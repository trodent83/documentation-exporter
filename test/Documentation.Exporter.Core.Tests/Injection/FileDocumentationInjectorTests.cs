using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Injection;

[IntegrationTest]
public sealed class FileDocumentationInjectorTests : IDisposable
{
    private readonly string temporaryDirectoryPath;

    public FileDocumentationInjectorTests()
    {
        temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), "DocExporter_InjectorTests_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(temporaryDirectoryPath);
    }

    [Fact]
    public async Task GetInjectionsAsyncWhenSidecarFileHasExtendFrontmatterShouldParseExtendMode()
    {
        // Arrange
        var sidecarPath = Path.Combine(temporaryDirectoryPath, "OrderService.md");
        var sidecarContent = @"---
injection_mode: extend
target_anchor: after_remarks
---

## Custom Security Guidelines
Ensure all callers pass an authenticated identity token.";

        await File.WriteAllTextAsync(sidecarPath, sidecarContent);
        var injector = new FileDocumentationInjector(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var injections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "OrderService");

        // Assert
        _ = injections.Should().HaveCount(1, "because one sidecar override file was parsed.");
        var model = injections.Single();
        _ = model.InjectionMode.Should().Be(InjectionMode.Extend, "because frontmatter specified extend mode.");
        _ = model.TargetAnchor.Should().Be(SidecarAnchor.AfterRemarks, "because frontmatter specified after_remarks anchor.");
        _ = model.CustomMarkdownContent.Should().Contain("## Custom Security Guidelines");
    }

    [Fact]
    public async Task GetInjectionsAsyncWhenSidecarFileHasReplaceFrontmatterShouldParseReplaceMode()
    {
        // Arrange
        var sidecarPath = Path.Combine(temporaryDirectoryPath, "PaymentGateway.md");
        var sidecarContent = @"---
injection_mode: replace
target_anchor: replace_summary
---

This payment gateway interface replaces the auto-generated summary completely.";

        await File.WriteAllTextAsync(sidecarPath, sidecarContent);
        var injector = new FileDocumentationInjector(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var injections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "PaymentGateway");

        // Assert
        _ = injections.Should().HaveCount(1, "because one sidecar override file was parsed.");
        var model = injections.Single();
        _ = model.InjectionMode.Should().Be(InjectionMode.Replace, "because frontmatter specified replace mode.");
        _ = model.TargetAnchor.Should().Be(SidecarAnchor.ReplaceSummary, "because frontmatter specified replace_summary anchor.");
        _ = model.CustomMarkdownContent.Should().Be("This payment gateway interface replaces the auto-generated summary completely.");
    }

    [Fact]
    public async Task GetInjectionsAsyncWithInvalidParametersShouldReturnEmpty()
    {
        // Arrange
        var injector = new FileDocumentationInjector(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var nullTypeInjections = await injector.GetInjectionsAsync(temporaryDirectoryPath, null);
        var emptyTypeInjections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "   ");
        var nonExistentOverridesDir = Path.Combine(Path.GetTempPath(), "NonExistent", "Overrides");
        var missingDirInjections = await injector.GetInjectionsAsync(overridesDirectoryPath: nonExistentOverridesDir, typeName: "OrderService");

        // Assert
        _ = nullTypeInjections.Should().BeEmpty("because null typeName cannot resolve an override.");
        _ = emptyTypeInjections.Should().BeEmpty("because whitespace typeName cannot resolve an override.");
        _ = missingDirInjections.Should().BeEmpty("because missing overrides directory returns empty.");
    }

    [Fact]
    public async Task GetInjectionsAsyncWithMemberNameShouldMatchMemberFile()
    {
        // Arrange
        var sidecarPath = Path.Combine(temporaryDirectoryPath, "OrderService.Process.md");
        await File.WriteAllTextAsync(sidecarPath, "### Custom Member Notes");
        var injector = new FileDocumentationInjector(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var injections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "OrderService", "Process");

        // Assert
        _ = injections.Should().HaveCount(1, "because one member sidecar file exists.");
        _ = injections[0].CustomMarkdownContent.Should().Be("### Custom Member Notes");
    }

    [Fact]
    public async Task GetInjectionsAsyncWithQualifiedGenericNameEmptyFileAndMalformedFrontmatterShouldHandleEdgeCases()
    {
        // Arrange
        var emptySidecarPath = Path.Combine(temporaryDirectoryPath, "EmptyType.md");
        await File.WriteAllTextAsync(emptySidecarPath, "   ");

        var frontmatterSidecarPath = Path.Combine(temporaryDirectoryPath, "GenericType.md");
        var frontmatterContent = @"---
malformed_line_without_colon
injection_mode: extend
target_anchor: before_remarks
---
Injected before remarks.";
        await File.WriteAllTextAsync(frontmatterSidecarPath, frontmatterContent);

        var injector = new FileDocumentationInjector(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var emptyInjections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "App.Models.EmptyType`1");
        var genericInjections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "App.Models.GenericType`1");
        var missingFileInjections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "App.Models.NonExistentType");

        // Assert
        _ = emptyInjections.Should().BeEmpty("because whitespace-only sidecar files should be ignored.");
        _ = genericInjections.Should().ContainSingle("because qualified generic type name resolves to GenericType.md.");
        _ = genericInjections[0].TargetAnchor.Should().Be(SidecarAnchor.BeforeRemarks);
        _ = genericInjections[0].CustomMarkdownContent.Should().Be("Injected before remarks.");
        _ = missingFileInjections.Should().BeEmpty("because non-existent sidecar file returns empty.");

        var qualifiedPath = Path.Combine(temporaryDirectoryPath, "App.Specific.GenericType.md");
        await File.WriteAllTextAsync(qualifiedPath, "---\ninjection_mode: extend\ntarget_anchor: page_append\n---\nQualified override with inline --- rule.");
        var qualifiedInjections = await injector.GetInjectionsAsync(temporaryDirectoryPath, "App.Specific.GenericType`1");
        _ = qualifiedInjections.Should().ContainSingle("because namespace-qualified sidecar files take precedence over simple type name files.");
        _ = qualifiedInjections[0].TargetAnchor.Should().Be(SidecarAnchor.PageAppend);
        _ = qualifiedInjections[0].CustomMarkdownContent.Should().Be("Qualified override with inline --- rule.");
    }

    [Fact]
    public async Task GetInjectionsAsyncWhenDirectoryEnumerationThrowsIoErrorShouldReturnEmptyGracefully()
    {
        // Arrange
        var throwingFileSystem = new StubThrowingFileSystem();
        var injector = new FileDocumentationInjector(throwingFileSystem);

        // Act
        var result = await injector.GetInjectionsAsync(temporaryDirectoryPath, "OrderService");

        // Assert
        _ = result.Should().BeEmpty("because IOException during case-insensitive sidecar enumeration should log a warning and return empty.");
        _ = Assert.Throws<ArgumentNullException>(() => new FileDocumentationInjector(null!));
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectoryPath))
        {
            Directory.Delete(temporaryDirectoryPath, true);
        }
    }

    private sealed class StubThrowingFileSystem : Documentation.Exporter.Abstractions.IO.IFileSystem
    {
        public bool FileExists(string? path) => false;
        public bool DirectoryExists(string? path) => true;
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public void DeleteFile(string path) => throw new NotSupportedException();
        public void DeleteDirectory(string path, bool recursive = false) => throw new NotSupportedException();
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => [];
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => [];
        public bool HasAnyFileSystemEntries(string directoryPath) => false;
        public Stream OpenRead(string path) => throw new NotSupportedException();
        public string ReadAllText(string path) => throw new NotSupportedException();
        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public string GetFullPath(string path) => path;
        public string GetCurrentDirectory() => string.Empty;
        public string? FindFileCaseInsensitive(string directoryPath, string targetFileName) => throw new UnauthorizedAccessException("Simulated access denied.");
        public string? FindDirectoryCaseInsensitive(string parentDirectoryPath, string targetDirectoryName) => throw new UnauthorizedAccessException("Simulated access denied.");
    }
}
