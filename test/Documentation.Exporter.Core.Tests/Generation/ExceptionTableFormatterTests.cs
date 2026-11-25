using Documentation.Exporter.Core.Generation;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class ExceptionTableFormatterTests
{
    [Fact]
    public void FormatExceptionsTableWhenInvalidArgsShouldThrow()
    {
        // Arrange
        var formatter = new ExceptionTableFormatter();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => formatter.FormatExceptionsTable(null!));
        _ = Assert.Throws<ArgumentException>(() => formatter.FormatExceptionsTable(new Dictionary<string, string>(), "   "));
    }

    [Fact]
    public void FormatExceptionsTableShouldRenderEmptyOrFormattedMarkdownTable()
    {
        // Arrange
        var formatter = new ExceptionTableFormatter();
        Dictionary<string, string> exceptions = new()
        {
            ["[ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception)"] = "When empty.",
            ["System.TimeoutException"] = "When timed out."
        };

        // Act & Assert
        _ = formatter.FormatExceptionsTable(new Dictionary<string, string>()).Should().BeEmpty();
        var rendered = formatter.FormatExceptionsTable(exceptions, "##");
        _ = rendered.Should().Contain("## Exceptions")
            .And.Contain("| [ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception) | When empty. |")
            .And.Contain("| `System.TimeoutException` | When timed out. |");
    }
}
