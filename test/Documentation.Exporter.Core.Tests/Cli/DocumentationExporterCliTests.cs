using System.CommandLine;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Cli;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Cli;

/// <summary>
/// Unit tests for DI-configured CLI command building, option parsing, and error handling.
/// </summary>
[Collection("ConsoleOutputTests")]
public sealed class DocumentationExporterCliTests
{
    private sealed class RecordingConsoleOutput : IConsoleOutput
    {
        public List<string> ErrorLines { get; } = [];

        public void WriteErrorLine(string message) => ErrorLines.Add(message);
    }

    private sealed class StubPipelineRunner(Func<ExporterOptions, LogLevel, Task<int>> handler) : ICliPipelineRunner
    {
        public Task<int> RunAsync(ExporterOptions exporterOptions, LogLevel logLevel, CancellationToken cancellationToken = default) =>
            handler(exporterOptions, logLevel);
    }

    private static RootCommand BuildTestRootCommand(
        Func<ExporterOptions, LogLevel, Task<int>>? handler = null,
        RecordingConsoleOutput? consoleOutput = null)
    {
        ServiceCollection serviceCollection = new();
        _ = serviceCollection.AddDocumentationExporterCli();
        _ = serviceCollection.AddSingleton<IConsoleOutput>(consoleOutput ?? new RecordingConsoleOutput());

        if (handler != null)
        {
            _ = serviceCollection.AddSingleton<ICliPipelineRunner>(new StubPipelineRunner(handler));
        }

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ICliCommandBuilder>().BuildRootCommand();
    }

    [Fact]
    [UnitTest]
    public void ParseExternalDocRulesWithValidRulesShouldReturnParsedRules()
    {
        // Arrange
        string[] rawRules =
        [
            "Serilog*=https://serilog.net/api/{type}",
            "Microsoft.Extensions.*=https://learn.microsoft.com/dotnet/api/{type}"
        ];

        // Act
        var parsedRules = SharedCliOptions.ParseExternalDocRules(rawRules);

        // Assert
        _ = parsedRules.Should().HaveCount(2, "because both valid rule expressions should be parsed successfully.");
        _ = parsedRules[0].NamespacePrefix.Should().Be("Serilog*");
        _ = parsedRules[0].UrlTemplate.Should().Be("https://serilog.net/api/{type}");
        _ = parsedRules[1].NamespacePrefix.Should().Be("Microsoft.Extensions.*");
    }

    [Fact]
    [UnitTest]
    public void ParseExternalDocRulesWithInvalidRuleFormatShouldThrowArgumentException()
    {
        // Arrange
        string[] invalidRules = ["InvalidRuleWithoutEqualsSign"];

        // Act
        var parseAction = () => SharedCliOptions.ParseExternalDocRules(invalidRules);

        // Assert
        _ = parseAction.Should().Throw<ArgumentException>("because external doc rules must contain an '=' delimiter.")
            .WithMessage("*Invalid external documentation link rule format*");
    }

    [Fact]
    [UnitTest]
    public void ParseExternalDocRulesWithNullArgumentShouldThrowArgumentNullException()
    {
        // Act
        var nullAction = () => SharedCliOptions.ParseExternalDocRules(null!);

        // Assert
        _ = nullAction.Should().Throw<ArgumentNullException>("because rawRules cannot be null.");
    }

