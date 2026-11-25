using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing;

[UnitTest]
public sealed class CustomAttributeExtractorTests
{
    [AttributeUsage(AttributeTargets.All)]
    private sealed class RichSampleAttribute(
        AttributeTargets targets,
        Type targetType,
        bool isActive = true,
        params string[] tags) : Attribute
    {
        public AttributeTargets Targets { get; } = targets;
        public Type TargetType { get; } = targetType;
        public bool IsActive { get; } = isActive;
        public string[] Tags { get; } = tags;
        public string? Note { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class GenericSampleAttribute<TValue> : Attribute;

    [Obsolete("Deprecated sample class")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RichSample(AttributeTargets.Class | AttributeTargets.Struct, typeof(string), false, "alpha", "beta", Note = "custom")]
    [GenericSample<string>]
    private sealed class AttributedSampleType
    {
        [return: Description("Non-null return")]
        public string MethodWithParametersAndReturn(
            [Required] string requiredInput,
            [CallerMemberName] string callerName = "") =>
            $"{requiredInput}:{callerName}";

        public async IAsyncEnumerable<int> StreamNumbersAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield return 1;
        }

        [Obsolete("Deprecated member", error: true)]
        public void ObsoleteErrorMethod()
        {
        }
    }

    [Fact]
    public void ConstructorWhenLinkResolverNullShouldThrowArgumentNullException() =>
        _ = Assert.Throws<ArgumentNullException>(() => new CustomAttributeExtractor(null!));

    [Fact]
    public void MethodsWhenMetadataNullShouldThrowArgumentNullException()
    {
        // Arrange
        var extractor = new CustomAttributeExtractor(new TypeLinkResolver());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => extractor.BuildAppliedAttributes((MemberInfo)null!));
        _ = Assert.Throws<ArgumentNullException>(() => extractor.BuildAppliedAttributes((ParameterInfo)null!));
        _ = Assert.Throws<ArgumentNullException>(() => extractor.BuildAppliedAttributes((Assembly)null!));
        _ = Assert.Throws<ArgumentNullException>(() => extractor.IsEditorBrowsableNever(null!));
        _ = Assert.Throws<ArgumentNullException>(() => extractor.ExtractObsoleteMetadata(null!));
        _ = Assert.Throws<ArgumentNullException>(() => extractor.ExtractCallerInformationAttribute(null!));
    }

    [Fact]
    public void ExtractMetadataMethodsShouldReadObsoleteEditorBrowsableAndCallerInfoAttributes()
    {
        // Arrange
        var extractor = new CustomAttributeExtractor(new TypeLinkResolver());
#pragma warning disable CS0618 // Type or member is obsolete
        var targetType = typeof(AttributedSampleType);
        var callerParameter = targetType.GetMethod(nameof(AttributedSampleType.MethodWithParametersAndReturn))!
            .GetParameters()[1];
        var obsoleteErrorMethod = targetType.GetMethod("ObsoleteErrorMethod", BindingFlags.Public | BindingFlags.Instance)!;
#pragma warning restore CS0618

        // Act
        var (isObsolete, message, isError, diagnosticId, urlFormat) = extractor.ExtractObsoleteMetadata(targetType);
        var (isMethodObsolete, methodMessage, isMethodError, _, _) = extractor.ExtractObsoleteMetadata(obsoleteErrorMethod);
        var isNeverBrowsable = extractor.IsEditorBrowsableNever(targetType);
        var callerAttribute = extractor.ExtractCallerInformationAttribute(callerParameter);
        var appliedAttributes = extractor.BuildAppliedAttributes(targetType);

        // Assert
        _ = isObsolete.Should().BeTrue("because AttributedSampleType is decorated with ObsoleteAttribute.");
        _ = message.Should().Be("Deprecated sample class");
        _ = isError.Should().BeFalse();
        _ = isMethodObsolete.Should().BeTrue("because ObsoleteErrorMethod is marked obsolete.");
        _ = methodMessage.Should().Be("Deprecated member");
        _ = isMethodError.Should().BeTrue("because ObsoleteErrorMethod marks error = true.");
        _ = isNeverBrowsable.Should().BeTrue("because AttributedSampleType marks EditorBrowsableState.Never.");
        _ = callerAttribute.Should().Be("CallerMemberName", "because parameter carries [CallerMemberName].");
        _ = appliedAttributes.Should().Contain(
            attribute => attribute.Contains("EditorBrowsableState.Never", StringComparison.Ordinal),
            "because single enum constructor arguments should format with the enum type prefix.");
        _ = appliedAttributes.Should().Contain(
            attribute => attribute.Contains("AttributeTargets.Class | AttributeTargets.Struct", StringComparison.Ordinal) &&
                         attribute.Contains("typeof(String)", StringComparison.Ordinal) &&
                         attribute.Contains("false", StringComparison.Ordinal) &&
                         attribute.Contains("[\"alpha\", \"beta\"]", StringComparison.Ordinal) &&
                         attribute.Contains("Note = \"custom\"", StringComparison.Ordinal),
            "because flags enums, typeof expressions, booleans, arrays, and named arguments must format cleanly.");
        _ = appliedAttributes.Should().Contain(
            attribute => attribute.Contains("GenericSampleAttribute", StringComparison.Ordinal) &&
                         attribute.Contains("String", StringComparison.Ordinal),
            "because generic attributes must resolve both the generic definition and type argument links.");
    }

