using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Centralizes shared CLI option definitions, parsing, and <see cref="ExporterOptions"/> binding.
/// </summary>
/// <param name="consoleOutput">The console error output writer.</param>
public sealed class SharedCliOptions(IConsoleOutput consoleOutput) : ISharedCliOptions
{
    private readonly IConsoleOutput console = ArgumentGuard.EnsureNotNull(consoleOutput);

    public Option<DirectoryInfo> Output { get; } = new(
        aliases: ["--output", "-o"],
        description: CliCommandResources.OutputOptionDescription)
    {
        IsRequired = true
    };

    public Option<DirectoryInfo?> OptionalOutput { get; } = new(
        aliases: ["--output", "-o"],
        description: CliCommandResources.OutputOptionDescription);

    public Option<DirectoryInfo?> ExamplesDir { get; } = new(
        aliases: ["--examples-dir", "-e"],
        description: CliCommandResources.ExamplesOptionDescription);

    public Option<DirectoryInfo?> OverridesDir { get; } = new(
        aliases: ["--overrides-dir", "-v"],
        description: CliCommandResources.OverridesOptionDescription);

    public Option<string> MsDocsUrl { get; } = new(
        aliases: ["--msdocs-url", "-m"],
        getDefaultValue: () => "https://learn.microsoft.com/en-us/dotnet/api/",
        description: CliCommandResources.MsDocsOptionDescription);

    public Option<string[]> ExternalDocs { get; } = new(
        aliases: ["--external-docs", "-d"],
        getDefaultValue: () => [],
        description: CliCommandResources.ExternalDocsOptionDescription)
    {
        AllowMultipleArgumentsPerToken = true
    };

    public Option<FileInfo?> NavFile { get; } = new(
        aliases: ["--nav-file", "-n"],
        description: CliCommandResources.NavFileOptionDescription);

    /// <summary>Gets the target navigation generator format option (<c>--nav-format</c>).</summary>
    public Option<NavigationFormat> NavFormat { get; } = new(
        aliases: ["--nav-format"],
        parseArgument: ParseNavigationFormat,
        isDefault: true,
        description: CliCommandResources.NavFormatOptionDescription);

    public Option<string?> NavEntryPoint { get; } = new(
        aliases: ["--nav-entry-point", "-p"],
        description: CliCommandResources.NavEntryPointOptionDescription);

    public Option<string?> NavUrlPrefix { get; } = new(
        aliases: ["--nav-url-prefix", "-u"],
        description: CliCommandResources.NavUrlPrefixOptionDescription);

    public Option<string[]> ExcludeProjects { get; } = new(
        aliases: ["--exclude"],
        getDefaultValue: () => [],
        description: CliCommandResources.ExcludeOptionDescription)
    {
        AllowMultipleArgumentsPerToken = true
    };

    public Option<string[]> ExcludeTypes { get; } = new(
        aliases: ["--exclude-types"],
        getDefaultValue: () => [],
        description: CliCommandResources.ExcludeTypesOptionDescription)
    {
        AllowMultipleArgumentsPerToken = true
    };

    public Option<string[]> ExcludeNamespaces { get; } = new(
        aliases: ["--exclude-namespaces"],
        getDefaultValue: () => [],
        description: CliCommandResources.ExcludeNamespacesOptionDescription)
    {
        AllowMultipleArgumentsPerToken = true
    };

    public Option<bool> ExcludeEditorBrowsableNever { get; } = new(
        aliases: ["--exclude-editor-browsable-never"],
        getDefaultValue: () => true,
        description: CliCommandResources.ExcludeEditorBrowsableNeverOptionDescription);

    public Option<DiagramFormat> DiagramFormat { get; } = new(
        aliases: ["--diagram-format", "-g"],
        parseArgument: ParseDiagramFormat,
        isDefault: true,
        description: CliCommandResources.DiagramFormatOptionDescription);

    /// <summary>Gets the optional custom plugins directory option (<c>--plugins-dir</c>).</summary>
    public Option<DirectoryInfo?> PluginsDir { get; } = new(
        aliases: ["--plugins-dir"],
        description: CliCommandResources.PluginsDirOptionDescription);

