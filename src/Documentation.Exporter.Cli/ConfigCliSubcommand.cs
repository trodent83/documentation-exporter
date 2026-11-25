using Documentation.Exporter.Core;
using System.CommandLine;
using Documentation.Exporter.Core.Configuration;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Builds the <c>config</c> CLI subcommand.
/// </summary>
/// <param name="sharedOptions">The shared CLI options registry.</param>
/// <param name="configurationLoader">The configuration loader service.</param>
/// <param name="pipelineRunner">The pipeline runner service.</param>
/// <param name="consoleOutput">The console error writer.</param>
public sealed class ConfigCliSubcommand(
    ISharedCliOptions sharedOptions,
    IExporterConfigurationLoader configurationLoader,
    ICliPipelineRunner pipelineRunner,
    IConsoleOutput consoleOutput) : ICliSubcommand
{
    private readonly ISharedCliOptions options = ArgumentGuard.EnsureNotNull(sharedOptions);
    private readonly IExporterConfigurationLoader configLoader = ArgumentGuard.EnsureNotNull(configurationLoader);
    private readonly ICliPipelineRunner runner = ArgumentGuard.EnsureNotNull(pipelineRunner);
    private readonly IConsoleOutput console = ArgumentGuard.EnsureNotNull(consoleOutput);

    /// <inheritdoc />
    public Command BuildCommand()
    {
        Option<FileInfo> configOption = new(
            aliases: ["--config", "-f"],
            description: CliCommandResources.ConfigOptionDescription)
        {
            IsRequired = true
        };

        Command configCommand = new("config", CliCommandResources.ConfigCommandDescription)
        {
            configOption,
            options.LogLevel
        };

        options.AddConfigOverrideOptionsTo(configCommand);

        configCommand.SetHandler(async invocationContext =>
        {
            var configFile = invocationContext.ParseResult.GetValueForOption(configOption)!;
            var logLevel = invocationContext.ParseResult.GetValueForOption(options.LogLevel);

            if (!configFile.Exists)
            {
                console.WriteErrorLine(ExecutionMessagesResources.FormatMessage(CliCommandResources.ConfigFileNotFound, configFile.FullName));
                invocationContext.ExitCode = 2;
                return;
            }

            try
            {
                var cancellationToken = invocationContext.GetCancellationToken();
                var exporterOptions = await configLoader.LoadAsync(configFile.FullName, cancellationToken).ConfigureAwait(false);
                options.ApplyOptionalOverrides(exporterOptions, invocationContext.ParseResult);

                invocationContext.ExitCode = await runner.RunAsync(exporterOptions, logLevel, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                console.WriteErrorLine(ExecutionMessagesResources.FormatMessage(CliCommandResources.ConfigFileLoadFailed, configFile.FullName, exception.Message));
                invocationContext.ExitCode = 2;
            }
        });

        return configCommand;
    }
}
