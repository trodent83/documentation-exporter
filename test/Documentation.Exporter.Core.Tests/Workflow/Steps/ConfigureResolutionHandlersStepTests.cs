using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Abstractions.Resolution;
using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Resolution.Providers;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

public sealed class ConfigureResolutionHandlersStepTests
{
    private sealed class CustomTestNamespaceHandler(int handlerOrder = 0) : INamespaceHandler
    {
        public int Order => handlerOrder;

        public bool CanHandleNamespace(string fullTypeName) => fullTypeName.StartsWith("Custom.", StringComparison.Ordinal);

        public string ResolveTypeLink(string fullTypeName) => $"[Custom:{Order}]({fullTypeName})";
    }

    private sealed class OrderedSystemOverrideHandler(int handlerOrder, string labelPrefix) : INamespaceHandler
    {
        public int Order => handlerOrder;

        public bool CanHandleNamespace(string fullTypeName) =>
            fullTypeName.StartsWith("System.Override.", StringComparison.Ordinal);

        public string ResolveTypeLink(string fullTypeName) =>
            $"[{labelPrefix}:{Order}]({fullTypeName})";
    }

    private sealed class OrderedProvider(int providerOrder, params INamespaceHandler[] handlersToReturn) : INamespaceHandlerProvider
    {
        public int Order => providerOrder;

        public IEnumerable<INamespaceHandler> CreateHandlers(ExportWorkflowContext context) => handlersToReturn;
    }

    private sealed class OverloadedTargetSample
    {
        public static string FormatValue(string textValue) => textValue;
        public static string FormatValue(int numericValue) => numericValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<INamespaceHandlerProvider> CreateBuiltInProviders() =>
    [
        new LocalProjectNamespaceHandlerProvider(new MemberCrefRouteIndexBuilder(new CSharpSignatureFormatter(), new MemberFileNameFormatter())),
        new CustomOptionsNamespaceHandlerProvider(),
        new PatternNamespaceHandlerProvider(),
        new MicrosoftDocsNamespaceHandlerProvider()
    ];

    [Fact]
    [UnitTest]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var step = new ConfigureResolutionHandlersStep(resolver, CreateBuiltInProviders());

        // Assert
        _ = step.StepName.Should().Be("Configure Resolution Handlers");
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var providers = CreateBuiltInProviders();
        var step = new ConfigureResolutionHandlersStep(resolver, providers);

