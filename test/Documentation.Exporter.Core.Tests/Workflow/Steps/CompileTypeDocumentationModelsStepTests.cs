using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Enrichment;
using System.ComponentModel;
using Documentation.Exporter.Core.Enrichment;
using Documentation.Exporter.Core.Generation;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

[UnitTest]
public sealed class CompileTypeDocumentationModelsStepTests
{
    private static AssemblyTypeReflector CreateReflector()
    {
        var linkResolver = new TypeLinkResolver();
        return new AssemblyTypeReflector(
            linkResolver,
            new CustomAttributeExtractor(linkResolver),
            new CSharpSignatureFormatter(),
            new MicrosoftLearnReferenceRegistry());
    }

    private static CustomAttributeExtractor CreateAttributeExtractor() =>
        new(new TypeLinkResolver());

    [Fact]
    public void StepNamePropertyShouldReturnExpectedName()
    {
        // Arrange
        var reflector = CreateReflector();
        var pipeline = new TypeDocumentationPipeline([]);
        var step = new CompileTypeDocumentationModelsStep(reflector, pipeline, new MemberFileNameFormatter(), CreateAttributeExtractor());

        // Assert
        _ = step.StepName.Should().Be("Compile Type Documentation Models");
    }

    [Fact]
    public async Task ExecuteAsyncWhenContextNullShouldThrowArgumentNullException()
    {
        // Arrange
        var reflector = CreateReflector();
        var pipeline = new TypeDocumentationPipeline([]);
        var fileNameFormatter = new MemberFileNameFormatter();
        var attributeExtractor = CreateAttributeExtractor();
        var step = new CompileTypeDocumentationModelsStep(reflector, pipeline, fileNameFormatter, attributeExtractor);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new CompileTypeDocumentationModelsStep(null!, pipeline, fileNameFormatter, attributeExtractor));
        _ = Assert.Throws<ArgumentNullException>(() => new CompileTypeDocumentationModelsStep(reflector, null!, fileNameFormatter, attributeExtractor));
        _ = Assert.Throws<ArgumentNullException>(() => new CompileTypeDocumentationModelsStep(reflector, pipeline, null!, attributeExtractor));
        _ = Assert.Throws<ArgumentNullException>(() => new CompileTypeDocumentationModelsStep(reflector, pipeline, fileNameFormatter, null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => step.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsyncWhenEmptyOrNullMapShouldReturnFalse()
    {
        // Arrange
        var reflector = CreateReflector();
        var pipeline = new TypeDocumentationPipeline([]);
        var step = new CompileTypeDocumentationModelsStep(reflector, pipeline, new MemberFileNameFormatter(), CreateAttributeExtractor());
        var context = new ExportWorkflowContext(new ExporterOptions())
        {
            ExportedTypesMap = null
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsyncWithExportedTypesShouldPopulateCompiledTypeModels()
    {
        // Arrange
        var reflector = CreateReflector();
        var pipeline = new TypeDocumentationPipeline([]);
        var step = new CompileTypeDocumentationModelsStep(reflector, pipeline, new MemberFileNameFormatter(), CreateAttributeExtractor());
        var context = new ExportWorkflowContext(new ExporterOptions())
        {
            ExportedTypesMap = new Dictionary<string, Type>
            {
                [typeof(ExportResult).FullName!] = typeof(ExportResult)
            }
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue();
        _ = context.CompiledTypeModels.Should().HaveCount(1);
        _ = context.CompiledTypeModels.First().TypeName.Should().Be(nameof(ExportResult));
        _ = context.AssemblyAttributesByAssemblyName.Should().ContainKey(typeof(ExportResult).Assembly.GetName().Name!,
            "because CompileTypeDocumentationModelsStep must populate assembly attributes per exported assembly.");
    }

    public sealed class SampleTypeWithHiddenMembers
    {
        public string VisibleProperty { get; set; } = string.Empty;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string HiddenProperty { get; set; } = string.Empty;

        public string XmlExcludedMethod() => VisibleProperty;
    }

    private sealed class XmlExcludeEnricher : ITypeDocumentationEnricher
    {
        public int Order => 1;

        public Task<TypeDocumentationModel> EnrichAsync(
            Type targetType,
            TypeDocumentationModel typeModel,
            ExporterOptions options,
            CancellationToken cancellationToken = default) =>
            targetType == typeof(ExportResult)
                ? Task.FromResult(typeModel with { IsExcludedByXmlDoc = true })
                : Task.FromResult(typeModel with
                {
                    Methods =
                    [
                        .. typeModel.Methods.Select(method => method.MemberName == nameof(SampleTypeWithHiddenMembers.XmlExcludedMethod)
                            ? method with { IsExcludedByXmlDoc = true }
                            : method)
                    ]
                });
    }

    [Fact]
    public async Task ExecuteAsyncShouldExcludeXmlExcludedAndEditorBrowsableNeverMembers()
    {
        // Arrange
        var reflector = CreateReflector();
        var pipeline = new TypeDocumentationPipeline([new XmlExcludeEnricher()]);
        var step = new CompileTypeDocumentationModelsStep(reflector, pipeline, new MemberFileNameFormatter(), CreateAttributeExtractor());
        var context = new ExportWorkflowContext(new ExporterOptions { ExcludeEditorBrowsableNever = true })
        {
            ExportedTypesMap = new Dictionary<string, Type>
            {
                [typeof(ExportResult).FullName!] = typeof(ExportResult),
                [typeof(SampleTypeWithHiddenMembers).FullName!] = typeof(SampleTypeWithHiddenMembers)
            }
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because non-excluded types remain.");
        _ = context.CompiledTypeModels.Should().ContainSingle(model => model.TypeName == nameof(SampleTypeWithHiddenMembers),
            "because ExportResult was marked IsExcludedByXmlDoc=true.");
        var compiledSample = context.CompiledTypeModels.Single();
        _ = compiledSample.Properties.Should().ContainSingle(property => property.MemberName == nameof(SampleTypeWithHiddenMembers.VisibleProperty),
            "because HiddenProperty is marked [EditorBrowsable(EditorBrowsableState.Never)].");
        _ = compiledSample.Methods.Should().NotContain(method => method.MemberName == nameof(SampleTypeWithHiddenMembers.XmlExcludedMethod),
            "because XmlExcludedMethod was marked IsExcludedByXmlDoc=true.");
    }

    [Fact]
    public async Task ExecuteAsyncShouldAttachCSharp14ExtensionMethodsAndExtensionPropertiesToClassesAndEnums()
    {
        // Arrange
        var reflector = CreateReflector();
        var pipeline = new TypeDocumentationPipeline([]);
        var step = new CompileTypeDocumentationModelsStep(reflector, pipeline, new MemberFileNameFormatter(), CreateAttributeExtractor());

        var context = new ExportWorkflowContext(new ExporterOptions())
        {
            ExportedTypesMap = new Dictionary<string, Type>
            {
                [typeof(SampleRichTargetClass).FullName!] = typeof(SampleRichTargetClass),
                [typeof(SampleTargetStatusEnum).FullName!] = typeof(SampleTargetStatusEnum),
                [typeof(SampleModernStepExtensions).FullName!] = typeof(SampleModernStepExtensions)
            }
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue();
        var classModel = context.CompiledTypeModels.Should().ContainSingle(model => model.TypeName == nameof(SampleRichTargetClass)).Subject;
        _ = classModel.ExtensionProperties.Should().ContainSingle(property => property.MemberName == "LabelLength");
        _ = classModel.ExtensionMethods.Should().ContainSingle(method => method.MemberName == "FormatExtended");

        var enumModel = context.CompiledTypeModels.Should().ContainSingle(model => model.TypeName == nameof(SampleTargetStatusEnum)).Subject;
        _ = enumModel.ExtensionProperties.Should().ContainSingle(property => property.MemberName == "IsTerminalState");
        _ = enumModel.ExtensionMethods.Should().ContainSingle(method => method.MemberName == "ToDisplayLabel");
    }

    internal sealed class SampleRichTargetClass
    {
        public string Name { get; set; } = string.Empty;
    }

    internal enum SampleTargetStatusEnum
    {
        Pending = 0,
        Settled = 1
    }
}

internal static class SampleModernStepExtensions
{
    extension(CompileTypeDocumentationModelsStepTests.SampleRichTargetClass instance)
    {
        public int LabelLength => instance.Name.Length;
        public string FormatExtended() => instance.Name;
    }

    extension(CompileTypeDocumentationModelsStepTests.SampleTargetStatusEnum statusValue)
    {
        public bool IsTerminalState => statusValue == CompileTypeDocumentationModelsStepTests.SampleTargetStatusEnum.Settled;
        public string ToDisplayLabel() => statusValue.ToString();
    }
}
