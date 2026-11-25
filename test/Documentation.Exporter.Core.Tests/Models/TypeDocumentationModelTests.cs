using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class TypeDocumentationModelTests
{
    [Fact]
    public void TypeDocumentationModelDefaultPropertiesShouldInitializeProperly()
    {
        // Act
        var model = new TypeDocumentationModel();

        // Assert
        _ = model.TypeName.Should().BeEmpty();
        _ = model.NamespaceName.Should().BeEmpty();
        _ = model.AssemblyName.Should().BeEmpty();
        _ = model.TypeCategory.Should().Be(TypeCategory.Class);
        _ = model.CSharpDeclarationSignature.Should().BeEmpty();
        _ = model.SummaryDescription.Should().BeEmpty();
        _ = model.RemarksDescription.Should().BeEmpty();
        _ = model.InheritanceHierarchy.Should().BeEmpty();
        _ = model.ImplementedInterfaces.Should().BeEmpty();
        _ = model.AppliedAttributes.Should().BeEmpty();
        _ = model.Constructors.Should().BeEmpty();
        _ = model.Properties.Should().BeEmpty();
        _ = model.Methods.Should().BeEmpty();
        _ = model.Fields.Should().BeEmpty();
        _ = model.Events.Should().BeEmpty();
        _ = model.CodeExamples.Should().BeEmpty();
        _ = model.SeeAlsoReferences.Should().BeEmpty();
        _ = model.InjectedDocumentationSections.Should().BeEmpty();
        _ = model.HasInheritDoc.Should().BeFalse();
        _ = model.InheritDocCref.Should().BeNull();
    }

    [Fact]
    public void TypeDocumentationModelWhenRequiredPropertiesEmptyShouldEmitLocalizedValidationErrors()
    {
        // Arrange
        var model = new TypeDocumentationModel
        {
            TypeName = string.Empty,
            NamespaceName = string.Empty,
            TypeCategory = (TypeCategory)999,
            DiagramFormat = default
        };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because TypeName and NamespaceName are required and enum properties must be defined values.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.TypeNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.NamespaceNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.TypeCategoryInvalid);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.DiagramFormatInvalid);
    }
}
