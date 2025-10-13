using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <summary>
/// Internal record representing a conditional resolver for HTML nodes.
/// </summary>
internal sealed record ConditionalHtmlNodeResolver(
    HtmlNodePredicate Predicate,
    BlockResolver<IHtmlNode> Resolver,
    string? Description = null);
