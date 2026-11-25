namespace Documentation.Exporter.Core.Tests.TestCategories;

/// <summary>
/// Standardized xUnit test category trait names and values used across the test suite.
/// </summary>
public static class TestCategoryNames
{
    /// <summary>
    /// The xUnit trait key used to classify tests (<c>"Category"</c>).
    /// </summary>
    public const string TraitName = "Category";

    /// <summary>
    /// Category value marking pure in-memory or mocked unit tests (<c>"Unit"</c>).
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Category value marking tests touching disk I/O, DI container composition, real assembly loading, or full CLI/solution exports (<c>"Integration"</c>).
    /// </summary>
    public const string Integration = "Integration";
}
