using Documentation.Exporter.Core.Injection;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Injection;

[IntegrationTest]
public sealed class FileExampleProviderTests : IDisposable
{
    private readonly string temporaryDirectoryPath;

    public FileExampleProviderTests()
    {
        temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), "DocExporter_ExampleTests_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(temporaryDirectoryPath);
    }

    [Fact]
    public async Task GetExamplesAsyncWhenSnippetFileExistsShouldReturnSnippetContent()
    {
        // Arrange
        var typeFolder = Path.Combine(temporaryDirectoryPath, "OrderProcessor");
        _ = Directory.CreateDirectory(typeFolder);

        var snippetFilePath = Path.Combine(typeFolder, "ProcessOrder.cs");
        await File.WriteAllTextAsync(snippetFilePath, "// Sample C# code for ProcessOrder");

        var exampleProvider = new FileExampleProvider(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var snippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "OrderProcessor", "ProcessOrder");

        // Assert
        _ = snippets.Should().NotBeNull("because the snippet list should be initialized.")
            .And.HaveCount(1, "because one snippet file was provided.")
            .And.Contain("// Sample C# code for ProcessOrder");
    }

    [Fact]
    public async Task GetExamplesAsyncWhenTypeExampleFileExistsShouldReturnTypeSnippetContent()
    {
        // Arrange
        var typeFilePath = Path.Combine(temporaryDirectoryPath, "OrderProcessor.cs");
        await File.WriteAllTextAsync(typeFilePath, "// Sample C# code for entire OrderProcessor");
        var exampleProvider = new FileExampleProvider(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var snippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "OrderProcessor");

        // Assert
        _ = snippets.Should().HaveCount(1, "because one type snippet file was provided.")
            .And.Contain("// Sample C# code for entire OrderProcessor");
    }

    [Fact]
    public async Task GetExamplesAsyncWhenTypeExampleFileEmptyOrMissingShouldReturnEmpty()
    {
        // Arrange
        var emptyTypeFilePath = Path.Combine(temporaryDirectoryPath, "EmptyType.cs");
        await File.WriteAllTextAsync(emptyTypeFilePath, "   ");
        var exampleProvider = new FileExampleProvider(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var emptySnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "EmptyType");
        var missingSnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "MissingType");

        // Assert
        _ = emptySnippets.Should().BeEmpty("because whitespace-only snippet files are ignored.");
        _ = missingSnippets.Should().BeEmpty("because missing files yield no snippets.");
    }

    [Fact]
    public async Task GetExamplesAsyncWhenMemberFileEmptyOrMissingShouldReturnEmpty()
    {
        // Arrange
        var typeFolder = Path.Combine(temporaryDirectoryPath, "OrderProcessor");
        _ = Directory.CreateDirectory(typeFolder);
        var emptyMemberFilePath = Path.Combine(typeFolder, "EmptyMethod.cs");
        await File.WriteAllTextAsync(emptyMemberFilePath, "   ");
        var exampleProvider = new FileExampleProvider(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var emptySnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "OrderProcessor", "EmptyMethod");
        var missingMemberSnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "OrderProcessor", "NonExistentMethod");
        var missingTypeSnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "NonExistentType", "SomeMethod");

        // Assert
        _ = emptySnippets.Should().BeEmpty("because whitespace-only member snippet files are ignored.");
        _ = missingMemberSnippets.Should().BeEmpty("because missing member snippet files return empty.");
        _ = missingTypeSnippets.Should().BeEmpty("because missing type folder returns empty.");
    }

    [Fact]
    public async Task GetExamplesAsyncWithInvalidParametersShouldReturnEmpty()
    {
        // Arrange
        var exampleProvider = new FileExampleProvider(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var nullTypeSnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, null);
        var emptyTypeSnippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "   ");
        var nonExistentExamplesDir = Path.Combine(Path.GetTempPath(), "NonExistent", "ExamplesFolder");
        var invalidDirSnippets = await exampleProvider.GetExamplesAsync(nonExistentExamplesDir, "OrderProcessor");

        // Assert
        _ = nullTypeSnippets.Should().BeEmpty("because null typeName cannot resolve a snippet.");
        _ = emptyTypeSnippets.Should().BeEmpty("because whitespace typeName cannot resolve a snippet.");
        _ = invalidDirSnippets.Should().BeEmpty("because a missing examples directory returns empty.");
    }

    [Fact]
    public async Task GetExamplesAsyncWithQualifiedAndGenericNamesShouldCleanCorrectly()
    {
        // Arrange
        var typeFilePath = Path.Combine(temporaryDirectoryPath, "GenericRepository.cs");
        await File.WriteAllTextAsync(typeFilePath, "// Generic snippet");
        var exampleProvider = new FileExampleProvider(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act
        var snippets = await exampleProvider.GetExamplesAsync(temporaryDirectoryPath, "MyNamespace.GenericRepository`1");

        // Assert
        _ = snippets.Should().HaveCount(1, "because generic arity and namespace prefix must be stripped cleanly.")
            .And.Contain("// Generic snippet");
    }

    [Fact]
    public async Task GetExamplesAsyncWhenDirectoryEnumerationThrowsIoOrAccessErrorShouldReturnEmptyGracefully()
    {
        // Arrange
        var ioThrowingFileSystem = new StubThrowingEnumerationFileSystem(() => new IOException("Simulated I/O failure."));
        var accessThrowingFileSystem = new StubThrowingEnumerationFileSystem(() => new UnauthorizedAccessException("Simulated access denied."));

        var ioProvider = new FileExampleProvider(ioThrowingFileSystem);
        var accessProvider = new FileExampleProvider(accessThrowingFileSystem);

        // Act
        var memberSnippets = await ioProvider.GetExamplesAsync(temporaryDirectoryPath, "OrderProcessor", "ProcessOrder");
        var typeSnippets = await accessProvider.GetExamplesAsync(temporaryDirectoryPath, "OrderProcessor");

        // Assert
        _ = memberSnippets.Should().BeEmpty("because IOException during case-insensitive enumeration should log a warning and return empty.");
        _ = typeSnippets.Should().BeEmpty("because UnauthorizedAccessException during enumeration should log a warning and return empty.");
        _ = Assert.Throws<ArgumentNullException>(() => new FileExampleProvider(null!));
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectoryPath))
        {
            Directory.Delete(temporaryDirectoryPath, true);
        }
    }

    private sealed class StubThrowingEnumerationFileSystem(Func<Exception> exceptionFactory) : Documentation.Exporter.Abstractions.IO.IFileSystem
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
        public string? FindFileCaseInsensitive(string directoryPath, string targetFileName) => throw exceptionFactory();
        public string? FindDirectoryCaseInsensitive(string parentDirectoryPath, string targetDirectoryName) => throw exceptionFactory();
    }
}
