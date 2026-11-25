using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class MemberDocumentationModelTests
{
    [Fact]
    public void MemberDocumentationModelDefaultPropertiesShouldInitializeProperly()
    {
        // Act
        var model = new MemberDocumentationModel();

        // Assert
        _ = model.MemberName.Should().BeEmpty();
        _ = model.ReturnTypeName.Should().BeEmpty();
        _ = model.SummaryDescription.Should().BeEmpty();
        _ = model.RemarksDescription.Should().BeEmpty();
        _ = model.ReturnValueDescription.Should().BeEmpty();
        _ = model.MemberParameters.Should().BeEmpty();
        _ = model.DocumentedExceptions.Should().BeEmpty();
        _ = model.CodeExamples.Should().BeEmpty();
        _ = model.SeeAlsoReferences.Should().BeEmpty();
        _ = model.HasInheritDoc.Should().BeFalse();
        _ = model.InheritDocCref.Should().BeNull();
    }

    [Fact]
    public void MemberDocumentationModelWhenMemberNameEmptyShouldEmitLocalizedValidationError()
    {
        // Arrange
        var model = new MemberDocumentationModel { MemberName = string.Empty };
        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because MemberName is required.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.MemberNameRequired);
    }
}
