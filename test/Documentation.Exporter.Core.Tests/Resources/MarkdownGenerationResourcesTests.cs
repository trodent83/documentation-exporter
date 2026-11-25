using System.Globalization;
using Documentation.Exporter.Core.Resources;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Resources;

[UnitTest]
public sealed class MarkdownGenerationResourcesTests
{
    [Fact]
    public void ResourceManagerShouldNotBeNull() =>
        _ = MarkdownGenerationResources.ResourceManager.Should().NotBeNull("because the embedded MarkdownGenerationResources manifest must be accessible.");

    [Fact]
    public void ResourceCultureWhenSetAndResetShouldPersistProperly()
    {
        try
        {
            // Arrange
            var targetCulture = new CultureInfo("en-US");

            // Act
            MarkdownGenerationResources.ResourceCulture = targetCulture;

            // Assert
            _ = MarkdownGenerationResources.ResourceCulture.Should().Be(targetCulture, "because ResourceCulture should store the assigned CultureInfo.");
            _ = MarkdownGenerationResources.IndexPageTitle.Should().Be("API Reference Documentation");
        }
        finally
        {
            MarkdownGenerationResources.ResourceCulture = null;
        }
    }

    [Fact]
    public void AllMarkdownGenerationResourcePropertiesShouldReturnExpectedNeutralStrings()
    {
        // Act & Assert
        _ = MarkdownGenerationResources.IndexPageTitle.Should().Be("API Reference Documentation");
        _ = MarkdownGenerationResources.IndexWelcomeMessage.Should().Be("Welcome to the API reference documentation.");
        _ = MarkdownGenerationResources.NamespaceLabel.Should().Be("Namespace");
        _ = MarkdownGenerationResources.AssemblyLabel.Should().Be("Assembly");
        _ = MarkdownGenerationResources.TableTypeHeader.Should().Be("Type");
        _ = MarkdownGenerationResources.TableCategoryHeader.Should().Be("Category");
        _ = MarkdownGenerationResources.TableDescriptionHeader.Should().Be("Description");
        _ = MarkdownGenerationResources.TableNameHeader.Should().Be("Name");
        _ = MarkdownGenerationResources.TableExceptionHeader.Should().Be("Exception");
        _ = MarkdownGenerationResources.TableConditionHeader.Should().Be("Condition");
        _ = MarkdownGenerationResources.InheritanceHierarchyHeading.Should().Be("Inheritance Hierarchy");
        _ = MarkdownGenerationResources.ImplementedInterfacesHeading.Should().Be("Implemented Interfaces");
        _ = MarkdownGenerationResources.ConstructorsHeading.Should().Be("Constructors");
        _ = MarkdownGenerationResources.PropertiesHeading.Should().Be("Properties");
        _ = MarkdownGenerationResources.MethodsHeading.Should().Be("Methods");
        _ = MarkdownGenerationResources.FieldsHeading.Should().Be("Fields");
        _ = MarkdownGenerationResources.EventsHeading.Should().Be("Events");
        _ = MarkdownGenerationResources.ParametersHeading.Should().Be("Parameters");
        _ = MarkdownGenerationResources.ReturnsHeading.Should().Be("Returns");
        _ = MarkdownGenerationResources.ExceptionsHeading.Should().Be("Exceptions");
        _ = MarkdownGenerationResources.ExamplesHeading.Should().Be("Examples");
        _ = MarkdownGenerationResources.RemarksHeading.Should().Be("Remarks");
        _ = MarkdownGenerationResources.SeeAlsoHeading.Should().Be("See Also");
        _ = MarkdownGenerationResources.TableAccessHeader.Should().Be("Access");
        _ = MarkdownGenerationResources.TableConstructorHeader.Should().Be("Constructor");
        _ = MarkdownGenerationResources.TableAbstractHeader.Should().Be("Abstract");
        _ = MarkdownGenerationResources.TableSealedHeader.Should().Be("Sealed");
        _ = MarkdownGenerationResources.TableStaticHeader.Should().Be("Static");
        _ = MarkdownGenerationResources.TableGenericHeader.Should().Be("Generic");
        _ = MarkdownGenerationResources.BooleanYes.Should().Be("Yes");
        _ = MarkdownGenerationResources.BooleanNo.Should().Be("No");
        _ = MarkdownGenerationResources.TableVisibilityHeader.Should().Be("Visibility");
        _ = MarkdownGenerationResources.TableReturnTypeHeader.Should().Be("Return Type");
        _ = MarkdownGenerationResources.DeclaringTypeLabel.Should().Be("Declaring Type");
        _ = MarkdownGenerationResources.TypeParametersHeading.Should().Be("Type Parameters");
        _ = MarkdownGenerationResources.TableConstraintsHeader.Should().Be("Constraints");
        _ = MarkdownGenerationResources.NoConstraintsLabel.Should().Be("None");
        _ = MarkdownGenerationResources.PrimaryConstructorLabel.Should().Be("Primary Constructor");
        _ = MarkdownGenerationResources.ObsoleteWarningTitle.Should().Be("Obsolete");
        _ = MarkdownGenerationResources.ObsoleteDefaultMessage.Should().Be("This API is obsolete and should no longer be used.");
        _ = MarkdownGenerationResources.AsyncHeaderLabel.Should().Be("Async");
        _ = MarkdownGenerationResources.AsyncCancellationBadge.Should().Be("Async Cancellation");
        _ = MarkdownGenerationResources.EnumeratorCancellationBadge.Should().Be("EnumeratorCancellation");
        _ = MarkdownGenerationResources.TableModifiersHeader.Should().Be("Modifiers");
        _ = MarkdownGenerationResources.TableDefaultHeader.Should().Be("Default");
        _ = MarkdownGenerationResources.OptionalParameterBadge.Should().Be("Optional");
        _ = MarkdownGenerationResources.NullableParameterBadge.Should().Be("Nullable");
        _ = MarkdownGenerationResources.EnumBaseTypeHeader.Should().Be("Base Type");
        _ = MarkdownGenerationResources.ValuesHeading.Should().Be("Values");
        _ = MarkdownGenerationResources.TableValueHeader.Should().Be("Value");
        _ = MarkdownGenerationResources.DerivedTypesHeading.Should().Be("Derived Types");
        _ = MarkdownGenerationResources.KnownImplementationsHeading.Should().Be("Known Implementations");
        _ = MarkdownGenerationResources.ExtensionMethodsHeading.Should().Be("Extension Methods");
        _ = MarkdownGenerationResources.TypeDiagramHeading.Should().Be("Type Diagram");
        _ = MarkdownGenerationResources.AttributesHeaderLabel.Should().Be("Attributes");
        _ = MarkdownGenerationResources.ArchitectureDiagramHeading.Should().Be("Architecture Diagram");
        _ = MarkdownGenerationResources.NestedTypesHeading.Should().Be("Nested Types");
        _ = MarkdownGenerationResources.AssemblyReferencesHeading.Should().Be("Assembly References");
        _ = MarkdownGenerationResources.TableReferenceHeader.Should().Be("Reference");
        _ = MarkdownGenerationResources.TableKindHeader.Should().Be("Kind");
        _ = MarkdownGenerationResources.TableVersionHeader.Should().Be("Version");
        _ = MarkdownGenerationResources.TableVersionLinkingHeader.Should().Be("Version Linking");
        _ = MarkdownGenerationResources.TableStrongNamedHeader.Should().Be("Strong-Named");
        _ = MarkdownGenerationResources.ReferenceKindProject.Should().Be("Project");
        _ = MarkdownGenerationResources.ReferenceKindPackage.Should().Be("Package");
        _ = MarkdownGenerationResources.ReferenceKindAssembly.Should().Be("Assembly");
        _ = MarkdownGenerationResources.ReferenceKindFramework.Should().Be("Framework");
        _ = MarkdownGenerationResources.VersionLinkingCompiled.Should().Be("Compiled");
        _ = MarkdownGenerationResources.VersionLinkingProjectLinked.Should().Be("Linked (Project)");
        _ = MarkdownGenerationResources.VersionLinkingFixed.Should().Be("Fixed");
        _ = MarkdownGenerationResources.VersionLinkingMinimum.Should().Be("Minimum");
        _ = MarkdownGenerationResources.VersionLinkingRange.Should().Be("Range");
        _ = MarkdownGenerationResources.VersionLinkingFloating.Should().Be("Floating");
        _ = MarkdownGenerationResources.VersionLinkingFramework.Should().Be("Framework");
        _ = MarkdownGenerationResources.AssemblyDependencyTreeHeading.Should().Be("Assembly Dependency Tree");
        _ = MarkdownGenerationResources.AssemblyDependencyTreeLegend.Should().Be(
            "Legend: solid green link = strong-named assembly; dashed gray link = unsigned assembly.");
        _ = MarkdownGenerationResources.ExtensionPropertiesHeading.Should().Be("Extension Properties");
        _ = MarkdownGenerationResources.TableRecordHeader.Should().Be("Record");
        _ = MarkdownGenerationResources.TableReadOnlyHeader.Should().Be("ReadOnly");
        _ = MarkdownGenerationResources.TableRefStructHeader.Should().Be("Ref Struct");
        _ = MarkdownGenerationResources.ExperimentalWarningTitle.Should().Be("Experimental");
        _ = MarkdownGenerationResources.ExperimentalDefaultMessage.Should().Be(
            "This API is experimental and subject to change or removal in future updates.");
        _ = MarkdownGenerationResources.ScopedModifierBadge.Should().Be("scoped");
        _ = MarkdownGenerationResources.TableTermHeader.Should().Be("Term");
        _ = MarkdownGenerationResources.QualityReportTitle.Should().Be("Documentation Quality Report");
    }
}
