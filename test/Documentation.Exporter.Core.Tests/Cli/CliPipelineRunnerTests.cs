using Documentation.Exporter.Cli;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Cli;

[UnitTest]
public sealed class CliPipelineRunnerTests
{
    private sealed class StubDocumentationExporterEngine(Func<ExporterOptions, CancellationToken, Task<ExportResult>> executeCallback) : IDocumentationExporterEngine
    {
        public Task<ExportResult> ExportDocumentationAsync(ExporterOptions options, CancellationToken cancellationToken = default) =>
            executeCallback(options, cancellationToken);
    }

    [Fact]
    public void ConstructorWhenDependenciesNullShouldThrowArgumentNullException()
    {
        // Arrange
        var engine = new StubDocumentationExporterEngine((_, _) => Task.FromResult(ExportResult.Success()));
        var logger = NullLogger<CliPipelineRunner>.Instance;
        CliLoggingLevelSwitch levelSwitch = new();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new CliPipelineRunner(null!, logger, levelSwitch));
        _ = Assert.Throws<ArgumentNullException>(() => new CliPipelineRunner(engine, null!, levelSwitch));
        _ = Assert.Throws<ArgumentNullException>(() => new CliPipelineRunner(engine, logger, null!));
    }

    [Fact]
    public async Task RunAsyncWhenOptionsNullShouldThrowArgumentNullException()
    {
        // Arrange
        var engine = new StubDocumentationExporterEngine((_, _) => Task.FromResult(ExportResult.Success()));
        CliPipelineRunner runner = new(engine, NullLogger<CliPipelineRunner>.Instance, new CliLoggingLevelSwitch());

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunAsync(null!, LogLevel.Information));
    }

    [Fact]
    public async Task RunAsyncShouldUpdateLogLevelAndReturnExpectedExitCodes()
    {
        // Arrange
        CliLoggingLevelSwitch levelSwitch = new();
        ExporterOptions options = new();

        CliPipelineRunner successRunner = new(
            new StubDocumentationExporterEngine((_, _) => Task.FromResult(ExportResult.Success())),
            NullLogger<CliPipelineRunner>.Instance,
            levelSwitch);

        CliPipelineRunner failureRunner = new(
            new StubDocumentationExporterEngine((_, _) => Task.FromResult(ExportResult.Failure())),
            NullLogger<CliPipelineRunner>.Instance,
            levelSwitch);

        CliPipelineRunner throwingRunner = new(
            new StubDocumentationExporterEngine((_, _) => throw new InvalidOperationException("Pipeline boom")),
            NullLogger<CliPipelineRunner>.Instance,
            levelSwitch);

        // Act
        var successExitCode = await successRunner.RunAsync(options, LogLevel.Debug);
        var capturedDebugLevel = levelSwitch.MinimumLevel;
        var failureExitCode = await failureRunner.RunAsync(options, LogLevel.Warning);
        var exceptionExitCode = await throwingRunner.RunAsync(options, LogLevel.Error);

        // Assert
        _ = successExitCode.Should().Be(0, "because successful export returns exit code 0.");
        _ = capturedDebugLevel.Should().Be(LogLevel.Debug, "because CliPipelineRunner must apply the requested LogLevel to CliLoggingLevelSwitch.");
        _ = failureExitCode.Should().Be(1, "because failed export returns exit code 1.");
        _ = levelSwitch.MinimumLevel.Should().Be(LogLevel.Error, "because latest invocation updates the active log switch.");
        _ = exceptionExitCode.Should().Be(1, "because unhandled exceptions inside the export engine are caught and mapped to exit code 1.");
    }
}
