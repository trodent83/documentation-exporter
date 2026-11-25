using Documentation.Exporter.Abstractions.Resolution;
namespace Documentation.Exporter.Core.Resolution;

/// <summary>
/// Orchestrates type link resolution across registered namespace handlers.
/// </summary>
public sealed class TypeLinkResolver : ITypeLinkResolver
{
    private readonly Lock handlersLock = new();
    private readonly List<INamespaceHandler> registeredNamespaceHandlers = [];
    private volatile INamespaceHandler[] handlersSnapshot = [];

    /// <inheritdoc />
    public void RegisterHandler(INamespaceHandler namespaceHandler)
    {
        ArgumentNullException.ThrowIfNull(namespaceHandler);

        lock (handlersLock)
        {
            registeredNamespaceHandlers.Add(namespaceHandler);
            handlersSnapshot = [.. registeredNamespaceHandlers];
        }
    }

    /// <inheritdoc />
    public void ClearHandlers()
    {
        lock (handlersLock)
        {
            registeredNamespaceHandlers.Clear();
            handlersSnapshot = [];
        }
    }

    /// <inheritdoc />
    public string ResolveLink(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return string.Empty;
        }

        var cleanedTypeName = CleanCrefPrefix(fullTypeName);

        foreach (var namespaceHandler in handlersSnapshot)
        {
            if (namespaceHandler.CanHandleNamespace(cleanedTypeName))
            {
                return namespaceHandler.ResolveTypeLink(cleanedTypeName);
            }
        }

        var simpleTypeName = GetSimpleTypeName(cleanedTypeName);
        return $"`{simpleTypeName}`";
    }

    private static string CleanCrefPrefix(string fullTypeName)
    {
        var text = fullTypeName.Trim();
        return text.Length > 2 && text[1] == ':' ? text[2..] : text;
    }

    private static string GetSimpleTypeName(string fullTypeName)
    {
        var sanitized = fullTypeName;
        var doubleBracketIndex = sanitized.IndexOf("[[", StringComparison.Ordinal);
        if (doubleBracketIndex >= 0)
        {
            sanitized = sanitized[..doubleBracketIndex];
        }

        var parenIndex = sanitized.IndexOf('(');
        if (parenIndex >= 0)
        {
            sanitized = sanitized[..parenIndex];
        }

        var backtickIndex = sanitized.IndexOf('`');
        if (backtickIndex >= 0)
        {
            sanitized = sanitized[..backtickIndex];
        }

        var lastDotIndex = sanitized.LastIndexOf('.');
        return lastDotIndex < 0 ? sanitized : sanitized[(lastDotIndex + 1)..];
    }
}
