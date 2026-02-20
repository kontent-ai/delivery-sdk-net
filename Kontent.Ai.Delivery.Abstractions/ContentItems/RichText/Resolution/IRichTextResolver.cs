namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Resolves structured rich text content into an output of type <typeparamref name="TOutput"/>.
/// </summary>
/// <typeparam name="TOutput">The output type of the resolution (e.g., <c>string</c> for HTML or Markdown).</typeparam>
public interface IRichTextResolver<TOutput>
{
    /// <summary>
    /// Resolves rich text content into the target output format.
    /// </summary>
    /// <param name="richText">The structured rich text content to resolve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resolved representation of the rich text content.</returns>
    ValueTask<TOutput> ResolveAsync(IRichTextContent richText, CancellationToken cancellationToken = default);
}
