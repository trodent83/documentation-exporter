using Documentation.Exporter.Abstractions.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Plugins.Navigation.MkDocs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Plugins;

[UnitTest]
public sealed class MkDocsNavigationPluginTests
{
    [Fact]
    public void ConfigureServicesShouldRegisterMkDocsNavigationGeneratorAsSingletonAndGuardNullServices()
    {
        // Arrange
        MkDocsNavigationPlugin plugin = new();
        ServiceCollection services = new();

        // Act & Assert
        _ = plugin.Name.Should().Be("MkDocs Navigation");
        _ = FluentActions.Invoking(() => plugin.ConfigureServices(null!))
            .Should().Throw<ArgumentNullException>("because services cannot be null.");

        plugin.ConfigureServices(services);
        using var provider = services.BuildServiceProvider();

        var generators = provider.GetServices<INavigationGenerator>().ToList();
        _ = generators.Should().ContainSingle(generator =>
                generator is MkDocsNavigationGenerator && generator.Format == NavigationFormat.MkDocs,
            "because MkDocsNavigationPlugin must register MkDocsNavigationGenerator for NavigationFormat.MkDocs.");

        _ = services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(INavigationGenerator) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}
