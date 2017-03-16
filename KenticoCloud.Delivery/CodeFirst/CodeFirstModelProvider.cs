using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// A default provider for mapping content items to code-first models.
    /// </summary>
    internal class CodeFirstModelProvider : ICodeFirstModelProvider
    {
        private readonly DeliveryClient _client;
        private ICodeFirstPropertyMapper _propertyMapper;

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
        public CodeFirstModelProvider(DeliveryClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Builds a code-first model based on given JSON input.
        /// </summary>
        /// <typeparam name="T">Strongly typed content item model.</typeparam>
        /// <param name="item">Content item data.</param>
        /// <param name="modularContent">Modular content items.</param>
        /// <returns>Strongly typed POCO model of the <see cref="T"/> type.</returns>
        public T GetContentItemModel<T>(JToken item, JToken modularContent)
        {
            return (T)GetContentItemModel(typeof(T), item, modularContent);
        }

        internal object GetContentItemModel(Type t, JToken item, JToken modularContent, Dictionary<string, object> processedItems = null)
        {
            processedItems = processedItems ?? new Dictionary<string, object>();
            ContentItemSystemAttributes system = (ContentItemSystemAttributes)((JObject)item["system"]).ToObject(typeof(ContentItemSystemAttributes));
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
                        var propValue = ((JObject)item["elements"]).Properties()
                            ?.FirstOrDefault(p => PropertyMapper.IsMatch(property, p.Name, system?.Type))
                            ?.FirstOrDefault()["value"];

                        if (propertyType == typeof(string))
                        {
                            var links = ((JObject)propValue?.Parent?.Parent)?.Property("links")?.Value;

                            // Handle rich_text link resolution
                            if (links != null && propValue != null && _client.ContentLinkResolver != null)
                            {
                                value = _client.ContentLinkResolver.ResolveContentLinks(propValue?.ToObject<string>(), links);
                            }
                            else
                            {
                                value = propValue?.ToObject(propertyType);
                            }
                        }
                        else if (propertyType == typeof(IEnumerable<MultipleChoiceOption>)
                                 || propertyType == typeof(IEnumerable<Asset>)
                                 || propertyType == typeof(IEnumerable<TaxonomyTerm>)
                                 || propertyType.GetTypeInfo().IsValueType)
                        {
                            // Handle non-hierarchical fields
                            value = propValue?.ToObject(propertyType);
                        }
                        else if (propertyType.GetTypeInfo().IsGenericType
                            && ((propertyType.GetInterfaces().Any(gt => gt.GetTypeInfo().IsGenericType && gt.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICollection<>)) && propertyType.GetTypeInfo().IsClass)
                            || propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                        {
                            // Handle modular content
                            var contentItemCodenames = propValue?.ToObject<IEnumerable<string>>();

                            if (contentItemCodenames != null && contentItemCodenames.Any())
                            {
                                var modularContentNode = (JObject)modularContent;
                                var genericArgs = propertyType.GetGenericArguments();

                                // Create a List<T> based on the generic parameter of the input type (IEnumerable<T> or derived types)
                                Type collectionType = propertyType.GetTypeInfo().IsInterface ? typeof(List<>).MakeGenericType(genericArgs) : propertyType;

                                object contentItems = Activator.CreateInstance(collectionType);

                                foreach (string codename in contentItemCodenames)
                                {
                                    var modularContentItemNode = modularContentNode.Properties().First(p => p.Name == codename).First;

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
                                            processedItems.Add(codename, contentItem);
                                        }

                                        // It certain that the instance is of the ICollection<> type at this point, we can call "Add"
                                        contentItems.GetType().GetMethod("Add").Invoke(contentItems, new[] { contentItem });
                                    }
                                }

                                value = contentItems;
                            }
                        }
                        if (value != null)
                        {
                            property.SetValue(instance, value);
                        }
                    }
                }
            }

            return instance;
        }
    }
}
