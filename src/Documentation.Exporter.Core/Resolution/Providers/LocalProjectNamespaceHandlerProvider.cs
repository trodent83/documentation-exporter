using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;

namespace Documentation.Exporter.Core.Resolution.Providers;

/// <summary>
/// Produces a <see cref="LocalProjectNamespaceHandler"/> populated with all exported local assembly types
/// and precomputed overloaded/nested member cref routes. Runs earliest (<see cref="Order"/> = <c>-1000</c>).
/// </summary>
/// <param name="memberCrefRouteIndexBuilder">The member cref route index builder.</param>
public sealed class LocalProjectNamespaceHandlerProvider(
    IMemberCrefRouteIndexBuilder memberCrefRouteIndexBuilder) : INamespaceHandlerProvider
{
    private readonly IMemberCrefRouteIndexBuilder routeIndexBuilder = ArgumentGuard.EnsureNotNull(memberCrefRouteIndexBuilder);

    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExportedTypesMap == null)
        {
            yield break;
        }

        var distinctAssemblyCount = context.ExportedTypesMap.Values
            .Select(runtimeType => runtimeType.Assembly.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Count();

        var useAssemblySubdirectories = context.ShouldUseAssemblySubdirectories(distinctAssemblyCount);

        var memberRoutes = routeIndexBuilder.BuildMemberCrefRoutes(
            context.ExportedTypesMap,
            useAssemblySubdirectories,
            context.Options.ExcludeEditorBrowsableNever);

        yield return LocalProjectNamespaceHandler.FromExportedTypesMap(
            context.ExportedTypesMap,
            useAssemblySubdirectories,
            memberRoutes);
    }
}
