using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class TypeDocumentationPathsTests
{
    [Fact]
    public void PathHelpersShouldComputeConsistentUrlsAndOsFilePaths()
    {
        // Arrange
        TypeDocumentationModel standardModel = new()
        {
            AssemblyName = "Contoso.Core",
            NamespaceName = "Contoso.Core.Services.Orders",
            TypeName = "OrderProcessor"
        };

        TypeDocumentationModel nestedGenericModel = new()
        {
            AssemblyName = "Contoso.Core",
            NamespaceName = "Contoso.Core",
            TypeName = "Builder",
            DirectorySegmentName = "Builder_T1",
            DeclaringTypeChain = ["OuterContainer", "InnerFactory"]
        };

        TypeDocumentationModel legacyDeclaringModel = new()
        {
            AssemblyName = "",
            NamespaceName = "Acme.Tools",
            TypeName = "Token",
            DeclaringTypeName = "Lexer"
        };

        // Act & Assert
        _ = TypeDocumentationPaths.GetNamespaceSubregionPath(null, "Contoso").Should().BeEmpty();
        _ = TypeDocumentationPaths.GetNamespaceSubregionPath("Contoso.Core", "Contoso.Core").Should().BeEmpty();
        _ = TypeDocumentationPaths.GetNamespaceSubregionPath("Other.Namespace", "Contoso.Core").Should().BeEmpty();
        _ = TypeDocumentationPaths.GetNamespaceSubregionPath("Acme.Tools.Parsing", null).Should().Be("Tools/Parsing");

        _ = TypeDocumentationPaths.GetTypeRelativeUrlPath(standardModel, includeAssemblyDirectory: false)
            .Should().Be("Services/Orders/OrderProcessor/index.md");
        _ = TypeDocumentationPaths.GetTypeRelativeUrlPath(standardModel, includeAssemblyDirectory: true)
            .Should().Be("Contoso.Core/Services/Orders/OrderProcessor/index.md");

        _ = TypeDocumentationPaths.GetTypeDirectoryUrlPath(nestedGenericModel, includeAssemblyDirectory: false)
            .Should().Be("OuterContainer/InnerFactory/Builder_T1");
        _ = TypeDocumentationPaths.GetTypeDirectoryUrlPath(legacyDeclaringModel, includeAssemblyDirectory: false)
            .Should().Be("Tools/Lexer/Token");

        _ = TypeDocumentationPaths.GetTypeRelativeFilePath(standardModel, includeAssemblyDirectory: false)
            .Should().Be(Path.Combine("Services", "Orders", "OrderProcessor", "index.md"));

        _ = FluentActions.Invoking(() => TypeDocumentationPaths.GetTypeDirectoryUrlPath(null!))
            .Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => TypeDocumentationPaths.GetTypeRelativeUrlPath(null!))
            .Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => TypeDocumentationPaths.GetTypeDirectoryFilePath(null!))
            .Should().Throw<ArgumentNullException>();
        _ = FluentActions.Invoking(() => TypeDocumentationPaths.GetTypeRelativeFilePath(null!))
            .Should().Throw<ArgumentNullException>();
    }
}
