using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Quality;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Quality;

[UnitTest]
public sealed class DocumentationQualityAnalyzerTests
{
    [Fact]
    public void AnalyzeWhenCompiledTypesNullShouldThrowArgumentNullException()
    {
        // Arrange
        var analyzer = new DocumentationQualityAnalyzer();

        // Act
        var act = () => analyzer.Analyze(null!);

        // Assert
        _ = act.Should().Throw<ArgumentNullException>("because compiledTypes cannot be null.");
    }

    [Fact]
    public void AnalyzeWhenCompiledTypesEmptyShouldReturnOneHundredPercentCoverageWithZeroDiagnostics()
    {
        // Arrange
        var analyzer = new DocumentationQualityAnalyzer();

        // Act
        var result = analyzer.Analyze([]);

        // Assert
        _ = result.TotalDocumentableSymbols.Should().Be(0);
        _ = result.DocumentedSymbols.Should().Be(0);
        _ = result.CoveragePercentage.Should().Be(100.0);
        _ = result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeShouldDetectMissingSummariesParametersTypeParametersAndNonVoidReturnsWhileIgnoringVoidAndTaskReturns()
    {
        // Arrange
        var analyzer = new DocumentationQualityAnalyzer();
        TypeDocumentationModel typeModel = new()
        {
            TypeName = "InnerService",
            DeclaringTypeChain = ["OuterScope"],
            NamespaceName = "Demo.Services",
            IsGenericType = true,
            GenericTypeParameterDetails =
            [
                new ParameterDocumentationModel { ParameterName = "TInput", ParameterTypeName = "TInput", ParameterDescription = string.Empty }
            ],
            SummaryDescription = string.Empty,
            Methods =
            [
                new MemberDocumentationModel
                {
                    MemberName = "Compute",
                    SummaryDescription = "Computes value.",
                    ReturnTypeName = "String",
                    ReturnValueDescription = string.Empty,
                    GenericTypeParameters =
                    [
                        new ParameterDocumentationModel { ParameterName = "TResult", ParameterTypeName = "TResult", ParameterDescription = string.Empty }
                    ],
                    MemberParameters =
                    [
                        new ParameterDocumentationModel { ParameterName = "payload", ParameterTypeName = "TInput", ParameterDescription = string.Empty }
                    ]
                },
                new MemberDocumentationModel
                {
                    MemberName = "RunAsync",
                    SummaryDescription = "Runs async work.",
                    ReturnTypeName = "Task",
                    ReturnValueDescription = string.Empty
                },
                new MemberDocumentationModel
                {
                    MemberName = "Reset",
                    SummaryDescription = "Resets state.",
                    ReturnTypeName = "Void",
                    ReturnValueDescription = string.Empty
                }
            ]
        };

        // Act
        var result = analyzer.Analyze([typeModel]);

        // Assert
        _ = result.TotalDocumentableSymbols.Should().Be(4);
        _ = result.DocumentedSymbols.Should().Be(3);
        _ = result.CoveragePercentage.Should().Be(75.0);

        _ = result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == "DOC001" &&
            diagnostic.SymbolName == "Demo.Services.OuterScope.InnerService");
        _ = result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == "DOC004" &&
            diagnostic.SymbolName == "Demo.Services.OuterScope.InnerService<TInput>");
        _ = result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == "DOC004" &&
            diagnostic.SymbolName == "Demo.Services.OuterScope.InnerService.Compute<TResult>");
        _ = result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == "DOC003" &&
            diagnostic.SymbolName == "Demo.Services.OuterScope.InnerService.Compute(payload)");
        _ = result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == "DOC005" &&
            diagnostic.SymbolName == "Demo.Services.OuterScope.InnerService.Compute");

        _ = result.Diagnostics.Should().NotContain(diagnostic =>
            diagnostic.Code == "DOC005" &&
            (diagnostic.SymbolName.Contains("RunAsync", StringComparison.Ordinal) ||
             diagnostic.SymbolName.Contains("Reset", StringComparison.Ordinal)),
            "because Task and Void methods do not return values.");
    }

    [Fact]
    public void AnalyzeShouldIgnoreNullModelsHandleGenericTypeNameFallbackAndSingleDeclaringType()
    {
        // Arrange
        var analyzer = new DocumentationQualityAnalyzer();
        TypeDocumentationModel typeWithGenericNamesOnly = new()
        {
            TypeName = "Repository",
            DeclaringTypeName = "DataScope",
            NamespaceName = "Demo.Data",
            SummaryDescription = "Repository summary.",
            GenericTypeParameters = ["TEntity", "  "]
        };

        // Act
        var result = analyzer.Analyze([null!, typeWithGenericNamesOnly]);

        // Assert
        _ = result.TotalDocumentableSymbols.Should().Be(1);
        _ = result.DocumentedSymbols.Should().Be(1);
        _ = result.CoveragePercentage.Should().Be(100.0);
        _ = result.Diagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Code == "DOC004" &&
            diagnostic.SymbolName == "Demo.Data.DataScope.Repository<TEntity>");
    }
}
