using System.Collections.Concurrent;
using System.Reflection;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

/// <summary>
/// Factory for creating strongly-typed embedded content instances from content items.
/// Shared by rich text parsing and linked items processing to ensure consistent behavior.
/// </summary>
internal static class EmbeddedContentFactory
{
    // Reflection cache for efficient generic type construction
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorCache = new();
    private static readonly ConcurrentDictionary<Type, Type> _embeddedContentTypeCache = new();

    /// <summary>
    /// Creates an IEmbeddedContent instance from a content item object.
    /// Attempts to create strongly-typed EmbeddedContent&lt;TModel&gt; when possible,
    /// falls back to non-generic EmbeddedContent for unknown types.
    /// </summary>
    /// <param name="contentItem">The content item to wrap. Can be ContentItem&lt;T&gt; or IContentItem&lt;IDynamicElements&gt;.</param>
    /// <returns>An IEmbeddedContent instance wrapping the content item.</returns>
    public static IEmbeddedContent CreateEmbeddedContent(object? contentItem)
    {
        // Handle null (e.g., depth limit reached)
        if (contentItem is null)
        {
            return new EmbeddedContent("unknown", "unknown", null, Guid.Empty, null);
        }

        // Try to extract type information and create generic EmbeddedContent<T>
        var contentItemType = contentItem.GetType();

        // Check if it's ContentItem<T>
        if (contentItemType.IsGenericType &&
            contentItemType.GetGenericTypeDefinition() == typeof(ContentItem<>))
        {
            return CreateStronglyTypedEmbeddedContent(contentItem, contentItemType);
        }

        // Fallback to non-generic for unknown types
        if (contentItem is IContentItem<IDynamicElements> typedItem)
        {
            return CreateNonGenericEmbeddedContent(typedItem);
        }

        // Ultimate fallback
        return new EmbeddedContent("unknown", "unknown", null, Guid.Empty, null);
    }

    /// <summary>
    /// Creates a strongly-typed EmbeddedContent&lt;TModel&gt; using reflection.
    /// Uses cached constructors for performance.
    /// </summary>
    private static IEmbeddedContent CreateStronglyTypedEmbeddedContent(object contentItem, Type contentItemType)
    {
        var modelType = contentItemType.GetGenericArguments()[0];

        // Get or create cached generic EmbeddedContent<T> type
        var embeddedContentType = _embeddedContentTypeCache.GetOrAdd(
            modelType,
            static t => typeof(EmbeddedContent<>).MakeGenericType(t));

        // Get or create cached constructor
        var constructor = _constructorCache.GetOrAdd(
            modelType,
            static t =>
            {
                var embeddedType = typeof(EmbeddedContent<>).MakeGenericType(t);
                return embeddedType.GetConstructor(
                    [typeof(string), typeof(string), typeof(string), typeof(Guid), t])
                    ?? throw new InvalidOperationException($"Constructor not found for EmbeddedContent<{t.Name}>");
            });

        // Extract metadata using dynamic to avoid complex reflection
        dynamic dynamicItem = contentItem;
        var id = Guid.TryParse((string)dynamicItem.System.Id, out var parsedId) ? parsedId : Guid.Empty;

        // Invoke constructor with cached ConstructorInfo
        var embeddedContent = constructor.Invoke(
        [
            (string)dynamicItem.System.Type,
            (string)dynamicItem.System.Codename,
            (string?)dynamicItem.System.Name,
            id,
            dynamicItem.Elements
        ]);

        return (IEmbeddedContent)embeddedContent;
    }

    /// <summary>
    /// Creates a non-generic EmbeddedContent from an IContentItem&lt;IDynamicElements&gt;.
    /// </summary>
    private static IEmbeddedContent CreateNonGenericEmbeddedContent(IContentItem<IDynamicElements> item)
    {
        var id = Guid.TryParse(item.System.Id, out var parsedId) ? parsedId : Guid.Empty;
        return new EmbeddedContent(
            item.System.Type,
            item.System.Codename,
            item.System.Name,
            id,
            item.Elements);
    }
}
