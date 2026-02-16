using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

internal sealed class LinkedItemResolver(
    IItemTypingStrategy typingStrategy,
    IContentDeserializer deserializer,
    ILogger<LinkedItemResolver>? logger = null)
{
    public async Task<object?> ResolveAsync(
        string codename,
        MappingContext context,
        Func<object, Type, MappingContext, Task> hydrateContentItemAsync)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(hydrateContentItemAsync);
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.ModularContent is null ||
            !context.ModularContent.TryGetValue(codename, out var linkedItem))
        {
            if (logger is not null)
            {
                LoggerMessages.LinkedItemNotFound(logger, codename);
            }
            return null;
        }

        // Already fully resolved in this request - return cached instance.
        if (context.ResolvedItems.TryGetValue(codename, out var cached))
        {
            return cached;
        }

        // Cycle detected: return the same instance being hydrated.
        if (context.ItemsBeingHydrated.TryGetValue(codename, out var inProgress))
        {
            if (logger is not null)
            {
                LoggerMessages.CircularReferenceDetected(logger, codename);
            }
            return inProgress;
        }

        // New item: deserialize and store before hydration.
        var contentType = ContentItemJsonHelper.ExtractContentType(linkedItem);
        var modelType = typingStrategy.ResolveModelType(contentType);
        var contentItem = deserializer.DeserializeContentItem(linkedItem, modelType);

        context.ItemsBeingHydrated[codename] = contentItem;

        try
        {
            await hydrateContentItemAsync(contentItem, modelType, context).ConfigureAwait(false);
            context.ResolvedItems[codename] = contentItem;
            return contentItem;
        }
        finally
        {
            context.ItemsBeingHydrated.Remove(codename);
        }
    }

}
