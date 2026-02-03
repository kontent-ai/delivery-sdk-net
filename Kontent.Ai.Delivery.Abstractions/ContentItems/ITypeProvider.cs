namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Defines the contract for mapping Kontent.ai content types to CLR types.
/// </summary>
public interface ITypeProvider
{
    /// <summary>
    /// Returns a CLR type corresponding to the given content type.
    /// Returns null if no mapping exists, allowing fallback to dynamic types.
    /// </summary>
    /// <param name="contentType">Content type identifier.</param>
    /// <returns>The CLR type for the content type, or null if no mapping exists.</returns>
    Type? GetType(string contentType);

    /// <summary>
    /// Attempts to return a codename corresponding to the given content type model.
    /// Returns null if no mapping exists.
    /// </summary>
    /// <param name="contentType">Content type model.</param>
    /// <returns>The content type codename, or null if no mapping exists.</returns>
    string? GetCodename(Type contentType);
}
