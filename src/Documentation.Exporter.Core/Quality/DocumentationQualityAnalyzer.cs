using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Default implementation of <see cref="IDocumentationQualityAnalyzer"/> producing deterministic
/// <c>DOC001</c>–<c>DOC005</c> quality diagnostics without altering legacy summary coverage percentage semantics.
/// </summary>
public sealed class DocumentationQualityAnalyzer : IDocumentationQualityAnalyzer
{
    private static readonly HashSet<string> VoidLikeReturnTypes =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "void",
        "System.Void",
        "Task",
        "ValueTask",
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.ValueTask"
    ];

    /// <inheritdoc />
    public DocumentationQualityAnalysisResult Analyze(IEnumerable<TypeDocumentationModel> compiledTypes)
    {
        ArgumentNullException.ThrowIfNull(compiledTypes);

        var totalSymbolCount = 0;
        var documentedSymbolCount = 0;
        List<DocumentationDiagnostic> diagnostics = [];

        foreach (var typeModel in compiledTypes)
        {
            if (typeModel == null)
            {
                continue;
            }

            var typeQualifiedName = BuildTypeQualifiedName(typeModel);
            totalSymbolCount++;

            if (!string.IsNullOrWhiteSpace(typeModel.SummaryDescription))
            {
                documentedSymbolCount++;
            }
            else
            {
                diagnostics.Add(new DocumentationDiagnostic
                {
                    Code = "DOC001",
                    Kind = DocumentationDiagnosticKind.MissingTypeSummary,
                    Severity = DocumentationDiagnosticSeverity.Warning,
                    SymbolName = typeQualifiedName,
                    Message = $"Type '{typeQualifiedName}' is missing an XML <summary> description."
                });
            }

            AnalyzeTypeParameters(diagnostics, typeQualifiedName, typeModel);
            AnalyzeAllMembers(
                diagnostics,
                typeQualifiedName,
                typeModel,
                ref totalSymbolCount,
                ref documentedSymbolCount);
        }

        var coveragePercentage = totalSymbolCount == 0
            ? 100.0
            : Math.Round(documentedSymbolCount / (double)totalSymbolCount * 100.0, 2);

        return new DocumentationQualityAnalysisResult
        {
            TotalDocumentableSymbols = totalSymbolCount,
            DocumentedSymbols = documentedSymbolCount,
            CoveragePercentage = coveragePercentage,
            Diagnostics = diagnostics
        };
    }

    private static void AnalyzeTypeParameters(
        List<DocumentationDiagnostic> diagnostics,
        string typeQualifiedName,
        TypeDocumentationModel typeModel)
    {
        if (typeModel.GenericTypeParameterDetails.Count > 0)
        {
            foreach (var typeParameter in typeModel.GenericTypeParameterDetails)
            {
                if (!string.IsNullOrWhiteSpace(typeParameter.ParameterDescription))
                {
                    continue;
                }

                diagnostics.Add(new DocumentationDiagnostic
                {
                    Code = "DOC004",
                    Kind = DocumentationDiagnosticKind.MissingTypeParameterDocumentation,
                    Severity = DocumentationDiagnosticSeverity.Warning,
                    SymbolName = $"{typeQualifiedName}<{typeParameter.ParameterName}>",
                    Message = $"Type parameter '{typeParameter.ParameterName}' on '{typeQualifiedName}' is missing an XML <typeparam> description."
                });
            }

            return;
        }

        foreach (var typeParameterName in typeModel.GenericTypeParameters)
        {
            if (string.IsNullOrWhiteSpace(typeParameterName))
            {
                continue;
            }

            diagnostics.Add(new DocumentationDiagnostic
            {
                Code = "DOC004",
                Kind = DocumentationDiagnosticKind.MissingTypeParameterDocumentation,
                Severity = DocumentationDiagnosticSeverity.Warning,
                SymbolName = $"{typeQualifiedName}<{typeParameterName}>",
                Message = $"Type parameter '{typeParameterName}' on '{typeQualifiedName}' is missing an XML <typeparam> description."
            });
        }
    }

    private static void AnalyzeAllMembers(
        List<DocumentationDiagnostic> diagnostics,
        string typeQualifiedName,
        TypeDocumentationModel typeModel,
        ref int totalSymbolCount,
        ref int documentedSymbolCount)
    {
        HashSet<MemberDocumentationModel> methodMembers = [with(ReferenceEqualityComparer.Instance)];
        foreach (var methodModel in typeModel.Methods)
        {
            _ = methodMembers.Add(methodModel);
        }

        HashSet<MemberDocumentationModel> parameterizedMembers = [with(ReferenceEqualityComparer.Instance)];
        foreach (var constructorModel in typeModel.Constructors)
        {
            _ = parameterizedMembers.Add(constructorModel);
        }

        foreach (var methodModel in typeModel.Methods)
        {
            _ = parameterizedMembers.Add(methodModel);
        }

        foreach (var propertyModel in typeModel.Properties)
        {
            if (propertyModel.MemberParameters.Count > 0)
            {
                _ = parameterizedMembers.Add(propertyModel);
            }
        }

        foreach (var memberModel in typeModel.EnumerateAllMembers())
        {
            totalSymbolCount++;
            var memberQualifiedName = $"{typeQualifiedName}.{memberModel.MemberName}";

            if (!string.IsNullOrWhiteSpace(memberModel.SummaryDescription))
            {
                documentedSymbolCount++;
            }
            else
            {
                diagnostics.Add(new DocumentationDiagnostic
                {
                    Code = "DOC002",
                    Kind = DocumentationDiagnosticKind.MissingMemberSummary,
                    Severity = DocumentationDiagnosticSeverity.Warning,
                    SymbolName = memberQualifiedName,
                    Message = $"Member '{memberQualifiedName}' is missing an XML <summary> description."
                });
            }

            if (parameterizedMembers.Contains(memberModel))
            {
                AnalyzeMemberParameters(diagnostics, memberQualifiedName, memberModel);
            }

            if (methodMembers.Contains(memberModel))
            {
                AnalyzeMethodGenericParameters(diagnostics, memberQualifiedName, memberModel);
                AnalyzeMethodReturnValue(diagnostics, memberQualifiedName, memberModel);
            }
        }
    }

    private static void AnalyzeMemberParameters(
        List<DocumentationDiagnostic> diagnostics,
        string memberQualifiedName,
        MemberDocumentationModel memberModel)
    {
        foreach (var parameterModel in memberModel.MemberParameters)
        {
            if (!string.IsNullOrWhiteSpace(parameterModel.ParameterDescription))
            {
                continue;
            }

            diagnostics.Add(new DocumentationDiagnostic
            {
                Code = "DOC003",
                Kind = DocumentationDiagnosticKind.MissingParameterDocumentation,
                Severity = DocumentationDiagnosticSeverity.Warning,
                SymbolName = $"{memberQualifiedName}({parameterModel.ParameterName})",
                Message = $"Parameter '{parameterModel.ParameterName}' on '{memberQualifiedName}' is missing an XML <param> description."
            });
        }
    }

    private static void AnalyzeMethodGenericParameters(
        List<DocumentationDiagnostic> diagnostics,
        string memberQualifiedName,
        MemberDocumentationModel methodModel)
    {
        foreach (var typeParameter in methodModel.GenericTypeParameters)
        {
            if (!string.IsNullOrWhiteSpace(typeParameter.ParameterDescription))
            {
                continue;
            }

            diagnostics.Add(new DocumentationDiagnostic
            {
                Code = "DOC004",
                Kind = DocumentationDiagnosticKind.MissingTypeParameterDocumentation,
                Severity = DocumentationDiagnosticSeverity.Warning,
                SymbolName = $"{memberQualifiedName}<{typeParameter.ParameterName}>",
                Message = $"Type parameter '{typeParameter.ParameterName}' on '{memberQualifiedName}' is missing an XML <typeparam> description."
            });
        }
    }

    private static void AnalyzeMethodReturnValue(
        List<DocumentationDiagnostic> diagnostics,
        string memberQualifiedName,
        MemberDocumentationModel methodModel)
    {
        if (string.IsNullOrWhiteSpace(methodModel.ReturnTypeName) ||
            !string.IsNullOrWhiteSpace(methodModel.ReturnValueDescription))
        {
            return;
        }

        var normalizedReturnType = methodModel.ReturnTypeName.Trim().TrimEnd('?');
        if (VoidLikeReturnTypes.Contains(normalizedReturnType))
        {
            return;
        }

        diagnostics.Add(new DocumentationDiagnostic
        {
            Code = "DOC005",
            Kind = DocumentationDiagnosticKind.MissingReturnDocumentation,
            Severity = DocumentationDiagnosticSeverity.Warning,
            SymbolName = memberQualifiedName,
            Message = $"Method '{memberQualifiedName}' returning '{methodModel.ReturnTypeName}' is missing an XML <returns> description."
        });
    }

    private static string BuildTypeQualifiedName(TypeDocumentationModel typeModel)
    {
        var nestedPart = typeModel.DeclaringTypeChain.Count > 0
            ? $"{string.Join('.', typeModel.DeclaringTypeChain)}.{typeModel.TypeName}"
            : !string.IsNullOrWhiteSpace(typeModel.DeclaringTypeName)
                ? $"{typeModel.DeclaringTypeName}.{typeModel.TypeName}"
                : typeModel.TypeName;

        return string.IsNullOrWhiteSpace(typeModel.NamespaceName)
            ? nestedPart
            : $"{typeModel.NamespaceName}.{nestedPart}";
    }
}
