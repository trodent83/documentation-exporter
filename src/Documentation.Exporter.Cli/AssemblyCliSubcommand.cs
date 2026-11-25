using Documentation.Exporter.Core;
using System.CommandLine;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Builds the <c>assembly</c> CLI subcommand.
/// </summary>
/// <param name="sharedOptions">The shared CLI options registry.</param>
/// <param name="pipelineRunner">The pipeline runner service.</param>
public sealed class AssemblyCliSubcommand(
    ISharedCliOptions sharedOptions,
    ICliPipelineRunner pipelineRunner) : ICliSubcommand
{
    private readonly ISharedCliOptions options = ArgumentGuard.EnsureNotNull(sharedOptions);
    private readonly ICliPipelineRunner runner = ArgumentGuard.EnsureNotNull(pipelineRunner);

    /// <inheritdoc />
    public Command BuildCommand()
    {
        var (assemblyOption, xmlOption) = SharedCliOptions.CreateAssemblyInputOptions();

        Command assemblyCommand = new("assembly", CliCommandResources.AssemblyCommandDescription)
        {
            assemblyOption,
            xmlOption
        };
        options.AddExportOptionsTo(assemblyCommand, includeFailOnMissing: false);

        assemblyCommand.SetHandler(async invocationContext =>
        {
            if (!options.TryCreateBaseExporterOptions(invocationContext.ParseResult, out var baseOptions, out var logLevel) ||
                baseOptions == null)
            {
                invocationContext.ExitCode = 2;
                return;
            }

            SharedCliOptions.ApplyAssemblyInputs(baseOptions, invocationContext.ParseResult, assemblyOption, xmlOption);
            var cancellationToken = invocationContext.GetCancellationToken();
            invocationContext.ExitCode = await runner.RunAsync(baseOptions, logLevel, cancellationToken).ConfigureAwait(false);
        });

        return assemblyCommand;
    }
}
