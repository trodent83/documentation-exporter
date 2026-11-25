using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class DocumentationInjectionModelTests
{
    [Fact]
    public void DocumentationInjectionModelDefaultPropertiesShouldInitializeProperly()
    {
        // Act
        var model = new DocumentationInjectionModel();

        // Assert
        _ = model.TargetName.Should().BeEmpty();
        _ = model.TargetAnchor.Should().Be(SidecarAnchor.AfterRemarks);
        _ = model.CustomMarkdownContent.Should().BeEmpty();
        _ = model.InjectionMode.Should().Be(InjectionMode.Extend);
    }

    [Fact]
    public void DocumentationInjectionModelWhenRequiredPropertiesEmptyShouldEmitLocalizedValidationErrors()
    {
        // Arrange
        var model = new DocumentationInjectionModel
        {
            TargetName = string.Empty,
            InjectionMode = (InjectionMode)999,
            TargetAnchor = (SidecarAnchor)999,
            CustomMarkdownContent = string.Empty
        };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because required fields are empty and enum fields must hold defined values.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.TargetNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.InjectionModeInvalid);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.TargetAnchorInvalid);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.CustomMarkdownContentRequired);
    }
}
