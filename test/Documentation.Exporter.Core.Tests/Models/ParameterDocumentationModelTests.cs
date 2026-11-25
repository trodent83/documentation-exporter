using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class ParameterDocumentationModelTests
{
    [Fact]
    public void ParameterDocumentationModelDefaultPropertiesShouldInitializeProperly()
    {
        // Act
        var model = new ParameterDocumentationModel();

        // Assert
        _ = model.ParameterName.Should().BeEmpty();
        _ = model.ParameterTypeName.Should().BeEmpty();
        _ = model.ParameterDescription.Should().BeEmpty();
        _ = model.ResolvedTypeDocumentationLink.Should().BeEmpty();
        _ = model.IsOptional.Should().BeFalse();
        _ = model.DefaultValue.Should().BeEmpty();
        _ = model.IsParams.Should().BeFalse();
        _ = model.ParameterModifier.Should().BeEmpty();
        _ = model.IsExtensionTarget.Should().BeFalse();
        _ = model.IsNullable.Should().BeFalse();
        _ = model.CallerInformationAttribute.Should().BeEmpty();
    }

    [Fact]
    public void ParameterDocumentationModelWhenRequiredPropertiesEmptyShouldEmitLocalizedValidationErrors()
    {
        // Arrange
        var model = new ParameterDocumentationModel
        {
            ParameterName = string.Empty,
            ParameterTypeName = string.Empty
        };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because ParameterName and ParameterTypeName are required.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.ParameterNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.ParameterTypeNameRequired);
    }
}
