using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class TypeLinkResolverTests
{
    [Fact]
    public void ResolveLinkWhenSystemTypeShouldReturnMicrosoftDocsUrl()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        resolver.RegisterHandler(new MicrosoftDocsNamespaceHandler("https://learn.microsoft.com/en-us/dotnet/api/"));

        // Act
        var resolvedLink = resolver.ResolveLink("T:System.Collections.Generic.List`1");
        var resolvedIntLink = resolver.ResolveLink("int");

        // Assert
        _ = resolvedLink.Should().Be("[List](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)");
        _ = resolvedIntLink.Should().Be("[int](https://learn.microsoft.com/en-us/dotnet/api/system.int32)");
    }

    [Fact]
    public void ResolveLinkWhenLocalProjectTypeShouldReturnRelativeMarkdownLink()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var localHandler = new LocalProjectNamespaceHandler(["Documentation.Exporter.Core.Models.ExporterOptions"]);
        resolver.RegisterHandler(localHandler);

        // Act
        var resolvedLink = resolver.ResolveLink("T:Documentation.Exporter.Core.Models.ExporterOptions");

        // Assert
        _ = resolvedLink.Should().Be("[ExporterOptions](ExporterOptions.md)");
    }

    [Fact]
    public void ResolveLinkWhenUnknownTypeWithoutHandlersShouldReturnBacktickedTypeName()
    {
        // Arrange
        var resolver = new TypeLinkResolver();

        // Act
        var resolvedLink = resolver.ResolveLink("T:ThirdParty.Unregistered.UnknownService");

        // Assert
        _ = resolvedLink.Should().Be("`UnknownService`");
    }

    [Fact]
    public void ResolveLinkWhenLocalProjectAndMicrosoftDocsRegisteredShouldPreferLocalLinkAndUseMicrosoftDocsAsFallback()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var localHandler = new LocalProjectNamespaceHandler(["Documentation.Exporter.Core.Models.ExporterOptions"]);
        resolver.RegisterHandler(localHandler);
        resolver.RegisterHandler(new MicrosoftDocsNamespaceHandler("https://learn.microsoft.com/en-us/dotnet/api/"));

        // Act
        var localResolvedLink = resolver.ResolveLink("T:Documentation.Exporter.Core.Models.ExporterOptions");
        var externalResolvedLink = resolver.ResolveLink("T:ThirdParty.Unregistered.ExternalService");

        // Assert
        _ = localResolvedLink.Should().Be("[ExporterOptions](ExporterOptions.md)");
        _ = externalResolvedLink.Should().Be("[ExternalService](https://learn.microsoft.com/en-us/dotnet/api/thirdparty.unregistered.externalservice)");
    }

    [Fact]
    public void RegisterHandlerAndResolveLinkShouldHandleNullWhitespaceAndComplexUnregisteredSignatures()
    {
        // Arrange
        var resolver = new TypeLinkResolver();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => resolver.RegisterHandler(null!));
        _ = resolver.ResolveLink(null!).Should().BeEmpty();
        _ = resolver.ResolveLink("   ").Should().BeEmpty();
        _ = resolver.ResolveLink("T:ThirdParty.GenericType`1[[System.String]]").Should().Be("`GenericType`");
        _ = resolver.ResolveLink("M:ThirdParty.Service.Execute(System.String)").Should().Be("`Execute`");
        _ = resolver.ResolveLink("StandaloneType").Should().Be("`StandaloneType`");
    }
}
