using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Models;

[UnitTest]
public sealed class AssemblyDependencyEdgeModelTests
{
    [Fact]
    public void AssemblyDependencyEdgeModelDefaultsShouldInitializeProperly()
    {
        // Act
        AssemblyDependencyEdgeModel edgeModel = new();

        // Assert
        _ = edgeModel.SourceAssemblyName.Should().BeEmpty("because SourceAssemblyName defaults to empty.");
        _ = edgeModel.TargetAssemblyName.Should().BeEmpty("because TargetAssemblyName defaults to empty.");
        _ = edgeModel.ReferenceKind.Should().Be(AssemblyReferenceKind.Assembly, "because Assembly is the default reference kind.");
        _ = edgeModel.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Compiled, "because Compiled is the default linking policy.");
        _ = edgeModel.VersionConstraintExpression.Should().BeEmpty();
        _ = edgeModel.ResolvedVersion.Should().BeEmpty();
        _ = edgeModel.IsTargetStrongNamed.Should().BeFalse();
        _ = edgeModel.TargetPublicKeyToken.Should().BeEmpty();
    }

    [Fact]
    public void AssemblyDependencyEdgeModelWhenPropertiesInvalidShouldEmitLocalizedValidationErrors()
    {
        // Arrange
        AssemblyDependencyEdgeModel invalidModel = new()
        {
            SourceAssemblyName = string.Empty,
            TargetAssemblyName = string.Empty,
            ReferenceKind = (AssemblyReferenceKind)999,
            VersionLinkingKind = (AssemblyVersionLinkingKind)999
        };
        ValidationContext validationContext = new(invalidModel);
        List<ValidationResult> validationResults = [];

        // Act
        var isValid = Validator.TryValidateObject(invalidModel, validationContext, validationResults, validateAllProperties: true);

        // Assert
        _ = isValid.Should().BeFalse("because source/target assembly names and enum properties must be valid.");
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.DependencySourceNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.ReferenceNameRequired);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.ReferenceKindInvalid);
        _ = validationResults.Should().Contain(result => result.ErrorMessage == ValidationMessagesResources.VersionLinkingKindInvalid);
    }
}
