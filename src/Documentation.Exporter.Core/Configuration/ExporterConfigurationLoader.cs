using System.Text.Json;
using System.Text.Json.Serialization;
using Documentation.Exporter.Abstractions.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Configuration;

/// <summary>
/// Loads and resolves <see cref="ExporterOptions"/> from a declarative JSON configuration file (<c>doc-exporter.json</c>).
/// </summary>
/// <param name="fileSystem">The file system abstraction.</param>
public sealed class ExporterConfigurationLoader(IFileSystem fileSystem) : IExporterConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly IFileSystem fileSystem = ArgumentGuard.EnsureNotNull(fileSystem);

    /// <inheritdoc />
    public async Task<ExporterOptions> LoadAsync(string configurationFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationFilePath);

        var fullConfigPath = this.fileSystem.GetFullPath(configurationFilePath);
        if (!this.fileSystem.FileExists(fullConfigPath))
        {
            throw new FileNotFoundException(
                ExecutionMessagesResources.FormatMessage(CliCommandResources.ConfigFileNotFound, fullConfigPath),
                fullConfigPath);
        }

        var baseDirectory = Path.GetDirectoryName(fullConfigPath) ?? this.fileSystem.GetCurrentDirectory();
        await using var configStream = this.fileSystem.OpenRead(fullConfigPath);
        var loadedOptions = await JsonSerializer.DeserializeAsync<ExporterOptions>(configStream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? new ExporterOptions();

        if (!string.IsNullOrWhiteSpace(loadedOptions.SolutionFilePath))
        {
            loadedOptions.SolutionFilePath = ResolvePath(baseDirectory, loadedOptions.SolutionFilePath);
        }

        if (loadedOptions.HasExplicitAssemblyFilePath)
        {
            loadedOptions.AssemblyFilePath = ResolvePath(baseDirectory, loadedOptions.AssemblyFilePath);
        }

        if (loadedOptions.HasExplicitXmlDocumentationFilePath)
        {
            loadedOptions.XmlDocumentationFilePath = ResolvePath(baseDirectory, loadedOptions.XmlDocumentationFilePath);
        }

        if (!string.IsNullOrWhiteSpace(loadedOptions.OutputDirectoryPath))
        {
            loadedOptions.OutputDirectoryPath = ResolvePath(baseDirectory, loadedOptions.OutputDirectoryPath);
        }

        if (!string.IsNullOrWhiteSpace(loadedOptions.ExamplesDirectoryPath))
        {
            loadedOptions.ExamplesDirectoryPath = ResolvePath(baseDirectory, loadedOptions.ExamplesDirectoryPath);
        }

        if (!string.IsNullOrWhiteSpace(loadedOptions.OverridesDirectoryPath))
        {
            loadedOptions.OverridesDirectoryPath = ResolvePath(baseDirectory, loadedOptions.OverridesDirectoryPath);
        }

        if (!string.IsNullOrWhiteSpace(loadedOptions.NavigationFilePath))
        {
            loadedOptions.NavigationFilePath = ResolvePath(baseDirectory, loadedOptions.NavigationFilePath);
        }

        if (!string.IsNullOrWhiteSpace(loadedOptions.ReportFilePath))
        {
            loadedOptions.ReportFilePath = ResolvePath(baseDirectory, loadedOptions.ReportFilePath);
        }

        if (loadedOptions.AssemblyFilePaths.Count > 0)
        {
            loadedOptions.AssemblyFilePaths = [.. loadedOptions.AssemblyFilePaths.Select(path => ResolvePath(baseDirectory, path))];
        }

        if (loadedOptions.XmlDocumentationFilePaths.Count > 0)
        {
            loadedOptions.XmlDocumentationFilePaths = [.. loadedOptions.XmlDocumentationFilePaths.Select(path => ResolvePath(baseDirectory, path))];
        }

        return loadedOptions;
    }

    private string ResolvePath(string baseDirectory, string candidatePath) =>
        Path.IsPathRooted(candidatePath)
            ? this.fileSystem.GetFullPath(candidatePath)
            : this.fileSystem.GetFullPath(Path.Combine(baseDirectory, candidatePath));
}
