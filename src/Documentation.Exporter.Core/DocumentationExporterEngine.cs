using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core;

/// <summary>
/// Core orchestrator initiating the documentation export workflow inside an isolated DI scope per run.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DocumentationExporterEngine"/> class.
/// </remarks>
/// <param name="serviceScopeFactory">The dependency injection scope factory used to isolate per-export state and collectible assembly contexts.</param>
/// <param name="logger">Optional ILogger instance.</param>
public sealed partial class DocumentationExporterEngine(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DocumentationExporterEngine>? logger = null) : IDocumentationExporterEngine
{
    private readonly IServiceScopeFactory scopeFactory = ArgumentGuard.EnsureNotNull(serviceScopeFactory);
    private readonly ILogger<DocumentationExporterEngine> logger = logger ?? NullLogger<DocumentationExporterEngine>.Instance;

    [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "{StatusMessage}")]
    private partial void LogStartingExport(string statusMessage);

    /// <inheritdoc />
    public async Task<ExportResult> ExportDocumentationAsync(ExporterOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var startMessage = ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.EngineStartingExport, options.AssemblyFilePath);
        LogStartingExport(startMessage);

        await using var exportScope = scopeFactory.CreateAsyncScope();
        var workflow = exportScope.ServiceProvider.GetRequiredService<IExportWorkflow>();
        ExportWorkflowContext context = new(options);
        return await workflow.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
    }
}
