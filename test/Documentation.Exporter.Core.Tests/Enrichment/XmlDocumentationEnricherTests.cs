using Documentation.Exporter.Core.DependencyInjection;
using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Enrichment;

public sealed class XmlDocumentationEnricherTests
{
    private sealed class TrackingXmlParser : IXmlDocumentationParser
    {
        public Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void Clear()
        {
        }

        public string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M") => null;

        public TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel) =>
            typeDocumentationModel with { SummaryDescription = "Parsed type summary for " + fullTypeName };

        public MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel) =>
            memberDocumentationModel with { SummaryDescription = "Parsed member summary for " + xmlMemberKey };
    }

    [Fact]
    [UnitTest]
    public void OrderPropertyShouldReturnTen()
    {
        // Arrange
        var parser = new TrackingXmlParser();
        var enricher = new XmlDocumentationEnricher(parser);

        // Assert
        _ = enricher.Order.Should().Be(10);
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWhenParametersNullShouldThrowArgumentNullException()
    {
        // Arrange
        var parser = new TrackingXmlParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var model = new TypeDocumentationModel();
        var options = new ExporterOptions();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new XmlDocumentationEnricher(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(null!, model, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(typeof(string), null!, options));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => enricher.EnrichAsync(typeof(string), model, null!));
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWithValidTypeShouldPopulateTypeAndMemberDocumentation()
    {
        // Arrange
        var parser = new TrackingXmlParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var model = new TypeDocumentationModel
        {
            TypeName = nameof(String),
            Properties = [new MemberDocumentationModel { MemberName = "Length" }],
            Methods = [new MemberDocumentationModel { MemberName = "Clone" }]
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(string), model, options);

        // Assert
        _ = enrichedModel.SummaryDescription.Should().Contain("System.String");
        _ = enrichedModel.Properties[0].SummaryDescription.Should().Contain("Length");
        _ = enrichedModel.Methods[0].SummaryDescription.Should().Contain("Clone");
    }

    private sealed class EmptyCtorDocsXmlParser : IXmlDocumentationParser
    {
        public Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void Clear()
        {
        }

        public string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M") => null;

        public TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel) =>
            typeDocumentationModel with { SummaryDescription = "Class-level description for " + fullTypeName };

        public MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel) =>
            xmlMemberKey.StartsWith("T:", StringComparison.Ordinal) && memberDocumentationModel.MemberParameters.Count > 0
                ? memberDocumentationModel with
                {
                    MemberParameters =
                    [
                        memberDocumentationModel.MemberParameters[0] with { ParameterDescription = "Primary ctor param description" },
                        .. memberDocumentationModel.MemberParameters.Skip(1)
                    ]
                }
                : memberDocumentationModel;
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWhenConstructorsHaveNoExplicitXmlShouldUseClassSummaryAndPrimaryCtorParams()
    {
        // Arrange
        var parser = new EmptyCtorDocsXmlParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var model = new TypeDocumentationModel
        {
            TypeName = "SampleType",
            Constructors =
            [
                new MemberDocumentationModel { MemberName = "SampleType" },
                new MemberDocumentationModel
                {
                    MemberName = "SampleType",
                    MemberParameters = [new ParameterDocumentationModel { ParameterName = "dependency", ParameterTypeName = "Object" }]
                }
            ]
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(object), model, options);

        // Assert
        _ = enrichedModel.Constructors[0].SummaryDescription.Should().Be("Class-level description for System.Object");
        _ = enrichedModel.Constructors[1].SummaryDescription.Should().Be("Class-level description for System.Object");
        _ = enrichedModel.Constructors[1].MemberParameters[0].ParameterDescription.Should().Be("Primary ctor param description");
    }

    private interface IBaseContract<TItem>
    {
        string ContractProperty { get; }
        event EventHandler? ContractEvent;
        TResult ExecuteContract<TResult>(TItem item, ref int count, int[] vector, int[,] matrix, List<TItem> items);
    }

    private abstract class BaseProcessor<TItem>
    {
        public int InheritedField = 5;
    }

    private sealed class DerivedProcessor<TClassParam> : BaseProcessor<TClassParam>, IBaseContract<TClassParam>
    {
        public string ContractProperty => "Ready";
        public event EventHandler? ContractEvent;

        public void Trigger() => ContractEvent?.Invoke(this, EventArgs.Empty);

        public TResult ExecuteContract<TResult>(TClassParam item, ref int count, int[] vector, int[,] matrix, List<TClassParam> items) =>
            default!;

        public static TResult SelectGeneric<TResult>(TResult input) => input;
    }

    private sealed class InheritedDocsXmlParser : IXmlDocumentationParser
    {
        public List<string> RequestedKeys { get; } = [];

        public Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void Clear()
        {
        }

        public string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M") => null;

        public TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel) =>
            typeDocumentationModel;

        public MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel)
        {
            RequestedKeys.Add(xmlMemberKey);
            return !xmlMemberKey.Contains("BaseProcessor", StringComparison.Ordinal) ||
                   xmlMemberKey.Contains("Undocumented", StringComparison.Ordinal)
                ? memberDocumentationModel
                : memberDocumentationModel with
                {
                    SummaryDescription = "Inherited summary from " + xmlMemberKey,
                    RemarksDescription = "Inherited remarks",
                    ReturnValueDescription = "Inherited return",
                    DocumentedExceptions = new Dictionary<string, string>(memberDocumentationModel.DocumentedExceptions)
                    {
                        ["ArgumentNullException"] = "When null."
                    },
                    CodeExamples = [.. memberDocumentationModel.CodeExamples, "Inherited example"],
                    SeeAlsoReferences = [.. memberDocumentationModel.SeeAlsoReferences, "Inherited see also"],
                    MemberParameters =
                    [
                        .. memberDocumentationModel.MemberParameters.Select(parameter =>
                            parameter with { ParameterDescription = "Inherited param " + parameter.ParameterName })
                    ]
                };
        }
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWithInheritedMembersAndComplexParametersShouldMergeAncestorDocsAndFormatXmlKeys()
    {
        // Arrange
        var parser = new InheritedDocsXmlParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var model = new TypeDocumentationModel
        {
            TypeName = "DerivedProcessor",
            Properties =
            [
                new MemberDocumentationModel { MemberName = nameof(DerivedProcessor<>.ContractProperty) },
                new MemberDocumentationModel { MemberName = "UndocumentedProp" }
            ],
            Fields = [new MemberDocumentationModel { MemberName = nameof(DerivedProcessor<>.InheritedField) }],
            Events = [new MemberDocumentationModel { MemberName = nameof(DerivedProcessor<>.ContractEvent) }],
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = nameof(DerivedProcessor<>.ExecuteContract),
                    MemberParameters =
                    [
                        new ParameterDocumentationModel { ParameterName = "item", ParameterTypeName = "TClassParam" },
                        new ParameterDocumentationModel { ParameterName = "count", ParameterTypeName = "Int32" },
                        new ParameterDocumentationModel { ParameterName = "vector", ParameterTypeName = "Int32[]" },
                        new ParameterDocumentationModel { ParameterName = "matrix", ParameterTypeName = "Int32[,]" },
                        new ParameterDocumentationModel { ParameterName = "items", ParameterTypeName = "List<TClassParam>" }
                    ]
                },
                new MemberDocumentationModel
                {
                    MemberName = nameof(DerivedProcessor<>.SelectGeneric),
                    MemberParameters = [new ParameterDocumentationModel { ParameterName = "input", ParameterTypeName = "TResult" }]
                },
                new MemberDocumentationModel
                {
                    MemberName = nameof(DerivedProcessor<>.SelectGeneric),
                    MemberParameters = [new ParameterDocumentationModel { ParameterName = "wrongParamName", ParameterTypeName = "TResult" }]
                },
                new MemberDocumentationModel
                {
                    MemberName = nameof(DerivedProcessor<>.Trigger)
                }
            ]
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(DerivedProcessor<>), model, options);

        // Assert
        _ = enrichedModel.Fields[0].SummaryDescription.Should().Contain("Inherited summary");
        _ = enrichedModel.Fields[0].RemarksDescription.Should().Be("Inherited remarks");
        _ = enrichedModel.Fields[0].DocumentedExceptions.Should().ContainKey("ArgumentNullException");
        _ = enrichedModel.Fields[0].CodeExamples.Should().ContainSingle().Which.Should().Be("Inherited example");
        _ = enrichedModel.Fields[0].SeeAlsoReferences.Should().ContainSingle().Which.Should().Be("Inherited see also");
        _ = parser.RequestedKeys.Should().Contain(key => key.Contains("`0", StringComparison.Ordinal) && key.Contains("System.Int32@", StringComparison.Ordinal) && key.Contains("System.Int32[0:,0:]", StringComparison.Ordinal));
        _ = parser.RequestedKeys.Should().Contain(key => key.Contains("``0", StringComparison.Ordinal));
    }

    private sealed class MockInheritDocParser : IXmlDocumentationParser
    {
        public Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void Clear()
        {
        }

        public string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M") =>
            cref.Contains(':') ? cref : $"{defaultPrefix}:{cref}";

        public TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel) =>
            fullTypeName.Contains("DerivedTypeWithInheritDoc", StringComparison.Ordinal)
                ? typeDocumentationModel with { HasInheritDoc = true }
                : fullTypeName.Contains("BaseContractType", StringComparison.Ordinal)
                    ? typeDocumentationModel with
                    {
                        SummaryDescription = "Base contract type summary.",
                        RemarksDescription = "Base contract type remarks.",
                        DocumentedExceptions = new Dictionary<string, string>(typeDocumentationModel.DocumentedExceptions)
                        {
                            ["InvalidOperationException"] = "When base fails."
                        },
                        CodeExamples = [.. typeDocumentationModel.CodeExamples, "var contract = new BaseContractType();"],
                        SeeAlsoReferences = [.. typeDocumentationModel.SeeAlsoReferences, "System.IDisposable"],
                        GenericTypeParameterDetails =
                        [
                            .. typeDocumentationModel.GenericTypeParameterDetails,
                            new ParameterDocumentationModel
                            {
                                ParameterName = "TItem",
                                ParameterDescription = "Base type argument description."
                            }
                        ]
                    }
                    : fullTypeName.Contains("ExplicitTargetType", StringComparison.Ordinal)
                        ? typeDocumentationModel with { SummaryDescription = "Explicit target type summary." }
                        : typeDocumentationModel;

        public MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel) =>
            xmlMemberKey.Contains("ExplicitTargetMethod", StringComparison.Ordinal)
                ? memberDocumentationModel with
                {
                    SummaryDescription = "Target method summary from cref.",
                    RemarksDescription = "Target method remarks from cref.",
                    ReturnValueDescription = "Target return description.",
                    DocumentedExceptions = new Dictionary<string, string>(memberDocumentationModel.DocumentedExceptions)
                    {
                        ["ArgumentException"] = "When invalid."
                    },
                    CodeExamples = [.. memberDocumentationModel.CodeExamples, "TargetMethod();"],
                    SeeAlsoReferences = [.. memberDocumentationModel.SeeAlsoReferences, "System.String"],
                    MemberParameters =
                    [
                        .. memberDocumentationModel.MemberParameters.Select(parameter =>
                            parameter with { ParameterDescription = "Target param " + parameter.ParameterName })
                    ]
                }
                : xmlMemberKey.Contains("BaseContractType", StringComparison.Ordinal) && xmlMemberKey.Contains("CustomSummaryMethod", StringComparison.Ordinal)
                ? memberDocumentationModel with
                {
                    SummaryDescription = "Base summary that should not overwrite custom.",
                    RemarksDescription = "Base remarks that should be inherited.",
                    ReturnValueDescription = "Base return that should be inherited.",
                    DocumentedExceptions = new Dictionary<string, string>(memberDocumentationModel.DocumentedExceptions)
                    {
                        ["TimeoutException"] = "When timed out."
                    }
                }
                : xmlMemberKey.Contains("CustomSummaryMethod", StringComparison.Ordinal)
                ? memberDocumentationModel with
                {
                    HasInheritDoc = true,
                    SummaryDescription = "Custom derived summary."
                }
                : xmlMemberKey.Contains("MethodWithCref", StringComparison.Ordinal) ||
                  xmlMemberKey.Contains("#ctor", StringComparison.Ordinal)
                ? memberDocumentationModel with
                {
                    HasInheritDoc = true,
                    InheritDocCref = "M:SampleNamespace.ExplicitTargetMethod"
                }
                : memberDocumentationModel;
    }

    private interface IBaseContractType<TItem>
    {
        void CustomSummaryMethod();
    }

    private sealed class DerivedTypeWithInheritDoc<TItem> : IBaseContractType<TItem>
    {
        public int InvocationCount { get; set; }
        public DerivedTypeWithInheritDoc() { }
        public void CustomSummaryMethod() => InvocationCount++;
        public void MethodWithCref() => InvocationCount++;
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWhenTypeHasInheritDocShouldMigrateDocumentationFromBaseInterface()
    {
        // Arrange
        var parser = new MockInheritDocParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var typeModel = new TypeDocumentationModel
        {
            TypeName = "DerivedTypeWithInheritDoc",
            GenericTypeParameterDetails = [new ParameterDocumentationModel { ParameterName = "TItem", ParameterTypeName = "TItem" }]
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(DerivedTypeWithInheritDoc<>), typeModel, options);

        // Assert
        _ = enrichedModel.SummaryDescription.Should().Be("Base contract type summary.", "because summary must be inherited from IBaseContractType.");
        _ = enrichedModel.RemarksDescription.Should().Be("Base contract type remarks.", "because remarks must be inherited from IBaseContractType.");
        _ = enrichedModel.DocumentedExceptions.Should().ContainKey("InvalidOperationException", "because exceptions must be inherited.");
        _ = enrichedModel.CodeExamples.Should().ContainSingle().Which.Should().Be("var contract = new BaseContractType();");
        _ = enrichedModel.SeeAlsoReferences.Should().ContainSingle().Which.Should().Be("System.IDisposable");
        _ = enrichedModel.GenericTypeParameterDetails[0].ParameterDescription.Should().Be("Base type argument description.");
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWhenTypeHasInheritDocCrefShouldMigrateDocumentationFromSpecifiedCref()
    {
        // Arrange
        var parser = new MockInheritDocParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var typeModel = new TypeDocumentationModel
        {
            TypeName = "DerivedTypeWithInheritDoc",
            HasInheritDoc = true,
            InheritDocCref = "T:SampleNamespace.ExplicitTargetType"
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(DerivedTypeWithInheritDoc<>), typeModel, options);

        // Assert
        _ = enrichedModel.SummaryDescription.Should().Be("Explicit target type summary.", "because summary must be migrated from target cref.");
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWhenMemberHasInheritDocCrefShouldMigrateFromTargetCref()
    {
        // Arrange
        var parser = new MockInheritDocParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var methodModel = new MemberDocumentationModel
        {
            MemberName = nameof(DerivedTypeWithInheritDoc<>.MethodWithCref),
            MemberParameters = [new ParameterDocumentationModel { ParameterName = "command", ParameterTypeName = "String" }]
        };
        var constructorModel = new MemberDocumentationModel
        {
            MemberName = "DerivedTypeWithInheritDoc"
        };
        var typeModel = new TypeDocumentationModel
        {
            TypeName = "DerivedTypeWithInheritDoc",
            Methods = [methodModel],
            Constructors = [constructorModel]
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(DerivedTypeWithInheritDoc<>), typeModel, options);

        // Assert
        var enrichedMethod = enrichedModel.Methods[0];
        var enrichedConstructor = enrichedModel.Constructors[0];
        _ = enrichedMethod.SummaryDescription.Should().Be("Target method summary from cref.");
        _ = enrichedMethod.RemarksDescription.Should().Be("Target method remarks from cref.");
        _ = enrichedMethod.ReturnValueDescription.Should().Be("Target return description.");
        _ = enrichedMethod.DocumentedExceptions.Should().ContainKey("ArgumentException");
        _ = enrichedMethod.CodeExamples.Should().ContainSingle().Which.Should().Be("TargetMethod();");
        _ = enrichedMethod.MemberParameters[0].ParameterDescription.Should().Be("Target param command");
        _ = enrichedConstructor.SummaryDescription.Should().Be("Target method summary from cref.");
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncWhenMemberHasInheritDocAndCustomSummaryShouldPreserveSummaryAndMergeRemainingDocs()
    {
        // Arrange
        var parser = new MockInheritDocParser();
        var enricher = new XmlDocumentationEnricher(parser);
        var methodModel = new MemberDocumentationModel
        {
            MemberName = nameof(DerivedTypeWithInheritDoc<>.CustomSummaryMethod)
        };
        var typeModel = new TypeDocumentationModel
        {
            TypeName = "DerivedTypeWithInheritDoc",
            Methods = [methodModel]
        };
        var options = new ExporterOptions();

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(DerivedTypeWithInheritDoc<>), typeModel, options);

        // Assert
        var enrichedMethod = enrichedModel.Methods[0];
        _ = enrichedMethod.SummaryDescription.Should().Be("Custom derived summary.", "because custom summary must not be overwritten by base.");
        _ = enrichedMethod.RemarksDescription.Should().Be("Base remarks that should be inherited.", "because missing remarks must be inherited.");
        _ = enrichedMethod.ReturnValueDescription.Should().Be("Base return that should be inherited.", "because missing return value must be inherited.");
        _ = enrichedMethod.DocumentedExceptions.Should().ContainKey("TimeoutException", "because missing exceptions must be inherited.");
    }

    private sealed class OverloadedSampleService
    {
        public static string Process(string value) => value;

        public static string Process(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    [IntegrationTest]
    public async Task EnrichAsyncWithOverloadedMethodsSharingParameterNameShouldAssignDistinctSummaries()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddDocumentationExporter();
        await using var serviceProvider = services.BuildServiceProvider();
        var parser = serviceProvider.GetRequiredService<IXmlDocumentationParser>();
        var reflector = serviceProvider.GetRequiredService<IAssemblyTypeReflector>();
        var reflectedType = reflector.ReflectTypeMetadata(typeof(OverloadedSampleService));

        var declaringXmlTypeName = typeof(OverloadedSampleService).FullName!.Replace('+', '.');
        var tempXmlPath = Path.Combine(Path.GetTempPath(), $"DocExporter_Overloads_{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(
            tempXmlPath,
            $"""
            <doc>
              <members>
                <member name="T:{declaringXmlTypeName}">
                  <summary>Service with identically named parameter overloads.</summary>
                </member>
                <member name="M:{declaringXmlTypeName}.Process(System.String)">
                  <summary>Processes a string payload.</summary>
                  <param name="value">Text payload.</param>
                </member>
                <member name="M:{declaringXmlTypeName}.Process(System.Int32)">
                  <summary>Processes a numeric identifier.</summary>
                  <param name="value">Numeric identifier.</param>
                </member>
              </members>
            </doc>
            """);

        try
        {
            _ = (await parser.TryLoadXmlDocumentationAsync(tempXmlPath)).Should().BeTrue("because overload XML documentation must load cleanly.");
            var enricher = new XmlDocumentationEnricher(parser);

            // Act
            var enrichedType = await enricher.EnrichAsync(typeof(OverloadedSampleService), reflectedType, new ExporterOptions());

            // Assert
            var stringOverload = enrichedType.Methods.Should().ContainSingle(
                method => method.MemberParameters.Count == 1 && method.MemberParameters[0].ParameterTypeName == "String",
                "because the reflected type declares a string overload.").Subject;
            var integerOverload = enrichedType.Methods.Should().ContainSingle(
                method => method.MemberParameters.Count == 1 && method.MemberParameters[0].ParameterTypeName == "Int32",
                "because the reflected type declares an integer overload.").Subject;

            _ = stringOverload.SummaryDescription.Should().Be("Processes a string payload.", "because exact XmlMemberKey matching must bind the System.String overload.");
            _ = integerOverload.SummaryDescription.Should().Be("Processes a numeric identifier.", "because exact XmlMemberKey matching must bind the System.Int32 overload.");
        }
        finally
        {
            if (File.Exists(tempXmlPath))
            {
                File.Delete(tempXmlPath);
            }
        }
    }

    private interface IGenericMetricContract<TSelf>
        where TSelf : IGenericMetricContract<TSelf>
    {
        static abstract string MetricCategory { get; }
    }

    private sealed class OverloadedUncapturedPrimarySample(string seedLabel) : IGenericMetricContract<OverloadedUncapturedPrimarySample>
    {
        public static string MetricCategory => "Metrics";

        public string SeedLabel { get; } = seedLabel;

        public OverloadedUncapturedPrimarySample()
            : this("default")
        {
        }
    }

    private sealed class DictionaryInheritDocParser(
        IReadOnlyDictionary<string, string> summariesByKey,
        Func<string, string?>? resolveCref = null) : IXmlDocumentationParser
    {
        public Task<bool> TryLoadXmlDocumentationAsync(string? xmlDocumentationFilePath, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> TryLoadXmlDocumentationFilesAsync(IEnumerable<string> xmlDocumentationFilePaths, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void Clear()
        {
        }

        public string? ResolveCrefKey(string cref, string? declaringTypeFullName = null, string defaultPrefix = "M") =>
            resolveCref?.Invoke(cref);

        public TypeDocumentationModel ApplyTypeDocumentation(string fullTypeName, TypeDocumentationModel typeDocumentationModel) =>
            summariesByKey.TryGetValue(fullTypeName, out var summary)
                ? typeDocumentationModel with { SummaryDescription = summary }
                : typeDocumentationModel;

        public MemberDocumentationModel ApplyMemberDocumentation(string xmlMemberKey, MemberDocumentationModel memberDocumentationModel) =>
            summariesByKey.TryGetValue(xmlMemberKey, out var summary)
                ? memberDocumentationModel with { SummaryDescription = summary }
                : memberDocumentationModel;
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncShouldPromoteOverloadedPrimaryConstructorAndInheritGenericInterfacePropertyDocs()
    {
        // Arrange
        var genericInterfaceXmlName = typeof(IGenericMetricContract<>).FullName!.Replace('+', '.');

        var parser = new DictionaryInheritDocParser(new Dictionary<string, string>
        {
            [$"P:{genericInterfaceXmlName}.MetricCategory"] = "Inherited generic interface metric category."
        });

        var enricher = new XmlDocumentationEnricher(parser);
        var model = new TypeDocumentationModel
        {
            TypeName = nameof(OverloadedUncapturedPrimarySample),
            SummaryDescription = "Sample summary.",
            Constructors =
            [
                new MemberDocumentationModel
                {
                    MemberName = nameof(OverloadedUncapturedPrimarySample),
                    IsPrimaryConstructor = false,
                    SummaryDescription = "Sample summary.",
                    MemberParameters =
                    [
                        new ParameterDocumentationModel
                        {
                            ParameterName = "seedLabel",
                            ParameterTypeName = "String",
                            ParameterDescription = "Initial seed label."
                        }
                    ]
                },
                new MemberDocumentationModel
                {
                    MemberName = nameof(OverloadedUncapturedPrimarySample),
                    IsPrimaryConstructor = false,
                    SummaryDescription = "Parameterless overload."
                }
            ],
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = nameof(OverloadedUncapturedPrimarySample.MetricCategory),
                    HasInheritDoc = true
                }
            ]
        };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(OverloadedUncapturedPrimarySample), model, new ExporterOptions());

        // Assert
        _ = enrichedModel.ConstructorKind.Should().Be("Primary (String seedLabel)",
            "because the constructor overload with documented parameters matching the type summary should be promoted to Primary.");
        _ = enrichedModel.Constructors.Should().ContainSingle(constructor => constructor.IsPrimaryConstructor,
            "because exactly the primary overload should be marked IsPrimaryConstructor = true.");
        _ = enrichedModel.Properties.Should().ContainSingle(property => property.MemberName == nameof(OverloadedUncapturedPrimarySample.MetricCategory))
            .Which.SummaryDescription.Should().Be("Inherited generic interface metric category.",
                "because generic interface candidate types must normalize to their generic type definition during <inheritdoc/> resolution.");
    }

    [Fact]
    [UnitTest]
    public async Task EnrichAsyncShouldHandleExplicitCrefFallbackUnmatchedMethodsAndExistingTypeCollections()
    {
        // Arrange
        DictionaryInheritDocParser parser = new(
            new Dictionary<string, string>
            {
                ["T:Sample.FallbackTypeTarget"] = "Fallback type summary.",
                ["P:Sample.ExplicitResolvedProperty"] = "Resolved property cref summary.",
                ["P:Sample.FallbackPropertyTarget"] = "Fallback property cref summary.",
                [$"M:{typeof(OverloadedSampleService).FullName!.Replace('+', '.')}.NonExistentMethod"] = "Fallback unmatched method summary."
            },
            resolveCref: cref => cref == "Resolved" ? "P:Sample.ExplicitResolvedProperty" : null);

        XmlDocumentationEnricher enricher = new(parser);
        TypeDocumentationModel typeModel = new()
        {
            TypeName = nameof(OverloadedSampleService),
            HasInheritDoc = true,
            InheritDocCref = "T:Sample.FallbackTypeTarget",
            CodeExamples = ["ExistingExample();"],
            SeeAlsoReferences = ["System.IDisposable"],
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "ResolvedProperty",
                    HasInheritDoc = true,
                    InheritDocCref = "Resolved"
                },
                new MemberDocumentationModel
                {
                    MemberName = "FallbackProperty",
                    HasInheritDoc = true,
                    InheritDocCref = "P:Sample.FallbackPropertyTarget"
                }
            ],
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "NonExistentMethod"
                }
            ]
        };

        // Act
        var enrichedModel = await enricher.EnrichAsync(typeof(OverloadedSampleService), typeModel, new ExporterOptions());

        // Assert
        _ = enrichedModel.SummaryDescription.Should().Be("Fallback type summary.");
        _ = enrichedModel.CodeExamples.Should().ContainSingle().Which.Should().Be("ExistingExample();");
        _ = enrichedModel.SeeAlsoReferences.Should().ContainSingle().Which.Should().Be("System.IDisposable");
        _ = enrichedModel.Properties.Should().ContainSingle(property => property.MemberName == "ResolvedProperty")
            .Which.SummaryDescription.Should().Be("Resolved property cref summary.");
        _ = enrichedModel.Properties.Should().ContainSingle(property => property.MemberName == "FallbackProperty")
            .Which.SummaryDescription.Should().Be("Fallback property cref summary.");
        _ = enrichedModel.Methods.Should().ContainSingle(method => method.MemberName == "NonExistentMethod")
            .Which.SummaryDescription.Should().Be("Fallback unmatched method summary.");
    }

    private interface IRenamedParameterContract<TContractItem>
    {
        TContractItem this[int index] { get; }

        TContractItem this[TContractItem lookupKey] { get; }

        string FormatPayload<TContractResult>(TContractItem originalItem, TContractResult originalResult);
    }

    private sealed class RenamedParameterImplementation<TDerivedItem> : IRenamedParameterContract<TDerivedItem>
    {
        public TDerivedItem this[int index] => default!;

        public TDerivedItem this[TDerivedItem lookupKey] => lookupKey;

        public string FormatPayload<TDerivedResult>(TDerivedItem renamedItem, TDerivedResult renamedResult) =>
            $"{renamedItem}:{renamedResult}";
    }

    private sealed class ClosedGenericParameterImplementation : IRenamedParameterContract<string>
    {
        public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public string this[string lookupKey] => lookupKey;

        public string FormatPayload<TDerivedResult>(string renamedItem, TDerivedResult renamedResult) =>
            $"{renamedItem}:{renamedResult}";
    }

    [Fact]
    [IntegrationTest]
    public async Task EnrichAsyncWhenImplementationRenamesParametersAndTypeParametersOrClosesGenericInterfaceShouldStillInheritDescriptions()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddDocumentationExporter();
        await using var serviceProvider = services.BuildServiceProvider();
        var parser = serviceProvider.GetRequiredService<IXmlDocumentationParser>();
        var reflector = serviceProvider.GetRequiredService<IAssemblyTypeReflector>();

        var interfaceXmlName = typeof(IRenamedParameterContract<>).FullName!.Replace('+', '.');
        var derivedXmlName = typeof(RenamedParameterImplementation<>).FullName!.Replace('+', '.');
        var closedDerivedXmlName = typeof(ClosedGenericParameterImplementation).FullName!.Replace('+', '.');
        var tempXmlPath = Path.Combine(Path.GetTempPath(), $"DocExporter_RenamedInherit_{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(
            tempXmlPath,
            $"""
            <doc>
              <members>
                <member name="T:{interfaceXmlName}">
                  <summary>Contract with original parameter names.</summary>
                  <typeparam name="TContractItem">The contract item type.</typeparam>
                </member>
                <member name="P:{interfaceXmlName}.Item(System.Int32)">
                  <summary>Gets item by integer index.</summary>
                  <param name="index">Zero-based position.</param>
                  <value>The indexed item.</value>
                </member>
                <member name="P:{interfaceXmlName}.Item(`0)">
                  <summary>Gets item by generic key.</summary>
                  <param name="lookupKey">The generic key.</param>
                  <value>The matched item.</value>
                </member>
                <member name="M:{interfaceXmlName}.FormatPayload``1(`0,``0)">
                  <summary>Formats a payload pair.</summary>
                  <typeparam name="TContractResult">The contract result type.</typeparam>
                  <param name="originalItem">The first payload item.</param>
                  <param name="originalResult">The second payload result.</param>
                  <returns>Formatted text.</returns>
                </member>
                <member name="T:{derivedXmlName}">
                  <inheritdoc />
                </member>
                <member name="M:{derivedXmlName}.FormatPayload``1(`0,``0)">
                  <inheritdoc />
                </member>
                <member name="T:{closedDerivedXmlName}">
                  <inheritdoc />
                </member>
                <member name="P:{closedDerivedXmlName}.Item(System.Int32)">
                  <inheritdoc />
                </member>
                <member name="P:{closedDerivedXmlName}.Item(System.String)">
                  <inheritdoc />
                </member>
                <member name="M:{closedDerivedXmlName}.FormatPayload``1(System.String,``0)">
                  <inheritdoc />
                </member>
              </members>
            </doc>
            """);

        try
        {
            _ = (await parser.TryLoadXmlDocumentationAsync(tempXmlPath)).Should().BeTrue();
            var enricher = new XmlDocumentationEnricher(parser);
            var reflectedDerived = reflector.ReflectTypeMetadata(typeof(RenamedParameterImplementation<>));
            var reflectedClosed = reflector.ReflectTypeMetadata(typeof(ClosedGenericParameterImplementation));

            // Act
            var enrichedDerived = await enricher.EnrichAsync(typeof(RenamedParameterImplementation<>), reflectedDerived, new ExporterOptions());
            var enrichedClosed = await enricher.EnrichAsync(typeof(ClosedGenericParameterImplementation), reflectedClosed, new ExporterOptions());

            // Assert
            _ = enrichedDerived.SummaryDescription.Should().Be("Contract with original parameter names.");
            _ = enrichedDerived.GenericTypeParameterDetails.Should().ContainSingle()
                .Which.ParameterDescription.Should().Be("The contract item type.",
                    "because renamed generic type parameters on inheriting types must fall back to positional matching.");

            var formatMethod = enrichedDerived.Methods.Should().ContainSingle(
                method => method.MemberName == nameof(RenamedParameterImplementation<>.FormatPayload)).Subject;
            _ = formatMethod.SummaryDescription.Should().Be("Formats a payload pair.");
            _ = formatMethod.GenericTypeParameters.Should().ContainSingle()
                .Which.ParameterDescription.Should().Be("The contract result type.",
                    "because renamed method generic type parameters must inherit positionally via <inheritdoc/>.");
            _ = formatMethod.MemberParameters.Should().HaveCount(2);
            _ = formatMethod.MemberParameters[0].ParameterDescription.Should().Be("The first payload item.",
                "because renamed method parameters must inherit positionally via <inheritdoc/>.");
            _ = formatMethod.MemberParameters[1].ParameterDescription.Should().Be("The second payload result.");

            // Assert closed generic implementation inherits method & indexers (both concrete and `0 generic-substituted)
            var closedIntIndexer = enrichedClosed.Properties.Should().ContainSingle(
                property => property.MemberName == "this" && property.MemberParameters[0].ParameterTypeName == "Int32").Subject;
            _ = closedIntIndexer.SummaryDescription.Should().Be("Gets item by integer index.",
                "because indexer <inheritdoc/> must use the Item(...) XML key suffix rather than .this.");
            _ = closedIntIndexer.MemberParameters[0].ParameterDescription.Should().Be("Zero-based position.");

            var closedGenericIndexer = enrichedClosed.Properties.Should().ContainSingle(
                property => property.MemberName == "this" && property.MemberParameters[0].ParameterTypeName == "String").Subject;
            _ = closedGenericIndexer.SummaryDescription.Should().Be("Gets item by generic key.",
                "because indexers on closed generic interfaces must resolve `0 on the generic interface definition.");
            _ = closedGenericIndexer.MemberParameters[0].ParameterDescription.Should().Be("The generic key.");

            var closedFormatMethod = enrichedClosed.Methods.Should().ContainSingle(
                method => method.MemberName == nameof(ClosedGenericParameterImplementation.FormatPayload)).Subject;
            _ = closedFormatMethod.SummaryDescription.Should().Be("Formats a payload pair.",
                "because methods on closed generic interface implementations must resolve `0 on the generic interface definition.");
            _ = closedFormatMethod.MemberParameters[0].ParameterDescription.Should().Be("The first payload item.");
            _ = closedFormatMethod.MemberParameters[1].ParameterDescription.Should().Be("The second payload result.");
        }
        finally
        {
            if (File.Exists(tempXmlPath))
            {
                File.Delete(tempXmlPath);
            }
        }
    }
}
