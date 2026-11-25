using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;

namespace Documentation.Exporter.Core.Resolution.Providers;

/// <summary>
/// Exposes caller-supplied custom <see cref="INamespaceHandler"/> instances configured directly on
/// <see cref="Abstractions.Models.ExporterOptions.CustomNamespaceHandlers"/> (<see cref="Order"/> = <c>-500</c>).
/// </summary>
public sealed class CustomOptionsNamespaceHandlerProvider : INamespaceHandlerProvider
{
    /// <inheritdoc />
    public int Order => -500;

    /// <inheritdoc />
    public IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Options.CustomNamespaceHandlers;
    }
}
