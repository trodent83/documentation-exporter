using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.TestCategories;

[UnitTest]
public sealed class TestCategoryGovernanceTests
{
    [Fact]
    public void AllTestMethodsInAssemblyShouldHaveExactlyOneCategoryAttribute()
    {
        // Arrange
        var testAssembly = typeof(TestCategoryGovernanceTests).Assembly;
        var allTestMethods = testAssembly
            .GetTypes()
            .Where(candidateType => candidateType.IsClass && !candidateType.IsAbstract)
            .SelectMany(candidateType => candidateType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Where(methodInfo => methodInfo.IsDefined(typeof(FactAttribute), inherit: true) ||
                                 methodInfo.IsDefined(typeof(TheoryAttribute), inherit: true))
            .ToList();

        // Act
        List<string> uncategorizedMethods = [];
        List<string> dualCategorizedMethods = [];
        var unitTestCount = 0;
        var integrationTestCount = 0;

        foreach (var testMethod in allTestMethods)
        {
            var declaringType = testMethod.DeclaringType;
            var hasUnitCategory = testMethod.IsDefined(typeof(UnitTestAttribute), inherit: true) ||
                                  (declaringType?.IsDefined(typeof(UnitTestAttribute), inherit: true) == true);
            var hasIntegrationCategory = testMethod.IsDefined(typeof(IntegrationTestAttribute), inherit: true) ||
                                         (declaringType?.IsDefined(typeof(IntegrationTestAttribute), inherit: true) == true);

            var methodDisplayName = $"{declaringType?.FullName}.{testMethod.Name}";
            if (!hasUnitCategory && !hasIntegrationCategory)
            {
                uncategorizedMethods.Add(methodDisplayName);
            }
            else if (hasUnitCategory && hasIntegrationCategory)
            {
                dualCategorizedMethods.Add(methodDisplayName);
            }
            else if (hasUnitCategory)
            {
                unitTestCount++;
            }
            else
            {
                integrationTestCount++;
            }
        }

        // Assert
        _ = allTestMethods.Should().NotBeEmpty("because the test assembly must contain discoverable xUnit test methods.");
        _ = uncategorizedMethods.Should().BeEmpty(
            "because every [Fact] and [Theory] test method must belong to either [UnitTest] or [IntegrationTest].");
        _ = dualCategorizedMethods.Should().BeEmpty(
            "because a test method cannot carry both [UnitTest] and [IntegrationTest] simultaneously.");
        _ = unitTestCount.Should().BeGreaterThan(0, "because the suite must contain categorized unit tests.");
        _ = integrationTestCount.Should().BeGreaterThan(0, "because the suite must contain categorized integration tests.");
    }
}
