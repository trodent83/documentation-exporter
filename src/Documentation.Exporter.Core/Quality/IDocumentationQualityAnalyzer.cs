using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Quality;

/// <summary>
/// Analyzes compiled type documentation models to compute summary coverage and record granular undocumented-API diagnostics.
/// </summary>
public interface IDocumentationQualityAnalyzer
{
    /// <summary>
    /// Evaluates <paramref name="compiledTypes"/> for missing type summaries (<c>DOC001</c>), member summaries (<c>DOC002</c>),
    /// parameters (<c>DOC003</c>), generic type parameters (<c>DOC004</c>), and non-void method returns (<c>DOC005</c>).
    /// </summary>
    /// <param name="compiledTypes">The compiled and enriched type documentation models.</param>
    /// <returns>An aggregated analysis result containing summary coverage metrics and diagnostics.</returns>
    DocumentationQualityAnalysisResult Analyze(IEnumerable<TypeDocumentationModel> compiledTypes);
}
