namespace Documentation.Exporter.Abstractions.Resolution;

/// <summary>
/// Strategy interface for resolving type references within specific namespaces into documentation links.
/// </summary>
public interface INamespaceHandler
{
    /// <summary>
    /// Gets the evaluation precedence for this handler when sorting multiple namespace resolution strategies.
    /// Lower values execute earlier in the link resolution chain; defaults to <c>0</c>.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Determines whether this handler can resolve links for the specified namespace or full type name.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified name of the type or cref attribute.</param>
    /// <returns>True if this handler can resolve the type; otherwise false.</returns>
    bool CanHandleNamespace(string fullTypeName);

    /// <summary>
    /// Resolves the specified type into a Markdown hyperlink or URL.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified name of the type or cref attribute.</param>
    /// <returns>The resolved Markdown link or external URL.</returns>
    string ResolveTypeLink(string fullTypeName);
}
