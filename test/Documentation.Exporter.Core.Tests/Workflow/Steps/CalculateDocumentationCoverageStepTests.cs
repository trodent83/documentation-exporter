using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Workflow.Steps;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Workflow.Steps;

/// <summary>
/// Unit tests for <see cref="CalculateDocumentationCoverageStep"/>.
/// </summary>
[UnitTest]
public sealed class CalculateDocumentationCoverageStepTests
{
    [Fact]
    public void StepNameShouldMatchExpectedResourceString()
    {
        // Arrange
        var step = new CalculateDocumentationCoverageStep(new DocumentationQualityAnalyzer());

        // Act & Assert
        _ = step.StepName.Should().Be("Calculate Documentation Coverage", "because the step name must match localized execution message resources.");
        _ = FluentActions.Invoking(() => new CalculateDocumentationCoverageStep(null!))
            .Should().Throw<ArgumentNullException>("because qualityAnalyzer cannot be null.");
    }

    [Fact]
    public async Task ExecuteAsyncWithNullContextShouldThrowArgumentNullException()
    {
        // Arrange
        var step = new CalculateDocumentationCoverageStep(new DocumentationQualityAnalyzer());

        // Act
        var nullAction = () => step.ExecuteAsync(null!);

        // Assert
        _ = await nullAction.Should().ThrowAsync<ArgumentNullException>("because context cannot be null.");
    }

    [Fact]
    public async Task ExecuteAsyncWithFullyDocumentedTypesAndMembersShouldCalculateOneHundredPercent()
    {
        // Arrange
        var step = new CalculateDocumentationCoverageStep(new DocumentationQualityAnalyzer());
        ExporterOptions options = new() { OutputDirectoryPath = "output" };
        ExportWorkflowContext context = new(options);

        TypeDocumentationModel fullyDocumentedModel = new()
        {
            TypeName = "SampleService",
            SummaryDescription = "A fully documented sample service.",
            Constructors =
            [
                new MemberDocumentationModel
                {
                    MemberName = "SampleService",
                    SummaryDescription = "Initializes a new instance."
                }
            ],
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "ProcessData",
                    SummaryDescription = "Processes the provided data."
                }
            ],
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "TimeoutSeconds",
                    SummaryDescription = "Gets the timeout in seconds."
                }
            ]
        };

        context.CompiledTypeModels.Add(fullyDocumentedModel);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because documentation calculation succeeded.");
        _ = context.DocumentationCoveragePercentage.Should().Be(100.0, "because all 4 items (type, constructor, method, property) have summaries.");
        _ = context.TotalDocumentableSymbols.Should().Be(4);
        _ = context.DocumentedSymbols.Should().Be(4);
        _ = context.Diagnostics.Should().BeEmpty("because all documentable elements are documented.");
        _ = context.HasFailed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsyncWithPartiallyDocumentedTypesShouldCalculateAccuratePercentageAndRecordDiagnostics()
    {
        // Arrange
        var step = new CalculateDocumentationCoverageStep(new DocumentationQualityAnalyzer());
        ExporterOptions options = new() { OutputDirectoryPath = "output" };
        ExportWorkflowContext context = new(options);

        // 2 items total: 1 documented type, 1 undocumented method => 50%
        TypeDocumentationModel partialModel = new()
        {
            TypeName = "PartialService",
            NamespaceName = "Demo.Services",
            SummaryDescription = "Has type summary.",
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "UndocumentedMethod",
                    SummaryDescription = string.Empty,
                    ReturnTypeName = "String",
                    MemberParameters =
                    [
                        new ParameterDocumentationModel { ParameterName = "input", ParameterTypeName = "String" }
                    ]
                }
            ]
        };

        context.CompiledTypeModels.Add(partialModel);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeTrue("because no threshold was configured to fail on.");
        _ = context.DocumentationCoveragePercentage.Should().Be(50.0, "because exactly 1 of 2 items is documented.");
        _ = context.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "DOC002" && diagnostic.SymbolName == "Demo.Services.PartialService.UndocumentedMethod");
        _ = context.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "DOC003" && diagnostic.SymbolName == "Demo.Services.PartialService.UndocumentedMethod(input)");
        _ = context.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "DOC005" && diagnostic.SymbolName == "Demo.Services.PartialService.UndocumentedMethod");
    }

    [Fact]
    public async Task ExecuteAsyncWhenCoverageBelowThresholdShouldReturnFalseAndSetHasFailed()
    {
        // Arrange
        var step = new CalculateDocumentationCoverageStep(new DocumentationQualityAnalyzer());
        ExporterOptions options = new()
        {
            OutputDirectoryPath = "output",
            FailOnUndocumentedThreshold = 80.0
        };
        ExportWorkflowContext context = new(options);

        // 2 items total: 1 documented, 1 undocumented => 50% < 80.0% threshold
        TypeDocumentationModel model = new()
        {
            TypeName = "UnderDocumentedClass",
            SummaryDescription = "Class summary.",
            Properties =
            [
                new MemberDocumentationModel
                {
                    MemberName = "UndocumentedProperty",
                    SummaryDescription = string.Empty
                }
            ]
        };

        context.CompiledTypeModels.Add(model);

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        _ = result.Should().BeFalse("because 50% coverage is below the 80% threshold.");
        _ = context.HasFailed.Should().BeTrue("because failing threshold must mark the context as failed.");
        _ = context.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "DOC002");
    }
}
