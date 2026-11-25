using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing;

[UnitTest]
public sealed class AssemblyTypeReflectorTests
{
    private struct SampleStruct
    {
        public int Value { get; set; }
    }

    private enum SampleEnum : byte
    {
        First = 1,
        Second = 5
    }

    private delegate void SampleDelegate(int number);

    private interface ISampleInterface
    {
        void DoWork();
    }

    private abstract class SampleAbstractClass
    {
        public abstract void AbstractMethod();
    }

    private sealed class SampleSealedClass
    {
        public int Counter { get; set; }
        public void NormalMethod() => Counter++;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Test fixture for reflection")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Test fixture for reflection")]
    private class SampleRichClass
    {
        public static int StaticField = 42;
        protected int ProtectedField = 10;

        protected SampleRichClass(string name) => ProtectedProperty = name.Length;

        public SampleRichClass() : this("default")
        {
        }

        public static int StaticProperty { get; set; }
        protected int ProtectedProperty { get; set; }

        public static void StaticMethod() { }
        protected void ProtectedMethod() => ProtectedProperty++;

        public event EventHandler? PublicEvent;
        protected event EventHandler? ProtectedEvent;

        public void TriggerEvents()
        {
            PublicEvent?.Invoke(this, EventArgs.Empty);
            ProtectedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class DerivedSampleRichClass : SampleRichClass
    {
    }

    private sealed class SamplePrimaryConstructorClass(string serviceName, int retryCount)
    {
        public string ServiceName { get; } = serviceName;
        public int RetryCount { get; } = retryCount;
    }

    private sealed class SampleGenericConstrainedClass<TEntity, TKey>(TEntity entity)
        where TEntity : class, ISampleInterface, new()
        where TKey : struct
    {
        public TEntity Entity { get; } = entity;

        public TResult Transform<TResult>(TKey key)
            where TResult : class, new() =>
            Entity.GetHashCode() == key.GetHashCode() ? new TResult() : new TResult();
    }

    [Obsolete("SampleObsoleteClass is deprecated; use SampleRichClass instead.")]
    private sealed class SampleObsoleteClass
    {
        [Obsolete("OldProperty is obsolete.", error: true)]
        public int OldProperty { get; set; }

        [Obsolete("OldMethod is obsolete.")]
        public void OldMethod() => OldProperty = 0;
    }

    private sealed class SampleAsyncStreamClass
    {
        public const string ConstantLabel = "v1";
        public const int ConstantCode = 7;
        public readonly string ReadOnlyTag = "tag";

        public int BaseSeed { get; set; } = 1;

        public TUnmanaged ProcessUnmanaged<TUnmanaged>(
            TUnmanaged value,
            ref readonly int readOnlyRef,
            int? nullableNumber = null,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string expression = "")
            where TUnmanaged : unmanaged =>
            readOnlyRef > 0 || nullableNumber.HasValue || filePath.Length > 0 || lineNumber > 0 || expression.Length > 0 || BaseSeed > 0
                ? value
                : default;

        public async Task<int> ComputeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            return BaseSeed;
        }

        public async IAsyncEnumerable<int> StreamItemsAsync(
            int limit = 10,
            string? prefix = "item",
            SampleEnum mode = SampleEnum.Second,
            bool includeEmpty = false,
            char separator = ':',
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            if (includeEmpty || limit > 0 || prefix != null || mode == SampleEnum.First || separator == ':')
            {
                yield return BaseSeed;
            }
        }

        public bool ValidateParameters(
            in int readOnlyThreshold,
            ref int mutableCounter,
            out string? failureReason,
            [System.Runtime.CompilerServices.CallerMemberName] string callerName = "",
            params string[] tags)
        {
            mutableCounter += BaseSeed + readOnlyThreshold + tags.Length;
            failureReason = string.IsNullOrEmpty(callerName) ? "unknown" : null;
            return failureReason == null;
        }
    }

    private static AssemblyTypeReflector CreateReflector()
    {
        var linkResolver = new TypeLinkResolver();
        return new AssemblyTypeReflector(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());
    }

    [Fact]
    public void ReflectTypeMetadataWithAsyncAndAsyncEnumerableMethodsShouldDetectAsyncAndCancellationFlags()
    {
        // Arrange
        var linkResolver = new TypeLinkResolver();
        var extractor = new CustomAttributeExtractor(linkResolver);
        var formatter = new CSharpSignatureFormatter();
        var registry = new MicrosoftLearnReferenceRegistry();

        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyTypeReflector(null!, extractor, formatter, registry));
        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyTypeReflector(linkResolver, null!, formatter, registry));
        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyTypeReflector(linkResolver, extractor, null!, registry));
        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyTypeReflector(linkResolver, extractor, formatter, null!));
        var reflector = CreateReflector();

        // Act
        var model = reflector.ReflectTypeMetadata(typeof(SampleAsyncStreamClass));

        // Assert
        var computeMethod = model.Methods.Should().ContainSingle(method => method.MemberName == "ComputeAsync").Subject;
        _ = computeMethod.IsAsyncMethod.Should().BeTrue();
        _ = computeMethod.IsAsyncEnumerable.Should().BeFalse();
        _ = computeMethod.MemberSignature.Should().Contain("public async Task<Int32> ComputeAsync");
        _ = computeMethod.MemberParameters[0].IsCancellationToken.Should().BeTrue();
        _ = computeMethod.MemberParameters[0].IsEnumeratorCancellation.Should().BeFalse();
        _ = computeMethod.MemberParameters[0].IsOptional.Should().BeTrue();
        _ = computeMethod.MemberParameters[0].DefaultValue.Should().Be("default");

        var streamMethod = model.Methods.Should().ContainSingle(method => method.MemberName == "StreamItemsAsync").Subject;
        _ = streamMethod.IsAsyncMethod.Should().BeTrue();
        _ = streamMethod.IsAsyncEnumerable.Should().BeTrue();
        _ = streamMethod.MemberSignature.Should().Contain("Int32 limit = 10")
            .And.Contain("String? prefix = \"item\"")
            .And.Contain("SampleEnum mode = SampleEnum.Second")
            .And.Contain("Boolean includeEmpty = false")
            .And.Contain("Char separator = ':'")
            .And.Contain("[EnumeratorCancellation] CancellationToken cancellationToken = default");
        _ = streamMethod.MemberParameters[0].IsOptional.Should().BeTrue();
        _ = streamMethod.MemberParameters[0].DefaultValue.Should().Be("10");
        _ = streamMethod.MemberParameters[1].DefaultValue.Should().Be("\"item\"");
        _ = streamMethod.MemberParameters[1].IsNullable.Should().BeTrue();
        _ = streamMethod.MemberParameters[2].DefaultValue.Should().Be("SampleEnum.Second");
        _ = streamMethod.MemberParameters[3].DefaultValue.Should().Be("false");
        _ = streamMethod.MemberParameters[4].DefaultValue.Should().Be("':'");
        _ = streamMethod.MemberParameters[5].IsCancellationToken.Should().BeTrue();
        _ = streamMethod.MemberParameters[5].IsEnumeratorCancellation.Should().BeTrue();

        var validateMethod = model.Methods.Should().ContainSingle(method => method.MemberName == "ValidateParameters").Subject;
        _ = validateMethod.MemberSignature.Should().Contain("in Int32 readOnlyThreshold")
            .And.Contain("ref Int32 mutableCounter")
            .And.Contain("out String? failureReason")
            .And.Contain("[CallerMemberName] String callerName = \"\"")
            .And.Contain("params String[] tags");
        _ = validateMethod.MemberParameters[0].ParameterModifier.Should().Be("in");
        _ = validateMethod.MemberParameters[1].ParameterModifier.Should().Be("ref");
        _ = validateMethod.MemberParameters[2].ParameterModifier.Should().Be("out");
        _ = validateMethod.MemberParameters[2].IsNullable.Should().BeTrue();
        _ = validateMethod.MemberParameters[3].CallerInformationAttribute.Should().Be("CallerMemberName");
        _ = validateMethod.MemberParameters[3].IsOptional.Should().BeTrue();
        _ = validateMethod.MemberParameters[3].DefaultValue.Should().Be("\"\"");
        _ = validateMethod.MemberParameters[4].IsParams.Should().BeTrue();
        _ = validateMethod.MemberParameters[4].ParameterModifier.Should().Be("params");
        _ = validateMethod.MemberParameters[4].ParameterTypeName.Should().Be("String[]");

        var unmanagedMethod = model.Methods.Should().ContainSingle(method => method.MemberName == "ProcessUnmanaged").Subject;
        _ = unmanagedMethod.GenericTypeParameters[0].ParameterConstraints.Should().Contain("[unmanaged](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/unmanaged-types)");
        _ = unmanagedMethod.MemberParameters[1].ParameterModifier.Should().Be("ref readonly");
        _ = unmanagedMethod.MemberParameters[2].IsNullable.Should().BeTrue();
        _ = unmanagedMethod.MemberParameters[2].DefaultValue.Should().Be("null");
        _ = unmanagedMethod.MemberParameters[3].CallerInformationAttribute.Should().Be("CallerFilePath");
        _ = unmanagedMethod.MemberParameters[4].CallerInformationAttribute.Should().Be("CallerLineNumber");
        _ = unmanagedMethod.MemberParameters[5].CallerInformationAttribute.Should().Be("CallerArgumentExpression(\"value\")");

        _ = model.Fields.Should().Contain(field => field.MemberSignature.Contains("const String ConstantLabel = \"v1\"", StringComparison.Ordinal));
        _ = model.Fields.Should().Contain(field => field.MemberSignature.Contains("const Int32 ConstantCode = 7", StringComparison.Ordinal));
        _ = model.Fields.Should().Contain(field => field.MemberSignature.Contains("readonly String ReadOnlyTag", StringComparison.Ordinal));
    }

    [Fact]
    public void ReflectTypeMetadataWithObsoleteTypeAndMembersShouldExtractObsoleteMessagesAndFlags()
    {
        // Arrange
        var reflector = CreateReflector();

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var model = reflector.ReflectTypeMetadata(typeof(SampleObsoleteClass));
#pragma warning restore CS0618

        // Assert
        _ = model.IsObsolete.Should().BeTrue();
        _ = model.ObsoleteMessage.Should().Be("SampleObsoleteClass is deprecated; use SampleRichClass instead.");
        _ = model.IsObsoleteError.Should().BeFalse();

        _ = model.Properties[0].IsObsolete.Should().BeTrue();
        _ = model.Properties[0].ObsoleteMessage.Should().Be("OldProperty is obsolete.");
        _ = model.Properties[0].IsObsoleteError.Should().BeTrue();

        _ = model.Methods[0].IsObsolete.Should().BeTrue();
        _ = model.Methods[0].ObsoleteMessage.Should().Be("OldMethod is obsolete.");
    }

    [Fact]
    public void ReflectTypeMetadataWithPrimaryConstructorShouldIncludeParametersInDeclarationSignature()
    {
        // Arrange
        var reflector = CreateReflector();

        // Act
        var model = reflector.ReflectTypeMetadata(typeof(SamplePrimaryConstructorClass));

        // Assert
        _ = model.CSharpDeclarationSignature.Should().Be("sealed class SamplePrimaryConstructorClass(String serviceName, Int32 retryCount)");
        _ = model.Constructors.Should().ContainSingle();
        _ = model.Constructors[0].IsPrimaryConstructor.Should().BeTrue("because the single parameterized constructor is a primary constructor.");
        _ = model.Constructors[0].MemberParameters.Should().HaveCount(2);
    }

    [Fact]
    public void ReflectTypeMetadataWithGenericClassAndMethodShouldExtractTypeParametersAndConstraints()
    {
        // Arrange
        var reflector = CreateReflector();

        // Act
        var model = reflector.ReflectTypeMetadata(typeof(SampleGenericConstrainedClass<,>));

        // Assert
        _ = model.TypeName.Should().Be("SampleGenericConstrainedClass");
        _ = model.IsGenericType.Should().BeTrue();
        _ = model.GenericTypeParameters.Should().BeEquivalentTo(["TEntity", "TKey"]);
        _ = model.GenericTypeParameterDetails.Should().HaveCount(2);
        _ = model.GenericTypeParameterDetails[0].ParameterName.Should().Be("TEntity");
        _ = model.GenericTypeParameterDetails[0].ParameterConstraints.Should().Contain("[class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class)")
            .And.Contain("ISampleInterface")
            .And.Contain("[new()](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/new-constraint)");
        _ = model.GenericTypeParameterDetails[1].ParameterName.Should().Be("TKey");
        _ = model.GenericTypeParameterDetails[1].ParameterConstraints.Should().Be("[struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct)");

        var transformMethod = model.Methods.Should().ContainSingle(method => method.MemberName == "Transform").Subject;
        _ = transformMethod.MemberSignature.Should().Contain("Transform<TResult>");
        _ = transformMethod.GenericTypeParameters.Should().ContainSingle();
        _ = transformMethod.GenericTypeParameters[0].ParameterName.Should().Be("TResult");
        _ = transformMethod.GenericTypeParameters[0].ParameterConstraints.Should().Be("[class](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/class), [new()](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/new-constraint)");
    }

    public sealed class OuterClass
    {
        public sealed class NestedPublicClass
        {
            public string Name { get; set; } = string.Empty;
        }
    }

    [Fact]
    public void ReflectTypeMetadataShouldPopulateDeclaringTypeNameForNestedTypes()
    {
        // Arrange
        var reflector = CreateReflector();

        // Act
        var topLevelModel = reflector.ReflectTypeMetadata(typeof(ExportResult));
        var nestedModel = reflector.ReflectTypeMetadata(typeof(OuterClass.NestedPublicClass));

        // Assert
        _ = topLevelModel.DeclaringTypeName.Should().BeNull("because top-level classes have no declaring type.");
        _ = nestedModel.DeclaringTypeName.Should().Be(nameof(OuterClass), "because nested classes report declaring type name.");
    }

    [Fact]
    public void ReflectTypeMetadataShouldIdentifyCategoriesCorrectly()
    {
        // Arrange
        var reflector = CreateReflector();

        // Act
        var structModel = reflector.ReflectTypeMetadata(typeof(SampleStruct));
        var enumModel = reflector.ReflectTypeMetadata(typeof(SampleEnum));
        var delegateModel = reflector.ReflectTypeMetadata(typeof(SampleDelegate));
        var interfaceModel = reflector.ReflectTypeMetadata(typeof(ISampleInterface));
        var abstractModel = reflector.ReflectTypeMetadata(typeof(SampleAbstractClass));
        var sealedModel = reflector.ReflectTypeMetadata(typeof(SampleSealedClass));

        // Assert
        _ = structModel.TypeCategory.Should().Be(TypeCategory.Struct);
        _ = enumModel.TypeCategory.Should().Be(TypeCategory.Enum);
        _ = enumModel.CSharpDeclarationSignature.Should().Be("enum SampleEnum : Byte");
        _ = enumModel.EnumUnderlyingTypeLink.Should().Contain("Byte");
        _ = enumModel.InheritanceHierarchy.Should().BeEmpty();
        _ = enumModel.ImplementedInterfaces.Should().BeEmpty();
        _ = enumModel.Fields.Should().HaveCount(2);
        _ = enumModel.Fields[0].MemberSignature.Should().Be("First = 1");
        _ = enumModel.Fields[0].ReturnTypeName.Should().Be("Byte");
        _ = enumModel.Fields[0].ReturnValueDescription.Should().Be("1");
        _ = enumModel.Fields[1].MemberSignature.Should().Be("Second = 5");
        _ = enumModel.Fields[1].ReturnValueDescription.Should().Be("5");
        _ = delegateModel.TypeCategory.Should().Be(TypeCategory.Delegate);
        _ = interfaceModel.TypeCategory.Should().Be(TypeCategory.Interface);
        _ = abstractModel.TypeCategory.Should().Be(TypeCategory.Class);
        _ = abstractModel.CSharpDeclarationSignature.Should().Contain("abstract");
        _ = sealedModel.CSharpDeclarationSignature.Should().Contain("sealed");
    }

    [Fact]
    public void ReflectTypeMetadataShouldCaptureEventsProtectedAndStaticMembers()
    {
        // Arrange
        var reflector = CreateReflector();

        // Act
        var richModel = reflector.ReflectTypeMetadata(typeof(SampleRichClass));

        // Assert
        _ = richModel.Events.Should().HaveCount(2);
        _ = richModel.Properties.Should().HaveCount(2);
        _ = richModel.Constructors.Should().HaveCount(2);
        _ = richModel.Methods.Should().Contain(method => method.IsStaticMember);
        _ = richModel.Fields.Should().Contain(field => field.IsStaticMember);
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class MemberContractAttribute(string tag) : Attribute
    {
        public string Tag { get; } = tag;
    }

    private enum AttributedSampleEnum
    {
        [Display(Name = "Initial State")]
        Initial = 0
    }

    private sealed class AttributedMembersContainer<[MemberContract("type-param")] TItem>
    {
        [return: MemberContract("return-contract")]
        public static TItem Execute([Required] TItem inputValue) => inputValue;
    }

    [Fact]
    public void ReflectTypeMetadataShouldPopulateCustomAttributesAcrossEnumValuesParametersGenericTypeParamsAndReturnValues()
    {
        // Arrange
        var linkResolver = new TypeLinkResolver();
        linkResolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        AssemblyTypeReflector reflector = new(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());

        // Act
        var enumModel = reflector.ReflectTypeMetadata(typeof(AttributedSampleEnum));
        var containerModel = reflector.ReflectTypeMetadata(typeof(AttributedMembersContainer<>));

        // Assert
        _ = enumModel.Fields.Should().ContainSingle(field => field.MemberName == nameof(AttributedSampleEnum.Initial))
            .Which.AppliedAttributes.Should().ContainSingle(
                attribute => attribute.Contains("DisplayAttribute", StringComparison.Ordinal) &&
                             attribute.Contains("Name = \"Initial State\"", StringComparison.Ordinal),
                "because enum values must extract applied custom attributes with formatted named arguments.");

        _ = containerModel.GenericTypeParameterDetails.Should().ContainSingle()
            .Which.AppliedAttributes.Should().ContainSingle(
                attribute => attribute.Contains("MemberContractAttribute", StringComparison.Ordinal) &&
                             attribute.Contains("\"type-param\"", StringComparison.Ordinal),
                "because generic type parameters must extract applied custom attributes.");

        var executeMethod = containerModel.Methods.Should().ContainSingle(method => method.MemberName == nameof(AttributedMembersContainer<>.Execute)).Subject;
        _ = executeMethod.ReturnAttributes.Should().ContainSingle(
            attribute => attribute.Contains("MemberContractAttribute", StringComparison.Ordinal) &&
                         attribute.Contains("\"return-contract\"", StringComparison.Ordinal),
            "because return-value custom attributes must be extracted onto MemberDocumentationModel.ReturnAttributes.");
        _ = executeMethod.MemberParameters.Should().ContainSingle()
            .Which.AppliedAttributes.Should().ContainSingle(
                attribute => attribute.Contains("RequiredAttribute", StringComparison.Ordinal),
                "because parameter custom attributes must be extracted onto ParameterDocumentationModel.AppliedAttributes.");
    }

    private sealed record UnsealedRecordSample(string Code)
    {
        public string? ExtraNote { get; init; }
    }

    private readonly struct ReadOnlyValueSample(int amount)
    {
        public int Amount { get; } = amount;
    }

    private ref struct StackOnlySpanSample
    {
        public int SliceLength { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.Experimental("REF001", UrlFormat = "https://example.com/{0}")]
    private sealed class ModernTypeShowcaseSample(string primaryId)
    {
        public string PrimaryId => primaryId;

        public string ContextualFieldBacked
        {
            get => field ?? PrimaryId;
            set;
        }

        public string this[int index] => $"{PrimaryId}:{index}";

        [System.Diagnostics.CodeAnalysis.Experimental("REF002")]
        public static void InspectNullableOut(scoped ref readonly StackOnlySpanSample spanSample, out string? failureReason) =>
            failureReason = spanSample.SliceLength > 0 ? null : "Empty";

        public static explicit operator int(ModernTypeShowcaseSample instance) => instance.PrimaryId.Length;
    }

    private interface IGenericVarianceContract<in TInput, out TOutput>
        where TInput : notnull
        where TOutput : allows ref struct
    {
        TOutput Transform(TInput inputValue);
    }

    private class ProtectedOuterHost
    {
        protected internal class InnerProtectedHost<TBuffer>
            where TBuffer : allows ref struct
        {
            protected internal int Process(TBuffer buffer)
            {
                _ = buffer;
                return GetHashCode();
            }
        }
    }

    private sealed class DerivedProtectedOuterHost : ProtectedOuterHost;

    private sealed class DerivedInnerProtectedHost<TBuffer> : ProtectedOuterHost.InnerProtectedHost<TBuffer>
        where TBuffer : allows ref struct;

    internal sealed class SampleExtensionBlockTarget
    {
        public string Label { get; set; } = string.Empty;
    }

    [Fact]
    public void ReflectTypeMetadataShouldCaptureModernCSharpConstructsRecordsConstraintsExperimentalAndNrtLinks()
    {
        // Arrange
        var linkResolver = new TypeLinkResolver();
        linkResolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        AssemblyTypeReflector reflector = new(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());

        // Act
        var unsealedRecordModel = reflector.ReflectTypeMetadata(typeof(UnsealedRecordSample));
        var readOnlyStructModel = reflector.ReflectTypeMetadata(typeof(ReadOnlyValueSample));
        var refStructModel = reflector.ReflectTypeMetadata(typeof(StackOnlySpanSample));
#pragma warning disable REF001, REF002
        var showcaseModel = reflector.ReflectTypeMetadata(typeof(ModernTypeShowcaseSample));
        var varianceInterfaceModel = reflector.ReflectTypeMetadata(typeof(IGenericVarianceContract<,>));
        var protectedNestedModel = reflector.ReflectTypeMetadata(typeof(ProtectedOuterHost.InnerProtectedHost<>));

        // Assert - Record, ReadOnly, RefStruct & EqualityContract filtering
        _ = unsealedRecordModel.IsRecordType.Should().BeTrue();
        _ = unsealedRecordModel.Properties.Should().NotContain(
            property => property.MemberName == "EqualityContract",
            "because compiler-generated record EqualityContract must be filtered.");
        _ = unsealedRecordModel.Properties.Should().Contain(property => property.MemberName == nameof(UnsealedRecordSample.Code));
        _ = unsealedRecordModel.Properties.Should().Contain(property => property.MemberName == nameof(UnsealedRecordSample.ExtraNote));
        _ = readOnlyStructModel.IsReadOnlyType.Should().BeTrue();
        _ = refStructModel.IsRefStructType.Should().BeTrue();

        // Assert - Experimental, field keyword, indexer, conversion operator XML key, scoped & NRT out string? link
        _ = showcaseModel.IsExperimental.Should().BeTrue();
        _ = showcaseModel.ExperimentalDiagnosticId.Should().Be("REF001");
        _ = showcaseModel.ExperimentalUrlFormat.Should().Be("https://example.com/{0}");
        _ = showcaseModel.Properties.Should().Contain(property => property.MemberName == nameof(ModernTypeShowcaseSample.ContextualFieldBacked));
        _ = showcaseModel.Properties.Should().Contain(property =>
            property.MemberName == "this" &&
            property.XmlMemberKey.EndsWith(".Item(System.Int32)", StringComparison.Ordinal));
        _ = showcaseModel.Methods.Should().ContainSingle(operatorMember =>
            operatorMember.MemberName == "operator explicit" &&
            operatorMember.XmlMemberKey.Contains("~System.Int32", StringComparison.Ordinal));

        var scopedOutMethod = showcaseModel.Methods.Should().ContainSingle(
            method => method.MemberName == nameof(ModernTypeShowcaseSample.InspectNullableOut)).Subject;
#pragma warning restore REF001, REF002
        _ = scopedOutMethod.IsExperimental.Should().BeTrue();
        _ = scopedOutMethod.ExperimentalDiagnosticId.Should().Be("REF002");
        _ = scopedOutMethod.MemberParameters.Should().HaveCount(2);
        _ = scopedOutMethod.MemberParameters[0].ParameterModifier.Should().Contain("scoped");
        _ = scopedOutMethod.MemberParameters[1].ResolvedTypeDocumentationLink.Should().EndWith("?",
            "because out string? parameter type link must preserve the nullable annotation suffix.");

        // Assert - Generic variance, notnull, allows ref struct, and protected internal
        _ = varianceInterfaceModel.GenericTypeParameterDetails.Should().HaveCount(2);
        _ = varianceInterfaceModel.GenericTypeParameterDetails[0].ParameterModifier.Should().Be("in");
        _ = varianceInterfaceModel.GenericTypeParameterDetails[0].ParameterConstraints.Should().Contain("notnull");
        _ = varianceInterfaceModel.GenericTypeParameterDetails[1].ParameterModifier.Should().Be("out");
        _ = varianceInterfaceModel.GenericTypeParameterDetails[1].ParameterConstraints.Should().Contain("allows ref struct");
        _ = protectedNestedModel.AccessModifier.Should().Be("protected internal");
        _ = protectedNestedModel.GenericTypeParameterDetails.Should().ContainSingle()
            .Which.ParameterConstraints.Should().Contain("allows ref struct");
        _ = protectedNestedModel.Methods.Should().ContainSingle()
            .Which.VisibilityModifier.Should().Be("protected internal");
    }

    [Fact]
    public void ReflectTypeMetadataShouldSurfaceCSharp14ExtensionBlocksAndFilterDirectAccessors()
    {
        // Arrange
        var linkResolver = new TypeLinkResolver();
        linkResolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        AssemblyTypeReflector reflector = new(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());

        // Act
        var extensionContainerModel = reflector.ReflectTypeMetadata(typeof(SampleModernExtensionBlockExtensions));

        // Assert
        var expectedTargetFullName = typeof(SampleExtensionBlockTarget).FullName!.Replace('+', '.');
        _ = extensionContainerModel.Methods.Should().Contain(
            method => method.MemberName == nameof(SampleModernExtensionBlockExtensions.FormatWithPrefix) &&
                      method.ExtensionTargetTypeFullName == expectedTargetFullName,
            "because C# 14 instance extension methods must surface with target metadata on the extension container.");
        _ = extensionContainerModel.Methods.Should().Contain(
            method => method.MemberName == nameof(SampleModernExtensionBlockExtensions.CreateNamed) &&
                      method.ExtensionTargetTypeFullName == expectedTargetFullName &&
                      method.IsStaticMember,
            "because C# 14 static extension methods must target SampleExtensionBlockTarget.");
        _ = extensionContainerModel.Properties.Should().Contain(
            property => property.MemberName == "HasRichState" &&
                        property.IsExtensionProperty &&
                        !property.IsStaticMember &&
                        property.ExtensionTargetTypeFullName == expectedTargetFullName,
            "because C# 14 instance extension properties must surface with target metadata on the extension container.");
        _ = extensionContainerModel.Properties.Should().Contain(
            property => property.MemberName == "DefaultCategoryTag" &&
                        property.IsExtensionProperty &&
                        property.IsStaticMember &&
                        property.ExtensionTargetTypeFullName == expectedTargetFullName,
            "because C# 14 static extension properties must surface as static extension properties.");
        _ = extensionContainerModel.Methods.Should().NotContain(
            method => method.MemberName.StartsWith("get_", StringComparison.Ordinal) ||
                      method.MemberName.StartsWith("set_", StringComparison.Ordinal),
            "because C# 14 extension property accessor methods must not leak as regular methods.");
    }

    private sealed class DeepNullabilityAndArrayHost<TValue>
    {
        public static Dictionary<string, IReadOnlyList<int?[]?>>? InspectComplexNullability(
            in TValue? inputValue,
            int[,]?[] nullableMatrixArray,
            out IReadOnlyList<string?> outputList)
        {
            _ = inputValue;
            _ = nullableMatrixArray;
            outputList = [];
            return null;
        }
    }

    [Fact]
    public void ReflectTypeMetadataShouldHandleComplexNullabilityPointersArraysAndSettableExtensionProperties()
    {
        // Arrange
        var linkResolver = new TypeLinkResolver();
        linkResolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        AssemblyTypeReflector reflector = new(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());

        // Act
        var complexModel = reflector.ReflectTypeMetadata(typeof(DeepNullabilityAndArrayHost<>));
        var bufferModel = reflector.ReflectTypeMetadata(typeof(Buffer));
#pragma warning disable CS0618, AEXT01
        var mutableExtensionModel = reflector.ReflectTypeMetadata(typeof(SampleModernExtensionBlockExtensions));
#pragma warning restore CS0618, AEXT01

        // Assert - Nested arrays, nullable generic type params, nullable collections
        var inspectMethod = complexModel.Methods.Should().ContainSingle(
            method => method.MemberName == nameof(DeepNullabilityAndArrayHost<>.InspectComplexNullability)).Subject;
        _ = inspectMethod.ResolvedReturnTypeDocumentationLink.Should().Contain("?")
            .And.Contain("Dictionary");
        _ = inspectMethod.MemberParameters.Should().HaveCount(3);
        _ = inspectMethod.MemberParameters[0].ResolvedTypeDocumentationLink.Should().Contain("TValue");
        _ = inspectMethod.MemberParameters[1].ResolvedTypeDocumentationLink.Should().Contain("[,]?[]");
        _ = inspectMethod.MemberParameters[2].ResolvedTypeDocumentationLink.Should().Contain("[String](https://learn.microsoft.com/en-us/dotnet/api/system.string)?");

        // Assert - Pointer parameters & return handling
        _ = bufferModel.Methods.Should().Contain(method =>
            method.MemberName == nameof(Buffer.MemoryCopy) &&
            method.MemberParameters.Any(parameter => parameter.ResolvedTypeDocumentationLink.Contains('*', StringComparison.Ordinal)));

        // Assert - Settable & attributed C# 14 extension property
        var mutableProp = mutableExtensionModel.Properties.Should().ContainSingle(
            property => property.MemberName == "MutableLabel").Subject;
        _ = mutableProp.IsExtensionProperty.Should().BeTrue();
        _ = mutableProp.IsObsolete.Should().BeTrue();
        _ = mutableProp.IsExperimental.Should().BeTrue();
        _ = mutableProp.ExperimentalDiagnosticId.Should().Be("AEXT01");
        _ = mutableProp.MemberSignature.Should().Contain("get;").And.Contain("set;");
        _ = mutableProp.AppliedAttributes.Should().Contain(attribute => attribute.Contains("DescriptionAttribute", StringComparison.Ordinal));
    }

    private interface IExplicitAuditSink<TEntry>
    {
        string SinkName { get; }
        TEntry this[int index] { get; }
        event EventHandler? EntryWritten;
        void WriteEntry(TEntry entry);
    }

    [System.Runtime.CompilerServices.InlineArray(8)]
    private struct InlineEightByteSlot
    {
        private byte firstSlot;
    }

    private unsafe struct UnsafeFixedAndFnPtrSample
    {
        public fixed int Samples[4];

        public static void InvokeCallback(delegate* unmanaged[Cdecl]<int, void> callback) => _ = callback;
    }

    private sealed class FullDotNetConstructsSample : IExplicitAuditSink<string>
    {
        ~FullDotNetConstructsSample() => _ = SummaryTuple;

        public (string Id, int Total) SummaryTuple { get; set; }

        string IExplicitAuditSink<string>.SinkName => "ExplicitSink";

        string IExplicitAuditSink<string>.this[int index] => $"item-{index}";

        event EventHandler? IExplicitAuditSink<string>.EntryWritten
        {
            add { }
            remove { }
        }

        void IExplicitAuditSink<string>.WriteEntry(string entry) => _ = entry;
    }

    [Fact]
    public void ReflectTypeMetadataShouldSupportInlineArraysFixedBuffersFunctionPointersTuplesFinalizersAndExplicitInterfaceMembers()
    {
        // Arrange
        var linkResolver = new TypeLinkResolver();
        linkResolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        AssemblyTypeReflector reflector = new(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());

        // Act
        var inlineArrayModel = reflector.ReflectTypeMetadata(typeof(InlineEightByteSlot));
        var unsafeModel = reflector.ReflectTypeMetadata(typeof(UnsafeFixedAndFnPtrSample));
        var fullConstructsModel = reflector.ReflectTypeMetadata(typeof(FullDotNetConstructsSample));

        // Assert - InlineArray(8) single private element field surfaced with length
        var inlineField = inlineArrayModel.Fields.Should().ContainSingle().Subject;
        _ = inlineField.ReturnTypeName.Should().Be("Byte[8]");
        _ = inlineField.ResolvedReturnTypeDocumentationLink.Should().Contain("Byte").And.EndWith("[8]");

        // Assert - FixedBuffer & Function pointer
        var fixedField = unsafeModel.Fields.Should().ContainSingle(field => field.MemberName == nameof(UnsafeFixedAndFnPtrSample.Samples)).Subject;
        _ = fixedField.ReturnTypeName.Should().Be("Int32[4]");
        _ = fixedField.MemberSignature.Should().Be("public fixed Int32 Samples[4]");

        var fnPtrMethod = unsafeModel.Methods.Should().ContainSingle(method => method.MemberName == nameof(UnsafeFixedAndFnPtrSample.InvokeCallback)).Subject;
        _ = fnPtrMethod.MemberParameters[0].ParameterTypeName.Should().Be("delegate* unmanaged[Cdecl]<Int32, Void>");
        _ = fnPtrMethod.XmlMemberKey.Should().Contain("=FUNC:System.Void(System.Int32)");

        // Assert - Named ValueTuple property, Finalizer, and Explicit Interface property/indexer/event/method
        var tupleProp = fullConstructsModel.Properties.Should().ContainSingle(property => property.MemberName == nameof(FullDotNetConstructsSample.SummaryTuple)).Subject;
        _ = tupleProp.ReturnTypeName.Should().Be("(String Id, Int32 Total)");
        _ = tupleProp.ResolvedReturnTypeDocumentationLink.Should().Contain("Id").And.Contain("Total");

        var explicitProp = fullConstructsModel.Properties.Should().ContainSingle(property => property.MemberName == "IExplicitAuditSink<String>.SinkName").Subject;
        _ = explicitProp.VisibilityModifier.Should().Be("explicit interface");
        _ = explicitProp.MemberSignature.Should().Be("String IExplicitAuditSink<String>.SinkName { get; }");

        var explicitIndexer = fullConstructsModel.Properties.Should().ContainSingle(property => property.MemberName == "IExplicitAuditSink<String>.this").Subject;
        _ = explicitIndexer.MemberSignature.Should().Be("String IExplicitAuditSink<String>.this[Int32 index] { get; }");

        var explicitEvent = fullConstructsModel.Events.Should().ContainSingle(evt => evt.MemberName == "IExplicitAuditSink<String>.EntryWritten").Subject;
        _ = explicitEvent.VisibilityModifier.Should().Be("explicit interface");
        _ = explicitEvent.MemberSignature.Should().Be("event EventHandler? IExplicitAuditSink<String>.EntryWritten");

        var explicitMethod = fullConstructsModel.Methods.Should().ContainSingle(method => method.MemberName == "IExplicitAuditSink<String>.WriteEntry").Subject;
        _ = explicitMethod.VisibilityModifier.Should().Be("explicit interface");
        _ = explicitMethod.MemberSignature.Should().Be("Void IExplicitAuditSink<String>.WriteEntry(String entry)");

        var finalizer = fullConstructsModel.Methods.Should().ContainSingle(method => method.MemberName == "~FullDotNetConstructsSample").Subject;
        _ = finalizer.MemberSignature.Should().Be("~FullDotNetConstructsSample()");
        _ = finalizer.XmlMemberKey.Should().EndWith(".Finalize");
    }
}

internal static class SampleModernExtensionBlockExtensions
{
    extension(AssemblyTypeReflectorTests.SampleExtensionBlockTarget target)
    {
        public bool HasRichState => target != null;

        [System.ComponentModel.Description("Gets or updates label")]
        public string MutableLabel
        {
            [System.ComponentModel.Description("Gets current label")]
            [Obsolete("Deprecated ext getter")]
            [System.Diagnostics.CodeAnalysis.Experimental("AEXT01")]
            get => target.Label;
            set => target.Label = value;
        }

        public string FormatWithPrefix(string prefix) => $"{prefix}:{target.Label}";
    }

    extension(AssemblyTypeReflectorTests.SampleExtensionBlockTarget)
    {
        public static string DefaultCategoryTag => "Rich";

        public static string CreateNamed(string label) => label;
    }
}
