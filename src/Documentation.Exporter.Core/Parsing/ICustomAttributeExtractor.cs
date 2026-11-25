using System.Reflection;

namespace Documentation.Exporter.Core.Parsing;

/// <summary>
/// Extracts applied custom attributes, obsolescence metadata, editor-browsability flags, and caller-information attributes from reflected metadata.
/// </summary>
public interface ICustomAttributeExtractor
{
    /// <summary>
    /// Formats custom attributes applied to the specified type or member with resolved documentation links.
    /// </summary>
    /// <param name="memberInfo">The reflected type or member metadata.</param>
    /// <returns>Formatted custom attribute entries.</returns>
    List<string> BuildAppliedAttributes(MemberInfo memberInfo);

    /// <summary>
    /// Formats custom attributes applied to the specified method or constructor parameter (or return parameter) with resolved documentation links.
    /// </summary>
    /// <param name="parameterInfo">The reflected parameter metadata.</param>
    /// <returns>Formatted custom attribute entries.</returns>
    List<string> BuildAppliedAttributes(ParameterInfo parameterInfo);

    /// <summary>
    /// Formats meaningful custom attributes applied at the assembly level with resolved documentation links.
    /// </summary>
    /// <param name="assembly">The reflected runtime assembly.</param>
    /// <returns>Formatted custom attribute entries.</returns>
    List<string> BuildAppliedAttributes(Assembly assembly);

    /// <summary>
    /// Determines whether the specified member or type is decorated with <c>[EditorBrowsable(EditorBrowsableState.Never)]</c>.
    /// </summary>
    /// <param name="memberInfo">The reflected type or member metadata.</param>
    /// <returns><see langword="true"/> if hidden from editor browsing; otherwise <see langword="false"/>.</returns>
    bool IsEditorBrowsableNever(MemberInfo memberInfo);

    /// <summary>
    /// Extracts obsolescence status, custom message, compiler error flag, diagnostic identifier, and documentation URL format from <see cref="ObsoleteAttribute"/> if present.
    /// </summary>
    /// <param name="memberInfo">The reflected type or member metadata.</param>
    /// <returns>A tuple describing obsolescence state, message, compiler error flag, diagnostic identifier, and URL format template.</returns>
    (bool IsObsolete, string Message, bool IsError, string DiagnosticId, string UrlFormat) ExtractObsoleteMetadata(MemberInfo memberInfo);

    /// <summary>
    /// Extracts experimental status, diagnostic identifier, and documentation URL format from <c>[Experimental]</c> if present.
    /// </summary>
    /// <param name="memberInfo">The reflected type or member metadata.</param>
    /// <returns>A tuple describing experimental status, diagnostic identifier, and optional URL format template.</returns>
    (bool IsExperimental, string DiagnosticId, string UrlFormat) ExtractExperimentalMetadata(MemberInfo memberInfo);

    /// <summary>
    /// Extracts caller-information attribute syntax (such as <c>CallerMemberName</c> or <c>CallerArgumentExpression</c>) from a parameter.
    /// </summary>
    /// <param name="parameterInfo">The reflected parameter metadata.</param>
    /// <returns>Formatted attribute string, or empty when none is applied.</returns>
    string ExtractCallerInformationAttribute(ParameterInfo parameterInfo);
}
