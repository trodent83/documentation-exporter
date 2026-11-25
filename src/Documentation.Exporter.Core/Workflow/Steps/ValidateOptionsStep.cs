using Documentation.Exporter.Abstractions.Workflow;
using Documentation.Exporter.Abstractions.Resources;
using System.ComponentModel.DataAnnotations;
using Documentation.Exporter.Abstractions.Models;
using Documentation.Exporter.Core.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Documentation.Exporter.Core.Workflow.Steps;

/// <summary>
/// Validates <see cref="ExporterOptions"/> using DataAnnotations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ValidateOptionsStep"/> class.
/// </remarks>
/// <param name="logger">Optional ILogger instance.</param>
public sealed partial class ValidateOptionsStep(ILogger<ValidateOptionsStep>? logger = null) : IExportWorkflowStep
{
    private readonly ILogger<ValidateOptionsStep> logger = logger ?? NullLogger<ValidateOptionsStep>.Instance;

    /// <inheritdoc />
    public string StepName => ExecutionMessagesResources.StepNameValidateOptions;

    [LoggerMessage(EventId = 811, Level = LogLevel.Error, Message = "{StatusMessage}")]
    private partial void LogValidationError(string statusMessage);

    /// <inheritdoc />
    public Task<bool> ExecuteAsync(ExportWorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        ValidationContext validationContext = new(context.Options);
        List<ValidationResult> validationResults = [];

        var optionsValid = Validator.TryValidateObject(context.Options, validationContext, validationResults, validateAllProperties: true);

        foreach (var externalRule in context.Options.ExternalDocLinkRules)
        {
            if (externalRule == null)
            {
                validationResults.Add(new ValidationResult(
                    ValidationMessagesResources.NamespacePrefixRequired,
                    [nameof(ExporterOptions.ExternalDocLinkRules)]));
                continue;
            }

            ValidationContext ruleValidationContext = new(externalRule);
            _ = Validator.TryValidateObject(externalRule, ruleValidationContext, validationResults, validateAllProperties: true);

            if (!string.IsNullOrWhiteSpace(externalRule.UrlTemplate) &&
                !HasValidUrlTemplatePlaceholders(externalRule.UrlTemplate))
            {
                validationResults.Add(new ValidationResult(
                    ValidationMessagesResources.UrlTemplateRequired,
                    [nameof(ExternalDocLinkRule.UrlTemplate)]));
            }
        }

        if (!string.IsNullOrWhiteSpace(context.Options.MicrosoftDocsBaseUrl) &&
            (!Uri.TryCreate(context.Options.MicrosoftDocsBaseUrl, UriKind.Absolute, out var msDocsUri) ||
             (msDocsUri.Scheme != Uri.UriSchemeHttp && msDocsUri.Scheme != Uri.UriSchemeHttps)))
        {
            validationResults.Add(new ValidationResult(
                ValidationMessagesResources.MicrosoftDocsBaseUrlRequired,
                [nameof(ExporterOptions.MicrosoftDocsBaseUrl)]));
        }

        if (!optionsValid || validationResults.Count > 0)
        {
            foreach (var validationResult in validationResults)
            {
                LogValidationError(ExecutionMessagesResources.FormatMessage(ExecutionMessagesResources.StepValidationError, validationResult.ErrorMessage ?? string.Empty));
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private static readonly HashSet<string> SupportedUrlPlaceholders =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "fullName",
        "fullNameLower",
        "fullNameUnderscore",
        "fullNameSlash",
        "namespace",
        "namespaceLower",
        "namespaceSlash",
        "name",
        "nameLower"
    ];

    private static bool HasValidUrlTemplatePlaceholders(string urlTemplate)
    {
        var searchIndex = 0;
        while (searchIndex < urlTemplate.Length)
        {
            var openBraceIndex = urlTemplate.IndexOf('{', searchIndex);
            var closeBraceIndex = urlTemplate.IndexOf('}', searchIndex);

            if (openBraceIndex < 0 && closeBraceIndex < 0)
            {
                return true;
            }

            if (openBraceIndex < 0 || closeBraceIndex < openBraceIndex)
            {
                return false;
            }

            var placeholderName = urlTemplate[(openBraceIndex + 1)..closeBraceIndex].Trim();
            if (!SupportedUrlPlaceholders.Contains(placeholderName))
            {
                return false;
            }

            searchIndex = closeBraceIndex + 1;
        }

        return true;
    }
}
