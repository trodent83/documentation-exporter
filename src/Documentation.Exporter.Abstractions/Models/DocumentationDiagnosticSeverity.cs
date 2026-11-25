using System.Text.Json.Serialization;

namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Specifies the severity level of a documentation quality diagnostic.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentationDiagnosticSeverity
{
    /// <summary>
    /// Informational note highlighting optional improvements.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Non-blocking warning indicating incomplete documentation or non-strict link issues.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Blocking error detected during strict validation or quality gate evaluation.
    /// </summary>
    Error = 2
}
