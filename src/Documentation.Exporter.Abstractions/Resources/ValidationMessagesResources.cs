using System.Globalization;
using System.Resources;

namespace Documentation.Exporter.Abstractions.Resources;

/// <summary>
/// Provides strongly-typed localized validation error messages for DataAnnotations attributes across domain models.
/// </summary>
public static class ValidationMessagesResources
{
    /// <summary>
    /// Gets or sets the culture to use for resource lookups, overriding <see cref="CultureInfo.CurrentUICulture"/> when specified.
    /// </summary>
    public static CultureInfo? ResourceCulture { get; set; }

    /// <summary>
    /// Gets the underlying <see cref="System.Resources.ResourceManager"/> instance.
    /// </summary>
    public static ResourceManager ResourceManager { get; } =
        new("Documentation.Exporter.Abstractions.Resources.ValidationMessagesResources", typeof(ValidationMessagesResources).Assembly);

    /// <summary>
    /// Gets the required validation message for <c>AssemblyFilePath</c>.
    /// </summary>
    public static string AssemblyFilePathRequired => GetResourceString(nameof(AssemblyFilePathRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>AssemblyFilePath</c>.
    /// </summary>
    public static string AssemblyFilePathMinLength => GetResourceString(nameof(AssemblyFilePathMinLength));

    /// <summary>
    /// Gets the required validation message for <c>XmlDocumentationFilePath</c>.
    /// </summary>
    public static string XmlDocumentationFilePathRequired => GetResourceString(nameof(XmlDocumentationFilePathRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>XmlDocumentationFilePath</c>.
    /// </summary>
    public static string XmlDocumentationFilePathMinLength => GetResourceString(nameof(XmlDocumentationFilePathMinLength));

    /// <summary>
    /// Gets the required validation message for <c>OutputDirectoryPath</c>.
    /// </summary>
    public static string OutputDirectoryPathRequired => GetResourceString(nameof(OutputDirectoryPathRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>OutputDirectoryPath</c>.
    /// </summary>
    public static string OutputDirectoryPathMinLength => GetResourceString(nameof(OutputDirectoryPathMinLength));

    /// <summary>
    /// Gets the required validation message for <c>MicrosoftDocsBaseUrl</c>.
    /// </summary>
    public static string MicrosoftDocsBaseUrlRequired => GetResourceString(nameof(MicrosoftDocsBaseUrlRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>MicrosoftDocsBaseUrl</c>.
    /// </summary>
    public static string MicrosoftDocsBaseUrlMinLength => GetResourceString(nameof(MicrosoftDocsBaseUrlMinLength));

    /// <summary>
    /// Gets the required validation message for <c>TargetName</c>.
    /// </summary>
    public static string TargetNameRequired => GetResourceString(nameof(TargetNameRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>TargetName</c>.
    /// </summary>
    public static string TargetNameMinLength => GetResourceString(nameof(TargetNameMinLength));

    /// <summary>
    /// Gets the required validation message for <c>CustomMarkdownContent</c>.
    /// </summary>
    public static string CustomMarkdownContentRequired => GetResourceString(nameof(CustomMarkdownContentRequired));

    /// <summary>
    /// Gets the required validation message for <c>MemberName</c>.
    /// </summary>
    public static string MemberNameRequired => GetResourceString(nameof(MemberNameRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>MemberName</c>.
    /// </summary>
    public static string MemberNameMinLength => GetResourceString(nameof(MemberNameMinLength));

    /// <summary>
    /// Gets the required validation message for <c>ParameterName</c>.
    /// </summary>
    public static string ParameterNameRequired => GetResourceString(nameof(ParameterNameRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>ParameterName</c>.
    /// </summary>
    public static string ParameterNameMinLength => GetResourceString(nameof(ParameterNameMinLength));

    /// <summary>
    /// Gets the required validation message for <c>ParameterTypeName</c>.
    /// </summary>
    public static string ParameterTypeNameRequired => GetResourceString(nameof(ParameterTypeNameRequired));

    /// <summary>
    /// Gets the required validation message for <c>TypeName</c>.
    /// </summary>
    public static string TypeNameRequired => GetResourceString(nameof(TypeNameRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>TypeName</c>.
    /// </summary>
    public static string TypeNameMinLength => GetResourceString(nameof(TypeNameMinLength));

    /// <summary>
    /// Gets the required validation message for <c>NamespaceName</c>.
    /// </summary>
    public static string NamespaceNameRequired => GetResourceString(nameof(NamespaceNameRequired));

    /// <summary>
    /// Gets the required validation message for <c>NamespacePrefix</c>.
    /// </summary>
    public static string NamespacePrefixRequired => GetResourceString(nameof(NamespacePrefixRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>NamespacePrefix</c>.
    /// </summary>
    public static string NamespacePrefixMinLength => GetResourceString(nameof(NamespacePrefixMinLength));

    /// <summary>
    /// Gets the required validation message for <c>UrlTemplate</c>.
    /// </summary>
    public static string UrlTemplateRequired => GetResourceString(nameof(UrlTemplateRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>UrlTemplate</c>.
    /// </summary>
    public static string UrlTemplateMinLength => GetResourceString(nameof(UrlTemplateMinLength));

    /// <summary>
    /// Gets the range validation message for <c>FailOnUndocumentedThreshold</c>.
    /// </summary>
    public static string FailOnUndocumentedThresholdRange => GetResourceString(nameof(FailOnUndocumentedThresholdRange));

    /// <summary>
    /// Gets the validation message for <c>DiagramFormat</c>.
    /// </summary>
    public static string DiagramFormatInvalid => GetResourceString(nameof(DiagramFormatInvalid));

    /// <summary>
    /// Gets the enum validation message for <c>TypeCategory</c>.
    /// </summary>
    public static string TypeCategoryInvalid => GetResourceString(nameof(TypeCategoryInvalid));

    /// <summary>
    /// Gets the enum validation message for <c>TargetAnchor</c>.
    /// </summary>
    public static string TargetAnchorInvalid => GetResourceString(nameof(TargetAnchorInvalid));

    /// <summary>
    /// Gets the enum validation message for <c>InjectionMode</c>.
    /// </summary>
    public static string InjectionModeInvalid => GetResourceString(nameof(InjectionModeInvalid));

    /// <summary>
    /// Gets the required validation message for <c>ReferenceName</c>.
    /// </summary>
    public static string ReferenceNameRequired => GetResourceString(nameof(ReferenceNameRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>ReferenceName</c>.
    /// </summary>
    public static string ReferenceNameMinLength => GetResourceString(nameof(ReferenceNameMinLength));

    /// <summary>
    /// Gets the enum validation message for <c>ReferenceKind</c>.
    /// </summary>
    public static string ReferenceKindInvalid => GetResourceString(nameof(ReferenceKindInvalid));

    /// <summary>
    /// Gets the enum validation message for <c>VersionLinkingKind</c>.
    /// </summary>
    public static string VersionLinkingKindInvalid => GetResourceString(nameof(VersionLinkingKindInvalid));

    /// <summary>
    /// Gets the required validation message for <c>SourceAssemblyName</c>.
    /// </summary>
    public static string DependencySourceNameRequired => GetResourceString(nameof(DependencySourceNameRequired));

    /// <summary>
    /// Gets the minimum length validation message for <c>SourceAssemblyName</c>.
    /// </summary>
    public static string DependencySourceNameMinLength => GetResourceString(nameof(DependencySourceNameMinLength));

    /// <summary>
    /// Gets the validation message for <c>ReportFormat</c>.
    /// </summary>
    public static string ReportFormatInvalid => GetResourceString(nameof(ReportFormatInvalid));

    /// <summary>
    /// Gets the validation message for <c>NavigationFormat</c>.
    /// </summary>
    public static string NavigationFormatInvalid => GetResourceString(nameof(NavigationFormatInvalid));

    /// <summary>
    /// Gets the required validation message for <c>DocumentationDiagnostic.Code</c>.
    /// </summary>
    public static string DiagnosticCodeRequired => GetResourceString(nameof(DiagnosticCodeRequired));

    /// <summary>
    /// Gets the required validation message for <c>DocumentationDiagnostic.Message</c>.
    /// </summary>
    public static string DiagnosticMessageRequired => GetResourceString(nameof(DiagnosticMessageRequired));

    /// <summary>
    /// Gets the enum validation message for <c>DocumentationDiagnostic.Severity</c>.
    /// </summary>
    public static string DiagnosticSeverityInvalid => GetResourceString(nameof(DiagnosticSeverityInvalid));

    /// <summary>
    /// Gets the enum validation message for <c>DocumentationDiagnostic.Kind</c>.
    /// </summary>
    public static string DiagnosticKindInvalid => GetResourceString(nameof(DiagnosticKindInvalid));

    /// <summary>
    /// Gets the range validation message for <c>DocumentationQualityReport.DocumentationCoveragePercentage</c>.
    /// </summary>
    public static string CoveragePercentageRange => GetResourceString(nameof(CoveragePercentageRange));

    /// <summary>
    /// Gets the required validation message for <c>PluginDescriptor.PluginName</c>.
    /// </summary>
    public static string PluginNameRequired => GetResourceString(nameof(PluginNameRequired));

    /// <summary>
    /// Gets the required validation message for <c>PluginDescriptor.AssemblyPath</c>.
    /// </summary>
    public static string PluginAssemblyPathRequired => GetResourceString(nameof(PluginAssemblyPathRequired));

    /// <summary>
    /// Gets the required validation message for <c>PluginDescriptor.PluginTypeFullName</c>.
    /// </summary>
    public static string PluginTypeFullNameRequired => GetResourceString(nameof(PluginTypeFullNameRequired));

    private static string GetResourceString(string resourceKey) =>
        ResourceManager.GetString(resourceKey, ResourceCulture ?? CultureInfo.CurrentUICulture)!;
}
