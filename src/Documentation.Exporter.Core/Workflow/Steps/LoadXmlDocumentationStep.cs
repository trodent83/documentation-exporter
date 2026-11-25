using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Loads and indexes compiler-generated XML documentation comments across one or more target XML files.
/// </summary>
/// <param name="xmlDocumentationParser">The XML documentation parser.</param>
/// <param name="artifactDiscoveryService">The export artifact discovery service.</param>
/// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> instance.</param>
public sealed partial class LoadXmlDocumentationStep(
    IXmlDocumentationParser xmlDocumentationParser,
    IExportArtifactDiscoveryService artifactDiscoveryService,
    ILogger<LoadXmlDocumentationStep>? logger = null) : IExportWorkflowStep
{
    private readonly IXmlDocumentationParser xmlParser = ArgumentGuard.EnsureNotNull(xmlDocumentationParser);
    private readonly IExportArtifactDiscoveryService discoveryService = ArgumentGuard.EnsureNotNull(artifactDiscoveryService);
    private readonly ILogger<LoadXmlDocumentationStep> logger = logger ?? NullLogger<LoadXmlDocumentationStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameLoadXmlDocumentation;

    [LoggerMessage(EventId = 814, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogFailedParseXml(string statusMessage);

    /// <inheritdoc />
    public async Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        xmlParser.Clear();

        IReadOnlyList<string> targetXmlPaths;
        try
        {
            targetXmlPaths = context.ResolvedXmlDocumentationFilePaths
                ?? discoveryService.DiscoverXmlDocumentationFilePaths(context.Options);
        }
        catch (FileNotFoundException missingArtifactException)
        {
            LogFailedParseXml(missingArtifactException.Message);
            return false;
        }

        if (targetXmlPaths.Count == 0 &&
            !string.IsNullOrWhiteSpace(context.Options.SolutionFilePath) &&
            !context.Options.HasExplicitXmlDocumentationFilePath &&
            context.Options.XmlDocumentationFilePaths.Count == 0)
        {
            return true;
        }

        var singleXmlPath = targetXmlPaths.Count == 1 ? targetXmlPaths[0] : context.Options.XmlDocumentationFilePath;
        var loadedSuccessfully = targetXmlPaths.Count <= 1
            ? await xmlParser.TryLoadXmlDocumentationAsync(singleXmlPath, cancellationToken).ConfigureAwait(false)
            : await xmlParser.TryLoadXmlDocumentationFilesAsync(targetXmlPaths, cancellationToken).ConfigureAwait(false);

        if (!loadedSuccessfully)
        {
            LogFailedParseXml(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepFailedParseXml, context.Options.XmlDocumentationFilePath));
            return false;
        }

        return true;
    }
}
