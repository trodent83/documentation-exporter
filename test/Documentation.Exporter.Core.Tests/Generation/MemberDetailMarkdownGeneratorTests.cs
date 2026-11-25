using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Generation;

[UnitTest]
public sealed class MemberDetailMarkdownGeneratorTests
{
    private static MemberDetailMarkdownGenerator CreateGenerator() =>
        new(
            new MemberFileNameFormatter(),
            new RelativeLinkRewriter(),
            new ExceptionTableFormatter(),
            new MicrosoftLearnReferenceRegistry());

    [Fact]
    public void ConstructorAndMethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var fileNameFormatter = new MemberFileNameFormatter();
        var linkRewriter = new RelativeLinkRewriter();
        var exceptionFormatter = new ExceptionTableFormatter();
        var referenceRegistry = new MicrosoftLearnReferenceRegistry();
        var generator = CreateGenerator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new MemberDetailMarkdownGenerator(null!, linkRewriter, exceptionFormatter, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new MemberDetailMarkdownGenerator(fileNameFormatter, null!, exceptionFormatter, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new MemberDetailMarkdownGenerator(fileNameFormatter, linkRewriter, null!, referenceRegistry));
        _ = Assert.Throws<ArgumentNullException>(() => new MemberDetailMarkdownGenerator(fileNameFormatter, linkRewriter, exceptionFormatter, null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.GenerateMemberMarkdownFiles(null!));
        _ = Assert.Throws<ArgumentNullException>(() => generator.RenderMemberDetailMarkdown(null!, new MemberDocumentationModel(), false));
        _ = Assert.Throws<ArgumentNullException>(() => generator.RenderMemberDetailMarkdown(new TypeDocumentationModel(), null!, false));
    }

    [Fact]
    public void GenerateMemberMarkdownFilesShouldProduceDetailedPagesForAllNonEnumMemberCategories()
    {
        // Arrange
        var generator = CreateGenerator();
        var model = new TypeDocumentationModel
        {
            TypeName = "OrderProcessingService",
            NamespaceName = "Demo.Services",
            AssemblyName = "Demo",
            Constructors =
            [
                new MemberDocumentationModel { MemberName = "OrderProcessingService", SummaryDescription = "Default ctor." },
                new MemberDocumentationModel
                {
                    MemberName = "OrderProcessingService",
                    SummaryDescription = "Parameterized ctor.",
                    MemberParameters = [new ParameterDocumentationModel { ParameterName = "gateway", ParameterTypeName = "String" }]
                }
            ],
            Properties = [new MemberDocumentationModel { MemberName = "GatewayName", ReturnTypeName = "String", SummaryDescription = "Gateway prop." }],
            Methods = [new MemberDocumentationModel { MemberName = "Execute", ReturnTypeName = "Boolean", SummaryDescription = "Executes order." }],
            Fields = [new MemberDocumentationModel { MemberName = "MaxThreshold", ReturnTypeName = "Decimal", SummaryDescription = "Threshold field." }],
            Events = [new MemberDocumentationModel { MemberName = "StatusChanged", ReturnTypeName = "EventHandler", SummaryDescription = "Status event." }]
        };

        // Act
        var memberFiles = generator.GenerateMemberMarkdownFiles(model);

        // Assert
        _ = memberFiles.Should().HaveCount(6);
        _ = memberFiles.Should().ContainKey("OrderProcessingService.ctor.md")
            .And.ContainKey("OrderProcessingService.ctor_String.md")
            .And.ContainKey("GatewayName.md")
            .And.ContainKey("Execute.md")
            .And.ContainKey("MaxThreshold.md")
            .And.ContainKey("StatusChanged.md");
        _ = memberFiles["Execute.md"].Should().Contain("# OrderProcessingService.Execute")
            .And.Contain("**Declaring Type:** [OrderProcessingService](index.md)")
            .And.Contain("Executes order.");

        var enumModel = new TypeDocumentationModel
        {
            TypeName = "PaymentStatus",
            TypeCategory = TypeCategory.Enum,
            Fields = [new MemberDocumentationModel { MemberName = "Pending", ReturnValueDescription = "0" }]
        };
        _ = generator.GenerateMemberMarkdownFiles(enumModel).Should().BeEmpty(
            "because enum values do not generate separate detail pages.");
    }

    [Fact]
    public void RenderMemberDetailMarkdownShouldRenderFullMemberDetailsTablesAndSections()
    {
        // Arrange
        var generator = CreateGenerator();
        var declaringType = new TypeDocumentationModel
        {
            TypeName = "OrderService",
            NamespaceName = "App.Services",
            AssemblyName = "App.Core"
        };
        var methodModel = new MemberDocumentationModel
        {
            MemberName = "Process",
            MemberSignature = "public bool Process<TPayload>(string orderId)",
            ReturnTypeName = "bool",
            SummaryDescription = "Processes an order by ID.",
            RemarksDescription = "Detailed member remarks.",
            ReturnValueDescription = "True if processed.",
            IsPrimaryConstructor = true,
            IsObsolete = true,
            ObsoleteMessage = "Process is deprecated; use ProcessAsync instead.",
            IsAsyncMethod = true,
            IsAsyncEnumerable = true,
            AppliedAttributes = ["MyAttribute"],
            ReturnAttributes = ["[NotNullIfNotNullAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.notnullifnotnullattribute)`(\"orderId\")`"],
            GenericTypeParameters =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "TPayload",
                    ParameterConstraints = "`class`, `new()`",
                    ParameterDescription = "The payload type.",
                    AppliedAttributes = ["[TypeArgAttribute](Attributes/TypeArgAttribute/index.md)`(Targets.A | Targets.B)`"]
                },
                new ParameterDocumentationModel
                {
                    ParameterName = "TMeta",
                    ParameterConstraints = string.Empty,
                    AppliedAttributes = ["[MetaAttribute](Attributes/MetaAttribute/index.md)"]
                }
            ],
            MemberParameters =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "orderId",
                    ParameterTypeName = "string",
                    ParameterDescription = "The order identifier.",
                    ParameterModifier = "in",
                    IsNullable = true,
                    AppliedAttributes = ["[RequiredAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)"]
                },
                new ParameterDocumentationModel
                {
                    ParameterName = "tags",
                    ParameterTypeName = "string[]",
                    ParameterDescription = "Audit tags.",
                    IsParams = true,
                    ParameterModifier = "params"
                },
                new ParameterDocumentationModel
                {
                    ParameterName = "cancellationToken",
                    ParameterTypeName = "CancellationToken",
                    ParameterDescription = "Token to cancel enumeration.",
                    IsCancellationToken = true,
                    IsEnumeratorCancellation = true,
                    IsOptional = true,
                    DefaultValue = "default",
                    CallerInformationAttribute = "CallerMemberName"
                }
            ],
            DocumentedExceptions = new Dictionary<string, string>
            {
                ["[ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception)"] = "When orderId is empty."
            },
            CodeExamples = ["service.Process(\"123\");"],
            SeeAlsoReferences = ["[OrderRequest](../Models/OrderRequest/index.md)"]
        };

        // Act
        var output = generator.RenderMemberDetailMarkdown(declaringType, methodModel, isOverloaded: false);

        // Assert
        _ = output.Should().Contain("# OrderService.Process<TPayload, TMeta>")
            .And.Contain("**Declaring Type:** [OrderService](OrderService/index.md)")
            .And.Contain("`[MyAttribute]`")
            .And.Contain("> [!WARNING]")
            .And.Contain("> **[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute):** Process is deprecated; use ProcessAsync instead.")
            .And.Contain("[Primary Constructor](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#primary-constructors)")
            .And.Contain("**[Async](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/):** [Yes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/async) ([IAsyncEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1))")
            .And.Contain("**[[EnumeratorCancellation]](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.enumeratorcancellationattribute):** Token to cancel enumeration.")
            .And.Contain("#### Type Parameters")
            .And.Contain("| `TPayload` | [class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class), [new()](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/new-constraint), [TypeArgAttribute](Attributes/TypeArgAttribute/index.md)`(Targets.A \\| Targets.B)` | The payload type. |")
            .And.Contain("| `TMeta` | [MetaAttribute](Attributes/MetaAttribute/index.md) |  |")
            .And.Contain("#### Parameters")
            .And.Contain("[RequiredAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)")
            .And.Contain("[in](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#in-parameter-modifier)")
            .And.Contain("[Nullable](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)")
            .And.Contain("[params](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#params-modifier)")
            .And.Contain("[Optional](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/named-and-optional-arguments#optional-arguments)")
            .And.Contain("[[CallerMemberName]](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information)")
            .And.Contain("#### Returns")
            .And.Contain("**[Attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/):** [NotNullIfNotNullAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.notnullifnotnullattribute)`(\"orderId\")`")
            .And.Contain("#### Exceptions")
            .And.Contain("## Remarks")
            .And.Contain("#### Examples")
            .And.Contain("## See Also");
    }

    [Fact]
    public void RenderMemberDetailMarkdownWithAllParameterModifiersAndCautionObsoleteShouldRenderExpectedLinks()
    {
        // Arrange
        var generator = CreateGenerator();
        var declaringType = new TypeDocumentationModel
        {
            TypeName = "ExtensionHelpers",
            NamespaceName = "App.Extensions",
            AssemblyName = "App.Core"
        };
        var methodModel = new MemberDocumentationModel
        {
            MemberName = "Extend",
            MemberSignature = "public static void Extend(this string target)",
            ReturnTypeName = "String",
            ResolvedReturnTypeDocumentationLink = "[String](https://learn.microsoft.com/en-us/dotnet/api/system.string)",
            VisibilityModifier = "custom",
            IsStaticMember = true,
            IsObsolete = true,
            IsObsoleteError = true,
            IsAsyncMethod = true,
            MemberParameters =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "target",
                    ParameterTypeName = "String",
                    ResolvedTypeDocumentationLink = "[String](https://learn.microsoft.com/en-us/dotnet/api/system.string)",
                    IsExtensionTarget = true,
                    ParameterModifier = "this ref readonly"
                },
                new ParameterDocumentationModel { ParameterName = "outputValue", ParameterTypeName = "Int32", ParameterModifier = "out" },
                new ParameterDocumentationModel { ParameterName = "mutableRef", ParameterTypeName = "Int32", ParameterModifier = "ref" },
                new ParameterDocumentationModel { ParameterName = "token", ParameterTypeName = "CancellationToken", IsCancellationToken = true, IsOptional = true }
            ],
            DocumentedExceptions = new Dictionary<string, string>
            {
                ["System.InvalidOperationException"] = "Raw exception name."
            }
        };

        // Act
        var output = generator.RenderMemberDetailMarkdown(declaringType, methodModel, isOverloaded: true);

        // Assert
        _ = output.Should().Contain("> [!CAUTION]")
            .And.Contain("[this](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)")
            .And.Contain("[ref readonly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#ref-readonly-modifier)")
            .And.Contain("[out](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#out-parameter-modifier)")
            .And.Contain("[ref](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref)")
            .And.Contain("**[Async Cancellation](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads):**")
            .And.Contain("`System.InvalidOperationException`");
    }

    [Fact]
    public void RenderMemberDetailMarkdownShouldRenderExperimentalBannerScopedModifierAndExtensionProperty()
    {
        // Arrange
        var generator = CreateGenerator();
        var declaringType = new TypeDocumentationModel
        {
            TypeName = "ModernLedger",
            NamespaceName = "App.Ledger",
            AssemblyName = "App"
        };
        var experimentalScopedMethod = new MemberDocumentationModel
        {
            MemberName = "CombineScoped",
            VisibilityModifier = "public",
            IsStaticMember = true,
            IsExperimental = true,
            ExperimentalDiagnosticId = "EXP900",
            ExperimentalUrlFormat = "https://docs.example.com/{0}",
            MemberSignature = "public static String CombineScoped(scoped ref readonly Int32 value)",
            ReturnTypeName = "String",
            MemberParameters =
            [
                new ParameterDocumentationModel
                {
                    ParameterName = "value",
                    ParameterTypeName = "Int32",
                    ParameterModifier = "scoped ref readonly"
                }
            ]
        };
        var extensionProperty = new MemberDocumentationModel
        {
            MemberName = "IsEmpty",
            VisibilityModifier = "public",
            IsExtensionProperty = true,
            ExtensionTargetTypeFullName = "App.Ledger.ModernLedger",
            MemberSignature = "public Boolean IsEmpty { get; }",
            ReturnTypeName = "Boolean",
            SummaryDescription = "Checks if empty."
        };

        // Act
        var methodMarkdown = generator.RenderMemberDetailMarkdown(declaringType, experimentalScopedMethod, isOverloaded: false);
        var propertyMarkdown = generator.RenderMemberDetailMarkdown(declaringType, extensionProperty, isOverloaded: false);

        // Assert
        _ = methodMarkdown.Should().Contain("> [!IMPORTANT]")
            .And.Contain("[EXP900](https://docs.example.com/EXP900)")
            .And.Contain("[scoped](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/declarations#scoped-ref)")
            .And.Contain("[ref readonly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#ref-readonly-modifier)");
        _ = propertyMarkdown.Should().Contain("# ModernLedger.IsEmpty")
            .And.Contain("Checks if empty.")
            .And.Contain("public Boolean IsEmpty { get; }");
    }
}
