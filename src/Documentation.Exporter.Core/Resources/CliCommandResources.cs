using System.Globalization;
using System.Resources;

namespace Documentation.Exporter.Core.Resources;

/// <summary>
/// Provides strongly-typed localized strings for CLI commands and option descriptions.
/// </summary>
public static class CliCommandResources
{
    /// <summary>
    /// Gets or sets the culture to use for resource lookups, overriding <see cref="CultureInfo.CurrentUICulture"/> when specified.
    /// </summary>
    public static CultureInfo? ResourceCulture { get; set; }

    /// <summary>
    /// Gets the underlying <see cref="System.Resources.ResourceManager"/> instance.
    /// </summary>
    public static ResourceManager ResourceManager { get; } =
        new("Documentation.Exporter.Core.Resources.CliCommandResources", typeof(CliCommandResources).Assembly);

    /// <summary>
    /// Gets the localized description for the CLI root command.
    /// </summary>
    public static string RootCommandDescription => GetResourceString(nameof(RootCommandDescription));

    /// <summary>
    /// Gets the localized description for the <c>--assembly</c> CLI option.
    /// </summary>
    public static string AssemblyOptionDescription => GetResourceString(nameof(AssemblyOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--xml</c> CLI option.
    /// </summary>
    public static string XmlOptionDescription => GetResourceString(nameof(XmlOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--output</c> CLI option.
    /// </summary>
    public static string OutputOptionDescription => GetResourceString(nameof(OutputOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--examples-dir</c> CLI option.
    /// </summary>
    public static string ExamplesOptionDescription => GetResourceString(nameof(ExamplesOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--overrides-dir</c> CLI option.
    /// </summary>
    public static string OverridesOptionDescription => GetResourceString(nameof(OverridesOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--msdocs-url</c> CLI option.
    /// </summary>
    public static string MsDocsOptionDescription => GetResourceString(nameof(MsDocsOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--log-level</c> CLI option.
    /// </summary>
    public static string LogLevelOptionDescription => GetResourceString(nameof(LogLevelOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>assembly</c> CLI subcommand.
    /// </summary>
    public static string AssemblyCommandDescription => GetResourceString(nameof(AssemblyCommandDescription));

    /// <summary>
    /// Gets the localized description for the <c>solution</c> CLI subcommand.
    /// </summary>
    public static string SolutionCommandDescription => GetResourceString(nameof(SolutionCommandDescription));

    /// <summary>
    /// Gets the localized description for the <c>--solution</c> CLI option.
    /// </summary>
    public static string SolutionOptionDescription => GetResourceString(nameof(SolutionOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--configuration</c> CLI option.
    /// </summary>
    public static string ConfigurationOptionDescription => GetResourceString(nameof(ConfigurationOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--include-tests</c> CLI option.
    /// </summary>
    public static string IncludeTestsOptionDescription => GetResourceString(nameof(IncludeTestsOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--external-docs</c> CLI option.
    /// </summary>
    public static string ExternalDocsOptionDescription => GetResourceString(nameof(ExternalDocsOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>config</c> CLI subcommand.
    /// </summary>
    public static string ConfigCommandDescription => GetResourceString(nameof(ConfigCommandDescription));

    /// <summary>
    /// Gets the localized description for the <c>--config</c> CLI option.
    /// </summary>
    public static string ConfigOptionDescription => GetResourceString(nameof(ConfigOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--nav-file</c> CLI option.
    /// </summary>
    public static string NavFileOptionDescription => GetResourceString(nameof(NavFileOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--nav-entry-point</c> CLI option.
    /// </summary>
    public static string NavEntryPointOptionDescription => GetResourceString(nameof(NavEntryPointOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--nav-url-prefix</c> CLI option.
    /// </summary>
    public static string NavUrlPrefixOptionDescription => GetResourceString(nameof(NavUrlPrefixOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--exclude</c> CLI option.
    /// </summary>
    public static string ExcludeOptionDescription => GetResourceString(nameof(ExcludeOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--diagram-format</c> CLI option.
    /// </summary>
    public static string DiagramFormatOptionDescription => GetResourceString(nameof(DiagramFormatOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--clean</c> CLI option.
    /// </summary>
    public static string CleanOptionDescription => GetResourceString(nameof(CleanOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--fail-on-missing</c> CLI option.
    /// </summary>
    public static string FailOnMissingOptionDescription => GetResourceString(nameof(FailOnMissingOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--fail-on-undocumented</c> CLI option.
    /// </summary>
    public static string FailOnUndocumentedOptionDescription => GetResourceString(nameof(FailOnUndocumentedOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--strict</c> CLI option.
    /// </summary>
    public static string StrictOptionDescription => GetResourceString(nameof(StrictOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--exclude-types</c> CLI option.
    /// </summary>
    public static string ExcludeTypesOptionDescription => GetResourceString(nameof(ExcludeTypesOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--exclude-namespaces</c> CLI option.
    /// </summary>
    public static string ExcludeNamespacesOptionDescription => GetResourceString(nameof(ExcludeNamespacesOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--exclude-editor-browsable-never</c> CLI option.
    /// </summary>
    public static string ExcludeEditorBrowsableNeverOptionDescription => GetResourceString(nameof(ExcludeEditorBrowsableNeverOptionDescription));

    /// <summary>
    /// Gets the localized composite error template when a <c>--external-docs</c> rule string has an invalid format.
    /// </summary>
    public static string InvalidExternalDocRuleFormat => GetResourceString(nameof(InvalidExternalDocRuleFormat));

    /// <summary>
    /// Gets the localized composite error template when a <c>--fail-on-undocumented</c> token cannot be parsed as a percentage.
    /// </summary>
    public static string InvalidFailOnUndocumentedPercentage => GetResourceString(nameof(InvalidFailOnUndocumentedPercentage));

    /// <summary>
    /// Gets the localized composite error template when a <c>--config</c> file path does not exist.
    /// </summary>
    public static string ConfigFileNotFound => GetResourceString(nameof(ConfigFileNotFound));

    /// <summary>
    /// Gets the localized composite error template when loading a <c>--config</c> file throws an exception.
    /// </summary>
    public static string ConfigFileLoadFailed => GetResourceString(nameof(ConfigFileLoadFailed));

    /// <summary>
    /// Gets the localized description for the <c>--report-file</c> CLI option.
    /// </summary>
    public static string ReportFileOptionDescription => GetResourceString(nameof(ReportFileOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--report-format</c> CLI option.
    /// </summary>
    public static string ReportFormatOptionDescription => GetResourceString(nameof(ReportFormatOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--nav-format</c> CLI option.
    /// </summary>
    public static string NavFormatOptionDescription => GetResourceString(nameof(NavFormatOptionDescription));

    /// <summary>
    /// Gets the localized description for the <c>--plugins-dir</c> CLI option.
    /// </summary>
    public static string PluginsDirOptionDescription => GetResourceString(nameof(PluginsDirOptionDescription));

    /// <summary>
    /// Gets the localized composite error template when a <c>--diagram-format</c> argument is invalid.
    /// </summary>
    public static string InvalidDiagramFormat => GetResourceString(nameof(InvalidDiagramFormat));

    /// <summary>
    /// Gets the localized composite error template when a <c>--report-format</c> argument is invalid.
    /// </summary>
    public static string InvalidReportFormat => GetResourceString(nameof(InvalidReportFormat));

    /// <summary>
    /// Gets the localized composite error template when a <c>--nav-format</c> argument is invalid.
    /// </summary>
    public static string InvalidNavigationFormat => GetResourceString(nameof(InvalidNavigationFormat));

    private static string GetResourceString(string resourceKey) =>
        ResourceManager.GetString(resourceKey, ResourceCulture ?? CultureInfo.CurrentUICulture)!;
}
