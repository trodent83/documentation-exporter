using Documentation.Exporter.Abstractions.IO;
namespace Documentation.Exporter.Core.IO;

/// <summary>
/// Default implementation of <see cref="IFileSystem"/> delegating to <see cref="File"/> and <see cref="Directory"/>.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path);

    /// <inheritdoc />
    public bool DirectoryExists(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _ = Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.Delete(path);
    }

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.Delete(path, recursive);
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        return Directory.EnumerateFiles(path, searchPattern, searchOption);
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(
        string path,
        string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        return Directory.EnumerateDirectories(path, searchPattern, searchOption);
    }

    /// <inheritdoc />
    public bool HasAnyFileSystemEntries(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        return Directory.EnumerateFileSystemEntries(directoryPath).Any();
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.OpenRead(path);
    }

    /// <inheritdoc />
    public string ReadAllText(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllText(path);
    }

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);
        return File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    /// <inheritdoc />
    public string GetFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }

    /// <inheritdoc />
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    /// <inheritdoc />
    public string? FindFileCaseInsensitive(string directoryPath, string targetFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFileName);

        return Directory.EnumerateFiles(directoryPath)
            .FirstOrDefault(filePath => string.Equals(Path.GetFileName(filePath), targetFileName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public string? FindDirectoryCaseInsensitive(string parentDirectoryPath, string targetDirectoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectoryName);

        return Directory.EnumerateDirectories(parentDirectoryPath)
            .FirstOrDefault(directoryPath => string.Equals(Path.GetFileName(directoryPath), targetDirectoryName, StringComparison.OrdinalIgnoreCase));
    }
}
