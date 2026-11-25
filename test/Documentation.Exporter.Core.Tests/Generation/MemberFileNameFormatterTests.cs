using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class MemberFileNameFormatterTests
{
    [Fact]
    public void MethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var formatter = new MemberFileNameFormatter();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => formatter.GetMemberDetailFileName(null!, isOverloaded: false));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.FormatMemberHeaderTitle(null!, isOverloaded: false));
    }

    [Fact]
    public void GetMemberDetailFileNameAndHeaderTitleShouldHandleConstructorsOverloadsAndFallbackIdentifier()
    {
        // Arrange
        var formatter = new MemberFileNameFormatter();
        var constructor = new MemberDocumentationModel
        {
            MemberName = "OrderService",
            MemberParameters = [new ParameterDocumentationModel { ParameterName = "timeout", ParameterTypeName = "Int32" }]
        };
        var voidParamsMethod = new MemberDocumentationModel
        {
            MemberName = "RunVoid",
            MemberParameters = [new ParameterDocumentationModel { ParameterName = "items", ParameterTypeName = "[]", IsParams = true }]
        };

        // Act & Assert
        _ = formatter.GetMemberDetailFileName(constructor, isOverloaded: false, isConstructor: true).Should().Be("OrderService.ctor.md");
        _ = formatter.GetMemberDetailFileName(constructor, isOverloaded: true, isConstructor: true).Should().Be("OrderService.ctor_Int32.md");
        _ = formatter.GetMemberDetailFileName(voidParamsMethod, isOverloaded: true).Should().Be("RunVoid_Param.md",
            "because empty sanitized parameter types fall back to Param.");
        _ = formatter.FormatMemberHeaderTitle(voidParamsMethod, isOverloaded: true).Should().Be("RunVoid(params [])");
    }

    [Fact]
    public void GetMemberDetailFileNameAndHeaderTitleShouldHandleOperatorsIndexersGenericArityAndByRefModifiers()
    {
        // Arrange
        var formatter = new MemberFileNameFormatter();
        var checkedAddOperator = new MemberDocumentationModel { MemberName = "operator checked +" };
        var checkedExplicitOperator = new MemberDocumentationModel
        {
            MemberName = "operator checked explicit",
            ReturnTypeName = "Byte",
            MemberParameters = [new ParameterDocumentationModel { ParameterName = "source", ParameterTypeName = "OrderId" }]
        };
        var explicitToInt = new MemberDocumentationModel
        {
            MemberName = "operator explicit",
            ReturnTypeName = "Int32",
            MemberParameters = [new ParameterDocumentationModel { ParameterName = "source", ParameterTypeName = "OrderId" }]
        };
        var explicitToLong = new MemberDocumentationModel
        {
            MemberName = "operator explicit",
            ReturnTypeName = "Int64",
            MemberParameters = [new ParameterDocumentationModel { ParameterName = "source", ParameterTypeName = "OrderId" }]
        };
        var compoundAssignOperator = new MemberDocumentationModel { MemberName = "operator +=" };
        var unsignedShiftOperator = new MemberDocumentationModel { MemberName = "operator >>>" };
        var indexerMember = new MemberDocumentationModel
        {
            MemberName = "this",
            MemberParameters =
            [
                new ParameterDocumentationModel { ParameterName = "row", ParameterTypeName = "Int32" },
                new ParameterDocumentationModel { ParameterName = "column", ParameterTypeName = "String" }
            ]
        };
        var genericOverload = new MemberDocumentationModel
        {
            MemberName = "Transform",
            GenericTypeParameters =
            [
                new ParameterDocumentationModel { ParameterName = "TInput", ParameterTypeName = "TInput" },
                new ParameterDocumentationModel { ParameterName = "TResult", ParameterTypeName = "TResult" }
            ],
            MemberParameters =
            [
                new ParameterDocumentationModel { ParameterName = "input", ParameterTypeName = "TInput", ParameterModifier = "in" },
                new ParameterDocumentationModel { ParameterName = "state", ParameterTypeName = "Int32", ParameterModifier = "ref" },
                new ParameterDocumentationModel { ParameterName = "error", ParameterTypeName = "String?", ParameterModifier = "out" },
                new ParameterDocumentationModel { ParameterName = "readonlyRef", ParameterTypeName = "Decimal", ParameterModifier = "ref readonly" }
            ]
        };

        // Act & Assert
        _ = formatter.GetMemberDetailFileName(checkedAddOperator, isOverloaded: false).Should().Be("op_CheckedAddition.md");
        _ = formatter.GetMemberDetailFileName(checkedExplicitOperator, isOverloaded: false).Should().Be("op_CheckedExplicit.md");
        _ = formatter.GetMemberDetailFileName(explicitToInt, isOverloaded: true).Should().Be("op_Explicit_OrderId_Int32.md");
        _ = formatter.GetMemberDetailFileName(explicitToLong, isOverloaded: true).Should().Be("op_Explicit_OrderId_Int64.md");
        _ = formatter.FormatMemberHeaderTitle(explicitToInt, isOverloaded: true).Should().Be("operator explicit(OrderId) to Int32");
        _ = formatter.GetMemberDetailFileName(compoundAssignOperator, isOverloaded: false).Should().Be("op_AdditionAssignment.md");
        _ = formatter.GetMemberDetailFileName(unsignedShiftOperator, isOverloaded: false).Should().Be("op_UnsignedRightShift.md");
        _ = formatter.GetMemberDetailFileName(indexerMember, isOverloaded: true).Should().Be("this_Int32_String.md");
        _ = formatter.FormatMemberHeaderTitle(indexerMember, isOverloaded: true).Should().Be("this[Int32, String]");

        var genericFileName = formatter.GetMemberDetailFileName(genericOverload, isOverloaded: true);
        _ = genericFileName.Should().Contain("_T2_")
            .And.Contain("in_TInput")
            .And.Contain("ref_Int32")
            .And.Contain("out_String")
            .And.Contain("ref_readonly_Decimal")
            .And.EndWith(".md");
        _ = formatter.FormatMemberHeaderTitle(genericOverload, isOverloaded: true)
            .Should().Be("Transform<TInput, TResult>(in TInput, ref Int32, out String?, ref readonly Decimal)");
    }

    [Fact]
    public void GetMemberDetailFileNameShouldHandleSymbolOnlyNamesSingleModifiersWhitespaceAndUnloadedOverloads()
    {
        // Arrange
        var formatter = new MemberFileNameFormatter();
        var symbolOnlyMember = new MemberDocumentationModel { MemberName = "$#@!" };
        var partialSymbolMember = new MemberDocumentationModel { MemberName = "Get_Value-1.0!" };
        var genericNonOverloaded = new MemberDocumentationModel
        {
            MemberName = "Convert",
            GenericTypeParameters = [new ParameterDocumentationModel { ParameterName = "T", ParameterTypeName = "T" }]
        };
        var singleModifiers = new MemberDocumentationModel
        {
            MemberName = "Dispatch",
            MemberParameters =
            [
                new ParameterDocumentationModel { ParameterName = "first", ParameterTypeName = "Int32", ParameterModifier = "out" },
                new ParameterDocumentationModel { ParameterName = "second", ParameterTypeName = "String", ParameterModifier = "in" },
                new ParameterDocumentationModel { ParameterName = "third", ParameterTypeName = "Boolean", ParameterModifier = "ref" },
                new ParameterDocumentationModel { ParameterName = "fourth", ParameterTypeName = "Byte", ParameterModifier = "scoped" },
                new ParameterDocumentationModel { ParameterName = "fifth", ParameterTypeName = "Double", ParameterModifier = "   " }
            ]
        };

        // Act & Assert
        _ = formatter.GetMemberDetailFileName(symbolOnlyMember, isOverloaded: false).Should().Be("Member.md");
        _ = formatter.GetMemberDetailFileName(partialSymbolMember, isOverloaded: false).Should().Be("Get_Value-1.0.md");
        _ = formatter.GetMemberDetailFileName(genericNonOverloaded, isOverloaded: false).Should().Be("Convert.md");
        _ = formatter.FormatMemberHeaderTitle(genericNonOverloaded, isOverloaded: false).Should().Be("Convert<T>");
        _ = formatter.GetMemberDetailFileName(singleModifiers, isOverloaded: true)
            .Should().Be("Dispatch_out_Int32_in_String_ref_Boolean_Byte_Double.md");
    }
}
