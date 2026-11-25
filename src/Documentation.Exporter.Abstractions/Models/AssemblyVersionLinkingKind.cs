namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Identifies the version-binding or constraint policy declared for a referenced assembly.
/// </summary>
public enum AssemblyVersionLinkingKind
{
    /// <summary>
    /// Resolved purely from the compiled assembly reference metadata without external project constraints.
    /// </summary>
    Compiled = 0,

    /// <summary>
    /// Linked directly to another project in the solution rather than a fixed version.
    /// </summary>
    ProjectLinked = 1,

    /// <summary>
    /// Pinned to an exact version (for example NuGet bracket notation <c>[1.2.3]</c> or <c>SpecificVersion=true</c>).
    /// </summary>
    Fixed = 2,

    /// <summary>
    /// Uses NuGet's default minimum inclusive version constraint (<c>&gt;= Version</c>).
    /// </summary>
    Minimum = 3,

    /// <summary>
    /// Uses an explicit NuGet version range expression (for example <c>[1.0.0, 2.0.0)</c>).
    /// </summary>
    Range = 4,

    /// <summary>
    /// Uses a wildcard floating NuGet version expression (for example <c>1.0.*</c>).
    /// </summary>
    Floating = 5,

    /// <summary>
    /// Supplied by the target .NET platform / framework pack.
    /// </summary>
    Framework = 6
}
