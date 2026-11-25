using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class MicrosoftDocsNamespaceHandlerTests
{
    [Fact]
    public void ConstructorWhenUrlNullOrWhitespaceShouldThrowArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MicrosoftDocsNamespaceHandler(null!));
        _ = Assert.Throws<ArgumentException>(() => new MicrosoftDocsNamespaceHandler(string.Empty));
    }

    [Fact]
    public void CanHandleNamespaceWhenNullOrWhitespaceShouldReturnFalse()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler();

        // Act & Assert
        _ = handler.CanHandleNamespace(null!).Should().BeFalse();
        _ = handler.CanHandleNamespace(string.Empty).Should().BeFalse();
        _ = handler.CanHandleNamespace("   ").Should().BeFalse();
    }

    [Fact]
    public void ResolveTypeLinkWhenNullOrWhitespaceShouldReturnEmpty()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler();

        // Act & Assert
        _ = handler.ResolveTypeLink(null!).Should().BeEmpty();
        _ = handler.ResolveTypeLink(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void ResolveTypeLinkWithoutTrailingSlashBaseUrlShouldNormalizeUrl()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler("https://learn.microsoft.com/en-us/dotnet/api");

        // Act
        var link = handler.ResolveTypeLink("System.String");

        // Assert
        _ = link.Should().Be("[String](https://learn.microsoft.com/en-us/dotnet/api/system.string)");
    }

    [Fact]
    public void ResolveTypeLinkWithGenericAndPrefixShouldCleanAndLink()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler();

        // Act
        var genericLink = handler.ResolveTypeLink("T:System.Collections.Generic.List`1");
        var simpleLink = handler.ResolveTypeLink("Int32");

        // Assert
        _ = genericLink.Should().Be("[List](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)");
        _ = simpleLink.Should().Be("[Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32)");
    }

    [Fact]
    public void ResolveTypeLinkWithPrimitiveKeywordsShouldResolveCanonicalSystemType()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler();

        // Act
        var intLink = handler.ResolveTypeLink("int");
        var stringLink = handler.ResolveTypeLink("string");
        var boolLink = handler.ResolveTypeLink("bool");

        // Assert
        _ = intLink.Should().Be("[int](https://learn.microsoft.com/en-us/dotnet/api/system.int32)");
        _ = stringLink.Should().Be("[string](https://learn.microsoft.com/en-us/dotnet/api/system.string)");
        _ = boolLink.Should().Be("[bool](https://learn.microsoft.com/en-us/dotnet/api/system.boolean)");
    }

    [Fact]
    public void ResolveTypeLinkWithCommonGenericUnqualifiedNamesShouldResolveCanonicalUrls()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler();

        // Act
        var listLink = handler.ResolveTypeLink("list");
        var listPascalLink = handler.ResolveTypeLink("List");
        var listXmlGenericLink = handler.ResolveTypeLink("List{T}");
        var dictLink = handler.ResolveTypeLink("System.Collections.Generic.Dictionary`2");
        var closedGenericLink = handler.ResolveTypeLink("System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]");

        // Assert
        _ = listLink.Should().Be("[list](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)");
        _ = listPascalLink.Should().Be("[List](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)");
        _ = listXmlGenericLink.Should().Be("[List](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)");
        _ = dictLink.Should().Be("[Dictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2)");
        _ = closedGenericLink.Should().Be("[List](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1)");
    }

    [Fact]
    public void ResolveTypeLinkWithDynamicallyLoadedTypesShouldResolveWithoutHardcoding()
    {
        // Arrange
        var handler = new MicrosoftDocsNamespaceHandler();

        // Act
        var queueLink = handler.ResolveTypeLink("Queue");
        var stackLink = handler.ResolveTypeLink("Stack`1");
        var guidLink = handler.ResolveTypeLink("Guid");
        var stringBuilderLink = handler.ResolveTypeLink("StringBuilder");
        var concurrentDictLink = handler.ResolveTypeLink("ConcurrentDictionary");

        // Assert
        _ = queueLink.Should().Be("[Queue](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1)");
        _ = stackLink.Should().Be("[Stack](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1)");
        _ = guidLink.Should().Be("[Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid)");
        _ = stringBuilderLink.Should().Be("[StringBuilder](https://learn.microsoft.com/en-us/dotnet/api/system.text.stringbuilder)");
        _ = concurrentDictLink.Should().Be("[ConcurrentDictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2)");
        _ = handler.ResolveTypeLink("System.Collections.Generic.Dictionary<String, Int32>")
            .Should().Be("[Dictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2)");
        _ = handler.ResolveTypeLink("System.Collections.Generic.Dictionary<String, List<KeyValuePair<Int32, String>>>")
            .Should().Be("[Dictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2)");
        _ = handler.ResolveTypeLink("NonExistentFrameworkTypeXYZ")
            .Should().Be("[NonExistentFrameworkTypeXYZ](https://learn.microsoft.com/en-us/dotnet/api/system.nonexistentframeworktypexyz)");
        _ = handler.ResolveTypeLink("M:System.String.IsNullOrWhiteSpace(System.String)")
            .Should().Be("[IsNullOrWhiteSpace](https://learn.microsoft.com/en-us/dotnet/api/system.string.isnullorwhitespace)",
                "because BCL method crefs must strip parameter parentheses so Markdown links and Microsoft Learn URLs are valid.");
        _ = handler.ResolveTypeLink("System.Environment+SpecialFolder")
            .Should().Be("[SpecialFolder](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder)",
                "because nested BCL reflection types must normalize '+' to '.' in Microsoft Learn URLs.");
    }
}