    /// <inheritdoc />
    public Option<LogLevel> LogLevel { get; } = new(
        aliases: ["--log-level", "-l"],
        getDefaultValue: () => Microsoft.Extensions.Logging.LogLevel.Information,
        description: CliCommandResources.LogLevelOptionDescription);

    public Option<bool> Clean { get; } = new(
        aliases: ["--clean"],
        getDefaultValue: () => false,
        description: CliCommandResources.CleanOptionDescription);

    public Option<bool> FailOnMissing { get; } = new(
        aliases: ["--fail-on-missing"],
        getDefaultValue: () => false,
        description: CliCommandResources.FailOnMissingOptionDescription);

    public Option<double?> FailOnUndocumented { get; } = new(
        aliases: ["--fail-on-undocumented"],
        parseArgument: ParseOptionalPercentage,
        isDefault: false,
        description: CliCommandResources.FailOnUndocumentedOptionDescription);

    public Option<bool> Strict { get; } = new(
        aliases: ["--strict"],
        getDefaultValue: () => false,
        description: CliCommandResources.StrictOptionDescription);

    public Option<FileInfo?> ReportFile { get; } = new(
        aliases: ["--report-file", "-r"],
        description: CliCommandResources.ReportFileOptionDescription);

    public Option<QualityReportFormat> ReportFormat { get; } = new(
        aliases: ["--report-format"],
        parseArgument: ParseQualityReportFormat,
        isDefault: true,
        description: CliCommandResources.ReportFormatOptionDescription);

    public static (Option<FileInfo[]> AssemblyOption, Option<FileInfo[]> XmlOption) CreateAssemblyInputOptions()
    {
        Option<FileInfo[]> assemblyOption = new(
            aliases: ["--assembly", "-a"],
            description: CliCommandResources.AssemblyOptionDescription)
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        Option<FileInfo[]> xmlOption = new(
            aliases: ["--xml", "-x"],
            getDefaultValue: () => [],
            description: CliCommandResources.XmlOptionDescription)
        {
            AllowMultipleArgumentsPerToken = true
        };

        return (assemblyOption, xmlOption);
    }

    public static void ApplyAssemblyInputs(
        ExporterOptions exporterOptions,
        ParseResult parseResult,
        Option<FileInfo[]> assemblyOption,
        Option<FileInfo[]> xmlOption)
    {
        ArgumentNullException.ThrowIfNull(exporterOptions);
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(assemblyOption);
        ArgumentNullException.ThrowIfNull(xmlOption);

        var assemblyFiles = parseResult.GetValueForOption(assemblyOption) ?? [];
        var xmlFiles = parseResult.GetValueForOption(xmlOption) ?? [];

        exporterOptions.AssemblyFilePaths = [.. assemblyFiles.Select(file => file.FullName)];
        exporterOptions.XmlDocumentationFilePaths = [.. xmlFiles.Select(file => file.FullName)];
    }

    /// <inheritdoc />
    public void AddExportOptionsTo(Command targetCommand, bool includeFailOnMissing)
    {
        ArgumentNullException.ThrowIfNull(targetCommand);

        targetCommand.AddOption(ExcludeProjects);
        targetCommand.AddOption(ExcludeTypes);
        targetCommand.AddOption(ExcludeNamespaces);
        targetCommand.AddOption(ExcludeEditorBrowsableNever);
        targetCommand.AddOption(Output);
        targetCommand.AddOption(ExamplesDir);
        targetCommand.AddOption(OverridesDir);
        targetCommand.AddOption(MsDocsUrl);
        targetCommand.AddOption(ExternalDocs);
        targetCommand.AddOption(NavFile);
        targetCommand.AddOption(NavFormat);
        targetCommand.AddOption(NavEntryPoint);
        targetCommand.AddOption(NavUrlPrefix);
        targetCommand.AddOption(DiagramFormat);
        targetCommand.AddOption(Clean);
        if (includeFailOnMissing)
        {
            targetCommand.AddOption(FailOnMissing);
        }

        targetCommand.AddOption(FailOnUndocumented);
        targetCommand.AddOption(Strict);
        targetCommand.AddOption(ReportFile);
        targetCommand.AddOption(ReportFormat);
        targetCommand.AddOption(PluginsDir);
        targetCommand.AddOption(LogLevel);
    }

