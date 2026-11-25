using Documentation.Exporter.Cli;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Cli;

[UnitTest]
[Collection("ConsoleOutputTests")]
public sealed class SystemConsoleOutputTests
{
    [Fact]
    public void WriteErrorLineWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        SystemConsoleOutput consoleOutput = new();

        // Act
        var nullAction = () => consoleOutput.WriteErrorLine(null!);

        // Assert
        _ = nullAction.Should().Throw<ArgumentNullException>("because error messages cannot be null.");
    }

    [Fact]
    public void WriteErrorLineShouldWriteFormattedMessageToStandardError()
    {
        // Arrange
        SystemConsoleOutput consoleOutput = new();
        var originalErrorWriter = Console.Error;
        using StringWriter capturedErrorWriter = new();

        try
        {
            Console.SetError(capturedErrorWriter);

            // Act
            consoleOutput.WriteErrorLine("Localized CLI error message");
        }
        finally
        {
            Console.SetError(originalErrorWriter);
        }

        // Assert
        _ = capturedErrorWriter.ToString().Trim().Should().Be(
            "Localized CLI error message",
            "because WriteErrorLine must emit the message line to Console.Error.");
    }
}
