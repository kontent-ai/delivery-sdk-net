using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Functional resolver delegate for transforming rich text blocks into HTML strings.
/// </summary>
/// <typeparam name="TBlock">The type of rich text block to resolve.</typeparam>
/// <param name="block">The block to resolve.</param>
/// <param name="resolveChildren">Function to resolve child blocks recursively.</param>
/// <returns>The HTML representation of the block.</returns>
public delegate ValueTask<string> BlockResolver<in TBlock>(
    TBlock block,
    Func<IEnumerable<IRichTextBlock>, ValueTask<string>> resolveChildren
) where TBlock : IRichTextBlock;

/// <summary>
/// Predicate delegate for determining if an HTML node should be handled by a specific resolver.
/// </summary>
/// <param name="node">The HTML node to evaluate.</param>
/// <returns>True if the resolver should handle this node; otherwise, false.</returns>
public delegate bool HtmlNodePredicate(IHtmlNode node);

/// <summary>
/// Resolves structured rich text content into HTML strings.
/// </summary>
public interface IHtmlResolver
{
    /// <summary>
    /// Resolves rich text content into an HTML string.
    /// </summary>
    /// <param name="richText">The structured rich text content to resolve.</param>
    /// <returns>The HTML representation of the rich text content.</returns>
    ValueTask<string> ResolveAsync(IRichTextContent richText);
}

