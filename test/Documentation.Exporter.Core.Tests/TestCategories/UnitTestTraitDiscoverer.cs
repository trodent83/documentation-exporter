using Xunit.Abstractions;
using Xunit.Sdk;

namespace Documentation.Exporter.Core.Tests.TestCategories;

/// <summary>
/// Supplies the <c>Category=Unit</c> xUnit trait for <see cref="UnitTestAttribute"/>.
/// </summary>
public sealed class UnitTestTraitDiscoverer : ITraitDiscoverer
{
    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        ArgumentNullException.ThrowIfNull(traitAttribute);
        yield return new KeyValuePair<string, string>(TestCategoryNames.TraitName, TestCategoryNames.Unit);
    }
}
