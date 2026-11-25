using Documentation.Exporter.Core.IO;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.IO;

[IntegrationTest]
public sealed class PhysicalFileSystemTests : IDisposable
{
    private readonly PhysicalFileSystem fileSystem = new();
    private readonly string temporaryRootDirectory;

    public PhysicalFileSystemTests()
    {
        temporaryRootDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_PhysicalFs_{Guid.NewGuid():N}");
        fileSystem.CreateDirectory(temporaryRootDirectory);
    }

    [Fact]
    public void ExistsHelpersWhenNullOrWhitespaceShouldReturnFalse()
    {
        // Assert
        _ = fileSystem.FileExists(null).Should().BeFalse("because null paths do not represent files.");
        _ = fileSystem.FileExists("   ").Should().BeFalse("because blank paths do not represent files.");
        _ = fileSystem.DirectoryExists(null).Should().BeFalse("because null paths do not represent directories.");
        _ = fileSystem.DirectoryExists("   ").Should().BeFalse("because blank paths do not represent directories.");
    }

    [Fact]
    public async Task FileAndDirectoryOperationsShouldReadWriteEnumerateAndDeleteProperly()
    {
        // Arrange
        var nestedDirectory = Path.Combine(temporaryRootDirectory, "NestedFolder");
        var filePath = Path.Combine(nestedDirectory, "SampleFile.txt");

        // Act
        fileSystem.CreateDirectory(nestedDirectory);
        await fileSystem.WriteAllTextAsync(filePath, "Hello Physical FileSystem");

        // Assert
        _ = fileSystem.DirectoryExists(nestedDirectory).Should().BeTrue("because the nested directory was created.");
        _ = fileSystem.FileExists(filePath).Should().BeTrue("because the file was written.");
        _ = fileSystem.HasAnyFileSystemEntries(temporaryRootDirectory).Should().BeTrue("because NestedFolder exists under root.");
        _ = fileSystem.GetFullPath(filePath).Should().Be(Path.GetFullPath(filePath));
        _ = fileSystem.GetCurrentDirectory().Should().NotBeNullOrWhiteSpace("because current directory is always valid.");

        _ = fileSystem.ReadAllText(filePath).Should().Be("Hello Physical FileSystem");
        _ = (await fileSystem.ReadAllTextAsync(filePath)).Should().Be("Hello Physical FileSystem");

        await using (var stream = fileSystem.OpenRead(filePath))
        using (StreamReader reader = new(stream))
        {
            _ = (await reader.ReadToEndAsync()).Should().Be("Hello Physical FileSystem");
        }

        _ = fileSystem.EnumerateFiles(nestedDirectory, "*.txt", SearchOption.TopDirectoryOnly)
            .Should().ContainSingle(candidate => candidate == filePath);
        _ = fileSystem.EnumerateDirectories(temporaryRootDirectory)
            .Should().ContainSingle(candidate => candidate == nestedDirectory);

        _ = fileSystem.FindFileCaseInsensitive(nestedDirectory, "samplefile.txt")
            .Should().Be(filePath, "because case-insensitive lookup should match SampleFile.txt.");
        _ = fileSystem.FindFileCaseInsensitive(nestedDirectory, "missing.txt")
            .Should().BeNull("because missing files should return null.");
        _ = fileSystem.FindDirectoryCaseInsensitive(temporaryRootDirectory, "nestedfolder")
            .Should().Be(nestedDirectory, "because case-insensitive directory lookup should match NestedFolder.");
        _ = fileSystem.FindDirectoryCaseInsensitive(temporaryRootDirectory, "missingfolder")
            .Should().BeNull("because missing directories should return null.");

        // Act cleanup
        fileSystem.DeleteFile(filePath);
        _ = fileSystem.FileExists(filePath).Should().BeFalse("because the file was deleted.");
        _ = fileSystem.HasAnyFileSystemEntries(nestedDirectory).Should().BeFalse("because the only file in NestedFolder was deleted.");

        fileSystem.DeleteDirectory(nestedDirectory, recursive: false);
        _ = fileSystem.DirectoryExists(nestedDirectory).Should().BeFalse("because the empty directory was deleted.");
    }

    [Fact]
    public async Task ArgumentGuardsWhenInvalidArgumentsProvidedShouldThrowExpectedExceptions()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => fileSystem.CreateDirectory(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.DeleteFile(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.DeleteDirectory(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.EnumerateFiles(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.EnumerateFiles(temporaryRootDirectory, " "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.EnumerateDirectories(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.EnumerateDirectories(temporaryRootDirectory, " "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.HasAnyFileSystemEntries(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.OpenRead(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.ReadAllText(" "));
        _ = await Assert.ThrowsAsync<ArgumentException>(() => fileSystem.ReadAllTextAsync(" "));
        _ = await Assert.ThrowsAsync<ArgumentException>(() => fileSystem.WriteAllTextAsync(" ", "data"));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fileSystem.WriteAllTextAsync(Path.Combine(temporaryRootDirectory, "x.txt"), null!));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.GetFullPath(" "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.FindFileCaseInsensitive(" ", "a.txt"));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.FindFileCaseInsensitive(temporaryRootDirectory, " "));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.FindDirectoryCaseInsensitive(" ", "dir"));
        _ = Assert.Throws<ArgumentException>(() => fileSystem.FindDirectoryCaseInsensitive(temporaryRootDirectory, " "));
    }

    public void Dispose()
    {
        if (fileSystem.DirectoryExists(temporaryRootDirectory))
        {
            fileSystem.DeleteDirectory(temporaryRootDirectory, recursive: true);
        }
    }
}
