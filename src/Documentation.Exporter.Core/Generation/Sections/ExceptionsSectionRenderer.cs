using Documentation.Exporter.Abstractions.Generation.Sections;
using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Core.Generation.Sections;

/// <summary>
/// Renders the documented exceptions table section for a type.
/// </summary>
/// <param name="exceptionTableFormatter">The exception table formatter.</param>
public sealed class ExceptionsSectionRenderer(IExceptionTableFormatter exceptionTableFormatter) : IMarkdownSectionRenderer
{
    private readonly IExceptionTableFormatter formatter = ArgumentGuard.EnsureNotNull(exceptionTableFormatter);

    /// <inheritdoc />
    public int Order => 70;

    /// <inheritdoc />
    public string RenderSection(TypeDocumentationModel typeModel)
    {
        ArgumentNullException.ThrowIfNull(typeModel);
        return formatter.FormatExceptionsTable(typeModel.DocumentedExceptions, "##");
    }
}
