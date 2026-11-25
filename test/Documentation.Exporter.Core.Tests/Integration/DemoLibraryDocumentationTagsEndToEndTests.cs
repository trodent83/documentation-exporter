using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.DemoLibrary.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Integration;

/// <summary>
/// Exports the real demo library and verifies that <c>&lt;remarks&gt;</c>, <c>&lt;exception&gt;</c> and
/// <c>&lt;example&gt;</c> documentation is supported for every kind of documented element.
/// </summary>
[IntegrationTest]
public sealed class DemoLibraryDocumentationTagsEndToEndTests : IDisposable
{
    private readonly string outputDirectoryPath;
    private readonly ExportResult exportResult;

    public DemoLibraryDocumentationTagsEndToEndTests()
    {
        outputDirectoryPath = Path.Combine(Path.GetTempPath(), "DocExporter_DemoTags_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(outputDirectoryPath);

        var demoAssemblyPath = typeof(TagShowcaseProcessor).Assembly.Location;
        var demoXmlPath = Path.ChangeExtension(demoAssemblyPath, ".xml");
        _ = File.Exists(demoXmlPath).Should().BeTrue($"because the demo library must ship its XML documentation next to '{demoAssemblyPath}'.");

        var services = new ServiceCollection();
        _ = services.AddDocumentationExporter();
        using var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IDocumentationExporterEngine>();

        exportResult = engine.ExportDocumentationAsync(new ExporterOptions
        {
            AssemblyFilePath = demoAssemblyPath,
            XmlDocumentationFilePath = demoXmlPath,
            OutputDirectoryPath = outputDirectoryPath,
            StrictLinkValidation = true
        }).GetAwaiter().GetResult();
    }

    private string ReadGeneratedPage(params string[] relativePathSegments)
    {
        var pagePath = Path.Combine([outputDirectoryPath, .. relativePathSegments]);
        _ = File.Exists(pagePath).Should().BeTrue($"because the exporter must generate '{string.Join('/', relativePathSegments)}'.");
        return File.ReadAllText(pagePath);
    }

    [Fact]
    public void ExportOfDemoLibraryShouldSucceed() =>
        _ = exportResult.IsSuccess.Should().BeTrue("because the demo library must export without errors.");

    [Fact]
    public void TypePageShouldRenderRemarksExamplesAndSeeAlso()
    {
        // Act
        var typePage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "index.md");

        // Assert
        _ = typePage.Should().Contain("## Remarks", "because the type has <remarks>.")
            .And.Contain("Type-level remarks may span several paragraphs")
            .And.Contain("1. Construct a")
            .And.Contain("[XML documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)")
            .And.Contain("## Examples", "because the type has <example> blocks.")
            .And.Contain("Create a processor and run a command:")
            .And.Contain("var processor = new TagShowcaseProcessor(\"Primary\");")
            .And.Contain("IShowcaseCommandHandler handler = new TagShowcaseProcessor();")
            .And.Contain("## See Also")
            .And.Contain("## Exceptions", "because type-level <exception> tags are rendered on the type page.")
            .And.Contain("Thrown by constructors and commands when a name or command text is empty or too long.")
            .And.Contain("[InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception)");
        _ = typePage.IndexOf("## Exceptions", StringComparison.Ordinal).Should().BeLessThan(
            typePage.IndexOf("## Remarks", StringComparison.Ordinal),
            "because the exceptions section precedes remarks.");
    }

    [Fact]
    public void ConstructorPageShouldRenderRemarksExceptionAndExample()
    {
        // Act
        var constructorPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "TagShowcaseProcessor.ctor_String.md");

