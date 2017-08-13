using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KenticoCloud.Delivery.InlineContentItems;
using Newtonsoft.Json;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// A default provider for mapping content items to code-first models.
    /// </summary>
    internal class CodeFirstModelProvider : ICodeFirstModelProvider
    {
        private readonly IDeliveryClient _client;
        private ICodeFirstPropertyMapper _propertyMapper;
        private ContentLinkResolver _contentLinkResolver;

        internal ContentLinkResolver ContentLinkResolver
        {
            get
            {
                if (_contentLinkResolver == null && _client.ContentLinkUrlResolver != null)
                {
                    _contentLinkResolver = new ContentLinkResolver(_client.ContentLinkUrlResolver);
                }
                return _contentLinkResolver;
            }
        }

        /// <summary>
        /// Ensures mapping between Kentico Cloud content types and CLR types.
        /// </summary>
        public ICodeFirstTypeProvider TypeProvider { get; set; }

        /// <summary>
        /// Ensures mapping between Kentico Cloud content item fields and model properties.
        /// </summary>
        public ICodeFirstPropertyMapper PropertyMapper
        {
            get { return _propertyMapper ?? (_propertyMapper = new CodeFirstPropertyMapper()); }
            set { _propertyMapper = value; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CodeFirstModelProvider"/>.
        /// </summary>
        public CodeFirstModelProvider(IDeliveryClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Builds a code-first model based on given JSON input.
        /// </summary>
        /// <typeparam name="T">Strongly typed content item model.</typeparam>
        /// <param name="item">Content item data.</param>
        /// <param name="modularContent">Modular content items.</param>
        /// <returns>Strongly typed POCO model of the generic type.</returns>
        public T GetContentItemModel<T>(JToken item, JToken modularContent)
        {
            return (T)GetContentItemModel(typeof(T), item, modularContent);
        }

        internal object GetContentItemModel(Type t, JToken item, JToken modularContent, Dictionary<string, object> processedItems = null, HashSet<RichTextContentElements> currentlyResolvedRichStrings = null)
        {
            processedItems = processedItems ?? new Dictionary<string, object>();
            currentlyResolvedRichStrings = currentlyResolvedRichStrings ?? new HashSet<RichTextContentElements>();

            var richTextPropertiesToBeProcessed = new List<PropertyInfo>();
            var system = item["system"].ToObject<ContentItemSystemAttributes>();

            if (t == typeof(object))
            {
                // Try to find a specific type
                t = TypeProvider?.GetType(system.Type);
                if (t == null)
                {
                    throw new Exception($"No corresponding CLR type found for the '{system.Type}' content type. Provide a correct implementation of '{nameof(ICodeFirstTypeProvider)}' to the '{nameof(TypeProvider)}' property.");
                } 
            }

            object instance = Activator.CreateInstance(t);

            if (!processedItems.ContainsKey(system.Codename))
            {
                processedItems.Add(system.Codename, instance);
            }

            var elementsData = (JObject)item["elements"];
            if (elementsData == null)
            {
                throw new InvalidOperationException("Missing elements node in the content item data.");
            }

            var context = new CodeFirstResolvingContext
            {
                GetModularContentItem = (codename) =>
                {
                    var modularContentNode = (JObject)modularContent;
                    var modularContentItemNode = modularContentNode.Properties().FirstOrDefault(p => p.Name == codename)?.First;
                    if (modularContentItemNode != null)
                    {
                        return GetContentItemModel(typeof(object), modularContentItemNode, modularContent, processedItems);
                    }
                    return null;
                },
                Client = _client,
            };

            foreach (var property in instance.GetType().GetProperties())
            {
                var propertyType = property.PropertyType;
                if (property.SetMethod != null)
                {
                    if (propertyType == typeof(ContentItemSystemAttributes))
                    {
                        // Handle the system metadata
                        if (system != null)
                        {
                            property.SetValue(instance, system);
                        }
                    }
                    else
                    {
                        object value = null;

                        var elementData = (JObject)elementsData.Properties()?.FirstOrDefault(p => PropertyMapper.IsMatch(property, p.Name, system?.Type))?.Value;
                        var elementValue = elementData?.Property("value")?.Value;

                        var valueConverter = GetValueConverter(property);
                        if (valueConverter != null)
                        {
                            value = valueConverter.GetPropertyValue(property, elementData, context);
                        }
                        else if (propertyType == typeof(string))
                        {
                            value = elementValue?.ToObject<string>();
                            var links = elementData?.Property("links")?.Value;
                            var modularContentInRichText = elementData?.Property("modular_content")?.Value;

                            // Handle rich_text link resolution
                            if (links != null && elementValue != null && ContentLinkResolver != null)
                            {
                                value = ContentLinkResolver.ResolveContentLinks((string)value, links);
                            }

                            if (modularContentInRichText != null && elementValue != null && _client.InlineContentItemsProcessor != null)
                            {
                                // At this point it's clear it's richtext because it contains modular content
                                richTextPropertiesToBeProcessed.Add(property);
                            }

                        }
                        else if (propertyType == typeof(IEnumerable<MultipleChoiceOption>)
                                 || propertyType == typeof(IEnumerable<Asset>)
                                 || propertyType == typeof(IEnumerable<TaxonomyTerm>)
                                 || propertyType.GetTypeInfo().IsValueType)
                        {
                            // Handle non-hierarchical fields
                            value = elementValue?.ToObject(propertyType);
                        }
                        else if (propertyType.GetTypeInfo().IsGenericType
                            && ((propertyType.GetInterfaces().Any(gt => gt.GetTypeInfo().IsGenericType && gt.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICollection<>)) && propertyType.GetTypeInfo().IsClass)
                            || propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                        {
                            // Handle modular content
                            var contentItemCodenames = elementValue?.ToObject<IEnumerable<string>>();

                            var modularContentNode = (JObject)modularContent;
                            var genericArgs = propertyType.GetGenericArguments();

                            // Create a List<T> based on the generic parameter of the input type (IEnumerable<T> or derived types)
                            Type collectionType = propertyType.GetTypeInfo().IsInterface ? typeof(List<>).MakeGenericType(genericArgs) : propertyType;

                            object contentItems = Activator.CreateInstance(collectionType);

                            if (contentItemCodenames != null && contentItemCodenames.Any())
                            {
                                foreach (string codename in contentItemCodenames)
                                {
                                    var modularContentItemNode = modularContentNode.Properties().FirstOrDefault(p => p.Name == codename)?.First;

                                    if (modularContentItemNode != null)
                                    {
                                        object contentItem = null;
                                        if (processedItems.ContainsKey(codename))
                                        {
                                            // Avoid infinite recursion by re-using already processed content items
                                            contentItem = processedItems[codename];
                                        }
                                        else
                                        {
                                            if (genericArgs.First() == typeof(ContentItem))
                                            {
                                                contentItem = new ContentItem(modularContentItemNode, modularContentNode, _client);
                                            }
                                            else
                                            {
                                                contentItem = GetContentItemModel(genericArgs.First(), modularContentItemNode, modularContentNode, processedItems);
                                            }
                                            if (!processedItems.ContainsKey(codename))
                                            {
                                                processedItems.Add(codename, contentItem);
                                            }
                                        }

                                        // It certain that the instance is of the ICollection<> type at this point, we can call "Add"
                                        contentItems.GetType().GetMethod("Add").Invoke(contentItems, new[] { contentItem });
                                    }
                                }
                            }

                            value = contentItems;
                        }
                        if (value != null)
                        {
                            property.SetValue(instance, value);
                        }
                    }
                }
            }

            // Richtext elements need to be processed last, so in case of circular dependency, content items resolved by
            // resolvers would have all elements already processed
            foreach (var property in richTextPropertiesToBeProcessed)
            {
                var value = property.GetValue(instance)?.ToString();
                var elementData = (JObject)elementsData.Properties()?.FirstOrDefault(p => PropertyMapper.IsMatch(property, p.Name, system?.Type))?.Value;

                var modularContentInRichText = elementData?.Property("modular_content")?.Value;

                var currentlyProcessedString = new RichTextContentElements()
                {
                    ContentItemCodeName = system.Codename,
                    RichTextElementCodeName = property.Name
                };
                if (currentlyResolvedRichStrings.Contains(currentlyProcessedString))
                {
                    // If this element is already being processed it's necessary to to use it as is (with removed inline content items)
                    // otherwise resolving would be stuck in an infinite loop
                    value = RemoveInlineContentItems(value);     
                                                                       
                }
                else
                {
                    currentlyResolvedRichStrings.Add(currentlyProcessedString);
                    value = ProcessInlineContentItems(modularContent, processedItems, value, modularContentInRichText, currentlyResolvedRichStrings);
                    currentlyResolvedRichStrings.Remove(currentlyProcessedString);
                }
                if (value != null)
                {
                    property.SetValue(instance, value);
                }

            }

            return instance;
        }

        private static IPropertyValueConverter GetValueConverter(PropertyInfo property)
        {
            // Converter defined by explicit attribute has the highest priority
            var attributeConverter = property.GetCustomAttributes().FirstOrDefault(attr => typeof(IPropertyValueConverter).IsAssignableFrom(attr.GetType())) as IPropertyValueConverter;
            if (attributeConverter != null)
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

        private string ProcessInlineContentItems(JToken modularContent, Dictionary<string, object> processedItems, string value, JToken modularContentInRichText, HashSet<RichTextContentElements> currentlyResolvedRichStrings)
        {
            var usedCodenames = JsonConvert.DeserializeObject<IEnumerable<string>>(modularContentInRichText.ToString());
            var contentItemsInRichText = new Dictionary<string, object>();
                
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
                    var modularContentNode = (JObject)modularContent;
                    var modularContentItemNode =
                        modularContentNode.Properties()
                            .FirstOrDefault(p => p.Name == codenameUsed)?.First;
                    if (modularContentItemNode != null)
                    {
                        contentItem = GetContentItemModel(typeof(object), modularContentItemNode, modularContentNode, processedItems, currentlyResolvedRichStrings);
                        if (!processedItems.ContainsKey(codenameUsed))
                        {
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
            value = _client.InlineContentItemsProcessor.Process(value, contentItemsInRichText);

            return value;
        }

        private string RemoveInlineContentItems(string value)
        {
            return _client.InlineContentItemsProcessor.RemoveAll(value);
        }
    }
}