    /// <inheritdoc />
    public void AddConfigOverrideOptionsTo(Command targetCommand)
    {
        ArgumentNullException.ThrowIfNull(targetCommand);

        targetCommand.AddOption(OptionalOutput);
        targetCommand.AddOption(Clean);
        targetCommand.AddOption(Strict);
        targetCommand.AddOption(FailOnUndocumented);
        targetCommand.AddOption(FailOnMissing);
        targetCommand.AddOption(DiagramFormat);
        targetCommand.AddOption(NavFormat);
        targetCommand.AddOption(ReportFile);
        targetCommand.AddOption(ReportFormat);
        targetCommand.AddOption(PluginsDir);
    }

    /// <inheritdoc />
    public bool TryCreateBaseExporterOptions(
        ParseResult parseResult,
        out ExporterOptions? exporterOptions,
        out LogLevel logLevel)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        logLevel = parseResult.GetValueForOption(LogLevel);
        var rawExternalDocRules = parseResult.GetValueForOption(ExternalDocs) ?? [];

        List<ExternalDocLinkRule> parsedExternalDocRules;
        try
        {
            parsedExternalDocRules = ParseExternalDocRules(rawExternalDocRules);
        }
        catch (ArgumentException argumentException)
        {
            console.WriteErrorLine(argumentException.Message);
            exporterOptions = null;
            return false;
        }

        var outputDirectory = parseResult.GetValueForOption(Output)!;
        exporterOptions = new ExporterOptions
        {
            ExcludedProjects = [.. parseResult.GetValueForOption(ExcludeProjects) ?? []],
            ExcludedTypes = [.. parseResult.GetValueForOption(ExcludeTypes) ?? []],
            ExcludedNamespaces = [.. parseResult.GetValueForOption(ExcludeNamespaces) ?? []],
            ExcludeEditorBrowsableNever = parseResult.GetValueForOption(ExcludeEditorBrowsableNever),
            OutputDirectoryPath = outputDirectory.FullName,
            ExamplesDirectoryPath = parseResult.GetValueForOption(ExamplesDir)?.FullName,
            OverridesDirectoryPath = parseResult.GetValueForOption(OverridesDir)?.FullName,
            MicrosoftDocsBaseUrl = parseResult.GetValueForOption(MsDocsUrl)!,
            ExternalDocLinkRules = parsedExternalDocRules,
            NavigationFilePath = parseResult.GetValueForOption(NavFile)?.FullName,
            NavigationFormat = parseResult.GetValueForOption(NavFormat),
            NavigationEntryPoint = parseResult.GetValueForOption(NavEntryPoint),
            NavigationUrlPrefix = parseResult.GetValueForOption(NavUrlPrefix),
            DiagramFormat = parseResult.GetValueForOption(DiagramFormat),
            CleanOutputDirectory = parseResult.GetValueForOption(Clean),
            FailOnMissingProjectArtifacts = parseResult.GetValueForOption(FailOnMissing),
            FailOnUndocumentedThreshold = parseResult.GetValueForOption(FailOnUndocumented),
            StrictLinkValidation = parseResult.GetValueForOption(Strict),
            ReportFilePath = parseResult.GetValueForOption(ReportFile)?.FullName,
            ReportFormat = parseResult.GetValueForOption(ReportFormat)
        };