    [Fact]
    public void BuildAppliedAttributesForParametersAndAssemblyShouldFilterNoisyCompilerAttributes()
    {
        // Arrange
        TypeLinkResolver linkResolver = new();
        linkResolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        var extractor = new CustomAttributeExtractor(linkResolver);

#pragma warning disable CS0618 // Type or member is obsolete
        var methodInfo = typeof(AttributedSampleType).GetMethod(nameof(AttributedSampleType.MethodWithParametersAndReturn))!;
        var streamingMethodInfo = typeof(AttributedSampleType).GetMethod(nameof(AttributedSampleType.StreamNumbersAsync))!;
#pragma warning restore CS0618
        var methodParameters = methodInfo.GetParameters();
        var streamingParameter = streamingMethodInfo.GetParameters()[0];

        // Act
        var requiredParamAttributes = extractor.BuildAppliedAttributes(methodParameters[0]);
        var callerParamAttributes = extractor.BuildAppliedAttributes(methodParameters[1]);
        var cancellationParamAttributes = extractor.BuildAppliedAttributes(streamingParameter);
        var returnAttributes = extractor.BuildAppliedAttributes(methodInfo.ReturnParameter);
        var coreAssemblyAttributes = extractor.BuildAppliedAttributes(typeof(CustomAttributeExtractor).Assembly);

        // Assert
        _ = requiredParamAttributes.Should().ContainSingle(
            attribute => attribute.Contains("RequiredAttribute", StringComparison.Ordinal) &&
                         attribute.Contains("learn.microsoft.com", StringComparison.Ordinal),
            "because validation attributes on parameters should be extracted with documentation links.");
        _ = callerParamAttributes.Should().BeEmpty(
            "because CallerMemberNameAttribute is already surfaced in dedicated caller metadata.");
        _ = cancellationParamAttributes.Should().BeEmpty(
            "because EnumeratorCancellationAttribute is already surfaced as a dedicated badge.");
        _ = returnAttributes.Should().ContainSingle(
            attribute => attribute.Contains("DescriptionAttribute", StringComparison.Ordinal) &&
                         attribute.Contains("Non-null return", StringComparison.Ordinal) &&
                         attribute.Contains("learn.microsoft.com", StringComparison.Ordinal),
            "because return-value custom attributes should be extracted with documentation links.");
        _ = coreAssemblyAttributes.Should().NotContain(
            attribute => attribute.Contains("CompilationRelaxationsAttribute", StringComparison.Ordinal) ||
                         attribute.Contains("TargetFrameworkAttribute", StringComparison.Ordinal) ||
                         attribute.Contains("AssemblyVersionAttribute", StringComparison.Ordinal),
            "because compiler/MSBuild auto-generated assembly plumbing attributes must be excluded.");
    }

    private sealed class RequiredMemberSample
    {
        public required string RequiredName { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.Experimental("EXP001", UrlFormat = "https://example.com/diagnostics/{0}")]
    private sealed class ExperimentalWithUrlSample
    {
        [System.Diagnostics.CodeAnalysis.Experimental("EXP002")]
        public static void InvokeWithoutUrl(params ReadOnlySpan<string> values) => _ = values.Length;

        public static nint SumNative(nint first, nint second) => first + second;
    }

    private readonly ref struct RefStructSample(int length)
    {
        public int Length { get; } = length;
    }

