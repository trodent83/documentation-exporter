using Documentation.Exporter.Core;
using System.CommandLine;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Builds the <c>solution</c> CLI subcommand.
/// </summary>
/// <param name="sharedOptions">The shared CLI options registry.</param>
/// <param name="pipelineRunner">The pipeline runner service.</param>
public sealed class SolutionCliSubcommand(
    ISharedCliOptions sharedOptions,
    ICliPipelineRunner pipelineRunner) : ICliSubcommand
{
    private readonly ISharedCliOptions options = ArgumentGuard.EnsureNotNull(sharedOptions);
    private readonly ICliPipelineRunner runner = ArgumentGuard.EnsureNotNull(pipelineRunner);

    /// <inheritdoc />
    public Command BuildCommand()
    {
        Option<FileInfo> solutionOption = new(
            aliases: ["--solution", "-s"],
            description: CliCommandResources.SolutionOptionDescription)
        {
            IsRequired = true
        };

        Option<string> configurationOption = new(
            aliases: ["--configuration", "-c"],
            getDefaultValue: () => "Debug",
            description: CliCommandResources.ConfigurationOptionDescription);

        Option<bool> includeTestsOption = new(
            aliases: ["--include-tests", "-t"],
            getDefaultValue: () => false,
            description: CliCommandResources.IncludeTestsOptionDescription);

        Command solutionCommand = new("solution", CliCommandResources.SolutionCommandDescription)
        {
            solutionOption,
            configurationOption,
            includeTestsOption
        };
        options.AddExportOptionsTo(solutionCommand, includeFailOnMissing: true);

        solutionCommand.SetHandler(async invocationContext =>
        {
            if (!options.TryCreateBaseExporterOptions(invocationContext.ParseResult, out var baseOptions, out var logLevel) ||
                baseOptions == null)
            {
                invocationContext.ExitCode = 2;
                return;
            }

            var solutionFile = invocationContext.ParseResult.GetValueForOption(solutionOption)!;
            baseOptions.SolutionFilePath = solutionFile.FullName;
            baseOptions.SolutionConfiguration = invocationContext.ParseResult.GetValueForOption(configurationOption)!;
            baseOptions.IncludeTestProjects = invocationContext.ParseResult.GetValueForOption(includeTestsOption);

            var cancellationToken = invocationContext.GetCancellationToken();
            invocationContext.ExitCode = await runner.RunAsync(baseOptions, logLevel, cancellationToken).ConfigureAwait(false);
        });

        return solutionCommand;
    }
}
