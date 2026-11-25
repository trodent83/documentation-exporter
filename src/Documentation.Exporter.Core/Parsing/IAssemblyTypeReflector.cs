using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Reflects on loaded runtime <see cref="Type"/> instances to construct rich <see cref="TypeDocumentationModel"/> metadata.
/// </summary>
public interface IAssemblyTypeReflector
{
    /// <summary>
    /// Builds a <see cref="TypeDocumentationModel"/> by reflecting on a target <see cref="Type"/>.
    /// </summary>
    /// <param name="targetType">The target type to reflect.</param>
    /// <returns>Populated type documentation model.</returns>
    TypeDocumentationModel ReflectTypeMetadata(Type targetType);
}