        // Act & Assert
        _ = FluentActions.Invoking(() => new ConfigureResolutionHandlersStep(null!, providers))
            .Should().Throw<ArgumentNullException>("because typeLinkResolver cannot be null.");
        _ = FluentActions.Invoking(() => new ConfigureResolutionHandlersStep(resolver, null!))
            .Should().Throw<ArgumentNullException>("because namespaceHandlerProviders cannot be null.");
        _ = await FluentActions.Awaiting(() => step.ExecuteAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>("because context cannot be null.");

        var emptyContext = new ExportWorkflowContext(new ExporterOptions()) { ExportedTypesMap = new Dictionary<string, Type>() };
        var emptyResult = await step.ExecuteAsync(emptyContext);
        _ = emptyResult.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncShouldRegisterLocalAndCustomHandlers()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var step = new ConfigureResolutionHandlersStep(resolver, CreateBuiltInProviders());

        var options = new ExporterOptions
        {
            CustomNamespaceHandlers = [new CustomTestNamespaceHandler()]
        };
        var context = new ExportWorkflowContext(options)
        {
            ExportedTypesMap = new Dictionary<string, Type>
            {
                ["App.Services.OrderService"] = typeof(string)
            }
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue();
        var localLink = resolver.ResolveLink("App.Services.OrderService");
        _ = localLink.Should().Be("[OrderService](OrderService.md)");

        var customLink = resolver.ResolveLink("Custom.SpecialType");
        _ = customLink.Should().Be("[Custom:0](Custom.SpecialType)");
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncShouldSortProvidersAndStandaloneHandlersByPrecedenceOrder()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        INamespaceHandlerProvider[] providers =
        [
            new MicrosoftDocsNamespaceHandlerProvider(),
            new PatternNamespaceHandlerProvider(),
            new CustomOptionsNamespaceHandlerProvider(),
            new LocalProjectNamespaceHandlerProvider(new MemberCrefRouteIndexBuilder(new CSharpSignatureFormatter(), new MemberFileNameFormatter())),
            new OrderedProvider(750, new OrderedSystemOverrideHandler(750, "MidProvider"))
        ];
        INamespaceHandler[] standaloneHandlers =
        [
            new OrderedSystemOverrideHandler(2000, "LateStandalone"),
            new OrderedSystemOverrideHandler(0, "BetweenCustomAndPattern"),
            new OrderedSystemOverrideHandler(-2000, "EarliestStandalone")
        ];
        var step = new ConfigureResolutionHandlersStep(resolver, providers, standaloneHandlers);

        ExporterOptions options = new()
        {
            ExternalDocLinkRules =
            [
                new ExternalDocLinkRule
                {
                    NamespacePrefix = "System.Override*",
                    UrlTemplate = "https://pattern.example/{type}"
                }
            ]
        };
        ExportWorkflowContext context = new(options);

        // Act
        var succeeded = await step.ExecuteAsync(context);

        // Assert
        _ = succeeded.Should().BeTrue();
        _ = resolver.ResolveLink("System.Override.Widget").Should().Be("[EarliestStandalone:-2000](System.Override.Widget)",
            "because a standalone handler with Order = -2000 must execute before all built-in and later handlers.");

        // Now omit -2000 and assert Order=0 beats Pattern (500), which beats Provider(750), which beats MicrosoftDocs (1000)
        TypeLinkResolver midTierResolver = new();
        ConfigureResolutionHandlersStep midTierStep = new(
            midTierResolver,
            [new MicrosoftDocsNamespaceHandlerProvider(), new OrderedProvider(750, new OrderedSystemOverrideHandler(0, "ProviderInherit")), new PatternNamespaceHandlerProvider()],
            [new OrderedSystemOverrideHandler(2000, "Late")]);

        _ = await midTierStep.ExecuteAsync(new ExportWorkflowContext(new ExporterOptions()));
        _ = midTierResolver.ResolveLink("System.Override.Widget").Should().Be("[ProviderInherit:0](System.Override.Widget)",
            "because handler at provider Order 750 runs before MicrosoftDocsNamespaceHandlerProvider (1000) and Late (2000).");
    }

    [Fact]
    [UnitTest]
    public async Task ExecuteAsyncShouldRegisterOverloadedMemberCrefRoutes()
    {
        // Arrange
        var resolver = new TypeLinkResolver();
        var step = new ConfigureResolutionHandlersStep(resolver, CreateBuiltInProviders());
        var targetType = typeof(OverloadedTargetSample);
        var targetXmlFullName = targetType.FullName!.Replace('+', '.');

        var context = new ExportWorkflowContext(new ExporterOptions())
        {
            ExportedTypesMap = new Dictionary<string, Type>
            {
                [targetType.FullName!] = targetType
            }
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue();
        var overloadedStringLink = resolver.ResolveLink($"M:{targetXmlFullName}.{nameof(OverloadedTargetSample.FormatValue)}(System.String)");
        _ = overloadedStringLink.Should().EndWith("/FormatValue_String.md)");
        _ = overloadedStringLink.Should().Contain("FormatValue(String)", "because exact overload cref routes must resolve to the overload-suffixed file.");

        var overloadedIntLink = resolver.ResolveLink($"M:{targetXmlFullName}.{nameof(OverloadedTargetSample.FormatValue)}(System.Int32)");
        _ = overloadedIntLink.Should().EndWith("/FormatValue_Int32.md)");
        _ = overloadedIntLink.Should().Contain("FormatValue(Int32)", "because exact overload cref routes must distinguish parameter types.");
    }
}
