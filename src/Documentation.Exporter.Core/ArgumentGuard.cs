using System.Runtime.CompilerServices;

namespace Documentation.Exporter.Core;

/// <summary>
/// Provides modern expression-compatible argument validation helpers delegating to <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/>
/// and <see cref="ArgumentException.ThrowIfNullOrWhiteSpace(string?, string?)"/> for primary constructor field initializers.
/// </summary>
internal static class ArgumentGuard
{
    /// <summary>
    /// Validates that <paramref name="argument"/> is not <see langword="null"/> using <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/>
    /// and returns the validated value.
    /// </summary>
    /// <typeparam name="T">The reference argument type.</typeparam>
    /// <param name="argument">The reference type argument to validate.</param>
    /// <param name="parameterName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <returns>The non-null <paramref name="argument"/>.</returns>
    public static T EnsureNotNull<T>(
        T? argument,
        [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(argument, parameterName);
        return argument;
    }

    /// <summary>
    /// Validates that <paramref name="argument"/> is not <see langword="null"/>, empty, or whitespace using <see cref="ArgumentException.ThrowIfNullOrWhiteSpace(string?, string?)"/>
    /// and returns the trimmed string.
    /// </summary>
    /// <param name="argument">The string argument to validate.</param>
    /// <param name="parameterName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <returns>The validated trimmed string.</returns>
    public static string EnsureNotNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression(nameof(argument))] string? parameterName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, parameterName);
        return argument.Trim();
    }
}
