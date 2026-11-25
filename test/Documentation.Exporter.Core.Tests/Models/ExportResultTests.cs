using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class ExportResultTests
{
    [Fact]
    public void SuccessShouldSetIsSuccessTrue()
    {
        // Act
        var result = ExportResult.Success();

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FailureShouldSetIsSuccessFalse()
    {
        // Act
        var result = ExportResult.Failure();

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
    }
}
