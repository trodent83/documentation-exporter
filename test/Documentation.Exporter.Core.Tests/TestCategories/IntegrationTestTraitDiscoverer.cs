using Xunit.Abstractions;
using Xunit.Sdk;

namespace Documentation.Exporter.Core.Tests.TestCategories;

/// <summary>
/// Supplies the <c>Category=Integration</c> xUnit trait for <see cref="IntegrationTestAttribute"/>.
/// </summary>
public sealed class IntegrationTestTraitDiscoverer : ITraitDiscoverer
{
    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        ArgumentNullException.ThrowIfNull(traitAttribute);
        yield return new KeyValuePair<string, string>(TestCategoryNames.TraitName, TestCategoryNames.Integration);
    }
}
