using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class LocalProjectNamespaceHandlerTests
{
    [Fact]
    public void ConstructorWhenTypesNullShouldThrowArgumentNullException() =>
        _ = Assert.Throws<ArgumentNullException>(() => new LocalProjectNamespaceHandler(null!));

    [Fact]
    public void CanHandleNamespaceWhenNullOrWhitespaceShouldReturnFalse()
    {
        // Arrange
        var handler = new LocalProjectNamespaceHandler(["App.Services.OrderService"]);

        // Act & Assert
        _ = handler.CanHandleNamespace(null!).Should().BeFalse();
        _ = handler.CanHandleNamespace(string.Empty).Should().BeFalse();
        _ = handler.CanHandleNamespace("   ").Should().BeFalse();
    }

    [Fact]
    public void CanHandleNamespaceWhenTypeMatchesRegisteredShouldReturnTrue()
    {
        // Arrange
        var handler = new LocalProjectNamespaceHandler(["App.Services.OrderService"]);

        // Act
        var result = handler.CanHandleNamespace("T:App.Services.OrderService");

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public void ResolveTypeLinkWhenNullOrWhitespaceShouldReturnEmpty()
    {
        // Arrange
        var handler = new LocalProjectNamespaceHandler(["App.Services.OrderService"]);

        // Act & Assert
        _ = handler.ResolveTypeLink(null!).Should().BeEmpty();
        _ = handler.ResolveTypeLink(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void ResolveTypeLinkWithSimpleOrGenericTypeNamesShouldFormatMarkdownLink()
    {
        // Arrange
        var handler = new LocalProjectNamespaceHandler(["SimpleType", "App.GenericService"]);

        // Act
        var simpleLink = handler.ResolveTypeLink("SimpleType");
        var genericLink = handler.ResolveTypeLink("T:App.GenericService`1");

        // Assert
        _ = simpleLink.Should().Be("[SimpleType](SimpleType.md)");
        _ = genericLink.Should().Be("[GenericService](GenericService.md)");
    }

    [Fact]
    public void ResolveTypeLinkWithRootNamespaceAndSubregionsShouldFormatSubdirectoryMarkdownLinks()
    {
        // Arrange
        var handler = new LocalProjectNamespaceHandler(
            ["DemoApp.Services.OrderService", "DemoApp.Models.OrderRequest", "DemoApp.RootComponent"],
            rootNamespace: "DemoApp");

        // Act
        var serviceLink = handler.ResolveTypeLink("T:DemoApp.Services.OrderService");
        var memberLink = handler.ResolveTypeLink("P:DemoApp.Models.OrderRequest.OrderId");
        var rootLink = handler.ResolveTypeLink("T:DemoApp.RootComponent");

        // Assert
        _ = handler.CanHandleNamespace("P:DemoApp.Models.OrderRequest.OrderId").Should().BeTrue();
        _ = serviceLink.Should().Be("[OrderService](Services/OrderService/index.md)");
        _ = memberLink.Should().Be("[OrderId](Models/OrderRequest/OrderId.md)");
        _ = rootLink.Should().Be("[RootComponent](RootComponent/index.md)");
    }

    [Fact]
    public void ResolveTypeLinkAndPathHelpersShouldHandleEnumValuesAndEdgeCaseNamespaces()
    {
        // Arrange
        var handler = new LocalProjectNamespaceHandler(
            ["DemoApp.Models.PaymentStatus", "DemoApp.Services.OrderService", "RootOnlyType"],
            rootNamespace: "DemoApp",
            enumTypeFullNames: ["DemoApp.Models.PaymentStatus"]);

        // Act
        var enumValueLink = handler.ResolveTypeLink("F:DemoApp.Models.PaymentStatus.Pending");
        var methodCrefLink = handler.ResolveTypeLink("M:DemoApp.Services.OrderService.Process(System.String)");
        var bracketGenericLink = handler.ResolveTypeLink("T:DemoApp.Services.OrderService[[System.String]]");
        var rootOnlyLink = handler.ResolveTypeLink("RootOnlyType");
        var unmatchedPrefixLink = handler.ResolveTypeLink("OtherRoot.Component");

        // Assert
        _ = enumValueLink.Should().Be("[Pending](Models/PaymentStatus/index.md#values)");
        _ = methodCrefLink.Should().Be("[Process](Services/OrderService/Process.md)");
        _ = bracketGenericLink.Should().Be("[OrderService](Services/OrderService/index.md)");
        _ = rootOnlyLink.Should().Be("[RootOnlyType](RootOnlyType/index.md)");
        _ = unmatchedPrefixLink.Should().Be("[Component](Component.md)");

        _ = LocalProjectNamespaceHandler.GetNamespaceSubregionPath(null, "DemoApp").Should().BeEmpty();
        _ = LocalProjectNamespaceHandler.GetNamespaceSubregionPath("DemoApp", "DemoApp").Should().BeEmpty();
        _ = LocalProjectNamespaceHandler.GetNamespaceSubregionPath("OtherNamespace.Sub", "DemoApp").Should().BeEmpty();
        _ = LocalProjectNamespaceHandler.GetNamespaceSubregionPath("SingleSegment", null).Should().BeEmpty();
        _ = LocalProjectNamespaceHandler.GetNamespaceSubregionPath("Root.Sub.Feature", null).Should().Be("Sub/Feature");

        var rootModel = new Documentation.Exporter.Abstractions.Models.TypeDocumentationModel
        {
            TypeName = "RootComponent",
            NamespaceName = "DemoApp",
            AssemblyName = "DemoApp"
        };
        var nestedModel = new Documentation.Exporter.Abstractions.Models.TypeDocumentationModel
        {
            TypeName = "NestedComponent",
            NamespaceName = "DemoApp.Services.Settlement",
            AssemblyName = "DemoApp"
        };

        _ = LocalProjectNamespaceHandler.GetTypeDirectoryUrlPath(rootModel).Should().Be("RootComponent");
        _ = LocalProjectNamespaceHandler.GetTypeDirectoryFilePath(rootModel).Should().Be("RootComponent");
        _ = LocalProjectNamespaceHandler.GetTypeRelativeFilePath(nestedModel).Should().EndWith(Path.Combine("Services", "Settlement", "NestedComponent", "index.md"));
    }

    public sealed class GenericOuter<TValue>
    {
        public sealed class NestedInner
        {
            public TValue? Value { get; set; }
        }
    }

    public sealed class DoublyNestedOuter
    {
        public sealed class MiddleContainer
        {
            public sealed class DeepLeaf
            {
                public int LeafCode { get; set; }
            }
        }
    }

    [Fact]
    public void FromExportedTypesMapInSingleAssemblyModeShouldResolveNestedGenericOuterTypeLinks()
    {
        // Arrange
        var nestedType = typeof(GenericOuter<>.NestedInner);
        Dictionary<string, Type> exportedTypes = new(StringComparer.Ordinal)
        {
            [nestedType.FullName!] = nestedType
        };

        var handler = LocalProjectNamespaceHandler.FromExportedTypesMap(exportedTypes, useAssemblySubdirectories: false);

        // Act
        var canHandle = handler.CanHandleNamespace(nestedType.FullName!);
        var resolvedLink = handler.ResolveTypeLink(nestedType.FullName!);

        // Assert
        _ = canHandle.Should().BeTrue("because nested types inside generic outer types should be recognized without truncating at the generic backtick.");
        _ = resolvedLink.Should().Be($"[NestedInner](Resolution/{nameof(LocalProjectNamespaceHandlerTests)}/GenericOuter/NestedInner/index.md)");
    }

    [Fact]
    public void FromExportedTypesMapAndModelPathsShouldPreserveFullDoublyNestedDeclaringChainAndExactMemberRoutes()
    {
        // Arrange
        var leafType = typeof(DoublyNestedOuter.MiddleContainer.DeepLeaf);
        Dictionary<string, Type> exportedTypes = new(StringComparer.Ordinal)
        {
            [leafType.FullName!] = leafType
        };

        Dictionary<string, (string DisplayName, string RelativeUrl)> exactMemberRoutes = new(StringComparer.Ordinal)
        {
            ["DemoApp.Services.OrderService.FormatResult(System.String)"] = ("FormatResult(String)", "Services/OrderService/FormatResult_String.md")
        };

        var handler = LocalProjectNamespaceHandler.FromExportedTypesMap(
            exportedTypes,
            useAssemblySubdirectories: false,
            memberXmlKeyRoutes: exactMemberRoutes);

        var leafModel = new Documentation.Exporter.Abstractions.Models.TypeDocumentationModel
        {
            TypeName = "DeepLeaf",
            DeclaringTypeName = "MiddleContainer",
            DeclaringTypeChain = ["DoublyNestedOuter", "MiddleContainer"],
            NamespaceName = "DemoApp.Models",
            AssemblyName = "DemoApp"
        };

        // Act
        var resolvedNestedLink = handler.ResolveTypeLink(leafType.FullName!);
        var resolvedOverloadLink = handler.ResolveTypeLink("M:DemoApp.Services.OrderService.FormatResult(System.String)");
        var canHandleOverload = handler.CanHandleNamespace("M:DemoApp.Services.OrderService.FormatResult(System.String)");
        var modelRelativeUrl = LocalProjectNamespaceHandler.GetTypeRelativeUrlPath(leafModel, includeAssemblyDirectory: false);

        // Assert
        _ = resolvedNestedLink.Should().Be($"[DeepLeaf](Resolution/{nameof(LocalProjectNamespaceHandlerTests)}/DoublyNestedOuter/MiddleContainer/DeepLeaf/index.md)",
            "because multi-level nested types must preserve the full outer-to-inner directory hierarchy.");
        _ = canHandleOverload.Should().BeTrue("because exact member cref routes must be recognized before generic stripping.");
        _ = resolvedOverloadLink.Should().Be("[FormatResult(String)](Services/OrderService/FormatResult_String.md)",
            "because exact overloaded method crefs must resolve directly to their disambiguated page.");
        _ = modelRelativeUrl.Should().Be("Models/DoublyNestedOuter/MiddleContainer/DeepLeaf/index.md");
    }

    [Fact]
    public void StringConstructorShouldRegisterNestedDeclaringChainUrlsWithAndWithoutRootNamespace()
    {
        // Arrange
        var handlerWithRoot = new LocalProjectNamespaceHandler(
            ["Demo.Services.Outer+Middle+Inner", "TrailingPlus+"],
            rootNamespace: "Demo");
        var handlerWithoutRoot = new LocalProjectNamespaceHandler(["GlobalOuter+Leaf"]);

        // Act
        var nestedLinkWithRoot = handlerWithRoot.ResolveTypeLink("Demo.Services.Outer.Middle.Inner");
        var globalNestedLink = handlerWithoutRoot.ResolveTypeLink("GlobalOuter.Leaf");

        // Assert
        _ = nestedLinkWithRoot.Should().Be("[Inner](Services/Outer/Middle/Inner/index.md)",
            "because string-based constructor should compute nested chain URLs relative to rootNamespace.");
        _ = globalNestedLink.Should().Be("[Leaf](GlobalOuter/Leaf/index.md)",
            "because nested types in global namespace without rootNamespace should preserve nested hierarchy.");
    }

    public sealed class NonGenericAndGenericCollisionSample
    {
        public int NonGenericValue { get; set; } = 1;
    }

    public sealed class NonGenericAndGenericCollisionSample<TItem>
    {
        public TItem? GenericItem { get; set; }
    }

    [Fact]
    public void FromExportedTypesMapWhenNonGenericAndGenericTypesShareNameShouldDisambiguateDirectoriesAndLinks()
    {
        // Arrange
        var nonGenericType = typeof(NonGenericAndGenericCollisionSample);
        var genericType = typeof(NonGenericAndGenericCollisionSample<>);
        Dictionary<string, Type> exportedTypes = new(StringComparer.Ordinal)
        {
            [nonGenericType.FullName!] = nonGenericType,
            [genericType.FullName!] = genericType
        };

        var collidingCleanNames = LocalProjectNamespaceHandler.ComputeCollidingCleanTypeNames(exportedTypes.Values);
        var handler = LocalProjectNamespaceHandler.FromExportedTypesMap(exportedTypes, useAssemblySubdirectories: false);

        // Act
        var nonGenericSegment = LocalProjectNamespaceHandler.ComputeTypeDirectorySegmentName(nonGenericType, collidingCleanNames);
        var genericSegment = LocalProjectNamespaceHandler.ComputeTypeDirectorySegmentName(genericType, collidingCleanNames);
        var nonGenericLink = handler.ResolveTypeLink(nonGenericType.FullName!);
        var genericLink = handler.ResolveTypeLink(genericType.FullName!);

        // Assert
        _ = nonGenericSegment.Should().Be(nameof(NonGenericAndGenericCollisionSample),
            "because the non-generic overload keeps its clean base directory segment.");
        _ = genericSegment.Should().Be($"{nameof(NonGenericAndGenericCollisionSample)}_T1",
            "because the generic arity overload appends _T1 to avoid directory and file collision.");
        _ = nonGenericLink.Should().EndWith($"/{nameof(NonGenericAndGenericCollisionSample)}/index.md)",
            "because non-generic cref links must resolve to the base directory.");
        _ = genericLink.Should().EndWith($"/{nameof(NonGenericAndGenericCollisionSample)}_T1/index.md)",
            "because generic cref links must resolve to the arity-disambiguated directory.");
    }
}
