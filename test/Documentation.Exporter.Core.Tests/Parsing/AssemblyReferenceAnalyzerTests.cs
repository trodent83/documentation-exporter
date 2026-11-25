using Documentation.Exporter.Abstractions.IO;
using System.Text;
using Documentation.Exporter.Cli;
using Documentation.Exporter.Core.IO;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Tests.TestCategories;
using Documentation.Exporter.DemoLibrary.Auditing;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Parsing;

public sealed class AssemblyReferenceAnalyzerTests
{
    private sealed class OverlayFileSystem(
        IReadOnlyDictionary<string, string> virtualFiles,
        IFileSystem innerFileSystem) : IFileSystem
    {
        private readonly Dictionary<string, string> overlayFiles = new(virtualFiles, StringComparer.OrdinalIgnoreCase);

        public bool FileExists(string? path) =>
            (!string.IsNullOrWhiteSpace(path) && overlayFiles.ContainsKey(path)) || innerFileSystem.FileExists(path);

        public bool DirectoryExists(string? path) => innerFileSystem.DirectoryExists(path);
        public void CreateDirectory(string path) => innerFileSystem.CreateDirectory(path);
        public void DeleteFile(string path) => innerFileSystem.DeleteFile(path);
        public void DeleteDirectory(string path, bool recursive = false) => innerFileSystem.DeleteDirectory(path, recursive);
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            innerFileSystem.EnumerateFiles(path, searchPattern, searchOption);
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            innerFileSystem.EnumerateDirectories(path, searchPattern, searchOption);
        public bool HasAnyFileSystemEntries(string directoryPath) => innerFileSystem.HasAnyFileSystemEntries(directoryPath);
        public Stream OpenRead(string path) =>
            overlayFiles.TryGetValue(path, out var virtualContent)
                ? new MemoryStream(Encoding.UTF8.GetBytes(virtualContent))
                : innerFileSystem.OpenRead(path);
        public string ReadAllText(string path) =>
            overlayFiles.TryGetValue(path, out var virtualContent) ? virtualContent : innerFileSystem.ReadAllText(path);
        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
            overlayFiles.TryGetValue(path, out var virtualContent)
                ? Task.FromResult(virtualContent)
                : innerFileSystem.ReadAllTextAsync(path, cancellationToken);
        public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) =>
            innerFileSystem.WriteAllTextAsync(path, contents, cancellationToken);
        public string GetFullPath(string path) => innerFileSystem.GetFullPath(path);
        public string GetCurrentDirectory() => innerFileSystem.GetCurrentDirectory();
        public string? FindFileCaseInsensitive(string directoryPath, string targetFileName) =>
            innerFileSystem.FindFileCaseInsensitive(directoryPath, targetFileName);
        public string? FindDirectoryCaseInsensitive(string parentDirectoryPath, string targetDirectoryName) =>
            innerFileSystem.FindDirectoryCaseInsensitive(parentDirectoryPath, targetDirectoryName);
    }

    [Fact]
    [UnitTest]
    public void ConstructorAndMethodWhenArgumentsNullShouldThrowArgumentNullException()
    {
        // Arrange
        AssemblyReferenceAnalyzer analyzer = new(new PhysicalFileSystem());

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyReferenceAnalyzer(null!));
        _ = Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeAssemblyReferences(null!));
    }

    [Fact]
    [IntegrationTest]
    public void AnalyzeAssemblyReferencesWithRealAssembliesShouldExtractStrongNamesPackagesAndProjects()
    {
        // Arrange
        var physicalFileSystem = new PhysicalFileSystem();
        AssemblyReferenceAnalyzer analyzer = new(physicalFileSystem);
        var repositoryRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent;
        var demoLibraryAssembly = typeof(CrossAssemblyAuditReporter).Assembly;
        var cliAssembly = typeof(CliServiceCollectionExtensions).Assembly;

        Dictionary<string, string> projectMap = [with(StringComparer.OrdinalIgnoreCase)];
        if (repositoryRoot != null)
        {
            var demoLibraryProject = Path.Combine(repositoryRoot.FullName, "demo", "Documentation.Exporter.DemoLibrary", "Documentation.Exporter.DemoLibrary.csproj");
            var cliProject = Path.Combine(repositoryRoot.FullName, "src", "Documentation.Exporter.Cli", "Documentation.Exporter.Cli.csproj");
            if (File.Exists(demoLibraryProject))
            {
                projectMap[demoLibraryAssembly.Location] = demoLibraryProject;
            }

            if (File.Exists(cliProject))
            {
                projectMap[cliAssembly.Location] = cliProject;
            }
        }

        // Act
        var analyzedMap = analyzer.AnalyzeAssemblyReferences([demoLibraryAssembly, cliAssembly, demoLibraryAssembly], projectMap);

        // Assert - DemoLibrary references
        _ = analyzedMap.Should().ContainKey("Documentation.Exporter.DemoLibrary", "because DemoLibrary was analyzed.");
        var demoReferences = analyzedMap["Documentation.Exporter.DemoLibrary"];
        var contractsReference = demoReferences.Should().ContainSingle(reference =>
            string.Equals(reference.ReferenceName, "Documentation.Exporter.DemoContracts", StringComparison.Ordinal)).Subject;
        _ = contractsReference.ReferenceKind.Should().Be(AssemblyReferenceKind.Project, "because DemoLibrary references DemoContracts via ProjectReference.");
        _ = contractsReference.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.ProjectLinked, "because project references follow solution project versions.");
        _ = contractsReference.IsStrongNamed.Should().BeFalse("because DemoContracts is not strong-named signed.");
        _ = contractsReference.PublicKeyToken.Should().BeEmpty("because unsigned assemblies do not carry a public key token.");

        var systemRuntimeReference = demoReferences.Should().ContainSingle(reference =>
            string.Equals(reference.ReferenceName, "System.Runtime", StringComparison.Ordinal)).Subject;
        _ = systemRuntimeReference.ReferenceKind.Should().Be(AssemblyReferenceKind.Framework, "because System.Runtime is a core .NET BCL assembly.");
        _ = systemRuntimeReference.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Framework, "because BCL framework references link against the target framework.");
        _ = systemRuntimeReference.VersionConstraintExpression.Should().Be("net11.0", "because DemoLibrary targets net11.0.");
        _ = systemRuntimeReference.IsStrongNamed.Should().BeTrue("because .NET BCL assemblies are strong-named.");
        _ = systemRuntimeReference.PublicKeyToken.Should().MatchRegex("^[0-9a-f]{16}$", "because strong-named assemblies expose a 16-hex-character public key token.");

        // Assert - CLI assembly project & NuGet package references via .deps.json + .csproj
        _ = analyzedMap.Should().ContainKey("Documentation.Exporter.Cli", "because CLI assembly was analyzed.");
        var cliReferences = analyzedMap["Documentation.Exporter.Cli"];

        var coreProjectReference = cliReferences.Should().ContainSingle(reference =>
            string.Equals(reference.ReferenceName, "Documentation.Exporter.Core", StringComparison.Ordinal)).Subject;
        _ = coreProjectReference.ReferenceKind.Should().Be(AssemblyReferenceKind.Project, "because CLI references Core via ProjectReference.");
        _ = coreProjectReference.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.ProjectLinked, "because project references link to the project output.");
        _ = coreProjectReference.IsStrongNamed.Should().BeFalse("because Documentation.Exporter.Core is unsigned.");
        _ = coreProjectReference.PublicKeyToken.Should().BeEmpty("because unsigned assemblies have no public key token.");

        var dependencyInjectionPackage = cliReferences.Should().ContainSingle(reference =>
            string.Equals(reference.ReferenceName, "Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal)).Subject;
        _ = dependencyInjectionPackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package, "because Microsoft.Extensions.DependencyInjection comes from NuGet PackageReference and .deps.json.");
        _ = dependencyInjectionPackage.PackageVersion.Should().NotBeNullOrWhiteSpace("because .deps.json resolves the exact NuGet package version.");
        _ = dependencyInjectionPackage.IsStrongNamed.Should().BeTrue("because Microsoft.Extensions packages are strong-named.");
        _ = dependencyInjectionPackage.PublicKeyToken.Should().MatchRegex("^[0-9a-f]{16}$", "because strong-named assemblies expose a 16-hex-character public key token.");

        var commandLinePackage = cliReferences.Should().ContainSingle(reference =>
            string.Equals(reference.ReferenceName, "System.CommandLine", StringComparison.Ordinal)).Subject;
        _ = commandLinePackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package, "because PackageReference classification takes precedence over Microsoft public key token framework detection.");
        _ = commandLinePackage.PackageVersion.Should().NotBeNullOrWhiteSpace("because System.CommandLine resolves a NuGet package version.");
        _ = commandLinePackage.IsStrongNamed.Should().BeTrue("because System.CommandLine is strong-named.");
        _ = commandLinePackage.PublicKeyToken.Should().Be("31bf3856ad364e35", "because System.CommandLine uses Microsoft's 31bf3856ad364e35 signing key.");
    }

    [Fact]
    [IntegrationTest]
    public void AnalyzeAssemblyReferencesWithSyntheticProjectCentralPackageManagementAndDepsShouldCoverAllLinkingModes()
    {
        // Arrange
        var physicalFileSystem = new PhysicalFileSystem();
        var targetAssembly = typeof(DocumentationExporterEngine).Assembly;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_RefAnalyzer_{Guid.NewGuid():N}");
        var projectFolder = Path.Combine(tempRoot, "Src", "SampleProject");
        var projectPath = Path.Combine(projectFolder, "SampleProject.csproj");
        var propsPath = Path.Combine(tempRoot, "Directory.Packages.props");
        var siblingProjectPath = Path.Combine(tempRoot, "Src", "SiblingProject", "SiblingProject.csproj");
        var depsPath = Path.ChangeExtension(targetAssembly.Location, ".deps.json")!;

        Dictionary<string, string> virtualFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            [propsPath] = """
                <Project>
                  <ItemGroup>
                    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="[11.0.0]" />
                  </ItemGroup>
                </Project>
                """,
            [siblingProjectPath] = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <AssemblyName>System.Text.RegularExpressions</AssemblyName>
                  </PropertyGroup>
                </Project>
                """,
            [projectPath] = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFrameworks>net11.0;net10.0</TargetFrameworks>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\SiblingProject\SiblingProject.csproj" />
                    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="[10.0.0, 12.0.0)" />
                    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
                    <PackageReference Include="WrapperDependencyPkg" VersionOverride="11.*" />
                    <PackageReference Include="System.Memory" Version="4.6.0" />
                    <PackageReference Include="System.ComponentModel" />
                    <Reference Include="System.Xml.XDocument, Version=11.0.0.0">
                      <SpecificVersion>true</SpecificVersion>
                    </Reference>
                  </ItemGroup>
                </Project>
                """,
            [depsPath] = """
                {
                  "targets": {
                    ".NETCoreApp,Version=v11.0": {
                      "WrapperDependencyPkg/11.0.0": {
                        "runtime": {
                          "lib/net11.0/System.Text.Json.dll": {}
                        }
                      },
                      "Microsoft.Extensions.DependencyInjection.Abstractions/11.0.0": {
                        "runtime": {
                          "lib/net11.0/Microsoft.Extensions.DependencyInjection.Abstractions.dll": {}
                        }
                      }
                    }
                  },
                  "libraries": {
                    "WrapperDependencyPkg/11.0.0": { "type": "package" },
                    "Microsoft.Extensions.DependencyInjection.Abstractions/11.0.0": { "type": "package" }
                  }
                }
                """
        };

        OverlayFileSystem overlayFileSystem = new(virtualFiles, physicalFileSystem);
        AssemblyReferenceAnalyzer analyzer = new(overlayFileSystem);
        Dictionary<string, string> projectLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            [targetAssembly.Location] = projectPath
        };

        // Act
        var analyzedMap = analyzer.AnalyzeAssemblyReferences([targetAssembly], projectLookup);
        var references = analyzedMap["Documentation.Exporter.Core"];

        // Assert
        var projectRef = references.Single(reference => reference.ReferenceName == "System.Text.RegularExpressions");
        _ = projectRef.ReferenceKind.Should().Be(AssemblyReferenceKind.Project);
        _ = projectRef.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.ProjectLinked);

        var rangePackage = references.Single(reference => reference.ReferenceName == "Microsoft.Extensions.DependencyInjection.Abstractions");
        _ = rangePackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package);
        _ = rangePackage.PackageVersion.Should().Be("11.0.0");
        _ = rangePackage.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Range);
        _ = rangePackage.VersionConstraintExpression.Should().Be("[10.0.0, 12.0.0)");

        var fixedCpmPackage = references.Single(reference => reference.ReferenceName == "Microsoft.Extensions.Logging.Abstractions");
        _ = fixedCpmPackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package);
        _ = fixedCpmPackage.PackageVersion.Should().Be("11.0.0");
        _ = fixedCpmPackage.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Fixed);
        _ = fixedCpmPackage.VersionConstraintExpression.Should().Be("[11.0.0]");

        var floatingWrapperPackage = references.Single(reference => reference.ReferenceName == "System.Text.Json");
        _ = floatingWrapperPackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package);
        _ = floatingWrapperPackage.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Floating);
        _ = floatingWrapperPackage.VersionConstraintExpression.Should().Be("11.*");

        var minimumPackage = references.Single(reference => reference.ReferenceName == "System.Memory");
        _ = minimumPackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package);
        _ = minimumPackage.PackageVersion.Should().Be("4.6.0");
        _ = minimumPackage.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Minimum);
        _ = minimumPackage.VersionConstraintExpression.Should().Be(">= 4.6.0");

        var versionlessPackage = references.Single(reference => reference.ReferenceName == "System.ComponentModel");
        _ = versionlessPackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package);
        _ = versionlessPackage.PackageVersion.Should().BeEmpty();
        _ = versionlessPackage.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Minimum);
        _ = versionlessPackage.VersionConstraintExpression.Should().BeEmpty();

        var fixedDirectRef = references.Single(reference => reference.ReferenceName == "System.Xml.XDocument");
        _ = fixedDirectRef.ReferenceKind.Should().Be(AssemblyReferenceKind.Assembly);
        _ = fixedDirectRef.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Fixed);
        _ = fixedDirectRef.VersionConstraintExpression.Should().Be("SpecificVersion=true");
    }

    [Fact]
    [IntegrationTest]
    public void AnalyzeAssemblyReferencesWhenMetadataMalformedOrDirectReferenceNoSpecificVersionShouldFallbackGracefully()
    {
        // Arrange
        var physicalFileSystem = new PhysicalFileSystem();
        var targetAssembly = typeof(DocumentationExporterEngine).Assembly;
        var badProjectPath = Path.Combine(Path.GetTempPath(), $"BadProj_{Guid.NewGuid():N}", "Bad.csproj");
        var specificFalseProjectPath = Path.Combine(Path.GetTempPath(), $"ValidProj_{Guid.NewGuid():N}", "Valid.csproj");
        var depsPath = Path.ChangeExtension(targetAssembly.Location, ".deps.json")!;

        OverlayFileSystem malformedOverlay = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [badProjectPath] = "<Project><BrokenXml",
                [depsPath] = "{ invalid json"
            },
            physicalFileSystem);
        AssemblyReferenceAnalyzer malformedAnalyzer = new(malformedOverlay);

        OverlayFileSystem validReferenceVariantsOverlay = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [specificFalseProjectPath] = """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <ItemGroup>
                        <Reference Include="System.Xml.XDocument, Version=11.0.0.0">
                          <SpecificVersion>false</SpecificVersion>
                        </Reference>
                        <Reference Include="Microsoft.Extensions.Logging.Abstractions, Version=11.0.0.0" />
                        <Reference Include="System.Runtime.Loader" />
                      </ItemGroup>
                    </Project>
                    """,
                [depsPath] = """
                    {
                      "targets": {
                        ".NETCoreApp,Version=v11.0": {
                          "CustomFileLib/1.0.0": {
                            "runtime": {
                              "Microsoft.Extensions.DependencyInjection.Abstractions.dll": {}
                            }
                          }
                        }
                      },
                      "libraries": {
                        "CustomFileLib/1.0.0": { "type": "reference" }
                      }
                    }
                    """
            },
            physicalFileSystem);
        AssemblyReferenceAnalyzer variantAnalyzer = new(validReferenceVariantsOverlay);

        // Act
        var malformedResult = malformedAnalyzer.AnalyzeAssemblyReferences(
            [targetAssembly],
            new Dictionary<string, string> { [targetAssembly.Location] = badProjectPath });

        var variantResult = variantAnalyzer.AnalyzeAssemblyReferences(
            [targetAssembly],
            new Dictionary<string, string> { [targetAssembly.Location] = specificFalseProjectPath });

        // Assert
        _ = malformedResult["Documentation.Exporter.Core"].Should().NotBeEmpty(
            "because malformed .csproj and .deps.json must fall back to compiled assembly metadata without throwing.");

        var variants = variantResult["Documentation.Exporter.Core"];
        var specificFalseRef = variants.Single(reference => reference.ReferenceName == "System.Xml.XDocument");
        _ = specificFalseRef.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Compiled);
        _ = specificFalseRef.VersionConstraintExpression.Should().Be("SpecificVersion=false");

        var versionAttrRef = variants.Single(reference => reference.ReferenceName == "Microsoft.Extensions.Logging.Abstractions");
        _ = versionAttrRef.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Fixed);
        _ = versionAttrRef.VersionConstraintExpression.Should().Be("SpecificVersion=true");

        var plainRef = variants.Single(reference => reference.ReferenceName == "System.Runtime.Loader");
        _ = plainRef.ReferenceKind.Should().Be(AssemblyReferenceKind.Assembly);
        _ = plainRef.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Compiled);

        var depsReferenceType = variants.Single(reference => reference.ReferenceName == "Microsoft.Extensions.DependencyInjection.Abstractions");
        _ = depsReferenceType.ReferenceKind.Should().Be(AssemblyReferenceKind.Assembly);
        _ = depsReferenceType.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Compiled);
    }

    [Fact]
    [IntegrationTest]
    public void AnalyzeAssemblyReferencesWithMalformedCpmMalformedProjectRefsAndEdgeDepsJsonShouldFallbackCleanly()
    {
        // Arrange
        var physicalFileSystem = new PhysicalFileSystem();
        var targetAssembly = typeof(DocumentationExporterEngine).Assembly;
        var depsPath = Path.ChangeExtension(targetAssembly.Location, ".deps.json")!;

        var brokenCpmRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_BrokenCpm_{Guid.NewGuid():N}");
        var brokenCpmProjectPath = Path.Combine(brokenCpmRoot, "App", "App.csproj");
        var brokenPropsPath = Path.Combine(brokenCpmRoot, "Directory.Packages.props");
        var brokenSiblingProjectPath = Path.Combine(brokenCpmRoot, "BrokenSibling", "BrokenSibling.csproj");
        var implicitNameProjectPath = Path.Combine(brokenCpmRoot, "ImplicitName", "System.Collections.Concurrent.csproj");
        var missingSiblingProjectPath = Path.Combine(brokenCpmRoot, "Missing", "System.Threading.Tasks.Parallel.csproj");

        var validCpmRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_ChildCpm_{Guid.NewGuid():N}");
        var validCpmPropsPath = Path.Combine(validCpmRoot, "Directory.Packages.props");
        var validCpmProjectPath = Path.Combine(validCpmRoot, "App", "ChildCpmApp.csproj");

        Dictionary<string, string> virtualFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            [brokenPropsPath] = "<Project><BrokenPropsXml",
            [brokenSiblingProjectPath] = "<Project><BrokenSiblingXml",
            [implicitNameProjectPath] = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>",
            [brokenCpmProjectPath] = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework></TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference />
                    <ProjectReference Include="..\BrokenSibling\BrokenSibling.csproj" />
                    <ProjectReference Include="..\ImplicitName\System.Collections.Concurrent.csproj" />
                    <ProjectReference Include="..\Missing\System.Threading.Tasks.Parallel.csproj" />
                    <PackageReference />
                    <PackageReference Include="System.Memory">
                      <Version>4.6.0</Version>
                    </PackageReference>
                    <Reference />
                    <Reference Include=", Version=1.0.0.0" />
                  </ItemGroup>
                </Project>
                """,
            [validCpmPropsPath] = """
                <Project>
                  <ItemGroup>
                    <PackageVersion Include="" Version="1.0.0" />
                    <PackageVersion Include="EmptyVersionPkg" />
                    <PackageVersion Include="System.Text.Json">
                      <Version>[11.0.0]</Version>
                    </PackageVersion>
                  </ItemGroup>
                </Project>
                """,
            [validCpmProjectPath] = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="System.Text.Json" />
                  </ItemGroup>
                </Project>
                """,
            [depsPath] = """
                {
                  "targets": {
                    "NonObjectTarget": "ignored",
                    ".NETCoreApp,Version=v11.0": {
                      "/1.0.0": {},
                      "NoRuntimeEntry/1.0.0": { "dependencies": {} },
                      "UnlistedWrapper/2.0.0": {
                        "runtime": {
                          "": {},
                          "lib/net11.0/System.Xml.ReaderWriter.dll": {}
                        }
                      }
                    }
                  },
                  "libraries": {
                    "/1.0.0": { "type": "package" },
                    "LibraryWithoutType/1.0.0": {},
                    "System.Collections/11.0.0": { "type": "package" }
                  }
                }
                """
        };

        OverlayFileSystem overlayFileSystem = new(virtualFiles, physicalFileSystem);
        AssemblyReferenceAnalyzer analyzer = new(overlayFileSystem);

        // Act - null lookup, empty lookup, whitespace keys, non-existent project
        var withoutProjectResult = analyzer.AnalyzeAssemblyReferences([targetAssembly]);
        var emptyProjectResult = analyzer.AnalyzeAssemblyReferences(
            [targetAssembly],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var missingProjectResult = analyzer.AnalyzeAssemblyReferences(
            [targetAssembly],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [string.Empty] = brokenCpmProjectPath,
                ["C:\\NonExistent\\WhitespaceValue.dll"] = " ",
                [targetAssembly.Location] = Path.Combine(brokenCpmRoot, "DoesNotExist.csproj")
            });

        // Act - broken Directory.Packages.props, broken/implicit/missing ProjectReferences, edge deps.json
        var edgeProjectResult = analyzer.AnalyzeAssemblyReferences(
            [targetAssembly],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [targetAssembly.Location] = brokenCpmProjectPath
            });

        // Act - child <Version> elements in Directory.Packages.props
        OverlayFileSystem childPropsOnlyOverlay = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [validCpmPropsPath] = virtualFiles[validCpmPropsPath],
                [validCpmProjectPath] = virtualFiles[validCpmProjectPath],
                [depsPath] = "{}"
            },
            physicalFileSystem);
        AssemblyReferenceAnalyzer childPropsAnalyzer = new(childPropsOnlyOverlay);
        var childPropsResult = childPropsAnalyzer.AnalyzeAssemblyReferences(
            [targetAssembly],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [targetAssembly.Location] = validCpmProjectPath
            });

        // Assert
        _ = withoutProjectResult.Should().ContainKey("Documentation.Exporter.Core", "because analyzer works without project mappings.");
        _ = emptyProjectResult.Should().ContainKey("Documentation.Exporter.Core", "because empty project mappings still analyze compiled assembly metadata.");
        _ = missingProjectResult.Should().ContainKey("Documentation.Exporter.Core", "because invalid mapping entries are ignored safely.");

        var edgeReferences = edgeProjectResult["Documentation.Exporter.Core"];
        _ = edgeReferences.Should().ContainSingle(reference => reference.ReferenceName == "System.Collections.Concurrent")
            .Which.ReferenceKind.Should().Be(AssemblyReferenceKind.Project, "because implicit csproj filename resolves ProjectReference.");
        _ = edgeReferences.Should().ContainSingle(reference => reference.ReferenceName == "System.Threading.Tasks.Parallel")
            .Which.ReferenceKind.Should().Be(AssemblyReferenceKind.Project, "because missing sibling csproj falls back to file stem.");
        _ = edgeReferences.Should().ContainSingle(reference => reference.ReferenceName == "System.Memory")
            .Which.PackageVersion.Should().Be("4.6.0", "because child <Version> elements under PackageReference are parsed.");
        _ = edgeReferences.Should().ContainSingle(reference => reference.ReferenceName == "System.Collections")
            .Which.ReferenceKind.Should().Be(AssemblyReferenceKind.Package, "because LibrariesByName fallback resolves package metadata without runtime target assets.");
        _ = edgeReferences.Should().ContainSingle(reference => reference.ReferenceName == "System.Xml.ReaderWriter")
            .Which.ReferenceKind.Should().Be(AssemblyReferenceKind.Framework, "because deps entry without library type falls back cleanly.");

        var childPropsReferences = childPropsResult["Documentation.Exporter.Core"];
        var jsonPackage = childPropsReferences.Should().ContainSingle(reference => reference.ReferenceName == "System.Text.Json").Subject;
        _ = jsonPackage.ReferenceKind.Should().Be(AssemblyReferenceKind.Package, "because child <Version> element under PackageVersion is resolved.");
        _ = jsonPackage.PackageVersion.Should().Be("11.0.0", "because fixed constraint unwraps inner version from brackets.");
        _ = jsonPackage.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Fixed, "because bracketed child version indicates fixed linking.");
    }

    [Fact]
    [IntegrationTest]
    public void AnalyzeAssemblyDependencyTreesShouldResolveTransitiveNonFrameworkGraphsAndEdgeCases()
    {
        // Arrange - Null guards
        var physicalFileSystem = new PhysicalFileSystem();
        AssemblyReferenceAnalyzer physicalAnalyzer = new(physicalFileSystem);
        Dictionary<string, IReadOnlyList<AssemblyReferenceModel>> emptyDirectRefs = [with(StringComparer.OrdinalIgnoreCase)];

        _ = Assert.Throws<ArgumentNullException>(() =>
            physicalAnalyzer.AnalyzeAssemblyDependencyTrees(null!, emptyDirectRefs));
        _ = Assert.Throws<ArgumentNullException>(() =>
            physicalAnalyzer.AnalyzeAssemblyDependencyTrees([], null!));

        // Arrange & Act - Real assemblies (DemoLibrary -> DemoContracts; CLI -> Core + transitive NuGet Logging tree)
        var repositoryRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent;
        var demoLibraryAssembly = typeof(CrossAssemblyAuditReporter).Assembly;
        var cliAssembly = typeof(CliServiceCollectionExtensions).Assembly;

        Dictionary<string, string> realProjectMap = [with(StringComparer.OrdinalIgnoreCase)];
        if (repositoryRoot != null)
        {
            var demoLibraryProject = Path.Combine(repositoryRoot.FullName, "demo", "Documentation.Exporter.DemoLibrary", "Documentation.Exporter.DemoLibrary.csproj");
            var cliProject = Path.Combine(repositoryRoot.FullName, "src", "Documentation.Exporter.Cli", "Documentation.Exporter.Cli.csproj");
            if (File.Exists(demoLibraryProject))
            {
                realProjectMap[demoLibraryAssembly.Location] = demoLibraryProject;
            }

            if (File.Exists(cliProject))
            {
                realProjectMap[cliAssembly.Location] = cliProject;
            }
        }

        var realDirectRefs = physicalAnalyzer.AnalyzeAssemblyReferences(
            [demoLibraryAssembly, cliAssembly, demoLibraryAssembly],
            realProjectMap);
        var realTrees = physicalAnalyzer.AnalyzeAssemblyDependencyTrees(
            [demoLibraryAssembly, cliAssembly, demoLibraryAssembly],
            realDirectRefs,
            realProjectMap);

        // Assert - Real DemoLibrary excludes System.* framework assemblies and keeps DemoContracts
        _ = realTrees.Should().ContainKey("Documentation.Exporter.DemoLibrary");
        var demoLibraryEdges = realTrees["Documentation.Exporter.DemoLibrary"];
        _ = demoLibraryEdges.Should().NotContain(edge => edge.ReferenceKind == AssemblyReferenceKind.Framework,
            "because framework assemblies must be excluded from the dependency tree.");
        var demoContractsEdge = demoLibraryEdges.Should().ContainSingle(edge =>
            edge.SourceAssemblyName == "Documentation.Exporter.DemoLibrary" &&
            edge.TargetAssemblyName == "Documentation.Exporter.DemoContracts").Subject;
        _ = demoContractsEdge.ReferenceKind.Should().Be(AssemblyReferenceKind.Project);
        _ = demoContractsEdge.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.ProjectLinked);
        _ = demoContractsEdge.IsTargetStrongNamed.Should().BeFalse();

        // Assert - Real CLI resolves both direct project/package edges and transitive NuGet dependencies
        _ = realTrees.Should().ContainKey("Documentation.Exporter.Cli");
        var cliEdges = realTrees["Documentation.Exporter.Cli"];
        _ = cliEdges.Should().NotContain(edge => edge.ReferenceKind == AssemblyReferenceKind.Framework,
            "because framework assemblies must be excluded from the CLI dependency tree.");
        _ = cliEdges.Should().Contain(edge =>
                edge.SourceAssemblyName == "Documentation.Exporter.Cli" &&
                edge.TargetAssemblyName == "Documentation.Exporter.Core" &&
                edge.VersionLinkingKind == AssemblyVersionLinkingKind.ProjectLinked &&
                !edge.IsTargetStrongNamed,
            "because CLI links directly to Documentation.Exporter.Core as a project reference.");
        _ = cliEdges.Should().Contain(edge =>
                edge.SourceAssemblyName == "Microsoft.Extensions.Logging.Console" &&
                edge.TargetAssemblyName == "Microsoft.Extensions.Logging.Configuration" &&
                edge.ReferenceKind == AssemblyReferenceKind.Package &&
                edge.IsTargetStrongNamed &&
                !string.IsNullOrWhiteSpace(edge.TargetPublicKeyToken),
            "because transitive NuGet dependencies from .deps.json must expand and probe strong-name tokens.");

        // Arrange - Synthetic diamond + cycle + corrupt local DLL + wrapper package + unknown framework token
        var tempRoot = Path.Combine(Path.GetTempPath(), $"DocExporter_DepTree_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(tempRoot);
        var coreAssembly = typeof(DocumentationExporterEngine).Assembly;
        var rootBinDir = Path.GetDirectoryName(coreAssembly.Location)!;
        var badDllName = $"BadImageProbe_{Guid.NewGuid():N}.dll";
        var badDllPath = Path.Combine(rootBinDir, badDllName);
        File.WriteAllBytes(badDllPath, [0x01, 0x02, 0x03, 0x04]);

        try
        {
            var syntheticDepsPath = Path.ChangeExtension(coreAssembly.Location, ".deps.json")!;
            var badLibName = Path.GetFileNameWithoutExtension(badDllName);
            Dictionary<string, string> syntheticFiles = new(StringComparer.OrdinalIgnoreCase)
            {
                [syntheticDepsPath] = $$"""
                    {
                      "targets": {
                        ".NETCoreApp,Version=v11.0": {
                          "Documentation.Exporter.Core/1.0.0": {
                            "dependencies": {
                              "Documentation.Exporter.Core": "1.0.0",
                              "CycleLeft": "[1.2.0]",
                              "TransitiveProject": "2.0.0",
                              "DirectRefLib": "1.0.0",
                              "System.Text.Json": "11.0.0"
                            }
                          },
                          "CycleLeft/1.2.0": {
                            "dependencies": {
                              "CycleRight": "[2.0.0, 3.0.0)",
                              "Documentation.Exporter.Core": "1.0.0"
                            }
                          },
                          "CycleRight/2.1.0": {
                            "dependencies": {
                              "CycleLeft": "1.*",
                              "{{badLibName}}": "3.0.0",
                              "Microsoft.Extensions.Logging.Abstractions": "11.0.0"
                            }
                          },
                          "TransitiveProject/2.0.0": {},
                          "DirectRefLib/1.0.0": {},
                          "{{badLibName}}/3.0.0": {
                            "runtime": {
                              "lib/net11.0/{{badDllName}}": {
                                "localPath": "{{badDllName}}"
                              }
                            }
                          }
                        }
                      },
                      "libraries": {
                        "CycleLeft/1.2.0": { "type": "package" },
                        "CycleRight/2.1.0": { "type": "package" },
                        "TransitiveProject/2.0.0": { "type": "project" },
                        "DirectRefLib/1.0.0": { "type": "reference" },
                        "{{badLibName}}/3.0.0": { "type": "unknown" }
                      }
                    }
                    """
            };

            OverlayFileSystem syntheticOverlay = new(syntheticFiles, physicalFileSystem);
            AssemblyReferenceAnalyzer syntheticAnalyzer = new(syntheticOverlay);
            Dictionary<string, IReadOnlyList<AssemblyReferenceModel>> seededDirectRefs =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Documentation.Exporter.Core"] =
                    [
                        new()
                        {
                            ReferenceName = "CycleLeft",
                            ReferenceKind = AssemblyReferenceKind.Package,
                            PackageVersion = "1.2.0",
                            VersionLinkingKind = AssemblyVersionLinkingKind.Fixed,
                            VersionConstraintExpression = "[1.2.0]",
                            IsStrongNamed = false
                        },
                        new()
                        {
                            ReferenceName = "System.Runtime",
                            ReferenceKind = AssemblyReferenceKind.Framework,
                            AssemblyVersion = "11.0.0.0",
                            VersionLinkingKind = AssemblyVersionLinkingKind.Framework,
                            IsStrongNamed = true,
                            PublicKeyToken = "b03f5f7f11d50a3a"
                        }
                    ]
                };

            // Act
            var syntheticTrees = syntheticAnalyzer.AnalyzeAssemblyDependencyTrees([coreAssembly], seededDirectRefs);

            // Assert
            var syntheticEdges = syntheticTrees["Documentation.Exporter.Core"];
            _ = syntheticEdges.Should().NotContain(edge => edge.TargetAssemblyName == "System.Text.Json",
                "because unknown framework assemblies must be filtered out of the dependency tree.");
            _ = syntheticEdges.Should().NotContain(edge => edge.TargetAssemblyName == "System.Runtime",
                "because direct framework references must be excluded.");
            _ = syntheticEdges.Should().NotContain(edge => edge.SourceAssemblyName == edge.TargetAssemblyName,
                "because self-referential dependency edges must be ignored.");
            _ = syntheticEdges.Should().ContainSingle(edge =>
                    edge.SourceAssemblyName == "Documentation.Exporter.Core" &&
                    edge.TargetAssemblyName == "CycleLeft")
                .Which.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Fixed,
                    "because seeded direct reference constraints take priority over .deps.json fallback.");
            _ = syntheticEdges.Should().ContainSingle(edge =>
                    edge.SourceAssemblyName == "CycleLeft" &&
                    edge.TargetAssemblyName == "CycleRight")
                .Which.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Range);
            _ = syntheticEdges.Should().ContainSingle(edge =>
                    edge.SourceAssemblyName == "CycleRight" &&
                    edge.TargetAssemblyName == "CycleLeft")
                .Which.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.Floating,
                    "because cycle back-edges are recorded once while CycleLeft is not re-expanded infinitely.");
            _ = syntheticEdges.Should().ContainSingle(edge =>
                    edge.SourceAssemblyName == "Documentation.Exporter.Core" &&
                    edge.TargetAssemblyName == "TransitiveProject")
                .Which.VersionLinkingKind.Should().Be(AssemblyVersionLinkingKind.ProjectLinked);
            _ = syntheticEdges.Should().ContainSingle(edge =>
                    edge.SourceAssemblyName == "Documentation.Exporter.Core" &&
                    edge.TargetAssemblyName == "DirectRefLib")
                .Which.ReferenceKind.Should().Be(AssemblyReferenceKind.Assembly);
            _ = syntheticEdges.Should().ContainSingle(edge =>
                    edge.SourceAssemblyName == "CycleRight" &&
                    edge.TargetAssemblyName == badLibName)
                .Which.IsTargetStrongNamed.Should().BeFalse(
                    "because malformed local DLL binaries fall back cleanly to unsigned without throwing.");
        }
        finally
        {
            if (File.Exists(badDllPath))
            {
                File.Delete(badDllPath);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
