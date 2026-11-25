using Documentation.Exporter.Abstractions.Parsing.Tags;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Parsing;
using Documentation.Exporter.Core.Parsing.Tags;
using Documentation.Exporter.Core.Resolution;
using Documentation.Exporter.Core.Tests.TestCategories;
using FluentAssertions;
using Xunit;

namespace Documentation.Exporter.Core.Tests.Parsing;

public sealed class XmlDocumentationParserTests
{
    private static XmlDocumentationParser CreateDefaultParser()
    {
        var linkResolver = new TypeLinkResolver();
        var tagHandlers = new IXmlTagHandler[]
        {
            new CodeTagHandler(),
            new SeeTagHandler(linkResolver),
            new ParamRefTagHandler(),
            new ParagraphTagHandler(),
            new ListTagHandler(),
            new PassthroughTagHandler()
        };
        var transformer = new XmlTagTransformer(tagHandlers);
        return new XmlDocumentationParser(transformer, linkResolver, new Documentation.Exporter.Core.IO.PhysicalFileSystem());
    }

    [Fact]
    [UnitTest]
    public void ConstructorWhenDependenciesNullShouldThrowArgumentNullException()
    {
        var transformer = new XmlTagTransformer([]);
        var linkResolver = new TypeLinkResolver();
        var fileSystem = new Documentation.Exporter.Core.IO.PhysicalFileSystem();

        _ = Assert.Throws<ArgumentNullException>(() => new XmlDocumentationParser(null!, linkResolver, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new XmlDocumentationParser(transformer, null!, fileSystem));
        _ = Assert.Throws<ArgumentNullException>(() => new XmlDocumentationParser(transformer, linkResolver, null!));
    }

    private static (XmlDocumentationParser Parser, string TempFilePath) CreateParserWithSampleXml()
    {
        var sampleXml = @"<doc>
    <assembly><name>SampleAssembly</name></assembly>
    <members>
        <member name=""T:SampleNamespace.SampleService"">
            <summary>A sample service class.</summary>
            <remarks>Detailed remarks about the service.</remarks>
        </member>
        <member name=""M:SampleNamespace.SampleService.#ctor(System.String)"">
            <summary>Initializes a new instance of the sample service.</summary>
            <param name=""name"">The service name.</param>
        </member>
        <member name=""M:SampleNamespace.SampleService.Execute(System.String)"">
            <summary>Executes the sample service.</summary>
            <param name=""input"">The input string.</param>
            <returns>True if successful.</returns>
            <exception cref=""T:System.ArgumentNullException"">Thrown when input is null.</exception>
        </member>
        <member name=""P:SampleNamespace.SampleService.Timeout"">
            <summary>The timeout in seconds.</summary>
            <value>Positive integer timeout.</value>
        </member>
        <member name=""F:SampleNamespace.SampleService.DefaultTimeout"">
            <summary>The default timeout value.</summary>
        </member>
        <member name=""E:SampleNamespace.SampleService.Completed"">
            <summary>Occurs when execution completes.</summary>
        </member>
    </members>
</doc>";
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, sampleXml);

        var parser = CreateDefaultParser();
        return (parser, tempFilePath);
    }

