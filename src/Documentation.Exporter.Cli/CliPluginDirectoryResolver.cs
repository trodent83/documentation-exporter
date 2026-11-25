namespace Documentation.Exporter.Cli;

/// <summary>
/// Extracts the optional <c>--plugins-dir</c> startup argument prior to DI container composition
/// so plugin assemblies are discovered before services are built.
/// </summary>
public static class CliPluginDirectoryResolver
{
    private const string PluginsDirectoryOptionFlag = "--plugins-dir";
    private const string PluginsDirectoryOptionPrefix = "--plugins-dir=";

    /// <summary>
    /// Resolves the custom plugins directory path specified in <paramref name="arguments"/>, if present.
    /// </summary>
    /// <param name="arguments">Raw command-line arguments passed to <see cref="Program.Main"/>.</param>
    /// <returns>The trimmed custom plugins directory path, or <see langword="null"/> when omitted or empty.</returns>
    public static string? ResolvePluginsDirectory(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        for (var index = 0; index < arguments.Length; index++)
        {
            var currentArgument = arguments[index];
            if (string.IsNullOrWhiteSpace(currentArgument))
            {
                continue;
            }

            if (currentArgument.StartsWith(PluginsDirectoryOptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var inlinePath = currentArgument[PluginsDirectoryOptionPrefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(inlinePath) ? null : inlinePath;
            }

            if (string.Equals(currentArgument, PluginsDirectoryOptionFlag, StringComparison.OrdinalIgnoreCase) &&
                index + 1 < arguments.Length)
            {
                var nextArgument = arguments[index + 1].Trim();
                return string.IsNullOrWhiteSpace(nextArgument) || nextArgument.StartsWith('-')
                    ? null
                    : nextArgument;
            }
        }

        return null;
    }
}
