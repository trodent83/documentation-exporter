using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class SyntaxDeclarationSectionRendererTests
{
    [Fact]
    public void OrderPropertyShouldReturnTwenty()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());

        // Assert
        _ = renderer.Order.Should().Be(20);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new SyntaxDeclarationSectionRenderer(null!));
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionWhenTypeCharacteristicsPresentShouldRenderLinkedCharacteristicList()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());
        var model = new TypeDocumentationModel
        {
            TypeName = "AuditableProcessorBase",
            CSharpDeclarationSignature = "public abstract class AuditableProcessorBase(String serviceName)",
            AccessModifier = "public",
            ConstructorKind = "Primary (String serviceName)",
            IsAbstractType = true,
            IsSealedType = false,
            IsStaticType = false,
            IsGenericType = true,
            GenericTypeParameters = ["TPayload"],
            GenericTypeParameterDetails =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "TPayload",
                    ParameterConstraints = "`class`, `new()`",
                    ParameterDescription = "The payload type."
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("```csharp")
            .And.Contain("public abstract class AuditableProcessorBase(String serviceName)")
            .And.Contain("**[Access](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers):** [public](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/public)")
            .And.Contain("**[Constructor](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constructors):** [Primary](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#primary-constructors) `(String serviceName)`")
            .And.Contain("**[Abstract](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/abstract):** `Yes`")
            .And.Contain("**[Sealed](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/sealed):** `No`")
            .And.Contain("**[Static](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/static):** `No`")
            .And.NotContain("**[Generic]")
            .And.Contain("#### Type Parameters")
            .And.Contain("| `TPayload` | [class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class), [new()](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/new-constraint) | The payload type. |");
    }

    [Fact]
    public void RenderSectionWithDefaultAndOverloadedConstructorsShouldRenderMicrosoftLinks()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());
        var defaultModel = new TypeDocumentationModel
        {
            TypeName = "OrderRequest",
            AccessModifier = "protected",
            ConstructorKind = "Default"
        };
        var overloadedModel = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            AccessModifier = "internal",
            ConstructorKind = "Overloaded (2)"
        };

        // Act
        var output = renderer.RenderSection(defaultModel) + renderer.RenderSection(overloadedModel);

        // Assert
        _ = output.Should().Contain("[protected](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected)")
            .And.Contain("[Default](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#default-constructors)")
            .And.Contain("[internal](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/internal)")
            .And.Contain("[Overloaded](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constructors) `(2)`");
    }

    [Fact]
    public void RenderSectionWhenEnumTypeShouldRenderOnlyAccessAndBaseType()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());
        var enumModel = new TypeDocumentationModel
        {
            TypeName = "PaymentStatus",
            TypeCategory = TypeCategory.Enum,
            AccessModifier = "public",
            EnumUnderlyingTypeLink = "[Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32)",
            AppliedAttributes = ["[FlagsAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.flagsattribute)"]
        };

        // Act
        var output = renderer.RenderSection(enumModel);

        // Assert
        _ = output.Should().Contain("**[Access](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers):** [public](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/public)")
            .And.Contain("**[Base Type](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum):** [Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32)")
            .And.Contain("**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [FlagsAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.flagsattribute)",
                "because custom attributes applied on enum types must render on enum documentation pages.")
            .And.NotContain("**[Constructor]")
            .And.NotContain("**[Abstract]")
            .And.NotContain("**[Sealed]")
            .And.NotContain("**[Static]");

        var enumWithUnlinkedBase = new TypeDocumentationModel
        {
            TypeName = "ByteStatus",
            TypeCategory = TypeCategory.Enum,
            AccessModifier = "private",
            EnumUnderlyingTypeLink = "Byte"
        };
        var enumWithFieldFallback = new TypeDocumentationModel
        {
            TypeName = "FallbackStatus",
            TypeCategory = TypeCategory.Enum,
            Fields = [new MemberDocumentationModel { MemberName = "Zero", ResolvedReturnTypeDocumentationLink = "[Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64)" }]
        };
        var enumWithDefaultFallback = new TypeDocumentationModel
        {
            TypeName = "DefaultEnum",
            TypeCategory = TypeCategory.Enum
        };
        var staticTypeModel = new TypeDocumentationModel
        {
            TypeName = "StaticHelper",
            AccessModifier = "custom_access",
            ConstructorKind = "None",
            IsStaticType = true,
            IsSealedType = true,
            IsGenericType = true,
            GenericTypeParameterDetails = [new ParameterDocumentationModel { ParameterName = "T", ParameterConstraints = string.Empty }]
        };

        var fallbackOutput = string.Concat(
            renderer.RenderSection(enumWithUnlinkedBase),
            renderer.RenderSection(enumWithFieldFallback),
            renderer.RenderSection(enumWithDefaultFallback),
            renderer.RenderSection(staticTypeModel));

        _ = fallbackOutput.Should().Contain("[Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte)")
            .And.Contain("[Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64)")
            .And.Contain("`None`");
    }

    [Fact]
    public void RenderSectionWhenNoSignatureAndNoTypeNameShouldRenderNothing()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());
        var model = new TypeDocumentationModel();

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().BeEmpty();
    }

    [Fact]
    public void RenderSectionWhenGenericTypeParametersHaveAppliedAttributesShouldRenderAndEscapePipes()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());
        TypeDocumentationModel model = new()
        {
            TypeName = "AttributedContainer",
            IsGenericType = true,
            GenericTypeParameterDetails =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "TUnconstrained",
                    ParameterConstraints = string.Empty,
                    AppliedAttributes = ["[TagAttribute](Attributes/TagAttribute/index.md)`(Targets.A | Targets.B)`"]
                },
                new ParameterDocumentationModel
                {
                    ParameterName = "TConstrained",
                    ParameterConstraints = "`class`",
                    AppliedAttributes = ["[MarkerAttribute](Attributes/MarkerAttribute/index.md)"]
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("| `TUnconstrained` | [TagAttribute](Attributes/TagAttribute/index.md)`(Targets.A \\| Targets.B)` |  |",
                "because type-parameter attributes should render even without type constraints and escape table pipes.")
            .And.Contain("| `TConstrained` | [class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class), [MarkerAttribute](Attributes/MarkerAttribute/index.md) |  |",
                "because type-parameter attributes should append cleanly to existing constraints.");
    }

    [Fact]
    public void RenderSectionShouldRenderRecordReadOnlyRefStructFlagsAndAllowsRefStructConstraint()
    {
        // Arrange
        var renderer = new SyntaxDeclarationSectionRenderer(new MicrosoftLearnReferenceRegistry());
        var model = new TypeDocumentationModel
        {
            TypeName = "ModernSpanRecord",
            TypeCategory = TypeCategory.Struct,
            AccessModifier = "protected internal",
            IsRecordType = true,
            IsReadOnlyType = true,
            IsRefStructType = true,
            IsGenericType = true,
            GenericTypeParameterDetails =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "TSpan",
                    ParameterConstraints = "`allows ref struct`",
                    ParameterDescription = "Any stack-only span type."
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("[protected](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected) [internal](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/internal)")
            .And.Contain("**[Record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record):** `Yes`")
            .And.Contain("**[ReadOnly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly):** `Yes`")
            .And.Contain("**[Ref Struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct):** `Yes`")
            .And.Contain("[allows ref struct](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters#allows-ref-struct)");
    }
}