        return true;
    }

    /// <inheritdoc />
    public void ApplyOptionalOverrides(ExporterOptions exporterOptions, ParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(exporterOptions);
        ArgumentNullException.ThrowIfNull(parseResult);

        if (parseResult.FindResultFor(OptionalOutput) is { IsImplicit: false } &&
            parseResult.GetValueForOption(OptionalOutput) is { } overrideOutputDir)
        {
            exporterOptions.OutputDirectoryPath = overrideOutputDir.FullName;
        }

        if (parseResult.FindResultFor(Clean) is { IsImplicit: false })
        {
            exporterOptions.CleanOutputDirectory = parseResult.GetValueForOption(Clean);
        }

        if (parseResult.FindResultFor(Strict) is { IsImplicit: false })
        {
            exporterOptions.StrictLinkValidation = parseResult.GetValueForOption(Strict);
        }

        if (parseResult.FindResultFor(FailOnUndocumented) is { IsImplicit: false })
        {
            exporterOptions.FailOnUndocumentedThreshold = parseResult.GetValueForOption(FailOnUndocumented);
        }

        if (parseResult.FindResultFor(FailOnMissing) is { IsImplicit: false })
        {
            exporterOptions.FailOnMissingProjectArtifacts = parseResult.GetValueForOption(FailOnMissing);
        }

        if (parseResult.FindResultFor(DiagramFormat) is { IsImplicit: false })
        {
            exporterOptions.DiagramFormat = parseResult.GetValueForOption(DiagramFormat);
        }

        if (parseResult.FindResultFor(NavFormat) is { IsImplicit: false })
        {
            exporterOptions.NavigationFormat = parseResult.GetValueForOption(NavFormat);
        }

        if (parseResult.FindResultFor(ReportFile) is { IsImplicit: false })
        {
            exporterOptions.ReportFilePath = parseResult.GetValueForOption(ReportFile)?.FullName;
        }

        if (parseResult.FindResultFor(ReportFormat) is { IsImplicit: false })
        {
            exporterOptions.ReportFormat = parseResult.GetValueForOption(ReportFormat);
        }
    }

    public static List<ExternalDocLinkRule> ParseExternalDocRules(IEnumerable<string> rawRules)
    {
        ArgumentNullException.ThrowIfNull(rawRules);

        List<ExternalDocLinkRule> rules = [];
        foreach (var rawRule in rawRules)
        {
            if (string.IsNullOrWhiteSpace(rawRule))
            {
                continue;
            }

            if (!ExternalDocLinkRule.TryParse(rawRule, out var parsedRule) || parsedRule == null)
            {
                throw new ArgumentException(
                    ExecutionMessagesResources.FormatMessage(CliCommandResources.InvalidExternalDocRuleFormat, rawRule),
                    nameof(rawRules));
            }

            rules.Add(parsedRule);
        }

        return rules;
    }

    private static double? ParseOptionalPercentage(ArgumentResult result)
    {
        if (result.Tokens.Count == 0)
        {
            return null;
        }

        var tokenValue = result.Tokens.Single().Value;
        if (double.TryParse(tokenValue, CultureInfo.InvariantCulture, out var parsedDouble) ||
            double.TryParse(tokenValue, CultureInfo.CurrentCulture, out parsedDouble))
        {
            return parsedDouble;
        }

        result.ErrorMessage = ExecutionMessagesResources.FormatMessage(CliCommandResources.InvalidFailOnUndocumentedPercentage, tokenValue);
        return null;
    }

    private static DiagramFormat ParseDiagramFormat(ArgumentResult result)
    {
        if (result.Tokens.Count == 0)
        {
            return Abstractions.Models.DiagramFormat.Mermaid;
        }

        var tokenValue = result.Tokens.Single().Value;
        if (Abstractions.Models.DiagramFormat.TryParse(tokenValue, out var parsedFormat))
        {
            return parsedFormat;
        }

        result.ErrorMessage = ExecutionMessagesResources.FormatMessage(CliCommandResources.InvalidDiagramFormat, tokenValue);
        return Abstractions.Models.DiagramFormat.None;
    }

    private static NavigationFormat ParseNavigationFormat(ArgumentResult result)
    {
        if (result.Tokens.Count == 0)
        {
            return Abstractions.Models.NavigationFormat.MkDocs;
        }

        var tokenValue = result.Tokens.Single().Value;
        if (Abstractions.Models.NavigationFormat.TryParse(tokenValue, out var parsedFormat))
        {
            return parsedFormat;
        }

        result.ErrorMessage = ExecutionMessagesResources.FormatMessage(CliCommandResources.InvalidNavigationFormat, tokenValue);
        return Abstractions.Models.NavigationFormat.None;
    }

    private static QualityReportFormat ParseQualityReportFormat(ArgumentResult result)
    {
        if (result.Tokens.Count == 0)
        {
            return Abstractions.Models.QualityReportFormat.Json;
        }

        var tokenValue = result.Tokens.Single().Value;
        if (Abstractions.Models.QualityReportFormat.TryParse(tokenValue, out var parsedFormat))
        {
            return parsedFormat;
        }

        result.ErrorMessage = ExecutionMessagesResources.FormatMessage(CliCommandResources.InvalidReportFormat, tokenValue);
        return default;
    }
}
