using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing;

[IntegrationTest]
public sealed class IsolatedAssemblyLoaderTests
{
    [Fact]
    public void TryLoadExportedTypesWithValidCurrentAssemblyShouldSucceed()
    {
        // Arrange
        using var loader = new IsolatedAssemblyLoader(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var currentAssemblyPath = typeof(ExportResult).Assembly.Location;

        // Act
        var success = loader.TryLoadExportedTypes(currentAssemblyPath, out var exportedTypesMap);

        // Assert
        _ = success.Should().BeTrue("because the compiled Core assembly exists on disk.");
        _ = exportedTypesMap.Should().NotBeNull("because loading a valid assembly must return exported types.");
        _ = exportedTypesMap!.ContainsKey(typeof(ExportResult).FullName!).Should().BeTrue("because ExportResult is public.");
    }

    [Fact]
    public void TryLoadExportedTypesWithInvalidPathShouldReturnFalse()
    {
        // Arrange
        using var loader = new IsolatedAssemblyLoader(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var nonExistentDllPath = Path.Combine(Path.GetTempPath(), "NonExistentFolder", "Missing.dll");

        // Act
        var nullResult = loader.TryLoadExportedTypes((string?)null, out _);
        var whitespaceResult = loader.TryLoadExportedTypes("   ", out _);
        var missingResult = loader.TryLoadExportedTypes(nonExistentDllPath, out _);

        // Assert
        _ = nullResult.Should().BeFalse("because null paths cannot load assemblies.");
        _ = whitespaceResult.Should().BeFalse("because blank paths cannot load assemblies.");
        _ = missingResult.Should().BeFalse("because non-existent assembly files cannot be loaded.");
    }

    [Fact]
    public void TryLoadExportedTypesWithMultipleAssembliesShouldHandleDuplicatesAndFailures()
    {
        // Arrange
        using var loader = new IsolatedAssemblyLoader(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var currentAssemblyPath = typeof(ExportResult).Assembly.Location;
        var nonExistentDllPath = Path.Combine(Path.GetTempPath(), "NonExistentFolder", "Missing.dll");

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => loader.TryLoadExportedTypes((IEnumerable<string>)null!, out _));

        var emptyResult = loader.TryLoadExportedTypes([], out var emptyTypesMap);
        _ = emptyResult.Should().BeFalse("because an empty collection of assembly paths provides no assemblies to load.");
        _ = emptyTypesMap.Should().BeNull();

        var failureResult = loader.TryLoadExportedTypes([currentAssemblyPath, nonExistentDllPath], out var failedTypesMap);
        _ = failureResult.Should().BeFalse("because failure to load one assembly aborts multi-assembly loading.");
        _ = failedTypesMap.Should().BeNull();

        var duplicateResult = loader.TryLoadExportedTypes([currentAssemblyPath, currentAssemblyPath], out var duplicateTypesMap);
        _ = duplicateResult.Should().BeTrue("because valid duplicate assemblies merge deterministically without crashing.");
        _ = duplicateTypesMap.Should().NotBeNull();
        _ = duplicateTypesMap!.ContainsKey(typeof(ExportResult).FullName!).Should().BeTrue();
    }

    [Fact]
    public void TryLoadExportedTypesShouldExcludeCompilerGeneratedAndCSharp14ExtensionMarkerTypes()
    {
        // Arrange
        using var loader = new IsolatedAssemblyLoader(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var testAssemblyPath = typeof(PublicExtensionLoaderContractExtensions).Assembly.Location;

        // Act
        var loaded = loader.TryLoadExportedTypes(testAssemblyPath, out var exportedTypesMap);

        // Assert
        _ = loaded.Should().BeTrue("because the compiled test assembly exists on disk.");
        _ = exportedTypesMap.Should().NotBeNull();
        _ = exportedTypesMap!.ContainsKey(typeof(PublicExtensionLoaderContractExtensions).FullName!)
            .Should().BeTrue("because the outer public extension container class should be exported.");
        _ = exportedTypesMap.Keys.Should().NotContain(
            typeFullName => typeFullName.Contains("<G>$", StringComparison.Ordinal) ||
                            typeFullName.Contains("<M>$", StringComparison.Ordinal) ||
                            typeFullName.Contains("<PrivateImplementationDetails>", StringComparison.Ordinal),
            "because Roslyn compiler-generated and C# 14 extension marker types must be excluded from exported types.");
    }
}

/// <summary>
/// Public C# 14 extension block fixture used to verify compiler nested marker exclusion in IsolatedAssemblyLoader.
/// </summary>
public static class PublicExtensionLoaderContractExtensions
{
    extension(string value)
    {
        public bool HasText => !string.IsNullOrWhiteSpace(value);
    }
}
