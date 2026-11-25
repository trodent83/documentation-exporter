using Documentation.Exporter.Core;
using System.CommandLine;
using Documentation.Exporter.Core.Resources;

namespace Documentation.Exporter.Cli;

/// <summary>
/// Default <see cref="ICliCommandBuilder"/> implementation assembling DI-registered <see cref="ICliSubcommand"/> instances.
/// </summary>
/// <param name="sharedOptions">The shared CLI options registry.</param>
/// <param name="subcommands">The registered subcommands injected via dependency injection.</param>
/// <param name="pipelineRunner">The pipeline runner service.</param>
public sealed class CliCommandBuilder(
    ISharedCliOptions sharedOptions,
    IEnumerable<ICliSubcommand> subcommands,
    ICliPipelineRunner pipelineRunner) : ICliCommandBuilder
{
    private readonly ISharedCliOptions options = ArgumentGuard.EnsureNotNull(sharedOptions);
    private readonly IReadOnlyList<ICliSubcommand> registeredSubcommands = [.. ArgumentGuard.EnsureNotNull(subcommands)];
    private readonly ICliPipelineRunner runner = ArgumentGuard.EnsureNotNull(pipelineRunner);

    /// <inheritdoc />
    public RootCommand BuildRootCommand()
    {
        var (assemblyOption, xmlOption) = SharedCliOptions.CreateAssemblyInputOptions();

        RootCommand rootCommand =
        [
            assemblyOption,
            xmlOption
        ];

        foreach (var subcommand in registeredSubcommands)
        {
            rootCommand.AddCommand(subcommand.BuildCommand());
        }

        options.AddExportOptionsTo(rootCommand, includeFailOnMissing: true);
        rootCommand.Description = CliCommandResources.RootCommandDescription;

        rootCommand.SetHandler(async invocationContext =>
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

        return rootCommand;
    }
}
