using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests;

public sealed class DocumentationExporterEngineTests
{
    private sealed class StubExportWorkflow(bool shouldSucceed) : IExportWorkflow
    {
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<ExportResult> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(shouldSucceed ? ExportResult.Success() : ExportResult.Failure());
        }
    }

    private sealed class TrackedScopedResource : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class ScopeTrackingWorkflow(TrackedScopedResource trackedResource) : IExportWorkflow
    {
        public Task<ExportResult> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(trackedResource.IsDisposed ? ExportResult.Failure() : ExportResult.Success());
    }

    private static IServiceScopeFactory CreateScopeFactory(IExportWorkflow workflow)
    {
        ServiceCollection services = new();
        _ = services.AddScoped(_ => workflow);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    [UnitTest]
    public async Task ExportDocumentationAsyncWhenOptionsNullShouldThrowArgumentNullException()
    {
        // Arrange
        var workflow = new StubExportWorkflow(shouldSucceed: true);
        var engine = new DocumentationExporterEngine(CreateScopeFactory(workflow));

        // Act
        var act = async () => await engine.ExportDocumentationAsync(null!);

        // Assert
        _ = Assert.Throws<ArgumentNullException>(() => new DocumentationExporterEngine(null!));
        _ = await act.Should().ThrowAsync<ArgumentNullException>("because options cannot be null.");
    }

    [Fact]
    [UnitTest]
    public async Task ExportDocumentationAsyncWhenWorkflowSucceedsShouldReturnSuccessResult()
    {
        // Arrange
        var workflow = new StubExportWorkflow(shouldSucceed: true);
        var engine = new DocumentationExporterEngine(CreateScopeFactory(workflow));
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.dll"),
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.xml"),
            OutputDirectoryPath = Path.Combine(Path.GetTempPath(), "Path", "Output")
        };

        // Act
        var result = await engine.ExportDocumentationAsync(options);

        // Assert
        _ = result.Should().NotBeNull("because the engine must return a workflow result.");
        _ = result.IsSuccess.Should().BeTrue("because the stub workflow succeeded.");
    }

    [Fact]
    [UnitTest]
    public async Task ExportDocumentationAsyncWhenWorkflowFailsShouldReturnFailureResult()
    {
        // Arrange
        var workflow = new StubExportWorkflow(shouldSucceed: false);
        var engine = new DocumentationExporterEngine(CreateScopeFactory(workflow));
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.dll"),
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.xml"),
            OutputDirectoryPath = Path.Combine(Path.GetTempPath(), "Path", "Output")
        };

        // Act
        var result = await engine.ExportDocumentationAsync(options);

        // Assert
        _ = result.Should().NotBeNull("because the engine must return a failure result.");
        _ = result.IsSuccess.Should().BeFalse("because the stub workflow failed.");
    }

    [Fact]
    [UnitTest]
    public async Task ExportDocumentationAsyncWhenCancelledShouldThrowOperationCanceledException()
    {
        // Arrange
        var workflow = new StubExportWorkflow(shouldSucceed: true);
        var engine = new DocumentationExporterEngine(CreateScopeFactory(workflow));
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.dll"),
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.xml"),
            OutputDirectoryPath = Path.Combine(Path.GetTempPath(), "Path", "Output")
        };
        using CancellationTokenSource cancellationSource = new();
        await cancellationSource.CancelAsync();

        // Act
        var cancelledAction = async () => await engine.ExportDocumentationAsync(options, cancellationSource.Token);

        // Assert
        _ = await cancelledAction.Should().ThrowAsync<OperationCanceledException>("because cooperative cancellation must propagate from the engine to the workflow.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ExportDocumentationAsyncShouldDisposeScopedServicesAfterExecution()
    {
        // Arrange
        TrackedScopedResource? capturedResource = null;
        ServiceCollection services = new();
        _ = services.AddScoped(_ =>
        {
            capturedResource = new TrackedScopedResource();
            return capturedResource;
        });
        _ = services.AddScoped<IExportWorkflow, ScopeTrackingWorkflow>();
        await using var provider = services.BuildServiceProvider();
        var engine = new DocumentationExporterEngine(provider.GetRequiredService<IServiceScopeFactory>());
        var options = new ExporterOptions
        {
            AssemblyFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.dll"),
            XmlDocumentationFilePath = Path.Combine(Path.GetTempPath(), "Path", "Assembly.xml"),
            OutputDirectoryPath = Path.Combine(Path.GetTempPath(), "Path", "Output")
        };

        // Act
        var result = await engine.ExportDocumentationAsync(options);

        // Assert
        _ = result.IsSuccess.Should().BeTrue("because scoped resources must remain alive during workflow execution.");
        _ = capturedResource.Should().NotBeNull("because the scoped resource should have been created.");
        _ = capturedResource!.IsDisposed.Should().BeTrue("because the export scope must dispose scoped resources immediately on completion.");
    }
}
