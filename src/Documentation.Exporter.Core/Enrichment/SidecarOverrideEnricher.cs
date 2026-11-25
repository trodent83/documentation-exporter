using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Enrichment;

/// <summary>
/// Enriches a <see cref="TypeDocumentationModel"/> by querying sidecar Markdown documentation overrides.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SidecarOverrideEnricher"/> class.
/// </remarks>
/// <param name="documentationInjector">The documentation injector.</param>
public sealed class SidecarOverrideEnricher(IDocumentationInjector documentationInjector) : ITypeDocumentationEnricher
{
    private readonly IDocumentationInjector injector = ArgumentGuard.EnsureNotNull(documentationInjector);

    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public async Task<TypeDocumentationModel> EnrichAsync(
        Type targetType,
        TypeDocumentationModel typeModel,
        ExporterOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(typeModel);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.OverridesDirectoryPath))
        {
            return typeModel;
        }

        var baseSegmentName = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
            ? typeModel.DirectorySegmentName
            : typeModel.TypeName;
        var lookupTypeName = !string.IsNullOrWhiteSpace(typeModel.NamespaceName)
            ? $"{typeModel.NamespaceName}.{baseSegmentName}"
            : baseSegmentName;

        var injections = await injector.GetInjectionsAsync(
            overridesDirectoryPath: options.OverridesDirectoryPath,
            typeName: lookupTypeName,
            memberName: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (injections.Count == 0 && !string.Equals(lookupTypeName, typeModel.TypeName, StringComparison.Ordinal))
        {
            injections = await injector.GetInjectionsAsync(
                overridesDirectoryPath: options.OverridesDirectoryPath,
                typeName: typeModel.TypeName,
                memberName: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var constructors = await AddMemberGroupInjectionsAsync(options.OverridesDirectoryPath, lookupTypeName, typeModel.Constructors, cancellationToken).ConfigureAwait(false);
        var properties = await AddMemberGroupInjectionsAsync(options.OverridesDirectoryPath, lookupTypeName, typeModel.Properties, cancellationToken).ConfigureAwait(false);
        var methods = await AddMemberGroupInjectionsAsync(options.OverridesDirectoryPath, lookupTypeName, typeModel.Methods, cancellationToken).ConfigureAwait(false);
        var fields = await AddMemberGroupInjectionsAsync(options.OverridesDirectoryPath, lookupTypeName, typeModel.Fields, cancellationToken).ConfigureAwait(false);
        var events = await AddMemberGroupInjectionsAsync(options.OverridesDirectoryPath, lookupTypeName, typeModel.Events, cancellationToken).ConfigureAwait(false);

        var typeReplaceSummary = injections.FirstOrDefault(injection =>
            injection.InjectionMode == InjectionMode.Replace &&
            injection.TargetAnchor == SidecarAnchor.ReplaceSummary);

        return typeModel with
        {
            SummaryDescription = typeReplaceSummary != null ? typeReplaceSummary.CustomMarkdownContent : typeModel.SummaryDescription,
            InjectedDocumentationSections = injections.Count == 0
                ? typeModel.InjectedDocumentationSections
                : [.. typeModel.InjectedDocumentationSections, .. injections],
            Constructors = constructors,
            Properties = properties,
            Methods = methods,
            Fields = fields,
            Events = events
        };
    }

    private async Task<List<MemberDocumentationModel>> AddMemberGroupInjectionsAsync(
        string overridesDirectoryPath,
        string typeName,
        IReadOnlyList<MemberDocumentationModel> members,
        CancellationToken cancellationToken)
    {
        if (members.Count == 0)
        {
            return [];
        }

        List<MemberDocumentationModel> enrichedMembers = [];
        foreach (var memberModel in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memberInjections = await injector.GetInjectionsAsync(
                overridesDirectoryPath,
                typeName,
                memberModel.MemberName,
                cancellationToken).ConfigureAwait(false);

            if (memberInjections.Count == 0)
            {
                enrichedMembers.Add(memberModel);
                continue;
            }

            var replaceSummary = memberInjections.FirstOrDefault(injection =>
                injection.InjectionMode == InjectionMode.Replace &&
                injection.TargetAnchor == SidecarAnchor.ReplaceSummary);

            enrichedMembers.Add(memberModel with
            {
                SummaryDescription = replaceSummary != null ? replaceSummary.CustomMarkdownContent : memberModel.SummaryDescription,
                InjectedDocumentationSections = [.. memberModel.InjectedDocumentationSections, .. memberInjections]
            });
        }

        return enrichedMembers;
    }
}
