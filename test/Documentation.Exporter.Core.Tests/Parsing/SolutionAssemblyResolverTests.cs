using Documentation.Exporter.Core.Parsing;
using FluentAssertions;
using Xunit;
using Documentation.Exporter.Core.Tests.TestCategories;

namespace Documentation.Exporter.Core.Tests.Parsing;

[IntegrationTest]
public sealed class SolutionAssemblyResolverTests
{
    [Fact]
    public void ResolveSolutionAssembliesWhenPathNullOrEmptyShouldThrowArgumentException()
    {
        // Arrange
        var resolver = new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => resolver.ResolveSolutionAssemblies(null!));
        _ = Assert.Throws<ArgumentException>(() => resolver.ResolveSolutionAssemblies("   "));
    }

    [Fact]
    public void ResolveSolutionAssembliesWhenFileDoesNotExistShouldReturnEmptyList()
    {
        // Arrange
        var resolver = new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Missing.slnx");

        // Act
        var resolved = resolver.ResolveSolutionAssemblies(missingPath);

        // Assert
        _ = resolved.Should().BeEmpty();
    }

    [Fact]
    public void ResolveSolutionAssembliesWithSlnxAndClassicSlnShouldDiscoverBuiltProjectsAndFilterTestProjects()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_SlnTest_{Guid.NewGuid():N}");
        var libDir = Path.Combine(tempRoot, "src", "MyLib");
        var testDir = Path.Combine(tempRoot, "test", "MyLib.Tests");
        var sdkTestDir = Path.Combine(tempRoot, "src", "MySdkVerification");
        var standardBinDir = Path.Combine(libDir, "bin", "Debug", "net10.0");
        var testBinDir = Path.Combine(testDir, "bin", "Debug", "net10.0");
        var sdkTestBinDir = Path.Combine(sdkTestDir, "bin", "Debug", "net10.0");

        _ = Directory.CreateDirectory(standardBinDir);
        _ = Directory.CreateDirectory(testBinDir);
        _ = Directory.CreateDirectory(sdkTestBinDir);

        try
        {
            var libCsproj = Path.Combine(libDir, "MyLib.csproj");
            File.WriteAllText(libCsproj, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><AssemblyName>CustomLibName</AssemblyName></PropertyGroup></Project>");
            var libDll = Path.Combine(standardBinDir, "CustomLibName.dll");
            var libXml = Path.Combine(standardBinDir, "CustomLibName.xml");
            File.WriteAllText(libDll, "dummy-dll");
            File.WriteAllText(libXml, "<doc/>");

            var testCsproj = Path.Combine(testDir, "MyLib.Tests.csproj");
            File.WriteAllText(testCsproj, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>");
            File.WriteAllText(Path.Combine(testBinDir, "MyLib.Tests.dll"), "dummy");
            File.WriteAllText(Path.Combine(testBinDir, "MyLib.Tests.xml"), "<doc/>");

            var sdkTestCsproj = Path.Combine(sdkTestDir, "MySdkVerification.csproj");
            File.WriteAllText(sdkTestCsproj, "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
            File.WriteAllText(Path.Combine(sdkTestBinDir, "MySdkVerification.dll"), "dummy");
            File.WriteAllText(Path.Combine(sdkTestBinDir, "MySdkVerification.xml"), "<doc/>");

            var slnxPath = Path.Combine(tempRoot, "Sample.slnx");
            File.WriteAllText(
                slnxPath,
                """
                <Solution>
                  <Project Path="src/MyLib/MyLib.csproj" />
                  <Project Path="src/Missing/Missing.csproj" />
                  <Project Path="test/MyLib.Tests/MyLib.Tests.csproj" />
                  <Project Path="src/MySdkVerification/MySdkVerification.csproj" />
                </Solution>
                """);

            var slnPath = Path.Combine(tempRoot, "Sample.sln");
            File.WriteAllText(
                slnPath,
                """
                Microsoft Visual Studio Solution File, Format Version 12.00
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyLib", "src\MyLib\MyLib.csproj", "{11111111-1111-1111-1111-111111111111}"
                EndProject
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyLib.Tests", "test\MyLib.Tests\MyLib.Tests.csproj", "{22222222-2222-2222-2222-222222222222}"
                EndProject
                """);

            var resolver = new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

            // Act
            var slnxArtifactsExcludingTests = resolver.ResolveSolutionAssemblies(slnxPath, "Debug", includeTestProjects: false);
            var slnxArtifactsIncludingTests = resolver.ResolveSolutionAssemblies(slnxPath, "Debug", includeTestProjects: true);
            var slnArtifacts = resolver.ResolveSolutionAssemblies(slnPath, "Debug", includeTestProjects: false);

            // Assert
            _ = slnxArtifactsExcludingTests.Should().ContainSingle("because test and missing projects must be excluded by default.");
            _ = slnxArtifactsExcludingTests[0].AssemblyFilePath.Should().Be(libDll);
            _ = slnxArtifactsExcludingTests[0].XmlDocumentationFilePath.Should().Be(libXml);

            _ = slnxArtifactsIncludingTests.Should().HaveCount(3, "because includeTestProjects=true includes test projects with built artifacts.");
            _ = slnArtifacts.Should().ContainSingle();
            _ = slnArtifacts[0].AssemblyFilePath.Should().Be(libDll);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveSolutionAssembliesWhenFailOnMissingIsTrueAndSolutionDoesNotExistShouldThrowFileNotFoundException()
    {
        // Arrange
        var resolver = new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var missingSolutionPath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid() + ".slnx");

        // Act
        var action = () => resolver.ResolveSolutionAssemblies(
            missingSolutionPath,
            buildConfiguration: "Debug",
            includeTestProjects: false,
            excludedProjects: null,
            failOnMissing: true);

        // Assert
        _ = action.Should().Throw<FileNotFoundException>("because failOnMissing=true must throw when the solution file does not exist.");
    }

    [Fact]
    public void ResolveSolutionAssembliesWhenFailOnMissingIsTrueAndProjectArtifactsAreMissingShouldThrowFileNotFoundException()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DocExporter_MissingArtifactsTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempDirectory);

        try
        {
            var projectPath = Path.Combine(tempDirectory, "UnbuiltProject.csproj");
            File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><AssemblyName>UnbuiltProject</AssemblyName></PropertyGroup></Project>");

            var slnxPath = Path.Combine(tempDirectory, "TestApp.slnx");
            File.WriteAllText(slnxPath, $"<Solution><Project Path=\"{projectPath}\" /></Solution>");

            var resolver = new SolutionAssemblyResolver(new Documentation.Exporter.Core.IO.PhysicalFileSystem());

            // Act
            var action = () => resolver.ResolveSolutionAssemblies(
                slnxPath,
                buildConfiguration: "Debug",
                includeTestProjects: false,
                excludedProjects: null,
                failOnMissing: true);

            // Assert
            _ = action.Should().Throw<FileNotFoundException>("because failOnMissing=true must throw when project artifacts (.dll / .xml) have not been compiled.");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