        // Assert
        _ = constructorPage.Should().Contain("## Remarks")
            .And.Contain("The name appears in every result")
            .And.Contain("#### Exceptions")
            .And.Contain("[ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception)")
            .And.Contain("is empty or whitespace.<br/>Thrown when", "because two <exception> tags share the same cref and both conditions must be kept.")
            .And.Contain("longer than 64 characters.")
            .And.Contain("#### Examples")
            .And.Contain("var processor = new TagShowcaseProcessor(\"Primary\");");
    }

    [Fact]
    public void PropertyPageShouldRenderRemarksExceptionAndExample()
    {
        // Act
        var propertyPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "RetryLimit.md");

        // Assert
        _ = propertyPage.Should().Contain("## Remarks")
            .And.Contain("Setting a value above")
            .And.Contain("[ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception)")
            .And.Contain("#### Examples")
            .And.Contain("processor.RetryLimit = 5;");
    }

    [Fact]
    public void FieldPageShouldRenderRemarksAndExample()
    {
        // Act
        var fieldPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "MaximumRetryLimit.md");

        // Assert
        _ = fieldPage.Should().Contain("## Remarks")
            .And.Contain("protects downstream systems from retry storms")
            .And.Contain("#### Examples")
            .And.Contain("TagShowcaseProcessor.MaximumRetryLimit");
    }

    [Fact]
    public void EventPageShouldRenderRemarksAndExample()
    {
        // Act
        var eventPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "CommandHandled.md");

        // Assert
        _ = eventPage.Should().Contain("## Remarks")
            .And.Contain("raised synchronously")
            .And.Contain("#### Examples")
            .And.Contain("processor.CommandHandled +=");
    }

    [Fact]
    public void MethodPagesShouldRenderRemarksAllExceptionsAndMultipleExamples()
    {
        // Act
        var executePage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "Execute.md");
        var normalizePage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "Normalize.md");

        // Assert
        _ = executePage.Should().Contain("## Remarks")
            .And.Contain("Execution proceeds in three steps:")
            .And.Contain("* Validate `command`.")
            .And.Contain("#### Exceptions")
            .And.Contain("[InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception)")
            .And.Contain("is empty or whitespace.<br/>Thrown when")
            .And.Contain("longer than 256 characters.")
            .And.Contain("Run a command immediately:")
            .And.Contain("var immediate = processor.Execute(\"ping\");")
            .And.Contain("var queued = processor.Execute(\"ping\", ShowcaseMode.Queued);")
            .And.Contain("## See Also");

        _ = normalizePage.Should().Contain("## Remarks")
            .And.Contain("Static members support the same documentation tags")
            .And.Contain("[ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception)")
            .And.Contain("TagShowcaseProcessor.Normalize(\"  PING \");");
    }

    [Fact]
    public void InheritDocShouldInheritRemarksExceptionsAndExamplesFromInterface()
    {
        // Act
        var implementationPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "Handle.md");
        var interfacePage = ReadGeneratedPage("Diagnostics", "IShowcaseCommandHandler", "Handle.md");

        // Assert
        foreach (var page in new[] { implementationPage, interfacePage })
        {
            _ = page.Should().Contain("Handles a single command.", "because summary must be migrated from interface.")
                .And.Contain("The command text to handle.", "because parameter description must be migrated from interface.")
                .And.Contain("`true` when the command was handled; otherwise, `false`.", "because return description must be migrated.")
                .And.Contain("## Remarks")
                .And.Contain("Implementations must be idempotent")
                .And.Contain("[ArgumentException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception)")
                .And.Contain("#### Examples")
                .And.Contain("handler.Handle(\"ping\");");
        }
    }

    [Fact]
    public void InheritDocWithCrefShouldMigrateDocumentationFromTargetMember()
    {
        // Act
        var cleanPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "Clean.md");

        // Assert
        _ = cleanPage.Should().Contain("Normalizes raw command text by trimming it and converting it to upper case.", "because summary was migrated via <inheritdoc cref=\"Normalize(string)\"/>.")
            .And.Contain("The raw command text.", "because parameter description was migrated from Normalize.")
            .And.Contain("The normalized command text.", "because return description was migrated from Normalize.")
            .And.Contain("## Remarks")
            .And.Contain("Static members support the same documentation tags as instance members.")
            .And.Contain("[ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception)")
            .And.Contain("#### Examples")
            .And.Contain("TagShowcaseProcessor.Normalize(\"  PING \");");
    }

    [Fact]
    public void TypeLevelInheritDocWithCrefShouldMigrateTypeDocumentation()
    {
        // Act
        var extendedHandlerPage = ReadGeneratedPage("Diagnostics", "IExtendedCommandHandler", "index.md");

        // Assert
        _ = extendedHandlerPage.Should().Contain("Contract for components that handle textual commands.", "because type summary was migrated from IShowcaseCommandHandler.")
            .And.Contain("## Remarks")
            .And.Contain("Interface members document");
    }

    [Fact]
    public void InheritDocOnPropertiesAndEventsShouldMigrateDocumentationFromInterface()
    {
        // Act
        var propertyPage = ReadGeneratedPage("Services", "OrderProcessingService", "GatewayProviderName.md");
        var eventPage = ReadGeneratedPage("Services", "OrderProcessingService", "OrderStatusChanged.md");

        // Assert
        _ = propertyPage.Should().Contain("Gets the display name of the configured payment gateway provider.", "because property summary was migrated via <inheritdoc />.")
            .And.Contain("The gateway provider name.", "because return/value description was migrated from interface.");

        _ = eventPage.Should().Contain("Occurs when an order's", "because event summary was migrated via <inheritdoc />.");
    }

    [Fact]
    public void InheritDocWithPartialOverrideShouldPreserveCustomDocsAndInheritMissingElements()
    {
        // Act
        var processOrderPage = ReadGeneratedPage("Services", "OrderProcessingService", "ProcessOrderAsync.md");

        // Assert
        _ = processOrderPage.Should().Contain("Processes a customer order asynchronously and returns the resulting", "because method summary was inherited from IOrderProcessor.")
            .And.Contain("The validated", "because orderRequest parameter was inherited from IOrderProcessor.")
            .And.Contain("A [CancellationToken]", "because cancellationToken parameter was inherited from IOrderProcessor.")
            .And.Contain("A task representing the asynchronous operation", "because return description was inherited from IOrderProcessor.")
            .And.Contain("## Remarks")
            .And.Contain("transitions to")
            .And.Contain("[ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception)")
            .And.Contain("[InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception)");
    }

    [Fact]
    public void MemberPagesShouldIncludeDeclaringTypeLinkForClassInterfaceAndStruct()
    {
        // Act
        var classMemberPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "Execute.md");
        var interfaceMemberPage = ReadGeneratedPage("Contracts", "IOrderProcessor", "ProcessOrderAsync.md");
        var structMemberPage = ReadGeneratedPage("Models", "MonetaryValue", "FormatDisplayValue.md");

        // Assert
        _ = classMemberPage.Should().Contain("**Declaring Type:** [TagShowcaseProcessor](index.md)", "because class member pages must link back to the declaring class.")
            .And.NotContain("[â† Back to ", "because redundant back navigation links should not be generated.");
        _ = interfaceMemberPage.Should().Contain("**Declaring Type:** [IOrderProcessor](index.md)", "because interface member pages must link back to the declaring interface.")
            .And.NotContain("[â† Back to ", "because redundant back navigation links should not be generated.");
        _ = structMemberPage.Should().Contain("**Declaring Type:** [MonetaryValue](index.md)", "because struct member pages must link back to the declaring struct.")
            .And.NotContain("[â† Back to ", "because redundant back navigation links should not be generated.");
    }

    [Fact]
    public void EnumPageShouldRenderPerValueRemarksAndExamplesWithoutSeparatePages()
    {
        // Act
        var enumPage = ReadGeneratedPage("Diagnostics", "ShowcaseMode", "index.md");

        // Assert
        _ = enumPage.Should().Contain("## Values")
            .And.Contain("### Immediate")
            .And.Contain("Immediate mode skips queueing entirely")
            .And.Contain("processor.Execute(\"ping\", ShowcaseMode.Immediate);")
            .And.Contain("### Queued")
            .And.Contain("Queued mode guarantees ordering.")
            .And.Contain("* Commands never run concurrently.");
        _ = File.Exists(Path.Combine(outputDirectoryPath, "Diagnostics", "ShowcaseMode", "Immediate.md"))
            .Should().BeFalse("because enum values are rendered inline.");
    }

    [Fact]
    public void TypeAndNestedPagesShouldRenderTablesNotesCustomLabelsOverloadedCrefsObsoleteBadgesAndDoublyNestedHierarchy()
    {
        // Act
        var showcaseTypePage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "index.md");
        var formatResultStringPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "FormatResult_String.md");
        var formatResultTimestampPage = ReadGeneratedPage("Diagnostics", "TagShowcaseProcessor", "FormatResult_String_Boolean.md");
        var legacyAdapterPage = ReadGeneratedPage("Adapters", "LegacyInvoiceAdapter", "index.md");
        var normalizeInvoiceCodePage = ReadGeneratedPage("Adapters", "LegacyInvoiceAdapter", "NormalizeInvoiceCode.md");
        var rootIndexPage = ReadGeneratedPage("index.md");
        var orderRequestPage = ReadGeneratedPage("Models", "OrderRequest", "index.md");
        var itemLinePage = ReadGeneratedPage("Models", "OrderRequest", "ItemLine", "index.md");
        var taxBreakdownPage = ReadGeneratedPage("Models", "OrderRequest", "ItemLine", "TaxBreakdown", "index.md");

        // Assert
        _ = showcaseTypePage.Should().Contain("| Mode | Behavior |", "because <list type=\"table\"> must render a Markdown table.")
            .And.Contain("* **Custom Label Link**: Implements the [command handler contract](../IShowcaseCommandHandler/index.md)",
                "because <term>+<description> and custom <see cref> label text must be preserved.")
            .And.Contain("> [!TIP]", "because <note type=\"tip\"> must render a GitHub alert callout.")
            .And.Contain("```json", "because <code language=\"json\"> must select the json code fence.")
            .And.Contain("FormatResult_String.md)", "because overloaded cref to FormatResult(string) must link to the overload-specific page.")
            .And.Contain("FormatResult_String_Boolean.md)", "because overloaded cref to FormatResult(string, bool) must link to the overload-specific page.");

        _ = formatResultStringPage.Should().Contain("Formats a normalized command payload without a timestamp prefix.",
            "because FormatResult_String.md must render the 1-parameter overload summary.");
        _ = formatResultTimestampPage.Should().Contain("Formats a normalized command payload with an optional UTC timestamp prefix.",
            "because FormatResult_String_Boolean.md must render the 2-parameter overload summary.");

        _ = legacyAdapterPage.Should().Contain("([DEMO002](https://docs.example.com/diagnostics/DEMO002))",
                "because [Obsolete(DiagnosticId, UrlFormat)] must render diagnostic badge links.")
            .And.Contain("**[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute) (`DEMO003`):**",
                "because member rows on type pages must render inline [Obsolete(DiagnosticId)] badges consistently.");

        _ = normalizeInvoiceCodePage.Should().Contain("**[Obsolete](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute) (`DEMO003`):**",
            "because member detail page for NormalizeInvoiceCode must render the [Obsolete(DiagnosticId)] badge.");

        _ = rootIndexPage.Should().Contain("([DEMO002](https://docs.example.com/diagnostics/DEMO002))",
            "because the root index table in single-assembly mode must surface [Obsolete(DiagnosticId, UrlFormat)] badges.");

        _ = orderRequestPage.Should().Contain("[ItemLine](ItemLine/index.md)",
            "because top-level declaring types must link to their nested child type.");
        _ = itemLinePage.Should().Contain("[TaxBreakdown](TaxBreakdown/index.md)",
            "because doubly nested types must be linked cleanly from their immediate declaring nested type.");

        _ = taxBreakdownPage.Should().Contain("# TaxBreakdown")
            .And.Contain("(../../../../index.md", "because a doubly nested type inside Models/OrderRequest/ItemLine/TaxBreakdown/ sits 4 levels beneath root index.md.")
            .And.Contain("[ItemLine](../index.md)", "because cross-links inside doubly nested types must resolve cleanly to parent declaring types.");
    }

    public void Dispose()
    {
        if (Directory.Exists(outputDirectoryPath))
        {
            Directory.Delete(outputDirectoryPath, recursive: true);
        }
    }
}
