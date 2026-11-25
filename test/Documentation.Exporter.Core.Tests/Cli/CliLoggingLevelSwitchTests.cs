using Documentation.Exporter.Cli;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Cli;

[UnitTest]
public sealed class CliLoggingLevelSwitchTests
{
    [Fact]
    public void MinimumLevelShouldDefaultToInformationAndPersistUpdates()
    {
        // Arrange
        CliLoggingLevelSwitch loggingSwitch = new();

        // Assert default
        _ = loggingSwitch.MinimumLevel.Should().Be(LogLevel.Information, "because Information is the default CLI log threshold.");

        // Act & Assert updates
        loggingSwitch.MinimumLevel = LogLevel.Debug;
        _ = loggingSwitch.MinimumLevel.Should().Be(LogLevel.Debug, "because updated log level must be visible immediately.");

        loggingSwitch.MinimumLevel = LogLevel.Warning;
        _ = loggingSwitch.MinimumLevel.Should().Be(LogLevel.Warning, "because subsequent level changes must persist.");
    }
}
