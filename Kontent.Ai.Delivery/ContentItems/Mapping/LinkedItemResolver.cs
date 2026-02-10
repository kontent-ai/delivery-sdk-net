using System.Text.Json;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

internal sealed class LinkedItemResolver(
    IItemTypingStrategy typingStrategy,
    IContentDeserializer deserializer,
    ILogger<LinkedItemResolver>? logger = null)
{
    private readonly IItemTypingStrategy _typingStrategy = typingStrategy ?? throw new ArgumentNullException(nameof(typingStrategy));
    private readonly IContentDeserializer _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    private readonly ILogger<LinkedItemResolver>? _logger = logger;

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
            if (_logger is not null)
            {
                LoggerMessages.LinkedItemNotFound(_logger, codename);
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
            if (_logger is not null)
            {
                LoggerMessages.CircularReferenceDetected(_logger, codename);
            }
            return inProgress;
        }

        // New item: deserialize and store before hydration.
        var contentType = ExtractContentType(linkedItem);
        var modelType = _typingStrategy.ResolveModelType(contentType);
        var contentItem = _deserializer.DeserializeContentItem(linkedItem, modelType);

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

    private static string ExtractContentType(JsonElement itemElement) =>
        itemElement.TryGetProperty("system", out var system) && system.TryGetProperty("type", out var type)
            ? type.GetString() ?? string.Empty
            : string.Empty;
}
