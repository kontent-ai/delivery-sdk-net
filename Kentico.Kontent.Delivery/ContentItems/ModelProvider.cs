using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.ContentItems.Elements;
using Kentico.Kontent.Delivery.ContentItems.InlineContentItems;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <summary>
    /// A default provider for mapping content items to models.
    /// </summary>
    internal class ModelProvider : IModelProvider
    {
        private ContentLinkResolver _contentLinkResolver;
        internal JsonSerializer _serializer;
        internal ITypeProvider TypeProvider { get; set; }

        internal IInlineContentItemsProcessor InlineContentItemsProcessor { get; }

        internal IPropertyMapper PropertyMapper { get; }

        internal IContentLinkUrlResolver ContentLinkUrlResolver { get; }

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
        /// Default serializer.
        /// </summary>
        internal JsonSerializer Serializer =>
            _serializer ??= new JsonSerializer
            {
                //TODO: get from DI container
                ContractResolver = new DeliveryContractResolver(new DeliveryServiceCollection().ServiceProvider)
            };

        /// <summary>
        /// Initializes a new instance of <see cref="ModelProvider"/>.
        /// </summary>
        public ModelProvider(
            IContentLinkUrlResolver contentLinkUrlResolver,
            IInlineContentItemsProcessor inlineContentItemsProcessor,
            ITypeProvider typeProvider,
            IPropertyMapper propertyMapper
        )
        {
            ContentLinkUrlResolver = contentLinkUrlResolver;
            InlineContentItemsProcessor = inlineContentItemsProcessor;
            TypeProvider = typeProvider;
            PropertyMapper = propertyMapper;
        }

        /// <summary>
        /// Builds a model based on given JSON input.
        /// </summary>
        /// <typeparam name="T">Strongly typed content item model.</typeparam>
        /// <param name="item">Content item data.</param>
        /// <param name="linkedItems">Linked items.</param>
        /// <returns>Strongly typed POCO model of the generic type.</returns>
        public async Task<T> GetContentItemModel<T>(object item, IEnumerable linkedItems)
            => (T)await GetContentItemModel(typeof(T), (JToken)item, (JObject)linkedItems);

        internal async Task<object> GetContentItemModel(Type modelType, JToken serializedItem, JObject linkedItems, Dictionary<string, object> processedItems = null, HashSet<RichTextContentElements> currentlyResolvedRichStrings = null)
        {
            processedItems ??= new Dictionary<string, object>();
            IContentItemSystemAttributes itemSystemAttributes = serializedItem["system"].ToObject<ContentItemSystemAttributes>();

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
                    var value = await GetPropertyValue(elementsData, property, linkedItems, context, itemSystemAttributes, processedItems, richTextPropertiesToBeProcessed);
                    if (value != null)
                    {
                        property.SetValue(instance, value);
                    }
                }
            }

            // Richtext elements need to be processed last, so in case of circular dependency, content items resolved by
            // resolvers would have all elements already processed
            currentlyResolvedRichStrings ??= new HashSet<RichTextContentElements>();
            foreach (var property in richTextPropertiesToBeProcessed)
            {
                var currentValue = property.GetValue(instance)?.ToString();

                var value = await GetRichTextValue(currentValue, elementsData, property, linkedItems, itemSystemAttributes, processedItems, currentlyResolvedRichStrings);

                if (value != null)
                {
                    property.SetValue(instance, value);
                }
            }

            return instance;
        }

        private async Task<object> GetRichTextValue(string value, JObject elementsData, PropertyInfo property, JObject linkedItems, IContentItemSystemAttributes itemSystemAttributes, Dictionary<string, object> processedItems, HashSet<RichTextContentElements> currentlyResolvedRichStrings)
        {
            var currentlyProcessedString = new RichTextContentElements(itemSystemAttributes?.Codename, property.Name);
            if (currentlyResolvedRichStrings.Contains(currentlyProcessedString))
            {
                // If this element is already being processed it's necessary to use it as is (with removed inline content items)
                // otherwise resolving would be stuck in an infinite loop
                return await RemoveInlineContentItems(value);
            }

            currentlyResolvedRichStrings.Add(currentlyProcessedString);

            var elementData = GetElementData(elementsData, property, itemSystemAttributes);
            var linkedItemsInRichText = GetLinkedItemsInRichText(elementData?.Value);
            value = await ProcessInlineContentItems(linkedItems, processedItems, value, linkedItemsInRichText, currentlyResolvedRichStrings);

            currentlyResolvedRichStrings.Remove(currentlyProcessedString);

            return value;
        }

        private object CreateInstance(Type detectedModelType, ref IContentItemSystemAttributes itemSystemAttributes, ref Dictionary<string, object> processedItems)
        {
            if (detectedModelType == typeof(object) || detectedModelType.IsInterface)
            {
                // Try to find a more specific type or a type that can be instantiated
                detectedModelType = TypeProvider?.GetType(itemSystemAttributes.Type);
            }

            if (detectedModelType == null)
            {
                return null;
            }

            var instance = Activator.CreateInstance(detectedModelType);
            if (!processedItems.ContainsKey(itemSystemAttributes.Codename))
            {
                processedItems.Add(itemSystemAttributes.Codename, instance);
            }

            return instance;
        }

        private static JObject GetElementData(JToken serializedItem)
        {
            var elementsData = (JObject)serializedItem["elements"];
            if (elementsData == null)
            {
                throw new InvalidOperationException("Missing elements node in the content item data.");
            }

            return elementsData;
        }


        private ResolvingContext CreateResolvingContext(JObject linkedItems, Dictionary<string, object> processedItems)
        {
            async Task<object> GetLinkedItem(string codename)
            {
                var linkedItemsElementNode = linkedItems.Properties().FirstOrDefault(p => p.Name == codename)?.First;
                if (linkedItemsElementNode == null)
                {
                    return null;
                }

                return processedItems.ContainsKey(codename)
                    ? processedItems[codename]
                    : await GetContentItemModel(typeof(object), linkedItemsElementNode, linkedItems, processedItems);
            }

            return new ResolvingContext
            {
                GetLinkedItem = GetLinkedItem,
                ContentLinkUrlResolver = ContentLinkUrlResolver
            };
        }

        private async Task<object> GetPropertyValue(JObject elementsData, PropertyInfo property, JObject linkedItems, ResolvingContext context, IContentItemSystemAttributes itemSystemAttributes, Dictionary<string, object> processedItems, List<PropertyInfo> richTextPropertiesToBeProcessed)
        {
            var elementDefinition = GetElementData(elementsData, property, itemSystemAttributes);

            var elementValue = elementDefinition?.Value;

            if (elementValue != null)
            {
                var valueConverter = GetValueConverter(property);
                if (valueConverter != null)
                {
                    //TODO: extend the list of supported types
                    return (elementValue["type"].ToString()) switch
                    {
                        "rich_text" => await GetElementModel<RichTextElement, string>(property, context, elementValue, valueConverter),
                        "asset" => await GetElementModel<ContentElementValue<Asset>, Asset>(property, context, elementValue, valueConverter),
                        "number" => await GetElementModel<ContentElementValue<decimal?>, decimal?>(property, context, elementValue, valueConverter),
                        "date_time" => await GetElementModel<ContentElementValue<DateTime>, DateTime>(property, context, elementValue, valueConverter),
                        // Custom element, text element, URL slug element
                        _ => await GetElementModel<ContentElementValue<string>, string>(property, context, elementValue, valueConverter),
                    };
                }
            }

            if (property.PropertyType == typeof(string))
            {
                var (value, isRichText) = await GetStringValue(elementValue);

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
                return await GetLinkedItemsValue(elementValue, linkedItems, property.PropertyType, processedItems);
            }

            return null;
        }

        private async Task<object> GetElementModel<R, T>(PropertyInfo property, ResolvingContext context, JObject elementValue, IPropertyValueConverter valueConverter) where R : IContentElementValue<T>
        {
            var contentElement = elementValue.ToObject<R>(Serializer);
            return await ((IPropertyValueConverter<T>)valueConverter).GetPropertyValue(property, contentElement, context);
        }

        private (string Name, JObject Value)? GetElementData(JObject elementsData, PropertyInfo property, IContentItemSystemAttributes itemSystemAttributes)
        => elementsData.Properties()?.Where(p => PropertyMapper.IsMatch(property, p.Name, itemSystemAttributes?.Type)).Select(p => (p.Name, (JObject)p.Value)).FirstOrDefault();

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

        private async Task<(string value, bool isRichText)> GetStringValue(JObject elementData)
        {
            var elementValue = GetRawValue(elementData);
            var value = elementValue?.ToObject<string>();
            var links = elementData?.Property("links")?.Value.ToObject<IDictionary<Guid, IContentLink>>(Serializer);

            // Handle rich_text link resolution
            if (links != null && elementValue != null && ContentLinkResolver != null)
            {
                value = await ContentLinkResolver.ResolveContentLinks(value, links);
            }

            var linkedItemsInRichText = GetLinkedItemsInRichText(elementData);

            // It's clear it's richtext because it contains linked items
            var isRichText = elementValue != null && linkedItemsInRichText != null && InlineContentItemsProcessor != null;

            return (value, isRichText);
        }

        private static JToken GetLinkedItemsInRichText(JObject elementData)
            => elementData?.Property("modular_content")?.Value;

        private async Task<object> GetLinkedItemsValue(JObject elementData, JObject linkedItems, Type propertyType, Dictionary<string, object> processedItems)
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
                ?.ToObject<IEnumerable<string>>()
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
                    contentItem = await GetContentItemModel(genericArgs.First(), linkedItemsElementNode, linkedItems, processedItems);

                    if (!processedItems.ContainsKey(codename))
                    {
                        processedItems.Add(codename, contentItem);
                    }
                }

                addMethod.Invoke(contentItems, new[] { contentItem });
            }

            return contentItems;
        }

        private static IPropertyValueConverter GetValueConverter(PropertyInfo property)
        {
            // Converter defined by explicit attribute has the highest priority
            if (property.GetCustomAttributes().OfType<IPropertyValueConverter>().FirstOrDefault() is { } attributeConverter)
            {
                return attributeConverter;
            }

            // Specific type converters
            if (typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            {
                return new RichTextContentConverter();
            }

            return null;
        }

        private static JToken GetRawValue(JObject elementData)
            => elementData?.Property("value")?.Value;

        private async Task<string> ProcessInlineContentItems(JObject linkedItems, Dictionary<string, object> processedItems, string value, JToken linkedItemsInRichText, HashSet<RichTextContentElements> currentlyResolvedRichStrings)
        {
            //TODO: pass json settings & get rid of the static context
            var usedCodenames = JsonConvert.DeserializeObject<IEnumerable<string>>(linkedItemsInRichText.ToString());

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
                            contentItem = await GetContentItemModel(typeof(object), linkedItemsElementNode, linkedItems, processedItems, currentlyResolvedRichStrings);
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

            value = await InlineContentItemsProcessor.Process(value, contentItemsInRichText);

            return value;
        }

        private async Task<string> RemoveInlineContentItems(string value)
            => await InlineContentItemsProcessor.RemoveAll(value);
    }
}