    [Fact]
    [IntegrationTest]
    public async Task BuildRootCommandWithAssemblyOptionsShouldBindAllOptionsCorrectly()
    {
        // Arrange
        ExporterOptions? capturedOptions = null;
        LogLevel? capturedLogLevel = null;

        var rootCommand = BuildTestRootCommand((options, logLevel) =>
        {
            capturedOptions = options;
            capturedLogLevel = logLevel;
            return Task.FromResult(0);
        });

        var dummyAssembly = Path.Combine(Path.GetTempPath(), "Dummy.dll");
        var dummyXml = Path.Combine(Path.GetTempPath(), "Dummy.xml");
        var dummyOutputDir = Path.Combine(Path.GetTempPath(), "DocOut");
        var dummyReportFile = Path.Combine(Path.GetTempPath(), "quality-report.sarif");

        string[] commandArguments =
        [
            "assembly",
            "--assembly", dummyAssembly,
            "--xml", dummyXml,
            "--output", dummyOutputDir,
            "--clean",
            "--strict",
            "--fail-on-undocumented", "85.5",
            "--exclude-types", "*Internal*", "Obsolete*",
            "--exclude-namespaces", "*.Migrations",
            "--exclude-editor-browsable-never", "false",
            "--diagram-format", "PlantUml",
            "--report-file", dummyReportFile,
            "--report-format", "Sarif",
            "--log-level", "Debug"
        ];

        // Act
        var parseResult = rootCommand.Parse(commandArguments);
        _ = parseResult.Errors.Should().BeEmpty("because options should parse without syntax errors: " + string.Join("; ", parseResult.Errors.Select(error => error.Message)));
        var exitCode = await rootCommand.InvokeAsync(commandArguments);

        // Assert
        _ = exitCode.Should().Be(0, "because the assembly command syntax is valid.");
        _ = capturedOptions.Should().NotBeNull("because the pipeline runner must be invoked.");
        _ = capturedOptions!.AssemblyFilePaths.Should().Contain(dummyAssembly);
        _ = capturedOptions.XmlDocumentationFilePaths.Should().Contain(dummyXml);
        _ = capturedOptions.OutputDirectoryPath.Should().Be(dummyOutputDir);
        _ = capturedOptions.CleanOutputDirectory.Should().BeTrue("because --clean was specified.");
        _ = capturedOptions.StrictLinkValidation.Should().BeTrue("because --strict was specified.");
        _ = capturedOptions.FailOnUndocumentedThreshold.Should().Be(85.5, "because --fail-on-undocumented was set to 85.5.");
        _ = capturedOptions.ExcludedTypes.Should().Contain(["*Internal*", "Obsolete*"]);
        _ = capturedOptions.ExcludedNamespaces.Should().Contain("*.Migrations");
        _ = capturedOptions.ExcludeEditorBrowsableNever.Should().BeFalse("because --exclude-editor-browsable-never false was passed.");
        _ = capturedOptions.DiagramFormat.Should().Be(DiagramFormat.PlantUml);
        _ = capturedOptions.ReportFilePath.Should().Be(dummyReportFile, "because --report-file must bind to ReportFilePath.");
        _ = capturedOptions.ReportFormat.Should().Be(QualityReportFormat.Sarif, "because --report-format Sarif must bind to ReportFormat.");
        _ = capturedLogLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    [IntegrationTest]
    public async Task BuildRootCommandWithSolutionOptionsShouldBindAllOptionsCorrectly()
    {
        // Arrange
        ExporterOptions? capturedOptions = null;

        var rootCommand = BuildTestRootCommand((options, _) =>
        {
            capturedOptions = options;
            return Task.FromResult(0);
        });

        var dummySolution = Path.Combine(Path.GetTempPath(), "App.sln");
        var dummyOutputDir = Path.Combine(Path.GetTempPath(), "DocOut");

        string[] commandArguments =
        [
            "solution",
            "--solution", dummySolution,
            "--output", dummyOutputDir,
            "--configuration", "Release",
            "--include-tests",
            "--fail-on-missing",
            "--exclude", "*.Benchmarks"
        ];

        // Act
        var parseResult = rootCommand.Parse(commandArguments);
        _ = parseResult.Errors.Should().BeEmpty("because options should parse without syntax errors: " + string.Join("; ", parseResult.Errors.Select(error => error.Message)));
        var exitCode = await rootCommand.InvokeAsync(commandArguments);

        // Assert
        _ = exitCode.Should().Be(0, "because valid solution options were provided.");
        _ = capturedOptions.Should().NotBeNull();
        _ = capturedOptions!.SolutionFilePath.Should().Be(dummySolution);
        _ = capturedOptions.SolutionConfiguration.Should().Be("Release");
        _ = capturedOptions.IncludeTestProjects.Should().BeTrue();
        _ = capturedOptions.FailOnMissingProjectArtifacts.Should().BeTrue();
        _ = capturedOptions.ExcludedProjects.Should().Contain("*.Benchmarks");
        _ = capturedOptions.DiagramFormat.Should().Be(DiagramFormat.Mermaid, "because DiagramFormat defaults to Mermaid when omitted.");
    }

    [Fact]
    [IntegrationTest]
    public async Task BuildRootCommandWithInvalidExternalDocsShouldReturnExitCode2()
    {
        // Arrange
        var rootCommand = BuildTestRootCommand();
        var dummyAssembly = Path.Combine(Path.GetTempPath(), "Dummy.dll");
        var dummyOutputDir = Path.Combine(Path.GetTempPath(), "DocOut");

        string[] commandArguments =
        [
            "assembly",
            "--assembly", dummyAssembly,
            "--output", dummyOutputDir,
            "--external-docs", "InvalidRuleWithoutEquals"
        ];

        // Act
        var exitCode = await rootCommand.InvokeAsync(commandArguments);

        // Assert
        _ = exitCode.Should().Be(2, "because malformed --external-docs rules must fail with exit code 2.");
    }

    [Fact]
    [IntegrationTest]
    public async Task BuildRootCommandWithMissingConfigFileShouldReturnExitCode2()
    {
        // Arrange
        var rootCommand = BuildTestRootCommand();
        var missingConfigPath = Path.Combine(Path.GetTempPath(), $"MissingConfig_{Guid.NewGuid():N}.json");

        string[] commandArguments =
        [
            "config",
            "--config", missingConfigPath
        ];

        // Act
        var exitCode = await rootCommand.InvokeAsync(commandArguments);

        // Assert
        _ = exitCode.Should().Be(2, "because a missing config file must fail with exit code 2.");
    }

    [Fact]
    [UnitTest]
    public void CliConstructorsAndSharedOptionsArgumentGuardsShouldThrowArgumentNullException()
    {
        // Arrange
        RecordingConsoleOutput consoleOutput = new();
        SharedCliOptions sharedOptions = new(consoleOutput);
        StubPipelineRunner runner = new((_, _) => Task.FromResult(0));
        var configLoader = new Documentation.Exporter.Core.Configuration.ExporterConfigurationLoader(
            new Documentation.Exporter.Core.IO.PhysicalFileSystem());
        var (assemblyOption, xmlOption) = SharedCliOptions.CreateAssemblyInputOptions();
        Command sampleCommand = new("sample");
        RootCommand rootCommand = BuildTestRootCommand();
        var parseResult = rootCommand.Parse(["--help"]);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new SharedCliOptions(null!));
        _ = Assert.Throws<ArgumentNullException>(() => new CliCommandBuilder(null!, [], runner));
        _ = Assert.Throws<ArgumentNullException>(() => new CliCommandBuilder(sharedOptions, null!, runner));
        _ = Assert.Throws<ArgumentNullException>(() => new CliCommandBuilder(sharedOptions, [], null!));
        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyCliSubcommand(null!, runner));
        _ = Assert.Throws<ArgumentNullException>(() => new AssemblyCliSubcommand(sharedOptions, null!));
        _ = Assert.Throws<ArgumentNullException>(() => new SolutionCliSubcommand(null!, runner));
        _ = Assert.Throws<ArgumentNullException>(() => new SolutionCliSubcommand(sharedOptions, null!));
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigCliSubcommand(null!, configLoader, runner, consoleOutput));
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigCliSubcommand(sharedOptions, null!, runner, consoleOutput));
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigCliSubcommand(sharedOptions, configLoader, null!, consoleOutput));
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigCliSubcommand(sharedOptions, configLoader, runner, null!));

        _ = Assert.Throws<ArgumentNullException>(() => sharedOptions.AddExportOptionsTo(null!, false));
        _ = Assert.Throws<ArgumentNullException>(() => sharedOptions.TryCreateBaseExporterOptions(null!, out _, out _));
        _ = Assert.Throws<ArgumentNullException>(() => SharedCliOptions.ApplyAssemblyInputs(null!, parseResult, assemblyOption, xmlOption));
        _ = Assert.Throws<ArgumentNullException>(() => SharedCliOptions.ApplyAssemblyInputs(new ExporterOptions(), null!, assemblyOption, xmlOption));
        _ = Assert.Throws<ArgumentNullException>(() => SharedCliOptions.ApplyAssemblyInputs(new ExporterOptions(), parseResult, null!, xmlOption));
        _ = Assert.Throws<ArgumentNullException>(() => SharedCliOptions.ApplyAssemblyInputs(new ExporterOptions(), parseResult, assemblyOption, null!));
        sharedOptions.AddExportOptionsTo(sampleCommand, includeFailOnMissing: true);
        _ = sampleCommand.Options.Should().Contain(option => option.Name == "fail-on-missing");
    }

    [Fact]
    [UnitTest]
    public void ParseExternalDocRulesShouldIgnoreBlankEntries()
    {
        // Act
        var rules = SharedCliOptions.ParseExternalDocRules(["   ", string.Empty, "Serilog*=https://serilog.net/{name}"]);

        // Assert
        _ = rules.Should().ContainSingle("because blank entries must be skipped safely.");
        _ = rules[0].NamespacePrefix.Should().Be("Serilog*");
    }

    [Fact]
    [IntegrationTest]
    public async Task RootCommandWithAssemblyOptionsAndOptionalFlagsShouldBindAndValidateErrors()
    {
        // Arrange
        ExporterOptions? capturedOptions = null;
        RecordingConsoleOutput recordingConsole = new();
        var rootCommand = BuildTestRootCommand(
            (options, _) =>
            {
                capturedOptions = options;
                return Task.FromResult(0);
            },
            recordingConsole);

        var tempRoot = Path.GetTempPath();
        var dummyAssembly = Path.Combine(tempRoot, "RootApp.dll");
        var dummyOutput = Path.Combine(tempRoot, "RootOut");
        var dummyExamples = Path.Combine(tempRoot, "Examples");
        var dummyOverrides = Path.Combine(tempRoot, "Overrides");
        var dummyNav = Path.Combine(tempRoot, "mkdocs.yml");

        // Act - valid root command with optional path flags
        var successExitCode = await rootCommand.InvokeAsync(
        [
            "--assembly", dummyAssembly,
            "--output", dummyOutput,
            "--examples-dir", dummyExamples,
            "--overrides-dir", dummyOverrides,
            "--nav-file", dummyNav,
            "--nav-entry-point", "API Reference",
            "--nav-url-prefix", "api",
            "--msdocs-url", "https://custom.docs.example/api/",
            "--external-docs", "Vendor*=https://vendor.example/{name}"
        ]);

        // Act - invalid external docs on root and solution commands
        var rootInvalidExitCode = await rootCommand.InvokeAsync(
        [
            "--assembly", dummyAssembly,
            "--output", dummyOutput,
            "--external-docs", "MalformedRule"
        ]);
        var solutionInvalidExitCode = await rootCommand.InvokeAsync(
        [
            "solution",
            "--solution", Path.Combine(tempRoot, "App.slnx"),
            "--output", dummyOutput,
            "--external-docs", "MalformedSolutionRule"
        ]);

        // Act - invalid percentage on --fail-on-undocumented
        var invalidPercentageParse = rootCommand.Parse(
        [
            "assembly",
            "--assembly", dummyAssembly,
            "--output", dummyOutput,
            "--fail-on-undocumented", "not-a-number"
        ]);

        // Assert
        _ = successExitCode.Should().Be(0, "because root command invocation with valid arguments must succeed.");
        _ = capturedOptions.Should().NotBeNull();
        _ = capturedOptions!.ExamplesDirectoryPath.Should().Be(dummyExamples);
        _ = capturedOptions.OverridesDirectoryPath.Should().Be(dummyOverrides);
        _ = capturedOptions.NavigationFilePath.Should().Be(dummyNav);
        _ = capturedOptions.NavigationEntryPoint.Should().Be("API Reference");
        _ = capturedOptions.NavigationUrlPrefix.Should().Be("api");
        _ = capturedOptions.MicrosoftDocsBaseUrl.Should().Be("https://custom.docs.example/api/");
        _ = capturedOptions.ExternalDocLinkRules.Should().ContainSingle();

        _ = rootInvalidExitCode.Should().Be(2, "because root command malformed external docs must return exit code 2.");
        _ = solutionInvalidExitCode.Should().Be(2, "because solution command malformed external docs must return exit code 2.");
        _ = recordingConsole.ErrorLines.Should().HaveCount(2, "because both malformed rule invocations must write error output.");
        _ = invalidPercentageParse.Errors.Should().NotBeEmpty("because non-numeric percentage must fail option parsing.");
    }

    [Fact]
    [IntegrationTest]
    public async Task ConfigSubcommandWithValidAndMalformedJsonFilesShouldReturnExpectedExitCodes()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"DocExporter_ConfigCli_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(tempDirectory);
        var validConfigPath = Path.Combine(tempDirectory, "doc-exporter.valid.json");
        var malformedConfigPath = Path.Combine(tempDirectory, "doc-exporter.invalid.json");

        await File.WriteAllTextAsync(validConfigPath, """
            {
              "assemblyFilePath": "Sample.dll",
              "xmlDocumentationFilePath": "Sample.xml",
              "outputDirectoryPath": "docs-out"
            }
            """);
        await File.WriteAllTextAsync(malformedConfigPath, "{ invalid json content ");

        try
        {
            ExporterOptions? loadedOptions = null;
            RecordingConsoleOutput recordingConsole = new();
            var rootCommand = BuildTestRootCommand(
                (options, _) =>
                {
                    loadedOptions = options;
                    return Task.FromResult(0);
                },
                recordingConsole);

            // Act
            var validExitCode = await rootCommand.InvokeAsync(["config", "--config", validConfigPath]);
            var malformedExitCode = await rootCommand.InvokeAsync(["config", "--config", malformedConfigPath]);

            // Assert
            _ = validExitCode.Should().Be(0, "because a valid config JSON file invokes the pipeline runner and returns exit code 0.");
            _ = loadedOptions.Should().NotBeNull();
            _ = loadedOptions!.OutputDirectoryPath.Should().Be(Path.GetFullPath(Path.Combine(tempDirectory, "docs-out")));
            _ = loadedOptions.AssemblyFilePath.Should().Be(Path.GetFullPath(Path.Combine(tempDirectory, "Sample.dll")),
                "because singular assemblyFilePath in JSON must be resolved relative to the config file directory.");
            _ = loadedOptions.XmlDocumentationFilePath.Should().Be(Path.GetFullPath(Path.Combine(tempDirectory, "Sample.xml")),
                "because singular xmlDocumentationFilePath in JSON must be resolved relative to the config file directory.");
            _ = malformedExitCode.Should().Be(2, "because malformed config JSON must return exit code 2.");
            _ = recordingConsole.ErrorLines.Should().ContainSingle(line => line.Contains(malformedConfigPath, StringComparison.Ordinal));

            // Act & Assert - Unknown property typo must fail with exit code 2 and CLI override flags on config must apply
            var typoConfigPath = Path.Combine(tempDirectory, "doc-exporter.typo.json");
            await File.WriteAllTextAsync(typoConfigPath, """
                {
                  "assemblyFilePath": "Sample.dll",
                  "outputDirectoryPath": "docs-out",
                  "strictLinkValidaton": true
                }
                """);
            var typoExitCode = await rootCommand.InvokeAsync(["config", "--config", typoConfigPath]);
            _ = typoExitCode.Should().Be(2, "because unknown/misspelled JSON properties must be rejected with exit code 2.");

            var customOverrideOutput = Path.Combine(tempDirectory, "custom-override-out");
            var overrideExitCode = await rootCommand.InvokeAsync(
            [
                "config",
                "--config", validConfigPath,
                "--output", customOverrideOutput,
                "--strict",
                "--clean",
                "--diagram-format", "PlantUml"
            ]);
            _ = overrideExitCode.Should().Be(0);
            _ = loadedOptions.OutputDirectoryPath.Should().Be(Path.GetFullPath(customOverrideOutput),
                "because --output CLI flag overrides outputDirectoryPath from config JSON.");
            _ = loadedOptions.StrictLinkValidation.Should().BeTrue("because --strict CLI flag overrides config JSON.");
            _ = loadedOptions.CleanOutputDirectory.Should().BeTrue("because --clean CLI flag overrides config JSON.");
            _ = loadedOptions.DiagramFormat.Should().Be(DiagramFormat.PlantUml, "because --diagram-format CLI flag overrides config JSON.");

            // Verify config preserves JSON diagramFormat when --diagram-format is omitted on CLI
            var jsonWithPlantUmlPath = Path.Combine(tempDirectory, "doc-exporter.plantuml.json");
            await File.WriteAllTextAsync(jsonWithPlantUmlPath, """
                {
                  "assemblyFilePath": "Sample.dll",
                  "xmlDocumentationFilePath": "Sample.xml",
                  "outputDirectoryPath": "docs-out",
                  "diagramFormat": "PlantUml"
                }
                """);
            var preserveExitCode = await rootCommand.InvokeAsync(["config", "--config", jsonWithPlantUmlPath]);
            _ = preserveExitCode.Should().Be(0);
            _ = loadedOptions.DiagramFormat.Should().Be(DiagramFormat.PlantUml,
                "because omitting --diagram-format on config CLI must preserve diagramFormat from JSON.");

            // Verify custom plugin diagram format name binds via CLI
            var customDiagramExitCode = await rootCommand.InvokeAsync(
            [
                "config",
                "--config", validConfigPath,
                "--diagram-format", "D2"
            ]);
            _ = customDiagramExitCode.Should().Be(0);
            _ = loadedOptions.DiagramFormat.Should().Be(DiagramFormat.Custom("D2"),
                "because CLI --diagram-format must accept custom plugin diagram format identifiers.");

            // Verify invalid --diagram-format fails parsing
            var invalidDiagramParse = rootCommand.Parse(
            [
                "config",
                "--config", validConfigPath,
                "--diagram-format", "123Invalid"
            ]);
            _ = invalidDiagramParse.Errors.Should().NotBeEmpty(
                "because diagram format names starting with a digit must be rejected at CLI parse time.");

            // Verify custom plugin --nav-format and --report-format bind cleanly and invalid values fail parsing
            var customFormatExitCode = await rootCommand.InvokeAsync(
            [
                "config",
                "--config", validConfigPath,
                "--nav-format", "Docusaurus",
                "--report-format", "JUnit"
            ]);
            _ = customFormatExitCode.Should().Be(0);
            _ = loadedOptions.NavigationFormat.Should().Be(NavigationFormat.Custom("Docusaurus"),
                "because CLI --nav-format must accept custom plugin navigation format identifiers.");
            _ = loadedOptions.ReportFormat.Should().Be(QualityReportFormat.Custom("JUnit"),
                "because CLI --report-format must accept custom plugin report format identifiers.");

            var invalidNavParse = rootCommand.Parse(
            [
                "config",
                "--config", validConfigPath,
                "--nav-format", "99Bad"
            ]);
            _ = invalidNavParse.Errors.Should().NotBeEmpty(
                "because navigation format names starting with a digit must be rejected at CLI parse time.");

            var invalidReportParse = rootCommand.Parse(
            [
                "config",
                "--config", validConfigPath,
                "--report-format", "99Bad"
            ]);
            _ = invalidReportParse.Errors.Should().NotBeEmpty(
                "because quality report format names starting with a digit must be rejected at CLI parse time.");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ProgramMainWithHelpArgumentShouldReturnZero()
    {
        // Arrange
        var originalOutWriter = Console.Out;
        using StringWriter capturedOutWriter = new();

        try
        {
            Console.SetOut(capturedOutWriter);

            // Act
            var exitCode = await Documentation.Exporter.Cli.Program.Main(["--help"]);

            // Assert
            _ = exitCode.Should().Be(0, "because --help invocation on the real CLI entrypoint must exit cleanly.");
            _ = capturedOutWriter.ToString().Should().Contain("assembly", "because --help must render subcommands.");
        }
        finally
        {
            Console.SetOut(originalOutWriter);
        }
    }
}
