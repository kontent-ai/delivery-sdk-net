using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.ContentItems.InlineContentItems;
using Kontent.Ai.Delivery.SharedModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <summary>
    /// A default provider for mapping content items to models.
    /// </summary>
    internal class ModelProvider : IModelProvider
    {
        private ContentLinkResolver _contentLinkResolver;

        internal ITypeProvider TypeProvider { get; set; }

        internal IInlineContentItemsProcessor InlineContentItemsProcessor { get; }

        internal IPropertyMapper PropertyMapper { get; }

        internal IContentLinkUrlResolver ContentLinkUrlResolver { get; }

        internal JsonSerializer Serializer { get; }

        internal IHtmlParser HtmlParser { get; }

        internal IOptionsMonitor<DeliveryOptions> DeliveryOptions { get; }

        private ContentLinkResolver ContentLinkResolver
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
        /// Initializes a new instance of <see cref="ModelProvider"/>.
        /// </summary>
        public ModelProvider(
            IContentLinkUrlResolver contentLinkUrlResolver,
            IInlineContentItemsProcessor inlineContentItemsProcessor,
            ITypeProvider typeProvider,
            IPropertyMapper propertyMapper,
            JsonSerializer serializer,
            IHtmlParser htmlParser,
            IOptionsMonitor<DeliveryOptions> deliveryOptions)
        {
            ContentLinkUrlResolver = contentLinkUrlResolver;
            InlineContentItemsProcessor = inlineContentItemsProcessor;
            TypeProvider = typeProvider;
            PropertyMapper = propertyMapper;
            Serializer = serializer;
            HtmlParser = htmlParser;
            DeliveryOptions = deliveryOptions;
        }

        /// <summary>
        /// Builds a model based on given JSON input.
        /// </summary>
        /// <typeparam name="T">Strongly typed content item model.</typeparam>
        /// <param name="item">Content item data.</param>
        /// <param name="linkedItems">Linked items.</param>
        /// <returns>Strongly typed POCO model of the generic type.</returns>
        public async Task<T> GetContentItemModelAsync<T>(object item, IEnumerable linkedItems)
            => (T)await GetContentItemModelAsync(typeof(T), (JToken)item, (JObject)linkedItems);

        internal async Task<object> GetContentItemModelAsync(Type modelType, JToken serializedItem, JObject linkedItems, Dictionary<string, object> processedItems = null, HashSet<RichTextContentElements> currentlyResolvedRichStrings = null)
        {
            processedItems ??= new Dictionary<string, object>();
            IContentItemSystemAttributes itemSystemAttributes = serializedItem?["system"]?.ToObject<IContentItemSystemAttributes>(Serializer);

            var instance = CreateInstance(modelType, ref itemSystemAttributes, ref processedItems);
            if (instance == null)
            {
                // modelType could not be resolved or instance could not be created
                return null;
            }

            var elementsData = GetElementData(serializedItem);
            var context = CreateResolvingContext(linkedItems, processedItems);
            var richTextPropertiesToBeProcessed = new List<PropertyInfo>();
            foreach (var property in instance.GetType().GetProperties().Where(property => property.SetMethod != null))
            {
                if (typeof(IContentItemSystemAttributes).IsAssignableFrom(property.PropertyType))
                {
                    // Handle the system metadata
                    if (itemSystemAttributes != null)
                    {
                        property.SetValue(instance, itemSystemAttributes);
                    }
                }
                else
                {
                    var value = await GetPropertyValueAsync(elementsData, property, linkedItems, context, itemSystemAttributes, processedItems, richTextPropertiesToBeProcessed);
                    if (value != null)
                    {
                        property.SetValue(instance, value);
                    }
                }
            }

            // Rich-text elements need to be processed last, so in case of circular dependency, content items resolved by
            // resolvers would have all elements already processed
            currentlyResolvedRichStrings ??= new HashSet<RichTextContentElements>();
            foreach (var property in richTextPropertiesToBeProcessed)
            {
                var currentValue = property.GetValue(instance)?.ToString();

                var value = await GetRichTextValueAsync(currentValue, elementsData, property, linkedItems, itemSystemAttributes, processedItems, currentlyResolvedRichStrings);

                if (value != null)
                {
                    property.SetValue(instance, value);
                }
            }

            return instance;
        }

        private async Task<object> GetRichTextValueAsync(string value, JObject elementsData, PropertyInfo property, JObject linkedItems, IContentItemSystemAttributes itemSystemAttributes, Dictionary<string, object> processedItems, HashSet<RichTextContentElements> currentlyResolvedRichStrings)
        {
            var currentlyProcessedString = new RichTextContentElements(itemSystemAttributes?.Codename, property.Name);
            if (currentlyResolvedRichStrings.Contains(currentlyProcessedString))
            {
                // If this element is already being processed it's necessary to use it as is (with removed inline content items)
                // otherwise resolving would be stuck in an infinite loop
                return await RemoveInlineContentItemsAsync(value);
            }

            currentlyResolvedRichStrings.Add(currentlyProcessedString);

            var elementData = GetElementData(elementsData, property, itemSystemAttributes);
            var linkedItemsInRichText = GetLinkedItemsInRichText(elementData?.Value);
            value = await ProcessInlineContentItemsAsync(linkedItems, processedItems, value, linkedItemsInRichText, currentlyResolvedRichStrings);

            currentlyResolvedRichStrings.Remove(currentlyProcessedString);

            return value;
        }

        private object CreateInstance(Type detectedModelType, ref IContentItemSystemAttributes itemSystemAttributes, ref Dictionary<string, object> processedItems)
        {
            if (detectedModelType == typeof(object) || detectedModelType.IsInterface)
            {
                // Try to find a more specific type or a type that can be instantiated
                detectedModelType = TypeProvider?.GetType(itemSystemAttributes?.Type);
            }

            if (detectedModelType == null || itemSystemAttributes == null)
            {
                return null;
            }

            var instance = Activator.CreateInstance(detectedModelType);
            if (!processedItems.ContainsKey(itemSystemAttributes?.Codename))
            {
                processedItems.Add(itemSystemAttributes.Codename, instance);
            }

            return instance;
        }

        private static JObject GetElementData(JToken serializedItem)
        {
            var elementsData = (JObject)serializedItem?["elements"];
            if (elementsData == null)
            {
                return null;
            }

            return elementsData;
        }


        private ResolvingContext CreateResolvingContext(JObject linkedItems, Dictionary<string, object> processedItems)
        {
            async Task<object> GetLinkedItemAsync(string codename)
            {
                var linkedItemsElementNode = linkedItems.Properties().FirstOrDefault(p => p.Name == codename)?.First;
                if (linkedItemsElementNode == null)
                {
                    return null;
                }

                return processedItems.ContainsKey(codename)
                    ? processedItems[codename]
                    : await GetContentItemModelAsync(typeof(object), linkedItemsElementNode, linkedItems, processedItems);
            }

            return new ResolvingContext
            {
                GetLinkedItem = GetLinkedItemAsync,
                ContentLinkUrlResolver = ContentLinkUrlResolver
            };
        }

        private async Task<object> GetPropertyValueAsync(JObject elementsData, PropertyInfo property, JObject linkedItems, ResolvingContext context, IContentItemSystemAttributes itemSystemAttributes, Dictionary<string, object> processedItems, List<PropertyInfo> richTextPropertiesToBeProcessed)
        {
            var elementDefinition = GetElementData(elementsData, property, itemSystemAttributes);

            var elementValue = elementDefinition?.Value;

            if (elementValue != null)
            {
                var valueConverter = GetValueConverter(property);
                if (valueConverter != null)
                {
                    return (elementValue["type"].ToString()) switch
                    {
                        "rich_text" => await GetElementModelAsync<RichTextElementValue, string>(property, context, elementValue, valueConverter),
                        "asset" => await GetElementModelAsync<AssetElementValue, IEnumerable<IAsset>>(property, context, elementValue, valueConverter),
                        "number" => await GetElementModelAsync<ContentElementValue<decimal?>, decimal?>(property, context, elementValue, valueConverter),
                        "date_time" => await GetElementModelAsync<DateTimeElementValue, DateTime?>(property, context, elementValue, valueConverter),
                        "multiple_choice" => await GetElementModelAsync<ContentElementValue<List<MultipleChoiceOption>>, List<MultipleChoiceOption>>(property, context, elementValue, valueConverter),
                        "taxonomy" => await GetElementModelAsync<TaxonomyElementValue, IEnumerable<ITaxonomyTerm>>(property, context, elementValue, valueConverter),
                        "modular_content" => await GetElementModelAsync<ContentElementValue<List<string>>, List<string>>(property, context, elementValue, valueConverter),
                        // Custom element, text element, URL slug element
                        _ => await GetElementModelAsync<ContentElementValue<string>, string>(property, context, elementValue, valueConverter),
                    };
                }
            }

            if (property.PropertyType == typeof(string))
            {
                var (value, isRichText) = await GetStringValueAsync(elementValue);

                if (isRichText)
                {
                    richTextPropertiesToBeProcessed.Add(property);
                }

                return value;
            }

            if (IsNonHierarchicalField(property.PropertyType))
            {
                return GetRawValue(elementValue)?.ToObject(property.PropertyType);
            }

            if (IsGenericHierarchicalField(property.PropertyType))
            {
                return await GetLinkedItemsValueAsync(elementValue, linkedItems, property.PropertyType, processedItems);
            }

            return null;
        }

        private async Task<object> GetElementModelAsync<TElement, TElementValue>(PropertyInfo property, ResolvingContext context, JObject elementValue, IPropertyValueConverter valueConverter) where TElement : IContentElementValue<TElementValue>
        {
            var contentElement = elementValue.ToObject<TElement>(Serializer);
            return await ((IPropertyValueConverter<TElementValue>)valueConverter).GetPropertyValueAsync(property, contentElement, context);
        }

        private (string Name, JObject Value)? GetElementData(JObject elementsData, PropertyInfo property, IContentItemSystemAttributes itemSystemAttributes)
        => elementsData?.Properties()?.Where(p => PropertyMapper.IsMatch(property, p.Name, itemSystemAttributes?.Type)).Select(p => (p.Name, (JObject)p.Value)).FirstOrDefault();

        private static bool IsGenericHierarchicalField(Type fieldType)
        {
            static bool IsGenericICollection(Type @interface)
                => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICollection<>);

            return fieldType.IsGenericType && (fieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                                               fieldType.IsClass &&
                                               fieldType.GetInterfaces().Any(IsGenericICollection));
        }

        private static bool IsNonHierarchicalField(Type propertyType)
            => propertyType.IsValueType && !(typeof(Enumerable).IsAssignableFrom(propertyType)
               || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

        private async Task<(string value, bool isRichText)> GetStringValueAsync(JObject elementData)
        {
            var elementValue = GetRawValue(elementData);
            var value = elementValue?.ToObject<string>(Serializer);
            var links = elementData?.Property("links")?.Value.ToObject<IDictionary<Guid, IContentLink>>(Serializer);

            // Handle rich_text link resolution
            if (links != null && elementValue != null && ContentLinkResolver != null)
            {
                value = await ContentLinkResolver.ResolveContentLinksAsync(value, links);
            }

            var linkedItemsInRichText = GetLinkedItemsInRichText(elementData);

            // It's clear it's rich-text because it contains linked items
            var isRichText = elementValue != null && linkedItemsInRichText != null && InlineContentItemsProcessor != null;

            return (value, isRichText);
        }

        private static JToken GetLinkedItemsInRichText(JObject elementData)
            => elementData?.Property("modular_content")?.Value;

        private async Task<object> GetLinkedItemsValueAsync(JObject elementData, JObject linkedItems, Type propertyType, Dictionary<string, object> processedItems)
        {
            // Create a List<T> based on the generic parameter of the input type (IEnumerable<T> or derived types)
            var genericArgs = propertyType.GetGenericArguments();
            var collectionType = propertyType.IsInterface
                ? typeof(List<>).MakeGenericType(genericArgs)
                : propertyType;

            if ((genericArgs.Length == 1) && (new[] { typeof(IAsset), typeof(ITaxonomyTerm), typeof(IMultipleChoiceOption) }.Contains(genericArgs.First())))
            {
                return GetRawValue(elementData)?.ToObject(collectionType, Serializer);
            }

            var codeNamesWithLinkedItems = GetRawValue(elementData)
                ?.ToObject<List<string>>(Serializer)
                ?.Select(codename => (codename, linkedItems.Properties().FirstOrDefault(p => p.Name == codename)?.First))
                .Where(pair => pair.First != null)
                .ToArray()
                ?? Array.Empty<(string, JToken)>();

            var contentItems = Activator.CreateInstance(collectionType);
            if (!codeNamesWithLinkedItems.Any())
            {
                return contentItems;
            }

            // It certain that the instance is of the ICollection<> type at this point, we can call "Add"
            var addMethod = contentItems.GetType().GetMethod("Add");
            if (addMethod == null)
            {
                throw new InvalidOperationException("Linked items are not stored in collection allowing adding new ones. This should have never happen.");
            }

            foreach (var (codename, linkedItemsElementNode) in codeNamesWithLinkedItems)
            {
                object contentItem;
                if (processedItems.ContainsKey(codename))
                {
                    // Avoid infinite recursion by re-using already processed content items
                    contentItem = processedItems[codename];
                }
                else
                {
                    // This is the entry-point for recursion mentioned above
                    contentItem = await GetContentItemModelAsync(genericArgs.First(), linkedItemsElementNode, linkedItems, processedItems);

                    if (!processedItems.ContainsKey(codename))
                    {
                        processedItems.Add(codename, contentItem);
                    }
                }

                addMethod.Invoke(contentItems, new[] { contentItem });
            }

            return contentItems;
        }

        private IPropertyValueConverter GetValueConverter(PropertyInfo property)
        {
            // Converter defined by explicit attribute has the highest priority
            if (property.GetCustomAttributes().OfType<IPropertyValueConverter>().FirstOrDefault() is { } attributeConverter)
            {
                return attributeConverter;
            }

            // Specific type converters
            if (typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            {
                return new RichTextContentConverter(HtmlParser);
            }

            if (typeof(IDateTimeContent).IsAssignableFrom(property.PropertyType))
            {
                return new DateTimeContentConverter();
            }

            if (property.PropertyType == typeof(IEnumerable<IAsset>))
            {
                return new AssetElementValueConverter(DeliveryOptions);
            }
            
            return null;
        }

        private static JToken GetRawValue(JObject elementData)
            => elementData?.Property("value")?.Value;

        private async Task<string> ProcessInlineContentItemsAsync(JObject linkedItems, Dictionary<string, object> processedItems, string value, JToken linkedItemsInRichText, HashSet<RichTextContentElements> currentlyResolvedRichStrings)
        {
            var usedCodenames = linkedItemsInRichText.ToObject<List<string>>(Serializer);
            var contentItemsInRichText = new Dictionary<string, object>();

            if (usedCodenames != null)
            {
                foreach (var codenameUsed in usedCodenames)
                {
                    object contentItem;
                    // This is to reuse content items which were processed already, but not those 
                    // that are calling this resolver as they may contain unprocessed rich text elements
                    if (processedItems.ContainsKey(codenameUsed) && currentlyResolvedRichStrings.All(x => x.ContentItemCodeName != codenameUsed))

                    {
                        contentItem = processedItems[codenameUsed];
                    }
                    else
                    {
                        var linkedItemsElementNode = linkedItems
                            .Properties()
                            .FirstOrDefault(p => p.Name == codenameUsed)
                            ?.First;
                        if (linkedItemsElementNode != null)
                        {
                            contentItem = await GetContentItemModelAsync(typeof(object), linkedItemsElementNode, linkedItems, processedItems, currentlyResolvedRichStrings);
                            if (!processedItems.ContainsKey(codenameUsed))
                            {
                                contentItem ??= new UnknownContentItem(linkedItemsElementNode
                                                                        .SelectToken("system.type", false)
                                                                        ?.ToString()
                                                                        ?? "unextractable system type");
                                processedItems.Add(codenameUsed, contentItem);
                            }
                        }
                        else
                        {
                            // This means that response from Delivery API didn't contain content of this item 
                            contentItem = new UnretrievedContentItem();
                        }
                    }
                    contentItemsInRichText.Add(codenameUsed, contentItem);
                }
            }

            value = await InlineContentItemsProcessor.ProcessAsync(value, contentItemsInRichText);

            return value;
        }

        private async Task<string> RemoveInlineContentItemsAsync(string value)
            => await InlineContentItemsProcessor.RemoveAllAsync(value);
    }
}
