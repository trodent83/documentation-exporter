using Documentation.Exporter.Cli;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Cli;

[UnitTest]
public sealed class CliPluginDirectoryResolverTests
{
    [Fact]
    public void ResolvePluginsDirectoryWhenArgumentsNullShouldThrowArgumentNullException()
    {
        // Act
        var act = () => CliPluginDirectoryResolver.ResolvePluginsDirectory(null!);

        // Assert
        _ = act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(new[] { "assembly", "--plugins-dir", "C:\\custom-plugins" }, "C:\\custom-plugins")]
    [InlineData(new[] { "solution", "--plugins-dir=/opt/plugins" }, "/opt/plugins")]
    [InlineData(new[] { "config", "--PLUGINS-DIR", "  ./my-plugins  " }, "./my-plugins")]
    [InlineData(new[] { "assembly", "--output", "out" }, null)]
    [InlineData(new[] { "assembly", "--plugins-dir=", "--output", "out" }, null)]
    [InlineData(new[] { "assembly", "--plugins-dir", "--output" }, null)]
    [InlineData(new[] { "   ", "--plugins-dir" }, null)]
    public void ResolvePluginsDirectoryShouldExtractConfiguredPathOrReturnNull(
        string[] arguments,
        string? expectedPluginsDirectory)
    {
        // Act
        var resolvedDirectory = CliPluginDirectoryResolver.ResolvePluginsDirectory(arguments);

        // Assert
        _ = resolvedDirectory.Should().Be(expectedPluginsDirectory);
    }
}
