using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class MicrosoftLearnReferenceRegistryTests
{
    [Fact]
    public void FormatTypeCategoryLinkShouldLinkKnownCategoriesAndFallbackForUnknown()
    {
        // Arrange
        var registry = new MicrosoftLearnReferenceRegistry();

        // Act & Assert
        _ = registry.FormatTypeCategoryLink(TypeCategory.Class).Should().Be("[Class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class)");
        _ = registry.FormatTypeCategoryLink(TypeCategory.Interface).Should().Be("[Interface](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/interface)");
        _ = registry.FormatTypeCategoryLink(TypeCategory.Struct).Should().Be("[Struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct)");
        _ = registry.FormatTypeCategoryLink(TypeCategory.Enum).Should().Be("[Enum](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum)");
        _ = registry.FormatTypeCategoryLink(TypeCategory.Delegate).Should().Be("[Delegate](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#the-delegate-type)");
        _ = registry.FormatTypeCategoryLink((TypeCategory)999).Should().Be("`999`");
    }

    [Fact]
    public void FormatHelpersShouldFormatAccessConstructorsConstraintsObsoleteAndMemberVisibility()
    {
        // Arrange
        var registry = new MicrosoftLearnReferenceRegistry();
        var member = new MemberDocumentationModel
        {
            MemberName = "RunAsync",
            VisibilityModifier = "public",
            IsStaticMember = true,
            IsAsyncMethod = true
        };

        // Act & Assert
        _ = registry.FormatAccessModifierLink("public").Should().Be($"[public]({registry.FormatAccessModifierLink("public").Split('(')[1].TrimEnd(')')})");
        _ = registry.FormatAccessModifierLink("custom").Should().Be("`custom`");
        _ = registry.FormatConstructorKindLink("Default").Should().Be($"[Default]({registry.DefaultConstructorsDocUrl})");
        _ = registry.FormatConstructorKindLink("Primary").Should().Be($"[Primary]({registry.PrimaryConstructorsDocUrl})");
        _ = registry.FormatConstructorKindLink("Overloaded").Should().Be($"[Overloaded]({registry.ConstructorsDocUrl})");
        _ = registry.FormatConstructorKindLink("None").Should().Be("`None`");
        _ = registry.FormatGenericConstraintsCell(null).Should().Be("`None`");
        _ = registry.FormatGenericConstraintsCell("`class`, `struct`, `unmanaged`, `new()`, `notnull`")
            .Should().Contain(registry.ClassConstraintDocUrl)
            .And.Contain(registry.StructConstraintDocUrl)
            .And.Contain(registry.UnmanagedConstraintDocUrl)
            .And.Contain(registry.NewConstraintDocUrl)
            .And.Contain(registry.NotNullConstraintDocUrl);
        _ = registry.FormatObsoleteWarningBlock(false, null, false).Should().BeEmpty();
        _ = registry.FormatObsoleteWarningBlock(true, null, isObsoleteError: true).Should().Contain("> [!CAUTION]");
        _ = registry.FormatObsoleteWarningBlock(true, "Use new API.", isObsoleteError: false, diagnosticId: "OBS001", urlFormat: "https://example.com/{0}")
            .Should().Contain("> [!WARNING]")
            .And.Contain("([OBS001](https://example.com/OBS001))")
            .And.Contain("Use new API.");
        _ = registry.FormatObsoleteWarningBlock(true, "Legacy.", isObsoleteError: false, diagnosticId: "OBS002", urlFormat: null)
            .Should().Contain("(`OBS002`)");
        _ = registry.FormatAttributeItem("MyAttribute").Should().Be("`[MyAttribute]`");
        _ = registry.FormatEnumBaseTypeLink("Byte").Should().Be("[Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte)");
        _ = registry.FormatMemberVisibility(member).Should().Contain("[public]").And.Contain("[static]").And.Contain("[async]");

        _ = registry.FormatAccessModifierLink("protected internal")
            .Should().Be("[protected](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected) [internal](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/internal)");
        _ = registry.FormatAccessModifierLink("private protected")
            .Should().Be("[private](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/private) [protected](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected)");
        _ = registry.FormatGenericConstraintsCell("`allows ref struct`")
            .Should().Contain(registry.AllowsRefStructConstraintDocUrl);
        _ = registry.RecordKeywordDocUrl.Should().Contain("record");
        _ = registry.ReadOnlyKeywordDocUrl.Should().Contain("readonly");
        _ = registry.RefStructKeywordDocUrl.Should().Contain("builtin-types/ref-struct");
        _ = registry.ExperimentalAttributeDocUrl.Should().Contain("experimentalattribute");
        _ = registry.ScopedModifierDocUrl.Should().Contain("scoped");

        _ = registry.FormatExperimentalWarningBlock(false, null, null).Should().BeEmpty();
        _ = registry.FormatExperimentalWarningBlock(true, "EXP001", "https://example.com/{0}")
            .Should().Contain("> [!IMPORTANT]")
            .And.Contain("([EXP001](https://example.com/EXP001))");
        _ = registry.FormatExperimentalWarningBlock(true, "EXP002", "{invalid")
            .Should().Contain("> [!IMPORTANT]")
            .And.Contain("([EXP002]({invalid))");
        _ = registry.FormatExperimentalWarningBlock(true, "EXP003", null)
            .Should().Contain("> [!IMPORTANT]")
            .And.Contain("(`EXP003`)");
        _ = registry.FormatExperimentalWarningBlock(true, null, null)
            .Should().Contain("> [!IMPORTANT]")
            .And.Contain("experimental");
    }
}
