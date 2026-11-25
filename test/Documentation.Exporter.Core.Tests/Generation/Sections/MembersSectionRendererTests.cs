using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation.Sections;

[UnitTest]
public sealed class MembersSectionRendererTests
{
    private static MembersSectionRenderer CreateRenderer() =>
        new(new MemberFileNameFormatter(), new MicrosoftLearnReferenceRegistry());

    [Fact]
    public void OrderPropertyShouldReturnSixty()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Assert
        _ = renderer.Order.Should().Be(60);
    }

    [Fact]
    public void RenderSectionWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MembersSectionRenderer(null!, new MicrosoftLearnReferenceRegistry()));
        _ = Assert.Throws<ArgumentNullException>(() => new MembersSectionRenderer(new MemberFileNameFormatter(), null!));
        _ = Assert.Throws<ArgumentNullException>(() => renderer.RenderSection(null!));
    }

    [Fact]
    public void RenderSectionShouldRenderOverviewTablesWithLinksToMemberDetailFiles()
    {
        // Arrange
        var renderer = CreateRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            Constructors =
            [
                new MemberDocumentationModel
                {
                    MemberName = "OrderService",
                    MemberSignature = "public OrderService(string name)",
                    SummaryDescription = "Creates a new order service.",
                    IsPrimaryConstructor = true,
                    MemberParameters =
                    [
                        new ParameterDocumentationModel { ParameterName = "name", ParameterTypeName = "string", ParameterDescription = "Service name." }
                    ]
                }
            ],
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Timeout",
                    MemberSignature = "public int Timeout { get; set; }",
                    ReturnTypeName = "int",
                    SummaryDescription = "Timeout in seconds.",
                    ReturnValueDescription = "Positive integer."
                }
            ],
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Process",
                    MemberSignature = "public bool Process()",
                    ReturnTypeName = "bool",
                    SummaryDescription = "Processes orders.",
                    ReturnValueDescription = "True if succeeded.",
                    DocumentedExceptions = new Dictionary<string, string> { ["System.InvalidOperationException"] = "When not ready." },
                    CodeExamples = ["service.Process();"]
                }
            ],
            Fields =
            [
                new MemberDocumentationModel
                {
                    MemberName = "DefaultCapacity",
                    MemberSignature = "public static int DefaultCapacity = 100",
                    ReturnTypeName = "int",
                    IsStaticMember = true,
                    SummaryDescription = "Default capacity."
                }
            ],
            Events =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Completed",
                    MemberSignature = "public event EventHandler Completed",
                    ReturnTypeName = "EventHandler",
                    SummaryDescription = "Fired when done."
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## Constructors")
            .And.Contain("[OrderService(string)](OrderService.ctor.md)")
            .And.Contain("[public](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/public)")
            .And.Contain("[static](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/static)")
            .And.Contain("[Primary Constructor](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#primary-constructors)")
            .And.Contain("## Properties")
            .And.Contain("[Timeout](Timeout.md)")
            .And.Contain("## Methods")
            .And.Contain("[Process](Process.md)")
            .And.Contain("## Fields")
            .And.Contain("[DefaultCapacity](DefaultCapacity.md)")
            .And.Contain("## Events")
            .And.Contain("[Completed](Completed.md)");
    }

    [Fact]
    public void RenderSectionWhenEnumTypeShouldRenderValuesTableWithoutMemberLinks()
    {
        // Arrange
        var renderer = CreateRenderer();
        var enumModel = new TypeDocumentationModel
        {
            TypeName = "PaymentStatus",
            TypeCategory = TypeCategory.Enum,
            Fields =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Pending",
                    ReturnValueDescription = "0",
                    SummaryDescription = "Awaiting verification."
                },
                new MemberDocumentationModel
                {
                    MemberName = "Settled",
                    ReturnValueDescription = "2",
                    SummaryDescription = "Payment settled.",
                    RemarksDescription = "Settled remarks.",
                    CodeExamples = ["```csharp\nvar status = Status.Settled;\n```"]
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(enumModel);

        // Assert
        _ = output.Should().Contain("## Values")
            .And.Contain("| Name | Value | Description |")
            .And.Contain("| `Pending` | `0` | Awaiting verification. |")
            .And.Contain("| `Settled` | `2` | Payment settled. |")
            .And.Contain("### Settled")
            .And.Contain("Settled remarks.")
            .And.Contain("var status = Status.Settled;")
            .And.NotContain("### Pending", "because values without remarks or examples need no detail block.")
            .And.NotContain("## Fields")
            .And.NotContain("Pending.md");

        var emptyEnumModel = new TypeDocumentationModel { TypeName = "EmptyEnum", TypeCategory = TypeCategory.Enum };
        var emptyOutput = renderer.RenderSection(emptyEnumModel);
        _ = emptyOutput.Should().BeEmpty();
    }

    [Fact]
    public void RenderSectionWhenMembersAndEnumValuesHaveAppliedAttributesShouldIncludeLinkedAttributesAndEscapePipes()
    {
        // Arrange
        var renderer = CreateRenderer();
        TypeDocumentationModel classModel = new()
        {
            TypeName = "OrderRequest",
            Constructors =
            [
                new MemberDocumentationModel
                {
                    MemberName = "OrderRequest",
                    AppliedAttributes = ["[JsonConstructorAttribute](https://example.com/json)"]
                }
            ],
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "CustomerEmail",
                    ReturnTypeName = "String",
                    SummaryDescription = "Contact email.",
                    AppliedAttributes =
                    [
                        "[RequiredAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)",
                        "[RangeAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute)`(Targets.A | Targets.B)`"
                    ]
                }
            ]
        };

        TypeDocumentationModel enumModel = new()
        {
            TypeName = "PaymentStatus",
            TypeCategory = TypeCategory.Enum,
            Fields =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Pending",
                    ReturnValueDescription = "0",
                    SummaryDescription = "Awaiting verification.",
                    AppliedAttributes = ["[DisplayAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.displayattribute)`(Name = \"Pending Verification\")`"]
                }
            ]
        };

        // Act
        var classOutput = renderer.RenderSection(classModel);
        var enumOutput = renderer.RenderSection(enumModel);

        // Assert
        _ = classOutput.Should().Contain("**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [JsonConstructorAttribute](https://example.com/json)",
                "because member overview tables should render attributes even when summary is empty.")
            .And.Contain("Contact email.<br/>**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [RequiredAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute), [RangeAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute)`(Targets.A \\| Targets.B)`",
                "because overview cells must join summary and escaped attributes cleanly.");

        _ = enumOutput.Should().Contain("Awaiting verification.<br/>**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [DisplayAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.displayattribute)`(Name = \"Pending Verification\")`",
            "because enum value tables must also display linked custom attributes.");
    }

    [Fact]
    public void RenderSectionShouldRenderExtensionPropertiesOperatorsAndIndexersWithModernBadges()
    {
        // Arrange
        var renderer = CreateRenderer();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderEnvelope",
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "this",
                    ReturnTypeName = "String",
                    VisibilityModifier = "public",
                    SummaryDescription = "Indexer summary.",
                    MemberParameters = [new ParameterDocumentationModel { ParameterName = "index", ParameterTypeName = "Int32" }]
                }
            ],
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "operator +",
                    ReturnTypeName = "OrderEnvelope",
                    VisibilityModifier = "public",
                    IsStaticMember = true,
                    SummaryDescription = "Adds two envelopes."
                }
            ],
            ExtensionProperties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "HasItems",
                    ReturnTypeName = "Boolean",
                    VisibilityModifier = "public",
                    IsExtensionProperty = true,
                    SummaryDescription = "Indicates whether items exist."
                }
            ]
        };

        // Act
        var output = renderer.RenderSection(model);

        // Assert
        _ = output.Should().Contain("## Properties")
            .And.Contain("[this[Int32]](this.md)")
            .And.Contain("## Methods")
            .And.Contain("[operator +](op_UnaryOrBinaryPlus.md)")
            .And.Contain("## Extension Properties")
            .And.Contain("HasItems");
    }

    [Fact]
    public void RenderSectionShouldHandleVoidReturnsCombinedStatusBadgesEmptyDiagnosticsAndZeroEnumValues()
    {
        // Arrange
        var renderer = CreateRenderer();
        var voidAndStatusType = new TypeDocumentationModel
        {
            TypeName = "StatusSample",
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "RunLegacyExperimental",
                    VisibilityModifier = "public",
                    IsObsolete = true,
                    ObsoleteMessage = string.Empty,
                    IsExperimental = true,
                    ExperimentalDiagnosticId = string.Empty
                },
                new MemberDocumentationModel
                {
                    MemberName = "RunPreview",
                    VisibilityModifier = "public",
                    ReturnTypeName = "Int32",
                    IsExperimental = true,
                    ExperimentalDiagnosticId = "EXP01",
                    SummaryDescription = "Preview method."
                },
                new MemberDocumentationModel
                {
                    MemberName = "RunDeprecatedWithCode",
                    VisibilityModifier = "public",
                    ReturnTypeName = "Int32",
                    IsObsolete = true,
                    ObsoleteMessage = "Use RunModern instead.",
                    ObsoleteDiagnosticId = "OBS009",
                    ObsoleteUrlFormat = "https://docs.example.com/warnings/{0}",
                    SummaryDescription = "Legacy operation."
                }
            ]
        };
        var enumWithBlankNumericValue = new TypeDocumentationModel
        {
            TypeName = "FallbackEnum",
            TypeCategory = TypeCategory.Enum,
            Fields =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Default",
                    ReturnValueDescription = string.Empty,
                    SummaryDescription = "Default entry."
                }
            ]
        };

        // Act
        var classMarkdown = renderer.RenderSection(voidAndStatusType);
        var enumMarkdown = renderer.RenderSection(enumWithBlankNumericValue);

        // Assert
        _ = classMarkdown.Should().Contain("`void`")
            .And.Contain("[Obsolete]")
            .And.Contain("**[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute) ([OBS009](https://docs.example.com/warnings/OBS009)):** Use RunModern instead.",
                "because member summary tables should surface inline [Obsolete(DiagnosticId, UrlFormat)] badges.")
            .And.Contain("[Experimental]")
            .And.Contain("`EXP01`");
        _ = enumMarkdown.Should().Contain("| `Default` | `0` | Default entry. |");
    }
}