    [Fact]
    [IntegrationTest]
    public async Task TryLoadXmlDocumentationWhenFileValidShouldReturnTrueAndIndexMembers()
    {
        // Arrange
        var (parser, tempFilePath) = CreateParserWithSampleXml();

        try
        {
            // Act
            var success = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            // Assert
            _ = success.Should().BeTrue("because valid XML documentation file was provided.");

            var typeModel = new TypeDocumentationModel { TypeName = "SampleService" };
            var documentedTypeModel = parser.ApplyTypeDocumentation("SampleNamespace.SampleService", typeModel);

            _ = documentedTypeModel.SummaryDescription.Should().Be("A sample service class.");
            _ = documentedTypeModel.RemarksDescription.Should().Be("Detailed remarks about the service.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [UnitTest]
    public async Task TryLoadXmlDocumentationWhenPathNullOrWhitespaceShouldReturnFalse()
    {
        // Arrange
        var parser = CreateDefaultParser();

        // Act
        var nullResult = await parser.TryLoadXmlDocumentationAsync(null);
        var emptyResult = await parser.TryLoadXmlDocumentationAsync("   ");

        // Assert
        _ = nullResult.Should().BeFalse("because null path is invalid.");
        _ = emptyResult.Should().BeFalse("because whitespace path is invalid.");
    }

    [Fact]
    [IntegrationTest]
    public async Task TryLoadXmlDocumentationWhenFileDoesNotExistShouldReturnFalse()
    {
        // Arrange
        var parser = CreateDefaultParser();

        // Act
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent", "MissingXmlFile.xml");
        var result = await parser.TryLoadXmlDocumentationAsync(nonExistentPath);

        // Assert
        _ = result.Should().BeFalse("because the file does not exist.");
    }

    [Fact]
    [IntegrationTest]
    public async Task TryLoadXmlDocumentationWhenXmlHasNoMembersRootShouldReturnTrue()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "<doc><assembly><name>Test</name></assembly></doc>");
        var parser = CreateDefaultParser();

        try
        {
            // Act
            var result = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            // Assert
            _ = result.Should().BeTrue("because XML documentation file without members element is valid XML with zero members.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task TryLoadXmlDocumentationWhenXmlMalformedShouldReturnFalse()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "<doc><unclosed-tag>");
        var parser = CreateDefaultParser();

        try
        {
            // Act
            var result = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            // Assert
            _ = result.Should().BeFalse("because XML documentation file is malformed.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ApplyMemberDocumentationShouldExtractSummaryParametersReturnsAndExceptions()
    {
        // Arrange
        var (parser, tempFilePath) = CreateParserWithSampleXml();

        try
        {
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            var methodModel = new MemberDocumentationModel
            {
                MemberName = "Execute",
                MemberParameters = [new ParameterDocumentationModel { ParameterName = "input" }]
            };

            // Act
            var documentedMethodModel = parser.ApplyMemberDocumentation("M:SampleNamespace.SampleService.Execute(System.String)", methodModel);

            // Assert
            _ = documentedMethodModel.SummaryDescription.Should().Be("Executes the sample service.");
            _ = documentedMethodModel.ReturnValueDescription.Should().Be("True if successful.");
            _ = documentedMethodModel.MemberParameters[0].ParameterDescription.Should().Be("The input string.");
            _ = documentedMethodModel.DocumentedExceptions.Should().ContainKey("System.ArgumentNullException")
                .WhoseValue.Should().Be("Thrown when input is null.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ApplyTypeDocumentationWithExamplesAndSeeAlsoShouldPopulateCollections()
    {
        // Arrange
        var sampleXml = @"<doc>
    <assembly><name>SampleAssembly</name></assembly>
    <members>
        <member name=""T:SampleNamespace.AdvancedService"">
            <summary>Advanced service summary.</summary>
            <example><code>var service = new AdvancedService();</code></example>
            <seealso cref=""T:SampleNamespace.SampleService""/>
        </member>
        <member name=""M:SampleNamespace.AdvancedService.Process"">
            <summary>Process summary.</summary>
            <returns>Integer code.</returns>
            <example><code>service.Process();</code></example>
            <seealso cref=""T:System.Int32""/>
            <exception cref=""T:System.InvalidOperationException"">Thrown when in invalid state.</exception>
        </member>
    </members>
</doc>";
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, sampleXml);
        var parser = CreateDefaultParser();

        try
        {
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);
            var typeDocumentationModel = new TypeDocumentationModel { TypeName = "AdvancedService" };
            var memberDocumentationModel = new MemberDocumentationModel { MemberName = "Process" };

            // Act
            var documentedType = parser.ApplyTypeDocumentation("SampleNamespace.AdvancedService", typeDocumentationModel);
            var documentedMember = parser.ApplyMemberDocumentation("M:SampleNamespace.AdvancedService.Process", memberDocumentationModel);

            // Assert
            _ = documentedType.CodeExamples.Should().HaveCount(1);
            _ = documentedType.SeeAlsoReferences.Should().HaveCount(1);
            _ = documentedMember.ReturnValueDescription.Should().Be("Integer code.");
            _ = documentedMember.CodeExamples.Should().HaveCount(1);
            _ = documentedMember.SeeAlsoReferences.Should().HaveCount(1);
            _ = documentedMember.DocumentedExceptions.Should().ContainKey("System.InvalidOperationException");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [UnitTest]
    public void ApplyDocumentationWithMissingOrEmptyIdentifiersShouldReturnUnchangedModel()
    {
        // Arrange
        var parser = CreateDefaultParser();
        var typeDocumentationModel = new TypeDocumentationModel();
        var memberDocumentationModel = new MemberDocumentationModel();

        // Act
        var emptyTypeResult = parser.ApplyTypeDocumentation(string.Empty, typeDocumentationModel);
        var missingTypeResult = parser.ApplyTypeDocumentation("NonExistent.Type", typeDocumentationModel);
        var emptyMemberResult = parser.ApplyMemberDocumentation(string.Empty, memberDocumentationModel);
        var missingMemberResult = parser.ApplyMemberDocumentation("M:NonExistent.Member", memberDocumentationModel);

        // Assert
        _ = emptyTypeResult.SummaryDescription.Should().BeEmpty();
        _ = missingTypeResult.SummaryDescription.Should().BeEmpty();
        _ = emptyMemberResult.SummaryDescription.Should().BeEmpty();
        _ = missingMemberResult.SummaryDescription.Should().BeEmpty();
    }

    [Fact]
    [IntegrationTest]
    public async Task ApplyTypeDocumentationWithPrimaryAndDefaultConstructorsShouldPopulateConstructorDescriptions()
    {
        // Arrange
        var sampleXml = @"<doc>
    <assembly><name>SampleAssembly</name></assembly>
    <members>
        <member name=""T:SampleNamespace.PrimaryService"">
            <summary>Service using a primary constructor.</summary>
            <param name=""endpointUrl"">The target endpoint URL.</param>
            <exception cref=""T:System.ArgumentNullException"">Thrown when endpointUrl is null.</exception>
        </member>
        <member name=""T:SampleNamespace.DefaultConstructorService"">
            <summary>Service using a default parameterless constructor.</summary>
        </member>
    </members>
</doc>";
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, sampleXml);
        var parser = CreateDefaultParser();

        try
        {
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            var primaryTypeModel = new TypeDocumentationModel
            {
                TypeName = "PrimaryService",
                Constructors =
                [
                    new MemberDocumentationModel
                    {
                        MemberName = "PrimaryService",
                        MemberParameters = [new ParameterDocumentationModel { ParameterName = "endpointUrl", ParameterTypeName = "String" }]
                    }
                ]
            };

            var defaultCtorTypeModel = new TypeDocumentationModel
            {
                TypeName = "DefaultConstructorService",
                Constructors =
                [
                    new MemberDocumentationModel
                    {
                        MemberName = "DefaultConstructorService"
                    }
                ]
            };

            var typeModel = new TypeDocumentationModel
            {
                TypeName = "PrimaryWithSecondary",
                Constructors =
                [
                    new MemberDocumentationModel
                    {
                        MemberName = "PrimaryWithSecondary",
                        IsPrimaryConstructor = true,
                        MemberParameters = [new ParameterDocumentationModel { ParameterName = "endpointUrl", ParameterTypeName = "String" }]
                    },
                    new MemberDocumentationModel
                    {
                        MemberName = "PrimaryWithSecondary",
                        IsPrimaryConstructor = false
                    }
                ]
            };

            // Act
            var documentedPrimary = parser.ApplyTypeDocumentation("SampleNamespace.PrimaryService", primaryTypeModel);
            var documentedDefaultCtor = parser.ApplyTypeDocumentation("SampleNamespace.DefaultConstructorService", defaultCtorTypeModel);

            var secondaryCheckParser = CreateDefaultParser();
            var secondaryXmlPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(
                    secondaryXmlPath,
                    """
                    <doc><members><member name="T:SampleNamespace.PrimaryWithSecondary">
                        <summary>Primary constructor type summary.</summary>
                        <param name="endpointUrl">Endpoint parameter.</param>
                    </member></members></doc>
                    """);
                _ = await secondaryCheckParser.TryLoadXmlDocumentationAsync(secondaryXmlPath);
                var documentedOverloaded = secondaryCheckParser.ApplyTypeDocumentation("SampleNamespace.PrimaryWithSecondary", typeModel);
                _ = documentedOverloaded.Constructors[0].SummaryDescription.Should().Be("Primary constructor type summary.");
                _ = documentedOverloaded.Constructors[1].SummaryDescription.Should().BeEmpty(
                    "because undocumented secondary constructors must not silently copy the declaring type summary.");
            }
            finally
            {
                if (File.Exists(secondaryXmlPath))
                {
                    File.Delete(secondaryXmlPath);
                }
            }

            // Assert
            _ = documentedPrimary.Constructors[0].SummaryDescription.Should().Be("Service using a primary constructor.");
            _ = documentedPrimary.Constructors[0].MemberParameters[0].ParameterDescription.Should().Be("The target endpoint URL.");
            _ = documentedPrimary.Constructors[0].DocumentedExceptions.Should().ContainKey("System.ArgumentNullException");
            _ = documentedDefaultCtor.Constructors[0].SummaryDescription.Should().Be("Service using a default parameterless constructor.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ApplyMemberDocumentationWithDuplicateExceptionCrefsAndMultilineTextShouldMergeConditionsAndNormalizeWhitespace()
    {
        // Arrange
        var sampleXml = @"<doc>
    <assembly><name>SampleAssembly</name></assembly>
    <members>
        <member name=""P:SampleNamespace.Showcase.Limit"">
            <summary>Limit.</summary>
            <remarks>
                First line of remarks
                    continues here.
            </remarks>
            <exception cref=""T:System.ArgumentException"">Condition one.</exception>
            <exception cref=""T:System.ArgumentException"">Condition two.</exception>
            <exception cref=""T:System.ArgumentException"">Condition two.</exception>
            <example>Intro text <code>var value = 1;</code></example>
        </member>
        <member name=""T:SampleNamespace.Showcase"">
            <summary>Showcase.</summary>
            <param name=""name"">Name.</param>
            <exception cref=""T:System.ArgumentException"">Type condition one.</exception>
            <exception cref=""T:System.ArgumentException"">Type condition two.</exception>
        </member>
    </members>
</doc>";
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, sampleXml);
        var resolver = new TypeLinkResolver();
        resolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        var tagHandlers = new IXmlTagHandler[] { new CodeTagHandler(), new SeeTagHandler(resolver), new ParamRefTagHandler(), new ParagraphTagHandler(), new ListTagHandler(), new PassthroughTagHandler() };
        var parser = new XmlDocumentationParser(new XmlTagTransformer(tagHandlers), resolver, new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        try
        {
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);
            var propertyModel = new MemberDocumentationModel { MemberName = "Limit" };
            var typeModel = new TypeDocumentationModel
            {
                TypeName = "Showcase",
                Constructors =
                [
                    new MemberDocumentationModel
                    {
                        MemberName = "Showcase",
                        MemberParameters = [new ParameterDocumentationModel { ParameterName = "name", ParameterTypeName = "String" }]
                    }
                ]
            };

            // Act
            var documentedProperty = parser.ApplyMemberDocumentation("P:SampleNamespace.Showcase.Limit", propertyModel);
            var documentedType = parser.ApplyTypeDocumentation("SampleNamespace.Showcase", typeModel);

            // Assert
            var exceptionCondition = documentedProperty.DocumentedExceptions.Should().ContainSingle().Subject.Value;
            _ = exceptionCondition.Should().Be("Condition one.<br/>Condition two.");
            _ = documentedProperty.RemarksDescription.Should().Be("First line of remarks\ncontinues here.");
            _ = documentedProperty.CodeExamples.Should().ContainSingle().Which.Should().Contain("Intro text").And.Contain("var value = 1;");
            _ = documentedType.Constructors[0].DocumentedExceptions.Should().ContainSingle().Which.Value.Should().Be("Type condition one.<br/>Type condition two.");
            _ = documentedType.DocumentedExceptions.Should().ContainSingle("because type-level exceptions are kept on the type itself.")
                .Which.Value.Should().Be("Type condition one.<br/>Type condition two.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ApplyTypeAndMemberDocumentationWithGenericTypeParamsAndDynamicMemberParamsShouldPopulateAllBranches()
    {
        // Arrange
        var sampleXml = @"<doc>
    <assembly><name>SampleAssembly</name></assembly>
    <members>
        <member name=""""/>
        <member name=""T:SampleNamespace.GenericBox`2"">
            <summary>Generic box type.</summary>
            <typeparam name="""">Ignored empty typeparam</typeparam>
            <typeparam name=""TExisting"">Updated existing type parameter description.</typeparam>
            <typeparam name=""TNew"">Added new type parameter description.</typeparam>
            <param name="""">Ignored empty param</param>
            <param name=""boxLabel"">Label for the box.</param>
            <exception cref="""">Ignored empty exception</exception>
            <exception cref=""T:System.ArgumentException"">When label is invalid.</exception>
        </member>
        <member name=""M:SampleNamespace.GenericBox`2.Convert``1(System.String)"">
            <summary>Converts box content.</summary>
            <remarks>Method remarks.</remarks>
            <typeparam name=""TOutput"">Target output type.</typeparam>
            <param name="""">Ignored empty param</param>
            <param name=""dynamicParam"">Dynamically added parameter description.</param>
            <exception cref="""">Ignored empty exception</exception>
        </member>
    </members>
</doc>";
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, sampleXml);

        var resolver = new TypeLinkResolver();
        resolver.RegisterHandler(new MicrosoftDocsNamespaceHandler());
        var tagHandlers = new IXmlTagHandler[]
        {
            new CodeTagHandler(),
            new SeeTagHandler(resolver),
            new ParamRefTagHandler(),
            new ParagraphTagHandler(),
            new ListTagHandler(),
            new PassthroughTagHandler()
        };
        var parser = new XmlDocumentationParser(new XmlTagTransformer(tagHandlers), resolver, new Documentation.Exporter.Core.IO.PhysicalFileSystem());

        try
        {
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            var typeModel = new TypeDocumentationModel
            {
                TypeName = "GenericBox",
                GenericTypeParameterDetails = [new ParameterDocumentationModel { ParameterName = "TExisting", ParameterTypeName = "TExisting" }],
                Constructors =
                [
                    new MemberDocumentationModel
                    {
                        MemberName = "GenericBox",
                        MemberParameters = [new ParameterDocumentationModel { ParameterName = "boxLabel", ParameterTypeName = "String" }]
                    }
                ]
            };

            var methodModel = new MemberDocumentationModel { MemberName = "Convert" };

            // Act
            var documentedType = parser.ApplyTypeDocumentation("T:SampleNamespace.GenericBox`2", typeModel);
            var documentedMethod = parser.ApplyMemberDocumentation("M:SampleNamespace.GenericBox`2.Convert``1(System.String)", methodModel);

            // Assert
            _ = documentedType.GenericTypeParameterDetails.Should().HaveCount(2);
            _ = documentedType.GenericTypeParameterDetails[0].ParameterDescription.Should().Be("Updated existing type parameter description.");
            _ = documentedType.GenericTypeParameterDetails[1].ParameterName.Should().Be("TNew");
            _ = documentedType.GenericTypeParameterDetails[1].ParameterDescription.Should().Be("Added new type parameter description.");
            _ = documentedType.Constructors[0].DocumentedExceptions.Keys.Should().Contain(key => key.Contains("ArgumentException", StringComparison.Ordinal));

            _ = documentedMethod.RemarksDescription.Should().Be("Method remarks.");
            _ = documentedMethod.GenericTypeParameters.Should().ContainSingle(param => param.ParameterName == "TOutput");
            _ = documentedMethod.MemberParameters.Should().ContainSingle(param => param.ParameterName == "dynamicParam");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ApplyTypeAndMemberDocumentationWhenInheritDocPresentShouldExtractFlagAndCref()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        try
        {
            var xmlContent = @"<doc>
    <members>
        <member name=""T:SampleNamespace.DerivedType"">
            <inheritdoc cref=""T:SampleNamespace.BaseType"" />
        </member>
        <member name=""M:SampleNamespace.DerivedType.Execute(System.String)"">
            <inheritdoc cref=""M:SampleNamespace.BaseType.Execute(System.String)"" />
        </member>
        <member name=""P:SampleNamespace.DerivedType.Status"">
            <inheritdoc />
        </member>
    </members>
</doc>";
            File.WriteAllText(tempFilePath, xmlContent);
            var parser = CreateDefaultParser();
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            var typeModel = new TypeDocumentationModel { TypeName = "DerivedType" };
            var methodModel = new MemberDocumentationModel { MemberName = "Execute" };
            var propertyModel = new MemberDocumentationModel { MemberName = "Status" };

            // Act
            var documentedType = parser.ApplyTypeDocumentation("SampleNamespace.DerivedType", typeModel);
            var documentedMethod = parser.ApplyMemberDocumentation("M:SampleNamespace.DerivedType.Execute(System.String)", methodModel);
            var documentedProperty = parser.ApplyMemberDocumentation("P:SampleNamespace.DerivedType.Status", propertyModel);

            // Assert
            _ = documentedType.HasInheritDoc.Should().BeTrue("because <inheritdoc /> is present on the type.");
            _ = documentedType.InheritDocCref.Should().Be("T:SampleNamespace.BaseType");
            _ = documentedMethod.HasInheritDoc.Should().BeTrue("because <inheritdoc /> is present on the method.");
            _ = documentedMethod.InheritDocCref.Should().Be("M:SampleNamespace.BaseType.Execute(System.String)");
            _ = documentedProperty.HasInheritDoc.Should().BeTrue("because <inheritdoc /> is present on the property.");
            _ = documentedProperty.InheritDocCref.Should().BeNull("because no cref attribute was provided.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task ResolveCrefKeyShouldResolveVariousCrefFormatsCorrectly()
    {
        // Arrange
        var (parser, tempFilePath) = CreateParserWithSampleXml();
        try
        {
            _ = await parser.TryLoadXmlDocumentationAsync(tempFilePath);

            // Act & Assert
            _ = parser.ResolveCrefKey("").Should().BeNull("because empty cref cannot be resolved.");
            _ = parser.ResolveCrefKey("   ").Should().BeNull("because whitespace cref cannot be resolved.");
            _ = parser.ResolveCrefKey("M:SampleNamespace.SampleService.Execute(System.String)")
                .Should().Be("M:SampleNamespace.SampleService.Execute(System.String)", "because exact full key matches.");
            _ = parser.ResolveCrefKey("SampleNamespace.SampleService.Execute(System.String)", defaultPrefix: "M")
                .Should().Be("M:SampleNamespace.SampleService.Execute(System.String)", "because prefix was appended.");
            _ = parser.ResolveCrefKey("Execute", declaringTypeFullName: "SampleNamespace.SampleService", defaultPrefix: "M")
                .Should().Be("M:SampleNamespace.SampleService.Execute(System.String)", "because relative method name was resolved against declaring type.");
            _ = parser.ResolveCrefKey("Timeout", declaringTypeFullName: "SampleNamespace.SampleService", defaultPrefix: "P")
                .Should().Be("P:SampleNamespace.SampleService.Timeout", "because relative property was resolved.");
            _ = parser.ResolveCrefKey("DefaultTimeout", defaultPrefix: "F")
                .Should().Be("F:SampleNamespace.SampleService.DefaultTimeout", "because suffix match resolved field.");
            _ = parser.ResolveCrefKey("NonExistentMember")
                .Should().BeNull("because the member does not exist in indexed XML.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    [IntegrationTest]
    public async Task TryLoadXmlDocumentationFilesAsyncWhenEmptyOrContainingInvalidPathShouldBehaveCorrectly()
    {
        // Arrange
        var (parser, tempFilePath) = CreateParserWithSampleXml();
        try
        {
            // Act & Assert
            _ = (await parser.TryLoadXmlDocumentationFilesAsync([])).Should().BeFalse("because an empty file collection provides no XML documentation files to load.");
            _ = (await parser.TryLoadXmlDocumentationFilesAsync([tempFilePath])).Should().BeTrue("because existing valid XML documentation should load cleanly.");
            _ = (await parser.TryLoadXmlDocumentationFilesAsync([tempFilePath, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.xml")]))
                .Should().BeFalse("because a missing file in the batch causes overall load status to be false.");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