    [Fact]
    public void BuildAppliedAttributesAndExtractObsoleteShouldExcludeModernCompilerAttributesAndSynthesizedRequiredCtorObsolete()
    {
        // Arrange
        var extractor = new CustomAttributeExtractor(new TypeLinkResolver());
        var requiredConstructor = typeof(RequiredMemberSample).GetConstructor(Type.EmptyTypes)!;
        var extensionMarkerType = typeof(SampleExtensionAttributesContainer)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .First(nestedType => nestedType.Name.StartsWith("<G>$", StringComparison.Ordinal));
#pragma warning disable EXP001, EXP002
        var spanParamsMethod = typeof(ExperimentalWithUrlSample).GetMethod(nameof(ExperimentalWithUrlSample.InvokeWithoutUrl))!;
        var nativeMethod = typeof(ExperimentalWithUrlSample).GetMethod(nameof(ExperimentalWithUrlSample.SumNative))!;
        var experimentalTypeAttributes = extractor.BuildAppliedAttributes(typeof(ExperimentalWithUrlSample));
#pragma warning restore EXP001, EXP002

        // Act
        var requiredCtorAttributes = extractor.BuildAppliedAttributes(requiredConstructor);
        var (isObsolete, obsoleteMessage, _, _, _) = extractor.ExtractObsoleteMetadata(requiredConstructor);
        var (isRefStructObsolete, refStructObsoleteMessage, _, _, _) = extractor.ExtractObsoleteMetadata(typeof(RefStructSample));
        var extensionTypeAttributes = extractor.BuildAppliedAttributes(extensionMarkerType);
        var spanParamAttributes = extractor.BuildAppliedAttributes(spanParamsMethod.GetParameters()[0]);
        var nativeReturnAttributes = extractor.BuildAppliedAttributes(nativeMethod.ReturnParameter);

        // Assert
        _ = requiredCtorAttributes.Should().NotContain(
            attribute => attribute.Contains("ObsoleteAttribute", StringComparison.Ordinal),
            "because compiler-synthesized ObsoleteAttribute on required-member constructors must be filtered.");
        _ = isObsolete.Should().BeFalse("because compiler-synthesized required-member constructor obsolescence is internal compiler metadata.");
        _ = obsoleteMessage.Should().BeEmpty("because filtered compiler obsolete metadata should return empty message.");
        _ = isRefStructObsolete.Should().BeFalse("because compiler-synthesized ref-struct obsolescence is internal compiler metadata.");
        _ = refStructObsoleteMessage.Should().BeEmpty("because filtered ref-struct obsolete metadata should return empty message.");
        _ = extensionTypeAttributes.Should().NotContain(
            attribute => attribute.Contains("ExtensionMarkerAttribute", StringComparison.Ordinal),
            "because C# 14 ExtensionMarkerAttribute metadata must not leak into documentation.");
        _ = spanParamAttributes.Should().NotContain(
            attribute => attribute.Contains("ParamCollectionAttribute", StringComparison.Ordinal),
            "because C# 13 ParamCollectionAttribute is rendered via params modifier syntax.");
        _ = nativeReturnAttributes.Should().NotContain(
            attribute => attribute.Contains("NativeIntegerAttribute", StringComparison.Ordinal),
            "because compiler NativeIntegerAttribute metadata must not leak as a user attribute.");
        _ = experimentalTypeAttributes.Should().NotContain(
            attribute => attribute.Contains("ExperimentalAttribute", StringComparison.Ordinal),
            "because ExperimentalAttribute is surfaced as a dedicated callout banner.");
    }

