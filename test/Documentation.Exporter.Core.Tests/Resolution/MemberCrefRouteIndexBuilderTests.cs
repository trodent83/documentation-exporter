using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Resolution;

[UnitTest]
public sealed class MemberCrefRouteIndexBuilderTests
{
    public sealed class OuterContainer
    {
        public sealed class MiddleContainer
        {
            public sealed class OverloadShowcase
            {
                private string storedLabel;

                public OverloadShowcase() => storedLabel = string.Empty;
                public OverloadShowcase(string label) => storedLabel = label;

                public int Value => storedLabel.Length;

                public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                public string this[string key] => key;

                public void Run(string text) => storedLabel = text;
                public void Run(ref int count, out bool updated, in Guid token) =>
                    updated = count > storedLabel.Length && token != Guid.Empty;

                public TOutput Convert<TOutput>(TOutput input) => storedLabel is not null ? input : input;

                public void SingleVisibleOverload(string text) => storedLabel = text;

                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public void SingleVisibleOverload(int hiddenCode) => storedLabel = hiddenCode.ToString(System.Globalization.CultureInfo.InvariantCulture);

                public static OverloadShowcase operator +(OverloadShowcase left, OverloadShowcase right) =>
                    left.storedLabel.Length >= right.storedLabel.Length ? left : right;
            }
        }
    }

    [Fact]
    public void ConstructorAndNullMapGuardsShouldThrowArgumentNullException()
    {
        // Arrange
        var signatureFormatter = new CSharpSignatureFormatter();
        var fileNameFormatter = new MemberFileNameFormatter();
        var indexBuilder = new MemberCrefRouteIndexBuilder(signatureFormatter, fileNameFormatter);

        // Act & Assert
        _ = FluentActions.Invoking(() => new MemberCrefRouteIndexBuilder(null!, fileNameFormatter))
            .Should().Throw<ArgumentNullException>("because signatureFormatter is required");
        _ = FluentActions.Invoking(() => new MemberCrefRouteIndexBuilder(signatureFormatter, null!))
            .Should().Throw<ArgumentNullException>("because memberFileNameFormatter is required");
        _ = FluentActions.Invoking(() => indexBuilder.BuildMemberCrefRoutes(null!, false))
            .Should().Throw<ArgumentNullException>("because exportedTypesMap cannot be null");
    }

    [Fact]
    public void BuildMemberCrefRoutesShouldIndexOverloadedConstructorsMethodsRefParamsGenericMethodsOperatorsIndexersAndNestedPaths()
    {
        // Arrange
        var targetType = typeof(OuterContainer.MiddleContainer.OverloadShowcase);
        var typeXmlFullName = targetType.FullName!.Replace('+', '.');
        Dictionary<string, Type> exportedTypes = new(StringComparer.Ordinal)
        {
            [targetType.FullName!] = targetType
        };

        var indexBuilder = new MemberCrefRouteIndexBuilder(new CSharpSignatureFormatter(), new MemberFileNameFormatter());

        // Act
        var routes = indexBuilder.BuildMemberCrefRoutes(exportedTypes, useAssemblySubdirectories: true);
        var assemblyName = targetType.Assembly.GetName().Name!;
        var expectedDirPrefix = $"{assemblyName}/Resolution/{nameof(MemberCrefRouteIndexBuilderTests)}/OuterContainer/MiddleContainer/OverloadShowcase";

        // Assert
        _ = routes.Should().ContainKey($"{typeXmlFullName}.#ctor")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/OverloadShowcase.ctor.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.#ctor(System.String)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/OverloadShowcase.ctor_String.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.Run(System.String)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/Run_String.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.Run(System.Int32@,System.Boolean@,System.Guid@)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/Run_ref_Int32_out_Boolean_in_Guid.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.Convert``1(``0)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/Convert.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.op_Addition({typeXmlFullName},{typeXmlFullName})")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/op_UnaryOrBinaryPlus.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.Item(System.Int32)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/this_Int32.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.Item(System.String)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/this_String.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.Value")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/Value.md");
        _ = routes.Should().ContainKey($"{typeXmlFullName}.SingleVisibleOverload(System.String)")
            .WhoseValue.RelativeUrl.Should().Be($"{expectedDirPrefix}/SingleVisibleOverload.md",
                "because excluding an [EditorBrowsable(Never)] overload leaves a single visible method whose emitted page is SingleVisibleOverload.md.");
        _ = routes.Should().NotContainKey($"{typeXmlFullName}.SingleVisibleOverload(System.Int32)");
    }
}
