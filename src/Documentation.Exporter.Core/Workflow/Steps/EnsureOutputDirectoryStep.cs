using System.Text.Json;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Ensures the configured output directory exists on disk.
/// </summary>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class EnsureOutputDirectoryStep(
    IFileSystem fileSystem,
    ILogger<EnsureOutputDirectoryStep>? logger = null) : IExportWorkflowStep
{
    /// <summary>
    /// File name of the manifest tracking generated documentation files inside <see cref="Documentation.Exporter.Abstractions.Models.ExporterOptions.OutputDirectoryPath"/>.
    /// </summary>
    public const string GeneratedFilesManifestFileName = ".docexporter-manifest.json";

    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);
    private readonly ILogger<EnsureOutputDirectoryStep> logger = logger ?? NullLogger<EnsureOutputDirectoryStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameEnsureOutputDirectory;

    [LoggerMessage(EventId = 816, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogFailedCreateOutputDir(Exception exception, string statusMessage);

    [LoggerMessage(EventId = 818, Level = LogLevel.Information, Message = "Cleaned output directory '{OutputDirectoryPath}' prior to generation ({CleanedFileCount} files removed).")]
    private partial void LogCleanedOutputDirectory(string outputDirectoryPath, int cleanedFileCount);

    /// <inheritdoc />
    public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var outputDirectoryPath = context.Options.OutputDirectoryPath;
        if (string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            return Task.FromResult(false);
        }

        if (this.fileSystem.DirectoryExists(outputDirectoryPath))
        {
            if (context.Options.CleanOutputDirectory)
            {
                CleanDirectory(outputDirectoryPath, cancellationToken);
            }

            return Task.FromResult(true);
        }

        try
        {
            this.fileSystem.CreateDirectory(outputDirectoryPath);
            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            LogFailedCreateOutputDir(exception, ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepFailedCreateOutputDir, outputDirectoryPath));
            return Task.FromResult(false);
        }
    }

    internal static IReadOnlyList<string> ParseManifestEntries(string rawManifestContent)
    {
        if (string.IsNullOrWhiteSpace(rawManifestContent))
        {
            return [];
        }

        var trimmed = rawManifestContent.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                var parsedEntries = JsonSerializer.Deserialize<string[]>(trimmed);
                if (parsedEntries != null)
                {
                    return [.. parsedEntries.Where(entry => !string.IsNullOrWhiteSpace(entry)).Select(entry => entry.Trim())];
                }
            }
            catch (JsonException)
            {
                // Fall back to newline-delimited parsing below.
            }
        }

        return trimmed.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private void CleanDirectory(string outputDirectoryPath, CancellationToken cancellationToken)
    {
        var cleanedFileCount = 0;
        var fullOutputRoot = Path.TrimEndingDirectorySeparator(this.fileSystem.GetFullPath(outputDirectoryPath)) + Path.DirectorySeparatorChar;
        var manifestFilePath = Path.Combine(outputDirectoryPath, GeneratedFilesManifestFileName);

        if (this.fileSystem.FileExists(manifestFilePath))
        {
            var manifestEntries = ParseManifestEntries(this.fileSystem.ReadAllText(manifestFilePath));

            foreach (var relativeEntry in manifestEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidatePath = this.fileSystem.GetFullPath(
                    Path.Combine(outputDirectoryPath, relativeEntry.Replace('/', Path.DirectorySeparatorChar)));
                if (candidatePath.StartsWith(fullOutputRoot, StringComparison.OrdinalIgnoreCase) &&
                    this.fileSystem.FileExists(candidatePath))
                {
                    this.fileSystem.DeleteFile(candidatePath);
                    cleanedFileCount++;
                }
            }

            this.fileSystem.DeleteFile(manifestFilePath);
        }
        else
        {
            foreach (var filePath in this.fileSystem.EnumerateFiles(outputDirectoryPath, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extension = Path.GetExtension(filePath);
                if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".puml", StringComparison.OrdinalIgnoreCase))
                {
                    this.fileSystem.DeleteFile(filePath);
                    cleanedFileCount++;
                }
            }
        }

        foreach (var subDirectoryPath in this.fileSystem.EnumerateDirectories(outputDirectoryPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!this.fileSystem.HasAnyFileSystemEntries(subDirectoryPath))
            {
                this.fileSystem.DeleteDirectory(subDirectoryPath);
            }
        }

        LogCleanedOutputDirectory(outputDirectoryPath, cleanedFileCount);
    }
}