    [Fact]
    public void ExtractExperimentalMetadataShouldReturnDiagnosticDetailsWhenAppliedAndThrowOnNull()
    {
        // Arrange
        var extractor = new CustomAttributeExtractor(new TypeLinkResolver());
#pragma warning disable EXP001, EXP002
        var methodWithoutUrl = typeof(ExperimentalWithUrlSample).GetMethod(nameof(ExperimentalWithUrlSample.InvokeWithoutUrl))!;

        // Act
        var (typeIsExperimental, typeDiagnosticId, typeUrlFormat) = extractor.ExtractExperimentalMetadata(typeof(ExperimentalWithUrlSample));
#pragma warning restore EXP001, EXP002
        var (methodIsExperimental, methodDiagnosticId, methodUrlFormat) = extractor.ExtractExperimentalMetadata(methodWithoutUrl);
        var (nonExperimentalFlag, nonExperimentalId, nonExperimentalUrl) = extractor.ExtractExperimentalMetadata(typeof(RequiredMemberSample));

        // Assert
        _ = typeIsExperimental.Should().BeTrue("because ExperimentalWithUrlSample is annotated with [Experimental].");
        _ = typeDiagnosticId.Should().Be("EXP001", "because DiagnosticId must match the attribute constructor argument.");
        _ = typeUrlFormat.Should().Be("https://example.com/diagnostics/{0}", "because UrlFormat named property must be preserved.");

        _ = methodIsExperimental.Should().BeTrue("because InvokeWithoutUrl is annotated with [Experimental].");
        _ = methodDiagnosticId.Should().Be("EXP002", "because method DiagnosticId must match EXP002.");
        _ = methodUrlFormat.Should().BeEmpty("because no UrlFormat was specified on EXP002.");

        _ = nonExperimentalFlag.Should().BeFalse("because RequiredMemberSample is not experimental.");
        _ = nonExperimentalId.Should().BeEmpty();
        _ = nonExperimentalUrl.Should().BeEmpty();
        _ = Assert.Throws<ArgumentNullException>(() => _ = extractor.ExtractExperimentalMetadata(null!));
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    private sealed class EdgeCaseArgumentAttribute(
        string? nullableLabel,
        AttributeTargets flagTargets,
        int numericCode,
        string[] labels) : Attribute
    {
        public string? NullableLabel { get; } = nullableLabel;
        public AttributeTargets FlagTargets { get; } = flagTargets;
        public int NumericCode { get; } = numericCode;
        public string[] Labels { get; } = labels;
        public string? OptionalNote { get; set; }
    }

    [RichSample(AttributeTargets.Method, typeof(List<string>), true, "single")]
    [EdgeCaseArgument(null, (AttributeTargets)999999, 42, ["alpha", "beta"], OptionalNote = null)]
    [EdgeCaseArgument("", AttributeTargets.Class, 7, [])]
    private sealed class EdgeCaseAttributedSample
    {
        [Obsolete]
        public static void ObsoleteWithoutMessage()
        {
        }

        [Obsolete("Deprecated with diagnostic.", DiagnosticId = "OBS123", UrlFormat = "https://docs.example.com/{0}")]
        public static void ObsoleteWithDiagnosticMetadata()
        {
        }
    }

    [Fact]
    public void BuildAppliedAttributesAndObsoleteShouldFormatNullArrayNumericAndUndefinedEnumArguments()
    {
        // Arrange
        var extractor = new CustomAttributeExtractor(new TypeLinkResolver());
        var targetType = typeof(EdgeCaseAttributedSample);
        var obsoleteWithoutMessageMethod = targetType.GetMethod(nameof(EdgeCaseAttributedSample.ObsoleteWithoutMessage))!;
#pragma warning disable OBS123
        var obsoleteWithDiagnosticMethod = targetType.GetMethod(nameof(EdgeCaseAttributedSample.ObsoleteWithDiagnosticMetadata))!;
#pragma warning restore OBS123

        // Act
        var formattedAttributes = extractor.BuildAppliedAttributes(targetType);
        var (isObsolete, obsoleteMessage, _, _, _) = extractor.ExtractObsoleteMetadata(obsoleteWithoutMessageMethod);
        var (hasDiagObsolete, diagMessage, isDiagError, diagId, diagUrl) = extractor.ExtractObsoleteMetadata(obsoleteWithDiagnosticMethod);

        // Assert
        _ = formattedAttributes.Should().Contain(attribute =>
            attribute.Contains("RichSampleAttribute", StringComparison.Ordinal) &&
            attribute.Contains("typeof(List<String>)", StringComparison.Ordinal) &&
            attribute.Contains("true", StringComparison.Ordinal));
        _ = formattedAttributes.Should().Contain(attribute =>
            attribute.Contains("EdgeCaseArgumentAttribute", StringComparison.Ordinal) &&
            attribute.Contains("999999", StringComparison.Ordinal) &&
            attribute.Contains("[\"alpha\", \"beta\"]", StringComparison.Ordinal) &&
            attribute.Contains("OptionalNote = null", StringComparison.Ordinal));
        _ = formattedAttributes.Should().Contain(attribute =>
            attribute.Contains("AttributeTargets.Class", StringComparison.Ordinal) &&
            attribute.Contains("[]", StringComparison.Ordinal));
        _ = isObsolete.Should().BeTrue();
        _ = obsoleteMessage.Should().BeEmpty();
        _ = hasDiagObsolete.Should().BeTrue("because [Obsolete] with DiagnosticId must be extracted.");
        _ = diagMessage.Should().Be("Deprecated with diagnostic.");
        _ = isDiagError.Should().BeFalse();
        _ = diagId.Should().Be("OBS123");
        _ = diagUrl.Should().Be("https://docs.example.com/{0}");
    }
}

internal static class SampleExtensionAttributesContainer
{
    extension(string targetText)
    {
        public int CharacterLength => targetText.Length;
    }
}
