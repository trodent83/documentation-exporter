using System.Globalization;
using System.Resources;

namespace Documentation.Exporter.Core.Resources;

/// <summary>
/// Provides strongly-typed localized strings for generated Markdown headings, metadata labels, and table column headers.
/// </summary>
public static class MarkdownGenerationResources
{
    /// <summary>
    /// Gets or sets the culture to use for resource lookups, overriding <see cref="CultureInfo.CurrentUICulture"/> when specified.
    /// </summary>
    public static CultureInfo? ResourceCulture { get; set; }

    /// <summary>
    /// Gets the underlying <see cref="System.Resources.ResourceManager"/> instance.
    /// </summary>
    public static ResourceManager ResourceManager { get; } =
        new("Documentation.Exporter.Core.Resources.MarkdownGenerationResources", typeof(MarkdownGenerationResources).Assembly);

    /// <summary>
    /// Gets the localized title for the root <c>index.md</c> page.
    /// </summary>
    public static string IndexPageTitle => GetResourceString(nameof(IndexPageTitle));

    /// <summary>
    /// Gets the localized welcome intro message for the root <c>index.md</c> page.
    /// </summary>
    public static string IndexWelcomeMessage => GetResourceString(nameof(IndexWelcomeMessage));

    /// <summary>
    /// Gets the localized label for <c>Namespace</c>.
    /// </summary>
    public static string NamespaceLabel => GetResourceString(nameof(NamespaceLabel));

    /// <summary>
    /// Gets the localized label for <c>Assembly</c>.
    /// </summary>
    public static string AssemblyLabel => GetResourceString(nameof(AssemblyLabel));

