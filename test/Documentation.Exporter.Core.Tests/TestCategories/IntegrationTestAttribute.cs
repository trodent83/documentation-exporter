using Xunit.Sdk;

namespace Documentation.Exporter.Core.Tests.TestCategories;

/// <summary>
/// Marks a test class or test method as an integration test (<c>Category=Integration</c>).
/// </summary>
[TraitDiscoverer(
    "Documentation.Exporter.Core.Tests.TestCategories.IntegrationTestTraitDiscoverer",
    "Documentation.Exporter.Core.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class IntegrationTestAttribute : Attribute, ITraitAttribute;
