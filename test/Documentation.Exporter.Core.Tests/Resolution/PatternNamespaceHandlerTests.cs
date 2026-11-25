using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class PatternNamespaceHandlerTests
{
    [Fact]
    public void ConstructorWhenArgumentsInvalidShouldThrowException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new PatternNamespaceHandler(null!));
        _ = Assert.Throws<ArgumentNullException>(() => new PatternNamespaceHandler(null!, "https://docs.example.com/"));
        _ = Assert.Throws<ArgumentException>(() => new PatternNamespaceHandler("   ", "https://docs.example.com/"));
        _ = Assert.Throws<ArgumentNullException>(() => new PatternNamespaceHandler("Vendor.Sdk", null!));
        _ = Assert.Throws<ArgumentException>(() => new PatternNamespaceHandler("Vendor.Sdk", "   "));
    }

    [Fact]
    public void ExternalDocLinkRuleTryParseShouldValidateMappingSyntax()
    {
        // Act & Assert
        _ = ExternalDocLinkRule.TryParse(null, out _).Should().BeFalse();
        _ = ExternalDocLinkRule.TryParse("   ", out _).Should().BeFalse();
        _ = ExternalDocLinkRule.TryParse("NoEqualsSign", out _).Should().BeFalse();
        _ = ExternalDocLinkRule.TryParse("=https://docs.example.com/", out _).Should().BeFalse();
        _ = ExternalDocLinkRule.TryParse("Vendor.Sdk=", out _).Should().BeFalse();
        _ = ExternalDocLinkRule.TryParse("   =   ", out _).Should().BeFalse();

        var parsedSuccessfully = ExternalDocLinkRule.TryParse(
            "Newtonsoft.Json = https://www.newtonsoft.com/json/help/html/T_{fullNameUnderscore}.htm",
            out var parsedRule);

        _ = parsedSuccessfully.Should().BeTrue();
        _ = parsedRule.Should().NotBeNull();
        _ = parsedRule.NamespacePrefix.Should().Be("Newtonsoft.Json");
        _ = parsedRule.UrlTemplate.Should().Be("https://www.newtonsoft.com/json/help/html/T_{fullNameUnderscore}.htm");
    }

    [Fact]
    public void CanHandleNamespaceAndResolveTypeLinkShouldSupportAllTemplatePlaceholdersAndBaseUrlFallback()
    {
        // Arrange
        var sandcastleRule = new ExternalDocLinkRule
        {
            NamespacePrefix = "Newtonsoft.Json",
            UrlTemplate = "https://www.newtonsoft.com/json/help/html/T_{fullNameUnderscore}.htm"
        };
        var sandcastleHandler = new PatternNamespaceHandler(sandcastleRule);

        var multiTokenHandler = new PatternNamespaceHandler(
            "Vendor.Sdk",
            "https://docs.vendor.io/{namespaceSlash}/{namespaceLower}/{namespace}/{fullName}/{fullNameLower}/{fullNameSlash}/{name}/{nameLower}.html");

        var baseUrlHandler = new PatternNamespaceHandler(
            "Polly",
            "https://www.pollydocs.org/api");

        // Act & Assert
        _ = sandcastleHandler.CanHandleNamespace("").Should().BeFalse();
        _ = sandcastleHandler.CanHandleNamespace("System.String").Should().BeFalse();
        _ = sandcastleHandler.CanHandleNamespace("Newtonsoft.Json").Should().BeTrue();
        _ = sandcastleHandler.CanHandleNamespace("T:Newtonsoft.Json.Linq.JObject`1[[System.String]]").Should().BeTrue();

        _ = sandcastleHandler.ResolveTypeLink("").Should().BeEmpty();
        _ = sandcastleHandler.ResolveTypeLink("T:Newtonsoft.Json.Linq.JObject`1[[System.String]]")
            .Should().Be("[JObject](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JObject.htm)");

        _ = multiTokenHandler.ResolveTypeLink("M:Vendor.Sdk.Client.HttpGateway<T>{U}(System.Int32)")
            .Should().Be("[HttpGateway](https://docs.vendor.io/Vendor/Sdk/Client/vendor.sdk.client/Vendor.Sdk.Client/Vendor.Sdk.Client.HttpGateway/vendor.sdk.client.httpgateway/Vendor/Sdk/Client/HttpGateway/HttpGateway/httpgateway.html)");

        _ = baseUrlHandler.ResolveTypeLink("Polly.Retry.AsyncRetryPolicy")
            .Should().Be("[AsyncRetryPolicy](https://www.pollydocs.org/api/polly.retry.asyncretrypolicy)");
        _ = baseUrlHandler.ResolveTypeLink("Polly")
            .Should().Be("[Polly](https://www.pollydocs.org/api/polly)");
    }
}
