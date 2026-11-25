using System.Runtime.CompilerServices;

namespace Documentation.Exporter.Plugins.Diagrams;

internal static class PluginArgumentGuard
{
    internal static TValue EnsureNotNull<TValue>(
        TValue? argumentValue,
        [CallerArgumentExpression(nameof(argumentValue))] string? parameterName = null)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(argumentValue, parameterName);
        return argumentValue;
    }
}
