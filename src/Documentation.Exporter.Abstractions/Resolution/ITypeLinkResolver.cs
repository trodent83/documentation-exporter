namespace Documentation.Exporter.Abstractions.Resolution;

/// <summary>
/// Service interface for resolving C# type names or XML cref attributes into Markdown hyperlinks.
/// </summary>
public interface ITypeLinkResolver
{
    /// <summary>
    /// Registers a namespace handler strategy into the resolution pipeline.
    /// </summary>
    /// <param name="namespaceHandler">The namespace handler instance to register.</param>
    void RegisterHandler(INamespaceHandler namespaceHandler);

    /// <summary>
    /// Clears all registered namespace handlers from the resolution pipeline.
    /// </summary>
    void ClearHandlers()
    {
    }

    /// <summary>
    /// Resolves a full type name or cref attribute string into a Markdown link or backticked text.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified name of the type or cref attribute string.</param>
    /// <returns>The resolved Markdown link or formatted string.</returns>
    string ResolveLink(string fullTypeName);
}
