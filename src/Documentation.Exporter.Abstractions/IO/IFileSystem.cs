namespace Documentation.Exporter.Abstractions.IO;

/// <summary>
/// Abstracts file system and directory operations so core services remain testable and decoupled from <see cref="File"/> and <see cref="Directory"/>.
/// </summary>
public interface IFileSystem
{
    /// <summary>Determines whether the specified file exists.</summary>
    /// <param name="path">The file path to check.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise <see langword="false"/>.</returns>
    bool FileExists(string? path);

    /// <summary>Determines whether the specified directory exists.</summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns><see langword="true"/> if the directory exists; otherwise <see langword="false"/>.</returns>
    bool DirectoryExists(string? path);

    /// <summary>Creates all directories and subdirectories in the specified path unless they already exist.</summary>
    /// <param name="path">The directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>Deletes the specified file.</summary>
    /// <param name="path">The file path to delete.</param>
    void DeleteFile(string path);

    /// <summary>Deletes the specified directory and, if indicated, any subdirectories and files.</summary>
    /// <param name="path">The directory path to delete.</param>
    /// <param name="recursive"><see langword="true"/> to remove directories, subdirectories, and files; otherwise <see langword="false"/>.</param>
    void DeleteDirectory(string path, bool recursive = false);

    /// <summary>Returns an enumerable collection of full file names that match a search pattern and search option in a path.</summary>
    /// <param name="path">The directory path to search.</param>
    /// <param name="searchPattern">The search string to match against file names.</param>
    /// <param name="searchOption">Whether to search top directory only or all subdirectories.</param>
    /// <returns>An enumerable collection of full file paths.</returns>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

    /// <summary>Returns an enumerable collection of full directory names that match a search pattern and search option in a path.</summary>
    /// <param name="path">The directory path to search.</param>
    /// <param name="searchPattern">The search string to match against directory names.</param>
    /// <param name="searchOption">Whether to search top directory only or all subdirectories.</param>
    /// <returns>An enumerable collection of full directory paths.</returns>
    IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

    /// <summary>Determines whether the specified directory contains any file system entries.</summary>
    /// <param name="directoryPath">The directory path to inspect.</param>
    /// <returns><see langword="true"/> if the directory contains one or more files or directories; otherwise <see langword="false"/>.</returns>
    bool HasAnyFileSystemEntries(string directoryPath);

    /// <summary>Opens an existing file for reading.</summary>
    /// <param name="path">The file to open for reading.</param>
    /// <returns>A read-only <see cref="Stream"/> on the specified path.</returns>
    Stream OpenRead(string path);

    /// <summary>Opens a text file, reads all text, and closes the file.</summary>
    /// <param name="path">The file to open for reading.</param>
    /// <returns>A string containing all text in the file.</returns>
    string ReadAllText(string path);

    /// <summary>Asynchronously opens a text file, reads all text, and closes the file.</summary>
    /// <param name="path">The file to open for reading.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task containing all text in the file.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously creates or overwrites a file with the specified string content.</summary>
    /// <param name="path">The target file path.</param>
    /// <param name="contents">The text content to write.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);

    /// <summary>Returns the absolute path for the specified path string.</summary>
    /// <param name="path">The file or directory path.</param>
    /// <returns>The fully qualified location of <paramref name="path"/>.</returns>
    string GetFullPath(string path);

    /// <summary>Gets the current working directory of the application.</summary>
    /// <returns>The path of the current working directory.</returns>
    string GetCurrentDirectory();

    /// <summary>Searches <paramref name="directoryPath"/> for a file matching <paramref name="targetFileName"/> case-insensitively.</summary>
    /// <param name="directoryPath">The directory to search.</param>
    /// <param name="targetFileName">The target file name.</param>
    /// <returns>The matching full file path if found; otherwise <see langword="null"/>.</returns>
    string? FindFileCaseInsensitive(string directoryPath, string targetFileName);

    /// <summary>Searches <paramref name="parentDirectoryPath"/> for a subdirectory matching <paramref name="targetDirectoryName"/> case-insensitively.</summary>
    /// <param name="parentDirectoryPath">The parent directory to search.</param>
    /// <param name="targetDirectoryName">The target subdirectory name.</param>
    /// <returns>The matching full subdirectory path if found; otherwise <see langword="null"/>.</returns>
    string? FindDirectoryCaseInsensitive(string parentDirectoryPath, string targetDirectoryName);
}
