namespace Documentation.Exporter.Core.Injection;

/// <summary>
/// Service interface for retrieving external code example snippets for types and members.
/// </summary>
public interface IExampleProvider
{
    /// <summary>
    /// Asynchronously retrieves code example snippets for a specific type or member from a given directory path.
    /// </summary>
    /// <param name="examplesDirectoryPath">Path to directory containing code example files.</param>
    /// <param name="typeName">The simple or full name of the target type.</param>
    /// <param name="memberName">The optional member name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of code snippet strings found for the specified target.</returns>
    Task<IReadOnlyList<string>> GetExamplesAsync(
        string? examplesDirectoryPath,
        string? typeName,
        string? memberName = null,
        CancellationToken cancellationToken = default);
}
