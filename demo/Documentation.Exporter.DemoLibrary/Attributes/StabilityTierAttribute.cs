[assembly: Documentation.Exporter.DemoLibrary.Attributes.StabilityTier("Stable")]

namespace Documentation.Exporter.DemoLibrary.Attributes;

/// <summary>
/// Marks a domain service, model, assembly, constructor, member, event, parameter, return value, generic type parameter, or delegate with an architectural stability tier for documentation metadata reflection.
/// </summary>
/// <param name="tierName">The stability tier label (for example <c>"Stable"</c> or <c>"Preview"</c>).</param>
[AttributeUsage(
    AttributeTargets.Assembly |
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Enum |
    AttributeTargets.Delegate |
    AttributeTargets.Constructor |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field |
    AttributeTargets.Event |
    AttributeTargets.Parameter |
    AttributeTargets.ReturnValue |
    AttributeTargets.GenericParameter)]
public sealed class StabilityTierAttribute(string tierName) : Attribute
{
    /// <summary>
    /// Gets the stability tier label assigned to the decorated type.
    /// </summary>
    /// <value>A non-empty string representing the stability tier.</value>
    public string TierName { get; } = tierName;
}
