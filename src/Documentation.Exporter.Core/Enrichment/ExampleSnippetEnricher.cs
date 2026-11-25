using Documentation.Exporter.Abstractions.Enrichment;
using Documentation.Exporter.Core.Injection;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Enrichment;

/// <summary>
/// Enriches a <see cref="TypeDocumentationModel"/> and its members by querying external code examples.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExampleSnippetEnricher"/> class.
/// </remarks>
/// <param name="exampleProvider">The code example provider.</param>
public sealed class ExampleSnippetEnricher(IExampleProvider exampleProvider) : ITypeDocumentationEnricher
{
    private readonly IExampleProvider provider = ArgumentGuard.EnsureNotNull(exampleProvider);

    /// <inheritdoc />
    public int Order => 20;

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

        var examplesDirectoryPath = options.ExamplesDirectoryPath;
        if (string.IsNullOrWhiteSpace(examplesDirectoryPath))
        {
            return typeModel;
        }

        var baseSegmentName = !string.IsNullOrWhiteSpace(typeModel.DirectorySegmentName)
            ? typeModel.DirectorySegmentName
            : typeModel.TypeName;
        var lookupTypeName = !string.IsNullOrWhiteSpace(typeModel.NamespaceName)
            ? $"{typeModel.NamespaceName}.{baseSegmentName}"
            : baseSegmentName;

        var typeExamples = await provider.GetExamplesAsync(examplesDirectoryPath, lookupTypeName, null, cancellationToken).ConfigureAwait(false);
        if (typeExamples.Count == 0 && !string.Equals(lookupTypeName, typeModel.TypeName, StringComparison.Ordinal))
        {
            typeExamples = await provider.GetExamplesAsync(examplesDirectoryPath, typeModel.TypeName, null, cancellationToken).ConfigureAwait(false);
        }

        var constructors = await AddMemberGroupExamplesAsync(examplesDirectoryPath, lookupTypeName, typeModel.Constructors, cancellationToken).ConfigureAwait(false);
        var properties = await AddMemberGroupExamplesAsync(examplesDirectoryPath, lookupTypeName, typeModel.Properties, cancellationToken).ConfigureAwait(false);
        var methods = await AddMemberGroupExamplesAsync(examplesDirectoryPath, lookupTypeName, typeModel.Methods, cancellationToken).ConfigureAwait(false);
        var fields = await AddMemberGroupExamplesAsync(examplesDirectoryPath, lookupTypeName, typeModel.Fields, cancellationToken).ConfigureAwait(false);
        var events = await AddMemberGroupExamplesAsync(examplesDirectoryPath, lookupTypeName, typeModel.Events, cancellationToken).ConfigureAwait(false);

        return typeModel with
        {
            CodeExamples = [.. typeModel.CodeExamples, .. typeExamples],
            Constructors = constructors,
            Properties = properties,
            Methods = methods,
            Fields = fields,
            Events = events
        };
    }

    private async Task<List<MemberDocumentationModel>> AddMemberGroupExamplesAsync(
        string examplesDirectoryPath,
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
            var memberExamples = await provider.GetExamplesAsync(examplesDirectoryPath, typeName, memberModel.MemberName, cancellationToken).ConfigureAwait(false);
            enrichedMembers.Add(memberExamples.Count == 0
                ? memberModel
                : memberModel with { CodeExamples = [.. memberModel.CodeExamples, .. memberExamples] });
        }

        return enrichedMembers;
    }
}
