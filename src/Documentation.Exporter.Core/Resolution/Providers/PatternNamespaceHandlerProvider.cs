using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;

namespace Documentation.Exporter.Core.Resolution.Providers;

/// <summary>
/// Materializes a <see cref="PatternNamespaceHandler"/> for each third-party documentation mapping configured in
/// <see cref="Abstractions.Models.ExporterOptions.ExternalDocLinkRules"/> (<see cref="Order"/> = <c>500</c>).
/// </summary>
public sealed class PatternNamespaceHandlerProvider : INamespaceHandlerProvider
{
    /// <inheritdoc />
    public int Order => 500;

    /// <inheritdoc />
    public IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var externalRule in context.Options.ExternalDocLinkRules)
        {
            yield return new PatternNamespaceHandler(externalRule);
        }
    }
}
