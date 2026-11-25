using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class AssemblyReferenceModelTests
{
    [Fact]
    public void AssemblyReferenceModelDefaultsShouldInitializeProperly()
    {
        // Act
        AssemblyReferenceModel referenceModel = new();

        // Assert
        _ = referenceModel.ReferenceName.Should().BeEmpty("because ReferenceName defaults to empty.");
        _ = referenceModel.ReferenceKind.Should().Be(AssemblyReferenceKind.Assembly, "because Assembly is the default reference kind.");
        _ = referenceModel.AssemblyVersion.Should().BeEmpty();
        _ = referenceModel.PackageVersion.Should().BeEmpty();
        _ = referenceModel.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Compiled, "because Compiled is the default linking policy.");
        _ = referenceModel.VersionConstraintExpression.Should().BeEmpty();
        _ = referenceModel.IsStrongNamed.Should().BeFalse();
        _ = referenceModel.PublicKeyToken.Should().BeEmpty();
    }

    [Fact]
    public void AssemblyReferenceModelWhenPropertiesInvalidShouldEmitLocalizedValidationErrors()
    {
        // Arrange
        AssemblyReferenceModel invalidModel = new()
        {
            ReferenceName = string.Empty,
            ReferenceKind = (AssemblyReferenceKind)999,
            VersionLinkingKind = (AssemblyVersionLinkingKind)999
        };
        ValidationContext validationContext = new(invalidModel);
        List<ValidationResult> validationResults = [];

        // Act
        var isValid = Validator.TryValidateObject(invalidModel, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because ReferenceName is required and enum properties must be valid.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.ReferenceNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.ReferenceKindInvalid);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.VersionLinkingKindInvalid);
    }
}
