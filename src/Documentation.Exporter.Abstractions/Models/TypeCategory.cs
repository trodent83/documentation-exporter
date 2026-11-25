namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Identifies the C# kind of a documented type.
/// </summary>
public enum TypeCategory
{
    /// <summary>
    /// A reference class type.
    /// </summary>
    Class = 0,

    /// <summary>
    /// An interface contract type.
    /// </summary>
    Interface = 1,

    /// <summary>
    /// A value-type structure.
    /// </summary>
    Struct = 2,

    /// <summary>
    /// An enumeration value type.
    /// </summary>
    Enum = 3,

    /// <summary>
    /// A delegate signature type.
    /// </summary>
    Delegate = 4
}