    /// <summary>
    /// Gets the localized table column header for <c>Type</c>.
    /// </summary>
    public static string TableTypeHeader => GetResourceString(nameof(TableTypeHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Category</c>.
    /// </summary>
    public static string TableCategoryHeader => GetResourceString(nameof(TableCategoryHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Description</c>.
    /// </summary>
    public static string TableDescriptionHeader => GetResourceString(nameof(TableDescriptionHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Name</c>.
    /// </summary>
    public static string TableNameHeader => GetResourceString(nameof(TableNameHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Exception</c>.
    /// </summary>
    public static string TableExceptionHeader => GetResourceString(nameof(TableExceptionHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Condition</c>.
    /// </summary>
    public static string TableConditionHeader => GetResourceString(nameof(TableConditionHeader));

    /// <summary>
    /// Gets the localized section heading for <c>Inheritance Hierarchy</c>.
    /// </summary>
    public static string InheritanceHierarchyHeading => GetResourceString(nameof(InheritanceHierarchyHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Implemented Interfaces</c>.
    /// </summary>
    public static string ImplementedInterfacesHeading => GetResourceString(nameof(ImplementedInterfacesHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Constructors</c>.
    /// </summary>
    public static string ConstructorsHeading => GetResourceString(nameof(ConstructorsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Properties</c>.
    /// </summary>
    public static string PropertiesHeading => GetResourceString(nameof(PropertiesHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Methods</c>.
    /// </summary>
    public static string MethodsHeading => GetResourceString(nameof(MethodsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Fields</c>.
    /// </summary>
    public static string FieldsHeading => GetResourceString(nameof(FieldsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Events</c>.
    /// </summary>
    public static string EventsHeading => GetResourceString(nameof(EventsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Parameters</c>.
    /// </summary>
    public static string ParametersHeading => GetResourceString(nameof(ParametersHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Returns</c>.
    /// </summary>
    public static string ReturnsHeading => GetResourceString(nameof(ReturnsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Exceptions</c>.
    /// </summary>
    public static string ExceptionsHeading => GetResourceString(nameof(ExceptionsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Examples</c>.
    /// </summary>
    public static string ExamplesHeading => GetResourceString(nameof(ExamplesHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Remarks</c>.
    /// </summary>
    public static string RemarksHeading => GetResourceString(nameof(RemarksHeading));

    /// <summary>
    /// Gets the localized section heading for <c>See Also</c>.
    /// </summary>
    public static string SeeAlsoHeading => GetResourceString(nameof(SeeAlsoHeading));

    /// <summary>
    /// Gets the localized table column header for <c>Access</c>.
    /// </summary>
    public static string TableAccessHeader => GetResourceString(nameof(TableAccessHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Constructor</c>.
    /// </summary>
    public static string TableConstructorHeader => GetResourceString(nameof(TableConstructorHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Abstract</c>.
    /// </summary>
    public static string TableAbstractHeader => GetResourceString(nameof(TableAbstractHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Sealed</c>.
    /// </summary>
    public static string TableSealedHeader => GetResourceString(nameof(TableSealedHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Static</c>.
    /// </summary>
    public static string TableStaticHeader => GetResourceString(nameof(TableStaticHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Generic</c>.
    /// </summary>
    public static string TableGenericHeader => GetResourceString(nameof(TableGenericHeader));

    /// <summary>
    /// Gets the localized affirmative label (<c>Yes</c>).
    /// </summary>
    public static string BooleanYes => GetResourceString(nameof(BooleanYes));

    /// <summary>
    /// Gets the localized negative label (<c>No</c>).
    /// </summary>
    public static string BooleanNo => GetResourceString(nameof(BooleanNo));

    /// <summary>
    /// Gets the localized table column header for <c>Visibility</c>.
    /// </summary>
    public static string TableVisibilityHeader => GetResourceString(nameof(TableVisibilityHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Return Type</c>.
    /// </summary>
    public static string TableReturnTypeHeader => GetResourceString(nameof(TableReturnTypeHeader));

    /// <summary>
    /// Gets the localized metadata label for <c>Declaring Type</c>.
    /// </summary>
    public static string DeclaringTypeLabel => GetResourceString(nameof(DeclaringTypeLabel));

    /// <summary>
    /// Gets the localized section heading for <c>Type Parameters</c>.
    /// </summary>
    public static string TypeParametersHeading => GetResourceString(nameof(TypeParametersHeading));

    /// <summary>
    /// Gets the localized table column header for <c>Constraints</c>.
    /// </summary>
    public static string TableConstraintsHeader => GetResourceString(nameof(TableConstraintsHeader));

    /// <summary>
    /// Gets the localized label when a generic parameter has no constraints (<c>None</c>).
    /// </summary>
    public static string NoConstraintsLabel => GetResourceString(nameof(NoConstraintsLabel));

    /// <summary>
    /// Gets the localized label for a <c>Primary Constructor</c>.
    /// </summary>
    public static string PrimaryConstructorLabel => GetResourceString(nameof(PrimaryConstructorLabel));

    /// <summary>
    /// Gets the localized warning label for an <c>[Obsolete]</c> element.
    /// </summary>
    public static string ObsoleteWarningTitle => GetResourceString(nameof(ObsoleteWarningTitle));

    /// <summary>
    /// Gets the default localized warning message when an <c>[Obsolete]</c> attribute has no custom message.
    /// </summary>
    public static string ObsoleteDefaultMessage => GetResourceString(nameof(ObsoleteDefaultMessage));

    /// <summary>
    /// Gets the localized metadata label for <c>Async</c>.
    /// </summary>
    public static string AsyncHeaderLabel => GetResourceString(nameof(AsyncHeaderLabel));

    /// <summary>
    /// Gets the localized badge text for a <see cref="CancellationToken"/> parameter (<c>Async Cancellation</c>).
    /// </summary>
    public static string AsyncCancellationBadge => GetResourceString(nameof(AsyncCancellationBadge));

    /// <summary>
    /// Gets the localized badge text for an <c>[EnumeratorCancellation]</c> parameter.
    /// </summary>
    public static string EnumeratorCancellationBadge => GetResourceString(nameof(EnumeratorCancellationBadge));

    /// <summary>
    /// Gets the localized table column header for parameter <c>Modifiers</c>.
    /// </summary>
    public static string TableModifiersHeader => GetResourceString(nameof(TableModifiersHeader));

    /// <summary>
    /// Gets the localized table column header for parameter <c>Default</c> values.
    /// </summary>
    public static string TableDefaultHeader => GetResourceString(nameof(TableDefaultHeader));

    /// <summary>
    /// Gets the localized badge text for an <c>Optional</c> parameter.
    /// </summary>
    public static string OptionalParameterBadge => GetResourceString(nameof(OptionalParameterBadge));

    /// <summary>
    /// Gets the localized badge text for a <c>Nullable</c> parameter.
    /// </summary>
    public static string NullableParameterBadge => GetResourceString(nameof(NullableParameterBadge));

    /// <summary>
    /// Gets the localized header label for an enumeration's underlying <c>Base Type</c>.
    /// </summary>
    public static string EnumBaseTypeHeader => GetResourceString(nameof(EnumBaseTypeHeader));

    /// <summary>
    /// Gets the localized section heading for enumeration <c>Values</c>.
    /// </summary>
    public static string ValuesHeading => GetResourceString(nameof(ValuesHeading));

    /// <summary>
    /// Gets the localized table column header for an enumeration member's numeric <c>Value</c>.
    /// </summary>
    public static string TableValueHeader => GetResourceString(nameof(TableValueHeader));

    /// <summary>
    /// Gets the localized section heading for <c>Derived Types</c>.
    /// </summary>
    public static string DerivedTypesHeading => GetResourceString(nameof(DerivedTypesHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Known Implementations</c>.
    /// </summary>
    public static string KnownImplementationsHeading => GetResourceString(nameof(KnownImplementationsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Extension Methods</c>.
    /// </summary>
    public static string ExtensionMethodsHeading => GetResourceString(nameof(ExtensionMethodsHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Type Diagram</c>.
    /// </summary>
    public static string TypeDiagramHeading => GetResourceString(nameof(TypeDiagramHeading));

    /// <summary>
    /// Gets the localized metadata label for <c>Attributes</c>.
    /// </summary>
    public static string AttributesHeaderLabel => GetResourceString(nameof(AttributesHeaderLabel));

    /// <summary>
    /// Gets the localized section heading for <c>Architecture Diagram</c>.
    /// </summary>
    public static string ArchitectureDiagramHeading => GetResourceString(nameof(ArchitectureDiagramHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Nested Types</c>.
    /// </summary>
    public static string NestedTypesHeading => GetResourceString(nameof(NestedTypesHeading));

    /// <summary>
    /// Gets the localized section heading for <c>Assembly References</c>.
    /// </summary>
    public static string AssemblyReferencesHeading => GetResourceString(nameof(AssemblyReferencesHeading));

    /// <summary>
    /// Gets the localized table column header for <c>Reference</c>.
    /// </summary>
    public static string TableReferenceHeader => GetResourceString(nameof(TableReferenceHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Kind</c>.
    /// </summary>
    public static string TableKindHeader => GetResourceString(nameof(TableKindHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Version</c>.
    /// </summary>
    public static string TableVersionHeader => GetResourceString(nameof(TableVersionHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Version Linking</c>.
    /// </summary>
    public static string TableVersionLinkingHeader => GetResourceString(nameof(TableVersionLinkingHeader));

    /// <summary>
    /// Gets the localized table column header for <c>Strong-Named</c>.
    /// </summary>
    public static string TableStrongNamedHeader => GetResourceString(nameof(TableStrongNamedHeader));

    /// <summary>
    /// Gets the localized label for project assembly references.
    /// </summary>
    public static string ReferenceKindProject => GetResourceString(nameof(ReferenceKindProject));

    /// <summary>
    /// Gets the localized label for NuGet package assembly references.
    /// </summary>
    public static string ReferenceKindPackage => GetResourceString(nameof(ReferenceKindPackage));

    /// <summary>
    /// Gets the localized label for direct file assembly references.
    /// </summary>
    public static string ReferenceKindAssembly => GetResourceString(nameof(ReferenceKindAssembly));

    /// <summary>
    /// Gets the localized label for .NET platform / framework assembly references.
    /// </summary>
    public static string ReferenceKindFramework => GetResourceString(nameof(ReferenceKindFramework));

    /// <summary>
    /// Gets the localized label for compiled assembly version linking without external constraint metadata.
    /// </summary>
    public static string VersionLinkingCompiled => GetResourceString(nameof(VersionLinkingCompiled));

    /// <summary>
    /// Gets the localized label for solution project-linked dependencies.
    /// </summary>
    public static string VersionLinkingProjectLinked => GetResourceString(nameof(VersionLinkingProjectLinked));

    /// <summary>
    /// Gets the localized label for fixed exact version linking.
    /// </summary>
    public static string VersionLinkingFixed => GetResourceString(nameof(VersionLinkingFixed));

    /// <summary>
    /// Gets the localized label for minimum version linking.
    /// </summary>
    public static string VersionLinkingMinimum => GetResourceString(nameof(VersionLinkingMinimum));

    /// <summary>
    /// Gets the localized label for bounded range version linking.
    /// </summary>
    public static string VersionLinkingRange => GetResourceString(nameof(VersionLinkingRange));

    /// <summary>
    /// Gets the localized label for floating wildcard version linking.
    /// </summary>
    public static string VersionLinkingFloating => GetResourceString(nameof(VersionLinkingFloating));

    /// <summary>
    /// Gets the localized label for platform framework version linking.
    /// </summary>
    public static string VersionLinkingFramework => GetResourceString(nameof(VersionLinkingFramework));

    /// <summary>
    /// Gets the localized section heading for <c>Assembly Dependency Tree</c>.
    /// </summary>
    public static string AssemblyDependencyTreeHeading => GetResourceString(nameof(AssemblyDependencyTreeHeading));

    /// <summary>
    /// Gets the localized legend text explaining strong-named vs. unsigned edge colors in assembly dependency tree diagrams.
    /// </summary>
    public static string AssemblyDependencyTreeLegend => GetResourceString(nameof(AssemblyDependencyTreeLegend));

    /// <summary>
    /// Gets the localized section heading for <c>Extension Properties</c>.
    /// </summary>
    public static string ExtensionPropertiesHeading => GetResourceString(nameof(ExtensionPropertiesHeading));

    /// <summary>
    /// Gets the localized metadata row label for <c>Record</c>.
    /// </summary>
    public static string TableRecordHeader => GetResourceString(nameof(TableRecordHeader));

    /// <summary>
    /// Gets the localized metadata row label for <c>ReadOnly</c>.
    /// </summary>
    public static string TableReadOnlyHeader => GetResourceString(nameof(TableReadOnlyHeader));

    /// <summary>
    /// Gets the localized metadata row label for <c>Ref Struct</c>.
    /// </summary>
    public static string TableRefStructHeader => GetResourceString(nameof(TableRefStructHeader));

    /// <summary>
    /// Gets the localized callout title for APIs decorated with <c>[Experimental]</c>.
    /// </summary>
    public static string ExperimentalWarningTitle => GetResourceString(nameof(ExperimentalWarningTitle));

    /// <summary>
    /// Gets the localized default warning body for APIs decorated with <c>[Experimental]</c>.
    /// </summary>
    public static string ExperimentalDefaultMessage => GetResourceString(nameof(ExperimentalDefaultMessage));

    /// <summary>
    /// Gets the localized badge label for the C# <c>scoped</c> parameter modifier.
    /// </summary>
    public static string ScopedModifierBadge => GetResourceString(nameof(ScopedModifierBadge));

    /// <summary>
    /// Gets the localized table column header for <c>Term</c>.
    /// </summary>
    public static string TableTermHeader => GetResourceString(nameof(TableTermHeader));

    /// <summary>
    /// Gets the localized document title for generated Markdown quality reports.
    /// </summary>
    public static string QualityReportTitle => GetResourceString(nameof(QualityReportTitle));

    private static string GetResourceString(string resourceKey) =>
        ResourceManager.GetString(resourceKey, ResourceCulture ?? CultureInfo.CurrentUICulture)!;
}
