using Xunit.Sdk;

namespace Documentation.Exporter.Core.Tests.TestCategories;

/// <summary>
/// Marks a test class or test method as a pure unit test (<c>Category=Unit</c>).
/// </summary>
[TraitDiscoverer(
    "Documentation.Exporter.Core.Tests.TestCategories.UnitTestTraitDiscoverer",
    "Documentation.Exporter.Core.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UnitTestAttribute : Attribute, ITraitAttribute;
