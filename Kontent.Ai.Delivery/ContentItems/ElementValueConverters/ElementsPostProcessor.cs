using System.Reflection;
using System.Text.Json;
using System.Threading;
using AngleSharp.Html.Parser;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Post-processes deserialized content items to hydrate advanced element types
/// such as rich text blocks using original element JSON and modular content.
/// </summary>
/// <param name="propertyMapper">The property mapper.</param>
/// <param name="modelProvider">The model provider.</param>
/// <param name="htmlParser">The HTML parser.</param>
internal sealed class ElementsPostProcessor(
    IPropertyMapper propertyMapper,
    IModelProvider modelProvider,
    IHtmlParser htmlParser) : IElementsPostProcessor
{
    private readonly IPropertyMapper _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
    private readonly IModelProvider _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
    private readonly IHtmlParser _htmlParser = htmlParser ?? throw new ArgumentNullException(nameof(htmlParser));

    /// <summary>
    /// Hydrates advanced element types on a strongly typed content item.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="item">The content item to process.</param>
    /// <param name="modularContent">The modular content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ProcessAsync<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        CancellationToken cancellationToken = default) where TModel : IElementsModel
    {
        if (item is not ContentItem<TModel> concrete || string.IsNullOrEmpty(concrete.RawElementsJson))
        {
            return;
        }

        using var doc = JsonDocument.Parse(concrete.RawElementsJson);
        var elementsJson = doc.RootElement;
        if (elementsJson.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var systemType = item.System.Type;
        var properties = item.Elements.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite);


        // precompute stuff shared by all props
        var resolvingContext = CreateResolvingContext(modularContent);
        var contentConverter = new RichTextContentConverter(_htmlParser);

        // pipeline: filter → map → run → apply
        var results = await Task.WhenAll(
            properties
                .Where(p => typeof(IRichTextContent).IsAssignableFrom(p.PropertyType))
                .Select(async prop =>
                {
                    // find the matching element
                    var element = FindElement(elementsJson, prop, systemType);
                    if (element is null) return (prop, null);

                    var (elementName, elementValue) = element.Value;

                    // must have a string value
                    if (!elementValue.TryGetProperty("value", out var valueEl) ||
                        valueEl.ValueKind != JsonValueKind.String)
                        return (prop, null);

                    // map json → IRichTextElementValue using custom converter that injects codename
                    var options = new JsonSerializerOptions();
                    var converter = new Elements.RichTextElementValueConverter { ElementCodename = elementName };
                    options.Converters.Add(converter);

                    var richElement = JsonSerializer.Deserialize<Elements.RichTextElementValue>(
                        elementValue.GetRawText(), options);
                    if (richElement is null) return (prop, null);

                    // produce blocks
                    var blocks = await contentConverter
                        .GetPropertyValueAsync(prop, richElement, resolvingContext)
                        .ConfigureAwait(false);

                    return (prop, blocks);
                })
                ).ConfigureAwait(false);

        foreach (var (prop, blocks) in results)
        {
            if (blocks is IRichTextContent richText)
            {
                prop.SetValue(item.Elements, richText);
            }
        }
    }

    private (string Name, JsonElement Value)? FindElement(JsonElement elementsJson, PropertyInfo propertyInfo, string contentType)
    {
        foreach (var prop in elementsJson.EnumerateObject())
        {
            if (_propertyMapper.IsMatch(propertyInfo, prop.Name, contentType))
            {
                return (prop.Name, prop.Value);
            }
        }
        return null;
    }

    private ResolvingContext CreateResolvingContext(IReadOnlyDictionary<string, JsonElement>? modularContent)
    {
        async Task<object> GetLinkedItemAsync(string codename)
        {
            if (modularContent is not null && modularContent.TryGetValue(codename, out var linked))
            {
                // Use ModelProvider to map nested items; type provider is supplied by customer
                return await _modelProvider.GetContentItemModelAsync<object>(linked.GetRawText(), Array.Empty<object>());
            }
            return null!;
        }

        return new ResolvingContext
        {
            GetLinkedItem = GetLinkedItemAsync,
            ContentLinkUrlResolver = new ContentLinks.DefaultContentLinkUrlResolver()
        };
    }
}



