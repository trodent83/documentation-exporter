using System.CommandLine;
using Documentation.Exporter.Cli;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Cli;

[IntegrationTest]
public sealed class CliServiceCollectionExtensionsTests
{
    private sealed class CustomStubSubcommand : ICliSubcommand
    {
        public Command BuildCommand() => new("custom-stub", "Custom test subcommand");
    }

    [Fact]
    public void RegistrationMethodsWhenServicesNullShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? nullServices = null;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => nullServices!.AddDocumentationExporterCli());
        _ = Assert.Throws<ArgumentNullException>(() => nullServices!.AddCliSubcommands());
        _ = Assert.Throws<ArgumentNullException>(() => nullServices!.AddCliSubcommand<CustomStubSubcommand>());
    }

    [Fact]
    public async Task AddDocumentationExporterCliShouldBuildWithValidateScopesAndResolveAllCliServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        var returnedServices = services.AddDocumentationExporterCli();
        _ = returnedServices.Should().BeSameAs(services, "because extension methods must return the service collection for chaining.");

        ServiceProviderOptions providerOptions = new()
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        };

        await using var serviceProvider = services.BuildServiceProvider(providerOptions);

        // Assert
        _ = serviceProvider.GetRequiredService<CliLoggingLevelSwitch>().Should().NotBeNull("because the log switch is registered as a singleton.");
        _ = serviceProvider.GetRequiredService<IConsoleOutput>().Should().BeOfType<SystemConsoleOutput>();
        _ = serviceProvider.GetRequiredService<ISharedCliOptions>().Should().BeOfType<SharedCliOptions>();
        _ = serviceProvider.GetRequiredService<ICliPipelineRunner>().Should().BeOfType<CliPipelineRunner>();
        _ = serviceProvider.GetRequiredService<ICliCommandBuilder>().Should().BeOfType<CliCommandBuilder>();

        var subcommands = serviceProvider.GetServices<ICliSubcommand>().ToList();
        _ = subcommands.Should().HaveCount(3, "because assembly, solution, and config subcommands are registered.");
        _ = subcommands.Should().Contain(subcommand => subcommand.GetType() == typeof(AssemblyCliSubcommand));
        _ = subcommands.Should().Contain(subcommand => subcommand.GetType() == typeof(SolutionCliSubcommand));
        _ = subcommands.Should().Contain(subcommand => subcommand.GetType() == typeof(ConfigCliSubcommand));
    }

    [Fact]
    public async Task AddCliSubcommandShouldRegisterCustomSubcommandImplementation()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddDocumentationExporterCli()
            .AddCliSubcommand<CustomStubSubcommand>();

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        // Act
        var subcommands = serviceProvider.GetServices<ICliSubcommand>().ToList();

        // Assert
        _ = subcommands.Should().HaveCount(4, "because the custom subcommand is appended alongside the 3 built-in subcommands.");
        _ = subcommands.Should().ContainSingle(subcommand => subcommand.GetType() == typeof(CustomStubSubcommand));
    }
}
