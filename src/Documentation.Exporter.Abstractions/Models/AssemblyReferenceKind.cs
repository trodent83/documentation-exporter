namespace Documentation.Exporter.Abstractions.Models;

/// <summary>
/// Categorizes how a referenced assembly is included by the target assembly.
/// </summary>
public enum AssemblyReferenceKind
{
    /// <summary>
    /// Referenced as a sibling project within the solution or multi-project build.
    /// </summary>
    Project = 0,

    /// <summary>
    /// Referenced via a NuGet package dependency.
    /// </summary>
    Package = 1,

    /// <summary>
    /// Referenced directly via an explicit assembly reference or third-party DLL.
    /// </summary>
    Assembly = 2,

    /// <summary>
    /// Provided by the .NET Framework / BCL runtime platform.
    /// </summary>
    Framework = 3
}
