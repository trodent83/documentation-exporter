using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow;

[UnitTest]
public sealed class ExportWorkflowContextTests
{
    [Fact]
    public void ConstructorWhenOptionsNullShouldThrowArgumentNullException() =>
        _ = Assert.Throws<ArgumentNullException>(() => new ExportWorkflowContext(null!));

    [Fact]
    public void ExportWorkflowContextShouldInitializeCollectionsAndProperties()
    {
        // Arrange
        var options = new ExporterOptions();

        // Act
        var context = new ExportWorkflowContext(options);

        // Assert
        _ = context.Options.Should().BeSameAs(options);
        _ = context.ExportedTypesMap.Should().BeNull();
        _ = context.CompiledTypeModels.Should().BeEmpty();
        _ = context.GeneratedFilesCount.Should().Be(0);
        _ = context.HasFailed.Should().BeFalse();
    }
}
