using Documentation.Exporter.Abstractions.Models;

namespace Documentation.Exporter.Abstractions.Generation.Sections;

/// <summary>
/// Defines a modular renderer for a specific section within a Markdown API page.
/// </summary>
public interface IMarkdownSectionRenderer
{
    /// <summary>
    /// Execution order of this section in the Markdown page (lower numbers render first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Renders the section content as Markdown.
    /// </summary>
    /// <param name="typeModel">The type documentation model containing reflected and parsed metadata.</param>
    /// <returns>The formatted Markdown of this section, or an empty string when the section does not apply.</returns>
    string RenderSection(TypeDocumentationModel typeModel);
}
