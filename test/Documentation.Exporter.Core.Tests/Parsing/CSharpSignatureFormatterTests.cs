using System.Reflection;
using System.Runtime.CompilerServices;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Parsing;

[UnitTest]
public sealed class CSharpSignatureFormatterTests
{
    private enum SampleByteEnum : byte
    {
        None = 0
    }

    private sealed class PrimaryCtorSample(string endpointUrl, int retryLimit)
    {
        public string EndpointUrl { get; } = endpointUrl;
        public int RetryLimit { get; } = retryLimit;

        public async Task<string> FetchAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return $"{EndpointUrl}:{RetryLimit}";
        }

        public async IAsyncEnumerable<string> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return EndpointUrl;
        }
    }

    [Fact]
    public void PublicMethodsWhenNullShouldThrowArgumentNullException()
    {
        // Arrange
        var formatter = new CSharpSignatureFormatter();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => formatter.GetCleanTypeName(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.FormatDisplayTypeName((Type)null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.FormatDisplayTypeName((NullabilityInfo)null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.FormatFullDisplayTypeName(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.BuildTypeDeclarationSignature(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.DetermineConstructorKind(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.IsAwaitableReturnType(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.IsAsyncEnumerableType(null!));
        _ = Assert.Throws<ArgumentNullException>(() => formatter.HasAsyncStateMachineAttribute(null!));
    }

    [Fact]
    public void FormatMethodsShouldProduceExpectedCSharpSignaturesAndKinds()
    {
        // Arrange
        var formatter = new CSharpSignatureFormatter();
        var fetchMethodInfo = typeof(PrimaryCtorSample).GetMethod(nameof(PrimaryCtorSample.FetchAsync))!;
        var streamMethodInfo = typeof(PrimaryCtorSample).GetMethod(nameof(PrimaryCtorSample.StreamAsync))!;

        // Act & Assert
        _ = formatter.GetCleanTypeName(typeof(List<string>)).Should().Be("List");
        _ = formatter.FormatDisplayTypeName(typeof(Dictionary<string, int[]>)).Should().Be("Dictionary<String, Int32[]>");
        _ = formatter.FormatFullDisplayTypeName(typeof(Dictionary<string, int>)).Should().Be("System.Collections.Generic.Dictionary<String, Int32>");
        _ = formatter.BuildTypeDeclarationSignature(typeof(SampleByteEnum)).Should().Be("enum SampleByteEnum : Byte");
        _ = formatter.BuildTypeDeclarationSignature(typeof(PrimaryCtorSample)).Should().Be("sealed class PrimaryCtorSample(String endpointUrl, Int32 retryLimit)");
        _ = formatter.DetermineConstructorKind(typeof(SampleByteEnum)).Should().Be("None");
        _ = formatter.DetermineConstructorKind(typeof(PrimaryCtorSample)).Should().Be("Primary (String endpointUrl, Int32 retryLimit)");
        _ = formatter.IsAwaitableReturnType(fetchMethodInfo.ReturnType).Should().BeTrue();
        _ = formatter.IsAsyncEnumerableType(streamMethodInfo.ReturnType).Should().BeTrue();
        _ = formatter.HasAsyncStateMachineAttribute(fetchMethodInfo).Should().BeTrue();
        _ = formatter.BuildMethodSignature(streamMethodInfo, isAsyncMethod: true)
            .Should().Contain("public async IAsyncEnumerable<String> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)");
    }

    private sealed class OverloadedPrimarySample(string primaryKey, int maxRetries)
    {
        public string PrimaryKey { get; } = primaryKey;

        public OverloadedPrimarySample(string primaryKey)
            : this(primaryKey, 3)
        {
        }

        public int ComputeEffectiveRetries() => maxRetries + PrimaryKey.Length;
    }

    private sealed record PrimaryRecordSample(string Code, int Quantity)
    {
        public string Formatted => $"{Code}:{Quantity}";
    }

    private readonly record struct ReadOnlyRecordStructSample(string CurrencyPair, decimal Rate);

    private readonly struct ReadOnlyStructSample(int scale)
    {
        public int Scale { get; } = scale;

        public int DoubleScale() => Scale * 2;
    }

    private struct MutableStructSample(int scale)
    {
        public int Scale { get; set; } = scale;

        public readonly int DoubleScale() => Scale * 2;
    }

    private ref struct RefStructSample
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Testing ref field formatting")]
        public ref readonly int ReferencedValue;

        public RefStructSample(ref readonly int referencedValue) => ReferencedValue = ref referencedValue;
    }

    private interface IModernOperatorContract<TSelf>
        where TSelf : IModernOperatorContract<TSelf>
    {
        static abstract string CategoryName { get; }
        static virtual int DefaultWeight => 1;
        static abstract TSelf operator +(TSelf left, TSelf right);
        static abstract TSelf operator checked +(TSelf left, TSelf right);
        static abstract TSelf operator >>>(TSelf value, int shiftAmount);
    }

    private class VirtualMethodBase
    {
        public virtual string Describe() => "base";
    }

    private class SealedOverrideDerived : VirtualMethodBase, IModernOperatorContract<SealedOverrideDerived>
    {
        public static string CategoryName => "Sealed";

        public string Description { get; private set; } = string.Empty;
        public string InternalTag { get; internal set; } = string.Empty;
        public string ProtectedInternalTag { get; protected internal set; } = string.Empty;
        public string PrivateProtectedTag { get; private protected set; } = string.Empty;
        public required string? RequiredTitle { get; init; }
        public string WriteOnlySecret { set => Description = value; }

        public string? this[int rowIndex, string columnName]
        {
            get => $"{rowIndex}:{columnName}:{Description}";
            private set => Description = value ?? string.Empty;
        }

        public sealed override string Describe() => "derived";

        public static string CombineScoped(scoped in RefStructSample sample) => sample.ReferencedValue.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public static int[,] CreateMatrix(out string? failureReason)
        {
            failureReason = null;
            return new int[1, 1];
        }

        public static Task<string?> FetchNullableAsync(int? optionalNumber) =>
            Task.FromResult(optionalNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public static SealedOverrideDerived operator +(SealedOverrideDerived left, SealedOverrideDerived right) => left;
        public static SealedOverrideDerived operator checked +(SealedOverrideDerived left, SealedOverrideDerived right) => right;
        public static SealedOverrideDerived operator >>>(SealedOverrideDerived value, int shiftAmount) => value;
        public void operator +=(int increment) => Description += increment.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public void operator checked +=(int increment) => Description += increment.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static implicit operator int(SealedOverrideDerived source) => source.Description.Length;
        public static explicit operator string(SealedOverrideDerived source) => source.Description;
        public static explicit operator byte(SealedOverrideDerived source) => (byte)source.Description.Length;
        public static explicit operator checked byte(SealedOverrideDerived source) => checked((byte)source.Description.Length);

        public event EventHandler? Changed;

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class SubOverrideDerived : SealedOverrideDerived;

    private class GenericOuter<TOuter>
    {
        protected internal sealed class NestedGeneric<TInner>
            where TInner : allows ref struct
        {
            public static void Inspect(TOuter outerValue) => _ = outerValue;
        }
    }

    private sealed class SubGenericOuter<TOuter> : GenericOuter<TOuter>;

    private delegate TResult VarianceTransform<in TInput, out TResult>(TInput inputValue)
        where TInput : notnull;

    [Fact]
    public void FormatterShouldHandlePrimaryAndSecondaryConstructorsAndModernTypeKinds()
    {
        // Arrange
        var formatter = new CSharpSignatureFormatter();

        // Act
        var hasOverloadedPrimary = formatter.TryGetPrimaryConstructor(typeof(OverloadedPrimarySample), out var overloadedPrimary);
        var hasRecordPrimary = formatter.TryGetPrimaryConstructor(typeof(PrimaryRecordSample), out var recordPrimary);
        var hasReadOnlyStructPrimary = formatter.TryGetPrimaryConstructor(typeof(ReadOnlyStructSample), out _);
        var hasBasePrimary = formatter.TryGetPrimaryConstructor(typeof(VirtualMethodBase), out _);

        // Assert
        _ = hasOverloadedPrimary.Should().BeTrue("because OverloadedPrimarySample has a captured primary parameter.");
        _ = overloadedPrimary.GetParameters().Should().HaveCount(2);
        _ = formatter.DetermineConstructorKind(typeof(OverloadedPrimarySample))
            .Should().Contain("String primaryKey, Int32 maxRetries");

        _ = hasRecordPrimary.Should().BeTrue("because positional record primary parameters match Deconstruct.");
        _ = recordPrimary.GetParameters().Should().HaveCount(2);

        _ = hasReadOnlyStructPrimary.Should().BeTrue("because ReadOnlyStructSample captures its primary constructor parameter.");
        _ = formatter.DetermineConstructorKind(typeof(ReadOnlyStructSample)).Should().Be("Primary (Int32 scale)");

        _ = hasBasePrimary.Should().BeFalse("because VirtualMethodBase has only a compiler default constructor.");

        _ = formatter.BuildTypeDeclarationSignature(typeof(PrimaryRecordSample)).Should().Contain("record PrimaryRecordSample");
        _ = formatter.BuildTypeDeclarationSignature(typeof(ReadOnlyRecordStructSample)).Should().Contain("readonly record struct ReadOnlyRecordStructSample");
        _ = formatter.BuildTypeDeclarationSignature(typeof(ReadOnlyStructSample)).Should().Contain("readonly struct ReadOnlyStructSample");
        _ = formatter.BuildTypeDeclarationSignature(typeof(RefStructSample)).Should().Contain("ref struct RefStructSample");
        _ = formatter.BuildTypeDeclarationSignature(typeof(GenericOuter<>.NestedGeneric<>))
            .Should().Contain("protected internal sealed class NestedGeneric<TInner>");
        _ = formatter.BuildTypeDeclarationSignature(typeof(VarianceTransform<,>))
            .Should().Contain("delegate TResult")
            .And.Contain("VarianceTransform<in TInput, out TResult>(TInput inputValue)");
    }

    [Fact]
    public void FormatterShouldFormatModernPropertiesIndexersEventsFieldsOperatorsAndNullabilityInfo()
    {
        // Arrange
        var formatter = new CSharpSignatureFormatter();
        var nullabilityContext = new NullabilityInfoContext();

        var requiredProperty = typeof(SealedOverrideDerived).GetProperty(nameof(SealedOverrideDerived.RequiredTitle))!;
        var privateSetterProperty = typeof(SealedOverrideDerived).GetProperty(nameof(SealedOverrideDerived.Description))!;
        var internalSetterProperty = typeof(SealedOverrideDerived).GetProperty(nameof(SealedOverrideDerived.InternalTag))!;
        var protectedInternalProperty = typeof(SealedOverrideDerived).GetProperty(nameof(SealedOverrideDerived.ProtectedInternalTag))!;
        var privateProtectedProperty = typeof(SealedOverrideDerived).GetProperty(nameof(SealedOverrideDerived.PrivateProtectedTag))!;
        var writeOnlyProperty = typeof(SealedOverrideDerived).GetProperty(nameof(SealedOverrideDerived.WriteOnlySecret))!;
        var indexerProperty = typeof(SealedOverrideDerived).GetProperties().Single(property => property.GetIndexParameters().Length == 2);
        var staticAbstractProperty = typeof(IModernOperatorContract<>).GetProperty(nameof(IModernOperatorContract<>.CategoryName))!;
        var staticVirtualProperty = typeof(IModernOperatorContract<>).GetProperty(nameof(IModernOperatorContract<>.DefaultWeight))!;
        var readOnlyMethod = typeof(MutableStructSample).GetMethod(nameof(MutableStructSample.DoubleScale))!;
        var sealedOverrideMethod = typeof(SealedOverrideDerived).GetMethod(nameof(SealedOverrideDerived.Describe))!;
        var virtualMethod = typeof(VirtualMethodBase).GetMethod(nameof(VirtualMethodBase.Describe))!;
        var scopedMethod = typeof(SealedOverrideDerived).GetMethod(nameof(SealedOverrideDerived.CombineScoped))!;
        var matrixMethod = typeof(SealedOverrideDerived).GetMethod(nameof(SealedOverrideDerived.CreateMatrix))!;
        var pointerMethod = typeof(Buffer).GetMethods()
            .First(method => method.Name == nameof(Buffer.MemoryCopy) && method.GetParameters()[0].ParameterType.IsPointer);
        var asyncNullableMethod = typeof(SealedOverrideDerived).GetMethod(nameof(SealedOverrideDerived.FetchNullableAsync))!;
        var refField = typeof(RefStructSample).GetField(nameof(RefStructSample.ReferencedValue))!;
        var eventInfo = typeof(SealedOverrideDerived).GetEvent(nameof(SealedOverrideDerived.Changed))!;

        var checkedAddition = typeof(SealedOverrideDerived).GetMethod("op_CheckedAddition")!;
        var unsignedRightShift = typeof(SealedOverrideDerived).GetMethod("op_UnsignedRightShift")!;
        var compoundAdd = typeof(SealedOverrideDerived).GetMethod("op_AdditionAssignment")!;
        var checkedCompoundAdd = typeof(SealedOverrideDerived).GetMethod("op_CheckedAdditionAssignment")!;
        var implicitOp = typeof(SealedOverrideDerived).GetMethod("op_Implicit")!;
        var explicitOp = typeof(SealedOverrideDerived).GetMethods()
            .Single(method => method.Name == "op_Explicit" && method.ReturnType == typeof(string));
        var checkedExplicitOp = typeof(SealedOverrideDerived).GetMethod("op_CheckedExplicit")!;

        // Assert - Properties & Indexer
        _ = formatter.BuildPropertySignature(requiredProperty).Should().Be("public required String? RequiredTitle { get; init; }");
        _ = formatter.BuildPropertySignature(privateSetterProperty).Should().Be("public String Description { get; private set; }");
        _ = formatter.BuildPropertySignature(internalSetterProperty).Should().Be("public String InternalTag { get; internal set; }");
        _ = formatter.BuildPropertySignature(protectedInternalProperty).Should().Be("public String ProtectedInternalTag { get; protected internal set; }");
        _ = formatter.BuildPropertySignature(privateProtectedProperty).Should().Be("public String PrivateProtectedTag { get; private protected set; }");
        _ = formatter.BuildPropertySignature(writeOnlyProperty).Should().Be("public String WriteOnlySecret { set; }");
        _ = formatter.BuildPropertySignature(indexerProperty).Should().Be("public String? this[Int32 rowIndex, String columnName] { get; private set; }");
        _ = formatter.BuildPropertySignature(staticAbstractProperty).Should().Contain("static abstract String CategoryName { get; }");
        _ = formatter.BuildPropertySignature(staticVirtualProperty).Should().Contain("static virtual Int32 DefaultWeight { get; }");

        // Assert - Fields, Events, Methods & Operators
        var refFieldTypeName = formatter.FormatDisplayTypeName(refField.FieldType);
        _ = formatter.BuildFieldSignature(refField, refFieldTypeName).Should().Contain("ref readonly Int32 ReferencedValue");
        _ = formatter.BuildEventSignature(eventInfo).Should().Contain("event EventHandler? Changed");
        _ = formatter.BuildMethodSignature(readOnlyMethod, isAsyncMethod: false).Should().Contain("readonly Int32 DoubleScale()");
        _ = formatter.BuildMethodSignature(virtualMethod, isAsyncMethod: false).Should().Contain("public virtual String Describe()");
        _ = formatter.BuildMethodSignature(sealedOverrideMethod, isAsyncMethod: false).Should().Contain("public sealed override String Describe()");
        _ = formatter.BuildMethodSignature(scopedMethod, isAsyncMethod: false).Should().Contain("scoped").And.Contain("RefStructSample sample");
        _ = formatter.BuildMethodSignature(matrixMethod, isAsyncMethod: false).Should().Contain("Int32[,] CreateMatrix(out String? failureReason)");
        _ = formatter.BuildMethodSignature(pointerMethod, isAsyncMethod: false).Should().Contain("Void* source");
        _ = formatter.BuildMethodSignature(checkedAddition, isAsyncMethod: false).Should().Contain("operator checked +(");
        _ = formatter.BuildMethodSignature(unsignedRightShift, isAsyncMethod: false).Should().Contain("operator >>>(");
        _ = formatter.BuildMethodSignature(compoundAdd, isAsyncMethod: false).Should().Contain("operator +=(");
        _ = formatter.BuildMethodSignature(checkedCompoundAdd, isAsyncMethod: false).Should().Contain("operator checked +=(");
        _ = formatter.BuildMethodSignature(implicitOp, isAsyncMethod: false).Should().Contain("implicit operator Int32(");
        _ = formatter.BuildMethodSignature(explicitOp, isAsyncMethod: false).Should().Contain("explicit operator String(");
        _ = formatter.BuildMethodSignature(checkedExplicitOp, isAsyncMethod: false).Should().Contain("explicit operator checked Byte(");

        // Assert - NullabilityInfo display formatting
        _ = formatter.FormatDisplayTypeName(nullabilityContext.Create(asyncNullableMethod.ReturnParameter))
            .Should().Be("Task<String?>");
        _ = formatter.FormatDisplayTypeName(nullabilityContext.Create(asyncNullableMethod.GetParameters()[0]))
            .Should().Be("Int32?");
        _ = formatter.FormatDisplayTypeName(nullabilityContext.Create(matrixMethod.GetParameters()[0]))
            .Should().Be("String?");
        _ = formatter.FormatDisplayTypeName(nullabilityContext.Create(matrixMethod.ReturnParameter))
            .Should().Be("Int32[,]");
        _ = formatter.FormatDisplayTypeName(nullabilityContext.Create(pointerMethod.GetParameters()[0]))
            .Should().Be("Void*");
        _ = formatter.FormatDisplayTypeName(typeof(byte).MakePointerType())
            .Should().Be("Byte*");
    }

    private class AccessibleOuterHost
    {
        protected internal class AccessibleMembersContainer
        {
            private int backingStorage = 10;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Testing field visibility modifiers")]
            protected int ProtectedCounter = 1;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Testing field visibility modifiers")]
            protected internal int SharedCounter = 2;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Testing field visibility modifiers")]
            private protected int PrivateProtectedCounter = 3;

            public const string EscapedConstant = "line1\r\n\"quoted\"\t\\slash";

            protected ref int MutableRefProperty => ref backingStorage;

            protected internal ref readonly int ReadOnlyRefProperty => ref backingStorage;

            private protected int PrivateProtectedProperty => PrivateProtectedCounter;

            protected ref int GetMutableRef() => ref backingStorage;

            protected internal ref readonly int GetReadOnlyRef() => ref backingStorage;

            private protected int GetPrivateProtectedValue() => PrivateProtectedProperty;

            public static async ValueTask ExecuteValueTaskAsync() => await Task.CompletedTask;

            public static string FormatOptionalDefaults(
                scoped ReadOnlySpan<int> scopedSpan,
                char symbol = 'x',
                bool enabled = true,
                bool disabled = false,
                DayOfWeek weekday = DayOfWeek.Monday,
                string? text = null,
                int count = 0,
                Guid correlation = default)
            {
                _ = scopedSpan.Length;
                return $"{symbol}:{enabled}:{disabled}:{weekday}:{text}:{count}:{correlation}";
            }
        }

        private sealed class DerivedAccessibleMembersContainer : AccessibleMembersContainer;

        protected delegate ref readonly int RefReadOnlyDelegate(scoped ref int counter);

        protected internal delegate ref int RefMutableDelegate(out string? reason);

        public static Type ReadOnlyDelegateType => typeof(RefReadOnlyDelegate);
        public static Type MutableDelegateType => typeof(RefMutableDelegate);
    }

    private sealed class DerivedAccessibleOuterHost : AccessibleOuterHost;

    [Fact]
    public void FormatterShouldHandleRefReturnsDelegatesDefaultsProtectedAccessAndAsyncHelpers()
    {
        // Arrange
        var formatter = new CSharpSignatureFormatter();
        var containerType = typeof(AccessibleOuterHost.AccessibleMembersContainer);
        var protectedField = containerType.GetField("ProtectedCounter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var protectedInternalField = containerType.GetField("SharedCounter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var privateProtectedField = containerType.GetField("PrivateProtectedCounter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var escapedConstField = containerType.GetField("EscapedConstant", BindingFlags.Static | BindingFlags.Public)!;
        var mutableRefProp = containerType.GetProperty("MutableRefProperty", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var readOnlyRefProp = containerType.GetProperty("ReadOnlyRefProperty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var privateProtectedProp = containerType.GetProperty("PrivateProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var mutableRefMethod = containerType.GetMethod("GetMutableRef", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var readOnlyRefMethod = containerType.GetMethod("GetReadOnlyRef", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var privateProtectedMethod = containerType.GetMethod("GetPrivateProtectedValue", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var defaultsMethod = containerType.GetMethod(nameof(AccessibleOuterHost.AccessibleMembersContainer.FormatOptionalDefaults))!;
        var valueTaskMethod = containerType.GetMethod(nameof(AccessibleOuterHost.AccessibleMembersContainer.ExecuteValueTaskAsync))!;
        var streamMethod = typeof(PrimaryCtorSample).GetMethod(nameof(PrimaryCtorSample.StreamAsync))!;

        // Assert - Ref returns & protected / protected internal / private protected visibility & escaped constants
        _ = formatter.BuildTypeDeclarationSignature(containerType).Should().StartWith("protected internal class AccessibleMembersContainer");
        _ = formatter.BuildFieldSignature(protectedField, "Int32").Should().Be("protected Int32 ProtectedCounter");
        _ = formatter.BuildFieldSignature(protectedInternalField, "Int32").Should().Be("protected internal Int32 SharedCounter");
        _ = formatter.BuildFieldSignature(privateProtectedField, "Int32").Should().Be("private protected Int32 PrivateProtectedCounter");
        _ = formatter.BuildFieldSignature(escapedConstField, "String").Should().Contain("= \"line1\\r\\n\\\"quoted\\\"\\t\\\\slash\"");
        _ = formatter.BuildPropertySignature(mutableRefProp).Should().Contain("protected ref Int32 MutableRefProperty");
        _ = formatter.BuildPropertySignature(readOnlyRefProp).Should().Contain("protected internal ref readonly Int32 ReadOnlyRefProperty");
        _ = formatter.BuildPropertySignature(privateProtectedProp).Should().Contain("private protected Int32 PrivateProtectedProperty");
        _ = formatter.BuildMethodSignature(mutableRefMethod, isAsyncMethod: false).Should().Be("protected ref Int32 GetMutableRef()");
        _ = formatter.BuildMethodSignature(readOnlyRefMethod, isAsyncMethod: false).Should().Be("protected internal ref readonly Int32 GetReadOnlyRef()");
        _ = formatter.BuildMethodSignature(privateProtectedMethod, isAsyncMethod: false).Should().Be("private protected Int32 GetPrivateProtectedValue()");
        _ = formatter.BuildTypeDeclarationSignature(AccessibleOuterHost.ReadOnlyDelegateType).Should().StartWith("protected delegate ref readonly Int32 RefReadOnlyDelegate(scoped ref Int32 counter)");
        _ = formatter.BuildTypeDeclarationSignature(AccessibleOuterHost.MutableDelegateType).Should().StartWith("protected internal delegate ref Int32 RefMutableDelegate(out String? reason)");

        // Assert - Defaults & scoped parameter
        var defaultsSig = formatter.BuildMethodSignature(defaultsMethod, isAsyncMethod: false);
        _ = defaultsSig.Should().Contain("scoped ReadOnlySpan<Int32> scopedSpan")
            .And.Contain("'x'")
            .And.Contain("true")
            .And.Contain("false")
            .And.Contain("DayOfWeek.Monday")
            .And.Contain("= null")
            .And.Contain("= 0")
            .And.Contain("= default");

        // Assert - Async & stream detection helpers
        _ = formatter.IsAwaitableReturnType(typeof(Task)).Should().BeTrue();
        _ = formatter.IsAwaitableReturnType(typeof(Task<string>)).Should().BeTrue();
        _ = formatter.IsAwaitableReturnType(typeof(ValueTask)).Should().BeTrue();
        _ = formatter.IsAwaitableReturnType(typeof(ValueTask<int>)).Should().BeTrue();
        _ = formatter.IsAwaitableReturnType(typeof(int)).Should().BeFalse();
        _ = formatter.IsAwaitableReturnType(typeof(List<int>)).Should().BeFalse();

        _ = formatter.IsAsyncEnumerableType(typeof(IAsyncEnumerable<string>)).Should().BeTrue();
        _ = formatter.IsAsyncEnumerableType(typeof(IAsyncEnumerator<int>)).Should().BeTrue();
        _ = formatter.IsAsyncEnumerableType(typeof(string)).Should().BeFalse();
        _ = formatter.IsAsyncEnumerableType(typeof(List<int>)).Should().BeFalse();

        _ = formatter.HasAsyncStateMachineAttribute(valueTaskMethod).Should().BeTrue();
        _ = formatter.HasAsyncStateMachineAttribute(streamMethod).Should().BeTrue();
        _ = formatter.HasAsyncStateMachineAttribute(defaultsMethod).Should().BeFalse();

        // Assert - Extension by-ref modifiers, [Optional] without default, and double default
        var mutateByRefMethod = typeof(FormatterExtensionSampleExtensions).GetMethod(nameof(FormatterExtensionSampleExtensions.MutateByRef))!;
        var readScopedByRefMethod = typeof(FormatterExtensionSampleExtensions).GetMethod(nameof(FormatterExtensionSampleExtensions.ReadScopedByRef))!;

        var mutateSignature = formatter.BuildMethodSignature(mutateByRefMethod, isAsyncMethod: false);
        _ = mutateSignature.Should().Contain("this ref Int32 value")
            .And.Contain("Int32 optionalInt = default")
            .And.Contain("String optionalText = null")
            .And.Contain("Double ratio = 1.5");
        _ = formatter.BuildMethodSignature(readScopedByRefMethod, isAsyncMethod: false)
            .Should().Contain("this scoped ref readonly Int32 value");
    }

    private interface IDefaultInterfaceContract
    {
        void AbstractContract();
        void VirtualDefaultImplementation() => AbstractContract();
        sealed void SealedDefaultImplementation() => AbstractContract();
    }

    [InlineArray(4)]
    private struct InlineFourBuffer
    {
        private int element0;
    }

    private unsafe struct FixedAndFunctionPointerHost
    {
        public fixed byte HeaderBytes[16];

        public static delegate* unmanaged[Cdecl]<int, int> GetUnmanagedCallback(delegate*<string, int, void> managedCallback)
        {
            _ = managedCallback;
            return null;
        }
    }

    private sealed class AdvancedClrFeaturesShowcase : VirtualMethodBase, IDefaultInterfaceContract
    {
        public required string RequiredIdentifier { get; init; }

        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public AdvancedClrFeaturesShowcase() => RequiredIdentifier = "DEFAULT";

        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        [OverloadResolutionPriority(2)]
        public AdvancedClrFeaturesShowcase(string identifier) => RequiredIdentifier = identifier;

        ~AdvancedClrFeaturesShowcase() => _ = RequiredIdentifier;

        public (string Label, int Count, nint NativeOffset, dynamic Payload) InspectTupleNativeAndDynamic(
            (string Key, NuintCapacityIndex) inputPair,
            nint stepSize,
            dynamic dynamicContext)
        {
            _ = inputPair;
            return (RequiredIdentifier, (int)stepSize, stepSize, dynamicContext);
        }

        [OverloadResolutionPriority(1)]
        public TItem TransformConstrained<TItem>(TItem item)
            where TItem : class, new() =>
            RequiredIdentifier.Length > 0 ? item : new TItem();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "Testing classic extern P/Invoke reflection")]
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern int GetCurrentProcessId();

        void IDefaultInterfaceContract.AbstractContract()
        {
        }
    }

    private readonly record struct NuintCapacityIndex(nuint Value);

    [Fact]
    public void FormatterShouldSupportInheritanceWhereClausesTuplesFunctionPointersFixedBuffersDimsFinalizersExplicitInterfacesAndExtern()
    {
        // Arrange
        var formatter = new CSharpSignatureFormatter();
        var showcaseType = typeof(AdvancedClrFeaturesShowcase);

        // Act & Assert - N-01: Base class, implemented interfaces, and generic where clauses
        _ = formatter.BuildTypeDeclarationSignature(showcaseType)
            .Should().Be("sealed class AdvancedClrFeaturesShowcase : VirtualMethodBase, IDefaultInterfaceContract");
        _ = formatter.BuildTypeDeclarationSignature(typeof(GenericOuter<>.NestedGeneric<>))
            .Should().Be("protected internal sealed class NestedGeneric<TInner> where TInner : allows ref struct");
        _ = formatter.BuildTypeDeclarationSignature(typeof(VarianceTransform<,>))
            .Should().Contain("where TInput : notnull");

        var genericConstrainedMethod = showcaseType.GetMethod(nameof(AdvancedClrFeaturesShowcase.TransformConstrained))!;
        _ = formatter.BuildMethodSignature(genericConstrainedMethod, isAsyncMethod: false)
            .Should().Be("[OverloadResolutionPriority(1)] public TItem TransformConstrained<TItem>(TItem item) where TItem : class, new()");

        // Act & Assert - N-02 & N-08: Named ValueTuples, IntPtr/nint, and dynamic
        var tupleMethod = showcaseType.GetMethod(nameof(AdvancedClrFeaturesShowcase.InspectTupleNativeAndDynamic))!;
        var tupleMethodSig = formatter.BuildMethodSignature(tupleMethod, isAsyncMethod: false);
        _ = tupleMethodSig.Should().Contain("(String Label, Int32 Count, IntPtr NativeOffset, dynamic Payload) InspectTupleNativeAndDynamic")
            .And.Contain("(String Key, NuintCapacityIndex) inputPair")
            .And.Contain("IntPtr stepSize")
            .And.Contain("dynamic dynamicContext");

        // Act & Assert - N-03 & N-04: Function pointers and unsafe fixed buffers
        var fixedField = typeof(FixedAndFunctionPointerHost).GetField(nameof(FixedAndFunctionPointerHost.HeaderBytes))!;
        _ = formatter.BuildFieldSignature(fixedField, "Byte[16]")
            .Should().Be("public fixed Byte HeaderBytes[16]");

        var fnPtrMethod = typeof(FixedAndFunctionPointerHost).GetMethod(nameof(FixedAndFunctionPointerHost.GetUnmanagedCallback))!;
        _ = formatter.BuildMethodSignature(fnPtrMethod, isAsyncMethod: false)
            .Should().Be("public static delegate* unmanaged[Cdecl]<Int32, Int32> GetUnmanagedCallback(delegate*<String, Int32, Void> managedCallback)");

        // Act & Assert - N-05: Default Interface Methods (DIMs)
        var abstractInterfaceMethod = typeof(IDefaultInterfaceContract).GetMethod(nameof(IDefaultInterfaceContract.AbstractContract))!;
        var virtualDimMethod = typeof(IDefaultInterfaceContract).GetMethod(nameof(IDefaultInterfaceContract.VirtualDefaultImplementation))!;
        var sealedDimMethod = typeof(IDefaultInterfaceContract).GetMethod(nameof(IDefaultInterfaceContract.SealedDefaultImplementation))!;
        _ = formatter.BuildMethodSignature(abstractInterfaceMethod, isAsyncMethod: false)
            .Should().Be("public Void AbstractContract()");
        _ = formatter.BuildMethodSignature(virtualDimMethod, isAsyncMethod: false)
            .Should().Be("public virtual Void VirtualDefaultImplementation()");
        _ = formatter.BuildMethodSignature(sealedDimMethod, isAsyncMethod: false)
            .Should().Be("public sealed Void SealedDefaultImplementation()");

        // Act & Assert - N-06: Finalizer (~ClassName())
        var finalizerMethod = showcaseType.GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _ = formatter.BuildMethodSignature(finalizerMethod, isAsyncMethod: false)
            .Should().Be("~AdvancedClrFeaturesShowcase()");

        // Act & Assert - N-07: Explicit interface implementation
        var explicitMethod = showcaseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name.EndsWith("AbstractContract", StringComparison.Ordinal));
        _ = formatter.BuildMethodSignature(explicitMethod, isAsyncMethod: false)
            .Should().Be("Void IDefaultInterfaceContract.AbstractContract()");

        // Act & Assert - N-11 & N-12: extern P/Invoke, [SetsRequiredMembers], and [OverloadResolutionPriority]
        var externMethod = showcaseType.GetMethod(nameof(AdvancedClrFeaturesShowcase.GetCurrentProcessId))!;
        _ = formatter.BuildMethodSignature(externMethod, isAsyncMethod: false)
            .Should().Be("public static extern Int32 GetCurrentProcessId()");

        var setsRequiredCtor = showcaseType.GetConstructors().Single(ctor => ctor.GetParameters().Length == 1);
        _ = formatter.BuildConstructorSignature(setsRequiredCtor, showcaseType)
            .Should().Be("[SetsRequiredMembers] [OverloadResolutionPriority(2)] public AdvancedClrFeaturesShowcase(String identifier)");
    }
}

internal static class FormatterExtensionSampleExtensions
{
    public static void MutateByRef(
        this ref int value,
        [System.Runtime.InteropServices.Optional] int optionalInt,
        [System.Runtime.InteropServices.Optional] string optionalText,
        double ratio = 1.5)
    {
        value += optionalInt + (int)ratio;
        _ = optionalText;
    }

    public static int ReadScopedByRef(this scoped ref readonly int value) => value;
}
