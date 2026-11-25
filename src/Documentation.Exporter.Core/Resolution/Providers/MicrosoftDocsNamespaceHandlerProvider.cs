using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Core.Resolution.Providers;

/// <summary>
/// Produces the fallback <see cref="MicrosoftDocsNamespaceHandler"/> for BCL <c>System.*</c> and <c>Microsoft.*</c>
/// references (<see cref="Order"/> = <c>1000</c>).
/// </summary>
/// <param name="loggerFactory">Optional logger factory used to create a logger for <see cref="MicrosoftDocsNamespaceHandler"/>.</param>
public sealed class MicrosoftDocsNamespaceHandlerProvider(ILoggerFactory? loggerFactory = null) : INamespaceHandlerProvider
{
    private readonly ILogger<MicrosoftDocsNamespaceHandler>? handlerLogger =
        loggerFactory?.CreateLogger<MicrosoftDocsNamespaceHandler>();

    /// <inheritdoc />
    public int Order => 1000;

    /// <inheritdoc />
    public IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        yield return new MicrosoftDocsNamespaceHandler(context.Options.MicrosoftDocsBaseUrl, handlerLogger);
    }
}
