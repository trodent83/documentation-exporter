using Documentation.Exporter.Abstractions.Workflow;

namespace Documentation.Exporter.Abstractions.Resolution;

/// <summary>
/// Factory strategy for producing run-scoped or option-configured <see cref="INamespaceHandler"/> instances
/// during link resolution configuration. Plugins and built-in subsystems implement this interface to dynamically
/// construct handlers from the active <see cref="ExportWorkflowContext"/> (for example, local exported types or URL templates).
/// </summary>
public interface INamespaceHandlerProvider
{
    /// <summary>
    /// Gets the baseline ordering priority for handlers produced by this provider.
    /// Lower values execute earlier in the resolution pipeline; defaults to <c>0</c>.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Creates zero or more <see cref="INamespaceHandler"/> instances configured for the current export run.
    /// </summary>
    /// <param name="context">The active workflow context containing options, reflected types, and run state.</param>
    /// <returns>An ordered sequence of handlers to register on <see cref="ITypeLinkResolver"/>.</returns>
    IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context);
}
