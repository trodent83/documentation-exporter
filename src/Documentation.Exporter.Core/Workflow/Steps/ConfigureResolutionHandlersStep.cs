using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Configures namespace resolution handlers on the <see cref="ITypeLinkResolver"/> by combining handlers
/// produced by registered <see cref="INamespaceHandlerProvider"/> factories and standalone <see cref="INamespaceHandler"/>
/// registrations, ordered deterministically by precedence.
/// </summary>
/// <param name="typeLinkResolver">The type link resolver.</param>
/// <param name="namespaceHandlerProviders">Registered run-scoped namespace handler providers.</param>
/// <param name="registeredNamespaceHandlers">Optional standalone namespace handlers registered via dependency injection or plugins.</param>
public sealed class ConfigureResolutionHandlersStep(
    ITypeLinkResolver typeLinkResolver,
    IEnumerable<INamespaceHandlerProvider> namespaceHandlerProviders,
    IEnumerable<INamespaceHandler>? registeredNamespaceHandlers = null) : IExportWorkflowStep
{
    private sealed record OrderedHandlerEntry(int EffectiveOrder, int SequenceIndex, INamespaceHandler Handler);

    private readonly ITypeLinkResolver linkResolver = ArgumentGuard.EnsureNotNull(typeLinkResolver);
    private readonly IReadOnlyList<INamespaceHandlerProvider> handlerProviders = [.. ArgumentGuard.EnsureNotNull(namespaceHandlerProviders)];
    private readonly IReadOnlyList<INamespaceHandler> standaloneHandlers = registeredNamespaceHandlers != null ? [.. registeredNamespaceHandlers] : [];

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameConfigureResolutionHandlers;

    /// <inheritdoc />
    public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        linkResolver.ClearHandlers();

        List<OrderedHandlerEntry> orderedEntries = [];
        var sequenceIndex = 0;

        foreach (var provider in handlerProviders)
        {
            if (provider == null)
            {
                continue;
            }

            foreach (var producedHandler in provider.CreateHandlers(context))
            {
                if (producedHandler == null)
                {
                    continue;
                }

                var effectiveOrder = producedHandler.Order != 0 ? producedHandler.Order : provider.Order;
                orderedEntries.Add(new OrderedHandlerEntry(effectiveOrder, sequenceIndex++, producedHandler));
            }
        }

        foreach (var standaloneHandler in standaloneHandlers)
        {
            if (standaloneHandler == null)
            {
                continue;
            }

            orderedEntries.Add(new OrderedHandlerEntry(standaloneHandler.Order, sequenceIndex++, standaloneHandler));
        }

        foreach (var sortedEntry in orderedEntries
                     .OrderBy(entry => entry.EffectiveOrder)
                     .ThenBy(entry => entry.SequenceIndex))
        {
            linkResolver.RegisterHandler(sortedEntry.Handler);
        }

        return Task.FromResult(true);
    }
}
