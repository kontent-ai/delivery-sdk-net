using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.InlineContentItems;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Provider for mapping content items to models using System.Text.Json.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ModelProvider"/>.
/// </remarks>
internal class ModelProvider(
    ITypeProvider typeProvider,
    IPropertyMapper propertyMapper,
    IInlineContentItemsProcessor inlineContentItemsProcessor,
    IContentLinkUrlResolver? contentLinkUrlResolver,
    JsonSerializerOptions jsonOptions,
    IHtmlParser htmlParser,
    IOptionsMonitor<DeliveryOptions> deliveryOptions) : IModelProvider
{
    private ContentLinkResolver? _contentLinkResolver;

    internal ITypeProvider TypeProvider { get; set; } = typeProvider ?? throw new ArgumentNullException(nameof(typeProvider));
    internal IInlineContentItemsProcessor InlineContentItemsProcessor { get; } = inlineContentItemsProcessor ?? throw new ArgumentNullException(nameof(inlineContentItemsProcessor));
    internal IPropertyMapper PropertyMapper { get; } = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
    internal IContentLinkUrlResolver? ContentLinkUrlResolver { get; } = contentLinkUrlResolver;
    internal JsonSerializerOptions JsonOptions { get; } = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
    internal IHtmlParser HtmlParser { get; } = htmlParser ?? throw new ArgumentNullException(nameof(htmlParser));
    internal IOptionsMonitor<DeliveryOptions> DeliveryOptions { get; } = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));

    private ContentLinkResolver? ContentLinkResolver
    {
        get
        {
            if (ContentLinkUrlResolver != null)
            {
                return _contentLinkResolver ??= new ContentLinkResolver(ContentLinkUrlResolver);
            }
            return _contentLinkResolver;
        }
    }

    /// <summary>
    /// Builds a model based on given JSON input.
    /// </summary>
    /// <typeparam name="T">Strongly typed content item model.</typeparam>
    /// <param name="item">Content item data.</param>
    /// <param name="linkedItems">Linked items.</param>
    /// <returns>Strongly typed POCO model of the generic type.</returns>
    public async Task<T> GetContentItemModelAsync<T>(object item, IEnumerable linkedItems)
    {
        var itemJson = item.ToString() ?? throw new ArgumentException("Item cannot be null or empty", nameof(item));
        var linkedItemsJson = linkedItems?.ToString() ?? "{}";

        using var itemDocument = JsonDocument.Parse(itemJson);
        using var linkedItemsDocument = JsonDocument.Parse(linkedItemsJson);

        // If caller requested object, try runtime mapping via type provider, otherwise fall back to dynamic item
        var requestedType = typeof(T);
        if (requestedType == typeof(object))
        {
            var targetType = GetModelTypeFromSystem(itemDocument.RootElement);
            if (targetType is null)
            {
                // Fallback: return raw JSON object
                return (T)(object)itemDocument.RootElement.Clone();
            }

            var mapped = await GetContentItemModelAsync(
                targetType,
                itemDocument.RootElement,
                linkedItemsDocument.RootElement);
            return (T)mapped;
        }

        var result = await GetContentItemModelAsync(
            requestedType,
            itemDocument.RootElement,
            linkedItemsDocument.RootElement);

        return (T)result;
    }

    internal async Task<object> GetContentItemModelAsync(
        Type modelType,
        JsonElement serializedItem,
        JsonElement linkedItems,
        Dictionary<string, object>? processedItems = null,
        HashSet<RichTextContentElements>? currentlyResolvedRichStrings = null)
    {
        processedItems ??= [];

        IContentItemSystemAttributes? itemSystemAttributes = null;

        if (serializedItem.TryGetProperty("system", out var systemElement))
        {
            var systemJson = systemElement.GetRawText();
            itemSystemAttributes = System.Text.Json.JsonSerializer.Deserialize<ContentItemSystemAttributes>(systemJson, JsonOptions);
        }

        var instance = CreateInstance(modelType, ref itemSystemAttributes, ref processedItems);
        if (instance == null)
        {
            // modelType could not be resolved or instance could not be created
            return null!;
        }

        var elementsData = GetElementData(serializedItem);
        if (elementsData == null)
        {
            return instance;
        }

        currentlyResolvedRichStrings ??= new HashSet<RichTextContentElements>();
        var richTextPropertiesToBeProcessed = new List<PropertyInfo>();

        foreach (var property in instance.GetType().GetProperties())
        {
            if (property.SetMethod?.IsPublic != true ||
                !property.SetMethod.GetParameters().Any() ||
                property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                continue;
            }

            if (PropertyMapper.IsMatch(property, "system", null!) && itemSystemAttributes != null)
            {
                property.SetValue(instance, itemSystemAttributes);
            }
            else
            {
                var context = CreateResolvingContext(linkedItems, processedItems);
                var value = await GetPropertyValueAsync(
                    elementsData.Value,
                    property,
                    linkedItems,
                    context,
                    itemSystemAttributes,
                    processedItems,
                    richTextPropertiesToBeProcessed);

                if (value != null)
                {
                    property.SetValue(instance, value);
                }
            }
        }

        // Process rich text properties that were deferred
        foreach (var property in richTextPropertiesToBeProcessed)
        {
            var elementData = GetElementData(elementsData.Value, property, itemSystemAttributes);
            if (elementData?.Value != null)
            {
                var (stringValue, isRichText) = GetStringValue(elementData.Value.Value);
                var value = await GetRichTextValueAsync(
                    stringValue,
                    elementsData.Value,
                    property,
                    linkedItems,
                    itemSystemAttributes,
                    processedItems,
                    currentlyResolvedRichStrings);

                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static JsonElement? GetElementData(JsonElement serializedItem)
    {
        return serializedItem.TryGetProperty("elements", out var elements) ? elements : null;
    }

    private Type? GetModelTypeFromSystem(JsonElement serializedItem)
    {
        if (serializedItem.TryGetProperty("system", out var systemEl) &&
            systemEl.TryGetProperty("type", out var typeEl))
        {
            var contentType = typeEl.GetString();
            if (!string.IsNullOrEmpty(contentType))
            {
                return TypeProvider.GetType(contentType);
            }
        }
        return null;
    }

    // Removed DynamicItem path; dynamic mapping is handled elsewhere.

    private ResolvingContext CreateResolvingContext(JsonElement linkedItems, Dictionary<string, object> processedItems)
    {
        async Task<object> GetLinkedItemAsync(string codename)
        {
            if (linkedItems.TryGetProperty(codename, out var linkedItemElement))
            {
                var contentItem = await GetContentItemModelAsync(
                    TypeProvider.GetType(codename) ?? typeof(object),
                    linkedItemElement,
                    linkedItems,
                    processedItems);
                return contentItem;
            }
            return null!;
        }

        return new ResolvingContext
        {
            GetLinkedItem = GetLinkedItemAsync,
            ContentLinkUrlResolver = ContentLinkUrlResolver!
        };
    }

    private async Task<object?> GetPropertyValueAsync(
        JsonElement elementsData,
        PropertyInfo property,
        JsonElement linkedItems,
        ResolvingContext context,
        IContentItemSystemAttributes? itemSystemAttributes,
        Dictionary<string, object> processedItems,
        List<PropertyInfo> richTextPropertiesToBeProcessed)
    {
        var elementDefinition = GetElementData(elementsData, property, itemSystemAttributes);

        if (elementDefinition == null)
        {
            return null;
        }

        var (elementName, elementValue) = elementDefinition.Value;

        var propertyType = property.PropertyType;

        if (propertyType == typeof(string))
        {
            var (value, isRichText) = GetStringValue(elementValue);

            if (isRichText)
            {
                // Stop mapping rich text into string; handled via post-processor for IRichTextContent only.
                return null;
            }

            return value;
        }

        if (IsGenericHierarchicalField(propertyType))
        {
            return await GetLinkedItemsValueAsync(elementValue, linkedItems, propertyType, processedItems);
        }

        // Handle other property types...
        var rawValue = GetRawValue(elementValue);
        if (rawValue.HasValue)
        {
            var rawJson = rawValue.Value.GetRawText();
            return System.Text.Json.JsonSerializer.Deserialize(rawJson, propertyType, JsonOptions);
        }

        return null;
    }

    private async Task<object?> GetRichTextValueAsync(
        string value,
        JsonElement elementsData,
        PropertyInfo property,
        JsonElement linkedItems,
        IContentItemSystemAttributes itemSystemAttributes,
        Dictionary<string, object> processedItems,
        HashSet<RichTextContentElements> currentlyResolvedRichStrings)
    {
        var currentlyProcessedString = new RichTextContentElements(itemSystemAttributes.Codename, property.Name);

        if (currentlyResolvedRichStrings.Contains(currentlyProcessedString))
        {
            return value;
        }

        currentlyResolvedRichStrings.Add(currentlyProcessedString);

        var elementDefinition = GetElementData(elementsData, property, itemSystemAttributes);
        if (elementDefinition?.Value == null)
        {
            return value;
        }

        var linkedItemsInRichText = GetLinkedItemsInRichText(elementDefinition.Value.Value);

        var processedValue = await ProcessInlineContentItemsAsync(
            linkedItems,
            processedItems,
            value,
            linkedItemsInRichText,
            currentlyResolvedRichStrings);

        currentlyResolvedRichStrings.Remove(currentlyProcessedString);
        return processedValue;
    }


    private (string Name, JsonElement Value)? GetElementData(
        JsonElement elementsData,
        PropertyInfo property,
        IContentItemSystemAttributes? itemSystemAttributes)
    {
        if (elementsData.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var elementProperty in elementsData.EnumerateObject())
        {
            if (PropertyMapper.IsMatch(property, elementProperty.Name, itemSystemAttributes?.Type))
            {
                return (elementProperty.Name, elementProperty.Value);
            }
        }

        return null;
    }

    private static bool IsGenericHierarchicalField(Type fieldType)
    {
        var propertyType = fieldType;
        return propertyType.IsGenericType &&
               (propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                propertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    private static (string value, bool isRichText) GetStringValue(JsonElement elementData)
    {
        var elementValue = GetRawValue(elementData);

        if (elementValue == null || elementValue.Value.ValueKind != JsonValueKind.String)
        {
            return (string.Empty, false);
        }

        var value = elementValue.Value.GetString() ?? string.Empty;

        if (elementData.TryGetProperty("type", out var typeElement) &&
            typeElement.GetString() == "rich_text")
        {
            return (value, true);
        }

        return (value, false);
    }

    private static JsonElement? GetLinkedItemsInRichText(JsonElement elementData)
    {
        return elementData.TryGetProperty("modular_content", out var modularContent) ? modularContent : null;
    }

    private async Task<object?> GetLinkedItemsValueAsync(
        JsonElement elementData,
        JsonElement linkedItems,
        Type propertyType,
        Dictionary<string, object> processedItems)
    {
        // Create a List<T> based on the generic parameter of the input type
        var genericArgs = propertyType.GetGenericArguments();
        if (!genericArgs.Any())
        {
            return null;
        }

        var collectionType = typeof(List<>).MakeGenericType(genericArgs.First());
        var rawValue = GetRawValue(elementData);

        if (rawValue == null)
        {
            return Activator.CreateInstance(collectionType);
        }

        var codenamesJson = rawValue.Value.GetRawText();
        var codenames = System.Text.Json.JsonSerializer.Deserialize<List<string>>(codenamesJson, JsonOptions) ?? new List<string>();

        var contentItems = Activator.CreateInstance(collectionType);
        var addMethod = collectionType.GetMethod("Add");

        foreach (var codename in codenames.Where(c => !string.IsNullOrEmpty(c)))
        {
            if (linkedItems.TryGetProperty(codename, out var linkedItemElement))
            {
                var linkedItemType = TypeProvider.GetType(codename) ?? typeof(object);
                var linkedItem = await GetContentItemModelAsync(
                    linkedItemType,
                    linkedItemElement,
                    linkedItems,
                    processedItems);

                addMethod?.Invoke(contentItems, new[] { linkedItem });
            }
        }

        return contentItems;
    }

    private static JsonElement? GetRawValue(JsonElement elementData)
    {
        return elementData.TryGetProperty("value", out var value) ? value : null;
    }

    private async Task<string> ProcessInlineContentItemsAsync(
        JsonElement linkedItems,
        Dictionary<string, object> processedItems,
        string value,
        JsonElement? linkedItemsInRichText,
        HashSet<RichTextContentElements> currentlyResolvedRichStrings)
    {
        if (linkedItemsInRichText == null)
        {
            return value;
        }

        var codenamesJson = linkedItemsInRichText.Value.GetRawText();
        var usedCodenames = JsonSerializer.Deserialize<List<string>>(codenamesJson) ?? new List<string>();

        var usedContentItems = new Dictionary<string, object>();

        foreach (var codename in usedCodenames.Where(c => !string.IsNullOrEmpty(c)))
        {
            if (linkedItems.TryGetProperty(codename, out var linkedItemElement))
            {
                var linkedItemType = TypeProvider.GetType(codename) ?? typeof(object);
                var linkedItem = await GetContentItemModelAsync(
                    linkedItemType,
                    linkedItemElement,
                    linkedItems,
                    processedItems,
                    currentlyResolvedRichStrings);

                usedContentItems[codename] = linkedItem;
            }
        }

        value = await InlineContentItemsProcessor.ProcessAsync(value, usedContentItems);

        return value;
    }

    private object? CreateInstance(
        Type modelType,
        ref IContentItemSystemAttributes? itemSystemAttributes,
        ref Dictionary<string, object> processedItems)
    {
        var codename = itemSystemAttributes?.Codename;

        if (!string.IsNullOrEmpty(codename) && processedItems.ContainsKey(codename))
        {
            return processedItems[codename];
        }

        try
        {
            var instance = Activator.CreateInstance(modelType);

            if (!string.IsNullOrEmpty(codename))
            {
                processedItems[codename] = instance!;
            }

            return instance;
        }
        catch
        {
            return null;
        }
    }
}